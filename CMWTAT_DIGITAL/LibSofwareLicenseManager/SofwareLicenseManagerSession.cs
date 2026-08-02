//
// SofwareLicenseManagerSession.cs -- slmgr.vbs 的 C# 复刻：基础设施部分。
//
// 对应原脚本中的：Connect / GetServiceObject / GetProductCollection / GetProductObject /
// LineOut / LineFlush / ExitScript / ShowError / QuitIfError / 注册表访问 / 各类判定函数。
//
// 目标框架：.NET Framework 4.5（仅使用 C# 5 语法，避免高版本编译器特性）。
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace CMWTAT_DIGITAL.LibSofwareLicenseManager
{
    /// <summary>
    /// 对应 slmgr.vbs 的 ExitScript：终止执行并带回退出码。
    /// </summary>
    internal sealed class SofwareLicenseManagerQuitException : Exception
    {
        private readonly int _exitCode;

        internal SofwareLicenseManagerQuitException(int exitCode)
        {
            _exitCode = exitCode;
        }

        internal int ExitCode
        {
            get { return _exitCode; }
        }
    }

    /// <summary>
    /// 对应 VBScript 的 Err 对象快照（原脚本中的 Class CErr）。
    /// </summary>
    internal sealed class SofwareLicenseManagerError
    {
        internal int Number;
        internal string Description;
        internal string Source;

        internal SofwareLicenseManagerError(int number, string description, string source)
        {
            Number = number;
            Description = description == null ? "" : description;
            Source = source == null ? "" : source;
        }

        /// <summary>由 .NET 异常构造，等价于 VBScript 里 On Error Resume Next 之后的 Err。</summary>
        internal static SofwareLicenseManagerError FromException(Exception ex)
        {
            return new SofwareLicenseManagerError(SofwareLicenseManagerSession.GetHResult(ex), ex.Message, ex.Source);
        }
    }

    /// <summary>
    /// 一次 slmgr 命令的执行上下文。等价于 slmgr.vbs 的一次进程运行。
    /// </summary>
    internal sealed partial class SofwareLicenseManagerSession : IDisposable
    {
        // ---------- WMI 类名 / 查询子句（对应原脚本同名 const） ----------
        internal const string ServiceClass = "SoftwareLicensingService";
        internal const string ProductClass = "SoftwareLicensingProduct";
        internal const string TkaLicenseClass = "SoftwareLicensingTokenActivationLicense";
        internal const string WindowsAppId = "55c92734-d682-4d71-983e-d6ec3f16059f";

        internal const string ProductIsPrimarySkuSelectClause =
            "ID, ApplicationId, PartialProductKey, LicenseIsAddon, Description, Name";
        internal const string KMSClientLookupClause =
            "KeyManagementServiceMachine, KeyManagementServicePort, KeyManagementServiceLookupDomain";
        internal const string PartialProductKeyNonNullWhereClause = "PartialProductKey <> null";
        internal const string EmptyWhereClause = "";

        internal const string DefaultPort = "1688";

        // ---------- 注册表 ----------
        internal const uint HKEY_LOCAL_MACHINE = 0x80000002;
        internal const string SLKeyPath =
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform";
        internal const string SLKeyPath32 =
            @"SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform";

        // ---------- HRESULT ----------
        internal const uint HR_SL_E_GRACE_TIME_EXPIRED = 0xC004F009;
        internal const uint HR_SL_E_NOT_GENUINE = 0xC004F200;
        internal const int HR_SL_E_PKEY_NOT_INSTALLED = unchecked((int)0xC004F014);
        internal const int HR_INVALID_ARG = unchecked((int)0x80070057);
        internal const int HR_ERROR_DS_NO_SUCH_OBJECT = unchecked((int)0x80072030);

        // ---------- 主 SKU 判定用标记 ----------
        internal const string NoPrimaryKeyFound = "NoPrimaryKeyFound";
        internal const string TblPrimaryKey = "TblPrimaryKey";
        internal const string NotSpecialCasePrimaryKey = "NotSpecialCasePrimaryKey";
        internal const string IndeterminatePrimaryKeyFound = "IndeterminatePrimaryKey";

        // ---------- 状态 ----------
        private readonly StringBuilder _output = new StringBuilder();
        private readonly StringBuilder _echo = new StringBuilder();   // 对应 g_EchoString
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private string _computer = ".";                               // g_strComputer
        private string _userName;                                     // g_strUserName
        private string _password;                                     // g_strPassword
        private bool _isRemoteComputer;                               // g_IsRemoteComputer

        private ManagementScope _cimv2;                               // g_objWMIService
        private ManagementClass _registry;                            // g_objRegistry (StdRegProv)

        internal int ExitCode { get; private set; }

        // =========================================================================
        // 输出（LineOut / LineFlush / ExitScript）
        // =========================================================================

        /// <summary>对应 LineOut：追加一行到缓冲区。</summary>
        private void LineOut(string str)
        {
            _echo.Append(str == null ? "" : str).Append("\r\n");
        }

        /// <summary>对应 LineFlush：把缓冲区连同本行一起 Echo 出去。</summary>
        private void LineFlush(string str)
        {
            _output.Append(_echo.ToString()).Append(str == null ? "" : str).Append("\r\n");
            _echo.Length = 0;
        }

        /// <summary>对应 ExitScript：冲刷缓冲区并终止本次执行。</summary>
        private void ExitScript(int retval)
        {
            FlushPending();
            ExitCode = retval;
            throw new SofwareLicenseManagerQuitException(retval);
        }

        /// <summary>把尚未 Echo 的缓冲区刷出（对应 ExitScript 开头那段）。</summary>
        internal void FlushPending()
        {
            if (_echo.Length > 0)
            {
                _output.Append(_echo.ToString()).Append("\r\n");
                _echo.Length = 0;
            }
        }

        internal string GetOutput()
        {
            return _output.ToString();
        }

        // =========================================================================
        // 错误处理（ShowError / QuitIfError / QuitWithError）
        // =========================================================================

        /// <summary>
        /// 从 .NET 异常里尽力取出真实 HRESULT。
        /// WMI 提供程序（SPP）返回的 0xC004xxxx 会出现在 ManagementException.ErrorCode 上。
        /// </summary>
        internal static int GetHResult(Exception ex)
        {
            if (ex == null)
            {
                return 0;
            }

            ManagementException mex = ex as ManagementException;
            if (mex != null)
            {
                int code = (int)mex.ErrorCode;
                if (code != 0)
                {
                    return code;
                }
            }

            COMException cex = ex as COMException;
            if (cex != null && cex.ErrorCode != 0)
            {
                return cex.ErrorCode;
            }

            try
            {
                int hr = Marshal.GetHRForException(ex);
                if (hr != 0)
                {
                    return hr;
                }
            }
            catch
            {
                // 忽略：退回到下面的默认值
            }

            return unchecked((int)0x80004005); // E_FAIL
        }

        /// <summary>VBScript Hex()：大写、无前导 0x、负数按无符号 32 位展开。</summary>
        internal static string VbHex(int number)
        {
            return number < 0
                ? ((uint)number).ToString("X", CultureInfo.InvariantCulture)
                : number.ToString("X", CultureInfo.InvariantCulture);
        }

        /// <summary>对应 ShowError。</summary>
        private void ShowError(string strMessage, SofwareLicenseManagerError objErr)
        {
            string strNumber = objErr.Number >= 0
                ? objErr.Number.ToString(CultureInfo.InvariantCulture)
                : "0x" + VbHex(objErr.Number);

            string strDescription = SofwareLicenseManagerMessages.GetErrorMessage(VbHex(objErr.Number));

            if (strDescription.Length == 0)
            {
                if (objErr.Description.Length == 0)
                {
                    strDescription = SofwareLicenseManagerMessages.L_MsgErrorText_6.Replace("0x%ERRCODE%", strNumber);
                }
                else if (objErr.Source.Length == 0)
                {
                    strDescription = objErr.Description;
                }
                else
                {
                    strDescription = objErr.Description + " (" + objErr.Source + ")";
                }
            }

            if (strMessage.IndexOf("0x%ERRCODE%", StringComparison.Ordinal) < 0)
            {
                strMessage = strMessage + "0x%ERRCODE%";
            }

            if (strMessage.IndexOf("%ERRTEXT%", StringComparison.Ordinal) < 0)
            {
                strMessage = strMessage + " %ERRTEXT%";
            }

            strMessage = strMessage.Replace("%COMPUTERNAME%", _computer);
            strMessage = strMessage.Replace("0x%ERRCODE%", strNumber);
            strMessage = strMessage.Replace("%ERRTEXT%", strDescription);

            LineOut(strMessage);
        }

        /// <summary>对应 QuitIfError()：报告错误并退出。</summary>
        private void Quit(Exception ex)
        {
            Quit2(ex, SofwareLicenseManagerMessages.L_MsgErrorText_8);
        }

        /// <summary>对应 QuitIfError2(strMessage)。</summary>
        private void Quit2(Exception ex, string strMessage)
        {
            SofwareLicenseManagerQuitException quit = ex as SofwareLicenseManagerQuitException;
            if (quit != null)
            {
                throw quit; // 已经在退出流程中，直接向上传递
            }

            SofwareLicenseManagerError err = SofwareLicenseManagerError.FromException(ex);
            ShowError(strMessage, err);
            ExitScript(err.Number);
        }

        /// <summary>对应 QuitWithError(errNum)。</summary>
        private void QuitWithError(int errNum)
        {
            ShowError(SofwareLicenseManagerMessages.L_MsgErrorText_8, new SofwareLicenseManagerError(errNum, "", ""));
            ExitScript(errNum);
        }

        /// <summary>对应 FailRemoteExec()：远程执行不支持的命令。</summary>
        private void FailRemoteExec()
        {
            if (_isRemoteComputer)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgRemoteExecNotSupported);
                ExitScript(1);
            }
        }

        // =========================================================================
        // 连接（Connect）
        // =========================================================================

        /// <summary>对应 Connect：建立 WMI(root\cimv2) 与注册表(root\default:StdRegProv) 连接。</summary>
        internal void Connect(string computer, string userName, string password)
        {
            _computer = string.IsNullOrEmpty(computer) ? "." : computer;
            _userName = userName;
            _password = password;

            if (_computer == ".")
            {
                try
                {
                    _cimv2 = new ManagementScope(@"\\.\root\cimv2");
                    _cimv2.Connect();
                }
                catch (Exception ex)
                {
                    Quit2(ex, SofwareLicenseManagerMessages.L_MsgErrorLocalWMI);
                }

                try
                {
                    ManagementScope defaultScope = new ManagementScope(@"\\.\root\default");
                    defaultScope.Connect();
                    _registry = new ManagementClass(defaultScope, new ManagementPath("StdRegProv"), null);
                    _disposables.Add(_registry);
                }
                catch (Exception ex)
                {
                    Quit2(ex, SofwareLicenseManagerMessages.L_MsgErrorLocalRegistry);
                }

                return;
            }

            // 远程连接
            ConnectionOptions options = new ConnectionOptions();
            options.Impersonation = ImpersonationLevel.Impersonate;
            options.Authentication = AuthenticationLevel.PacketPrivacy;
            if (!string.IsNullOrEmpty(_userName))
            {
                options.Username = _userName;
                options.Password = _password;
            }

            try
            {
                _cimv2 = new ManagementScope(@"\\" + _computer + @"\root\cimv2", options);
                _cimv2.Connect();
            }
            catch (Exception ex)
            {
                Quit2(ex, SofwareLicenseManagerMessages.L_MsgErrorConnection);
            }

            _isRemoteComputer = true;

            // Windows 8 起的 slmgr 不支持远程连接 Vista/Win7
            ManagementObject service = GetServiceObject("Version");
            string version = PropString(service, "Version");
            if (!string.IsNullOrEmpty(version) && version.Length >= 3)
            {
                string major = version.Substring(0, 3);
                if (major == "6.0" || major == "6.1")
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgRemoteWmiVersionMismatch);
                    ExitScript(1);
                }
            }

            try
            {
                ManagementScope defaultScope =
                    new ManagementScope(@"\\" + _computer + @"\root\default", options);
                defaultScope.Connect();
                _registry = new ManagementClass(defaultScope, new ManagementPath("StdRegProv"), null);
                _disposables.Add(_registry);
            }
            catch (Exception ex)
            {
                Quit2(ex, SofwareLicenseManagerMessages.L_MsgErrorConnectionRegistry);
            }
        }

        // =========================================================================
        // WMI 查询（GetServiceObject / GetProductCollection / GetProductObject）
        // =========================================================================

        /// <summary>对应 GetServiceObject。</summary>
        internal ManagementObject GetServiceObject(string strQuery)
        {
            try
            {
                ObjectQuery query = new ObjectQuery("SELECT " + strQuery + " FROM " + ServiceClass);
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(_cimv2, query))
                {
                    foreach (ManagementBaseObject item in searcher.Get())
                    {
                        ManagementObject obj = (ManagementObject)item;
                        _disposables.Add(obj);
                        return obj;
                    }
                }
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            return null;
        }

        /// <summary>对应 GetProductCollection。结果立即物化，便于多次枚举。</summary>
        internal List<ManagementObject> GetProductCollection(string strSelect, string strWhere)
        {
            List<ManagementObject> result = new List<ManagementObject>();

            try
            {
                string sql = string.IsNullOrEmpty(strWhere)
                    ? "SELECT " + strSelect + " FROM " + ProductClass
                    : "SELECT " + strSelect + " FROM " + ProductClass + " WHERE " + strWhere;

                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(_cimv2, new ObjectQuery(sql)))
                {
                    foreach (ManagementBaseObject item in searcher.Get())
                    {
                        ManagementObject obj = (ManagementObject)item;
                        _disposables.Add(obj);
                        result.Add(obj);
                    }
                }
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            return result;
        }

        /// <summary>对应 GetProductObject：必须恰好命中一条。</summary>
        internal ManagementObject GetProductObject(string strSelect, string strWhere)
        {
            List<ManagementObject> products = GetProductCollection(strSelect, strWhere);

            if (products.Count == 0)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgErrorPKey);
                QuitWithError(HR_SL_E_PKEY_NOT_INSTALLED);
            }
            else if (products.Count != 1)
            {
                QuitWithError(HR_INVALID_ARG);
            }

            return products[0];
        }

        /// <summary>枚举 SoftwareLicensingTokenActivationLicense 的全部实例。</summary>
        internal List<ManagementObject> GetTokenActivationLicenses()
        {
            List<ManagementObject> result = new List<ManagementObject>();

            try
            {
                using (ManagementClass cls =
                    new ManagementClass(_cimv2, new ManagementPath(TkaLicenseClass), null))
                {
                    foreach (ManagementBaseObject item in cls.GetInstances())
                    {
                        ManagementObject obj = (ManagementObject)item;
                        _disposables.Add(obj);
                        result.Add(obj);
                    }
                }
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            return result;
        }

        // =========================================================================
        // WMI 方法调用
        // =========================================================================

        /// <summary>
        /// 调用 WMI 方法。nameValuePairs 为「参数名, 参数值」交替序列。
        /// 与原脚本一致：失败通过异常反映（SPP 提供程序把 HRESULT 作为 WMI 错误抛出）。
        /// 若按名字找不到形参，则退化为按位置匹配，以兼容不同 Windows 版本的 MOF 命名差异。
        /// </summary>
        internal ManagementBaseObject Invoke(ManagementObject obj, string methodName,
                                             params object[] nameValuePairs)
        {
            ManagementBaseObject inParams = null;

            if (nameValuePairs != null && nameValuePairs.Length >= 2)
            {
                inParams = obj.GetMethodParameters(methodName);

                List<string> ordered = GetOrderedParameterNames(inParams);
                int positional = 0;

                for (int i = 0; i + 1 < nameValuePairs.Length; i += 2, positional++)
                {
                    string wanted = (string)nameValuePairs[i];
                    string actual = null;

                    for (int k = 0; k < ordered.Count; k++)
                    {
                        if (string.Equals(ordered[k], wanted, StringComparison.OrdinalIgnoreCase))
                        {
                            actual = ordered[k];
                            break;
                        }
                    }

                    if (actual == null && positional < ordered.Count)
                    {
                        actual = ordered[positional];
                    }

                    if (actual == null)
                    {
                        actual = wanted;
                    }

                    inParams[actual] = nameValuePairs[i + 1];
                }
            }

            return obj.InvokeMethod(methodName, inParams, null);
        }

        /// <summary>按 WMI 的 ID 限定符还原形参声明顺序。</summary>
        private static List<string> GetOrderedParameterNames(ManagementBaseObject inParams)
        {
            List<string> names = new List<string>();
            List<int> ids = new List<int>();

            if (inParams == null)
            {
                return names;
            }

            foreach (PropertyData p in inParams.Properties)
            {
                int id = int.MaxValue;
                try
                {
                    object q = p.Qualifiers["ID"].Value;
                    if (q != null)
                    {
                        id = Convert.ToInt32(q, CultureInfo.InvariantCulture);
                    }
                }
                catch
                {
                    id = int.MaxValue;
                }

                int pos = names.Count;
                for (int k = 0; k < ids.Count; k++)
                {
                    if (ids[k] > id)
                    {
                        pos = k;
                        break;
                    }
                }

                names.Insert(pos, p.Name);
                ids.Insert(pos, id);
            }

            return names;
        }

        /// <summary>取方法输出参数：优先按名字，取不到就用第一个非 ReturnValue 的属性。</summary>
        internal static object GetOutParam(ManagementBaseObject outParams, string preferredName)
        {
            if (outParams == null)
            {
                return null;
            }

            object value = TryGetProperty(outParams, preferredName);
            if (value != null)
            {
                return value;
            }

            try
            {
                foreach (PropertyData p in outParams.Properties)
                {
                    if (!string.Equals(p.Name, "ReturnValue", StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Value != null)
                        {
                            return p.Value;
                        }
                    }
                }
            }
            catch
            {
                // 忽略
            }

            return null;
        }

        /// <summary>对应 objProduct.refresh_。</summary>
        internal static void Refresh(ManagementObject obj)
        {
            try
            {
                obj.Get();
            }
            catch
            {
                // 与原脚本的 On Error Resume Next 语义一致
            }
        }

        // =========================================================================
        // 属性读取辅助（等价于 VBScript 的宽松取值）
        // =========================================================================

        internal static object TryGetProperty(ManagementBaseObject obj, string name)
        {
            object value;
            TryReadProperty(obj, name, out value);
            return value;
        }

        /// <summary>
        /// 读取属性。返回值表示「读取本身是否成功」（对应 VBScript 里 Err.Number = 0），
        /// value 为 null 表示属性存在但值为 NULL。二者在原脚本中含义不同，必须区分。
        /// </summary>
        internal static bool TryReadProperty(ManagementBaseObject obj, string name, out object value)
        {
            value = null;

            if (obj == null)
            {
                return false;
            }

            try
            {
                value = obj[name];
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        internal static string PropString(ManagementBaseObject obj, string name)
        {
            object v = TryGetProperty(obj, name);
            return v == null ? "" : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 读取 uint32 属性。WMI 脚本接口把 uint32 当作有符号 32 位返回，
        /// 这里统一按 uint 处理，比较时也用 uint，避免符号问题。
        /// </summary>
        internal static uint PropUInt(ManagementBaseObject obj, string name)
        {
            object v = TryGetProperty(obj, name);
            if (v == null)
            {
                return 0;
            }

            try
            {
                if (v is uint)
                {
                    return (uint)v;
                }
                if (v is int)
                {
                    return unchecked((uint)(int)v);
                }
                return unchecked((uint)Convert.ToInt64(v, CultureInfo.InvariantCulture));
            }
            catch
            {
                return 0;
            }
        }

        internal static bool PropBool(ManagementBaseObject obj, string name)
        {
            object v = TryGetProperty(obj, name);
            if (v == null)
            {
                return false;
            }

            try
            {
                return Convert.ToBoolean(v, CultureInfo.InvariantCulture);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>属性是否存在且非 NULL（对应 VBScript 的 Not IsNull(...)）。</summary>
        internal static bool HasValue(ManagementBaseObject obj, string name)
        {
            return TryGetProperty(obj, name) != null;
        }

        // =========================================================================
        // 日期辅助
        // =========================================================================

        /// <summary>
        /// 模拟 VBScript 把 Date 拼进字符串时的格式：当前区域的短日期 + 长时间。
        /// </summary>
        internal static string VbDateToString(DateTime value)
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            if (value.TimeOfDay == TimeSpan.Zero)
            {
                return value.ToString("d", ci);
            }
            return value.ToString("d", ci) + " " + value.ToString("T", ci);
        }

        /// <summary>
        /// 解析 CIM_DATETIME。对应原脚本中
        /// SWbemDateTime.Value = x : If GetFileTime(false) &lt;&gt; 0 Then ... GetVarDate
        /// 的用法：返回 false 表示该时间为空/零值，调用方应跳过显示。
        /// </summary>
        internal static bool TryGetWmiDate(ManagementBaseObject obj, string name, out DateTime value)
        {
            value = DateTime.MinValue;

            string raw = PropString(obj, name);
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            try
            {
                value = ManagementDateTimeConverter.ToDateTime(raw);
            }
            catch
            {
                return false;
            }

            // FILETIME 0 对应 1601-01-01；SPP 用它表示「无值」
            return value.Year > 1601;
        }

        // =========================================================================
        // 描述判定（IsKmsClient / IsKmsServer / IsTBL / IsAVMA / IsMAK）
        // 注意：VBScript 的 InStr 默认二进制比较，因此这里用区分大小写的序号比较。
        // =========================================================================

        internal static bool IsKmsClient(string strDescription)
        {
            return Contains(strDescription, "VOLUME_KMSCLIENT");
        }

        internal static bool IsTkaClient(string strDescription)
        {
            return IsKmsClient(strDescription);
        }

        internal static bool IsKmsServer(string strDescription)
        {
            if (IsKmsClient(strDescription))
            {
                return false;
            }
            return Contains(strDescription, "VOLUME_KMS");
        }

        internal static bool IsTBL(string strDescription)
        {
            return Contains(strDescription, "TIMEBASED_");
        }

        internal static bool IsAVMA(string strDescription)
        {
            return Contains(strDescription, "VIRTUAL_MACHINE_ACTIVATION");
        }

        internal static bool IsMAK(string strDescription)
        {
            return Contains(strDescription, "MAK");
        }

        private static bool Contains(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(haystack))
            {
                return false;
            }
            return haystack.IndexOf(needle, StringComparison.Ordinal) >= 0;
        }

        // =========================================================================
        // 主 SKU 判定
        // =========================================================================

        /// <summary>对应 GetIsPrimaryWindowsSKU：0=不是，1=是，2=无法确定。</summary>
        internal static int GetIsPrimaryWindowsSKU(ManagementObject objProduct)
        {
            int iPrimarySku = 0;

            string appId = PropString(objProduct, "ApplicationId");
            string partialKey = PropString(objProduct, "PartialProductKey");

            if (string.Equals(appId, WindowsAppId, StringComparison.OrdinalIgnoreCase)
                && partialKey.Length != 0)
            {
                object addOn;
                if (TryReadProperty(objProduct, "LicenseIsAddon", out addOn))
                {
                    // 与原脚本一致：只有取值为 True 才算加载项；NULL 视为非加载项
                    bool bIsAddOn = false;
                    try
                    {
                        bIsAddOn = addOn != null && Convert.ToBoolean(addOn, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        bIsAddOn = false;
                    }

                    iPrimarySku = bIsAddOn ? 0 : 1;
                }
                else
                {
                    // 取不到 LicenseIsAddon：老版本客户端。能确认 KMS 才敢断定是主 SKU。
                    string desc = PropString(objProduct, "Description");
                    iPrimarySku = (IsKmsClient(desc) || IsKmsServer(desc)) ? 1 : 2;
                }
            }

            return iPrimarySku;
        }

        /// <summary>对应 GetPrimarySKUType。</summary>
        internal string GetPrimarySKUType()
        {
            string strPrimarySKUType = "";

            foreach (ManagementObject objProduct in
                     GetProductCollection(ProductIsPrimarySkuSelectClause, PartialProductKeyNonNullWhereClause))
            {
                string strDescription = PropString(objProduct, "Description");

                if (string.Equals(PropString(objProduct, "ApplicationId"), WindowsAppId,
                                  StringComparison.OrdinalIgnoreCase))
                {
                    int iIsPrimaryWindowsSku = GetIsPrimaryWindowsSKU(objProduct);
                    if (iIsPrimaryWindowsSku == 1)
                    {
                        if (IsKmsServer(strDescription) || IsKmsClient(strDescription))
                        {
                            strPrimarySKUType = strDescription;
                            break;
                        }

                        if (IsTBL(strDescription))
                        {
                            strPrimarySKUType = TblPrimaryKey;
                            break;
                        }

                        strPrimarySKUType = NotSpecialCasePrimaryKey;
                    }
                    else if (iIsPrimaryWindowsSku == 2 && strPrimarySKUType.Length == 0)
                    {
                        strPrimarySKUType = IndeterminatePrimaryKeyFound;
                    }
                }
                else
                {
                    strPrimarySKUType = strDescription;
                    break;
                }
            }

            if (strPrimarySKUType.Length == 0)
            {
                strPrimarySKUType = NoPrimaryKeyFound;
            }

            return strPrimarySKUType;
        }

        /// <summary>对应 CheckProductForCommand。</summary>
        internal static bool CheckProductForCommand(ManagementObject objProduct, string strActivationID)
        {
            bool result = false;

            object addOn;
            bool addOnReadable = TryReadProperty(objProduct, "LicenseIsAddon", out addOn);

            // 原脚本写的是 (objProduct.LicenseIsAddon = False)：
            // 属性为 NULL 时整个条件求值为 Null，即不成立，因此这里要求取到明确的 False。
            bool addOnIsExplicitlyFalse = false;
            if (addOnReadable && addOn != null)
            {
                try
                {
                    addOnIsExplicitlyFalse = !Convert.ToBoolean(addOn, CultureInfo.InvariantCulture);
                }
                catch
                {
                    addOnIsExplicitlyFalse = false;
                }
            }

            if (strActivationID.Length == 0
                && string.Equals(PropString(objProduct, "ApplicationId"), WindowsAppId,
                                 StringComparison.OrdinalIgnoreCase)
                && addOnIsExplicitlyFalse)
            {
                result = true;
            }

            if (string.Equals(PropString(objProduct, "ID").ToLowerInvariant(), strActivationID,
                              StringComparison.Ordinal))
            {
                result = true;
            }

            return result;
        }

        /// <summary>对应 OutputIndeterminateOperationWarning。</summary>
        private void OutputIndeterminateOperationWarning(ManagementObject objProduct)
        {
            LineOut(SofwareLicenseManagerMessages.L_MsgUndeterminedPrimaryKeyOperation);

            string strOutput = SofwareLicenseManagerMessages.L_MsgUndeterminedOperationFormat
                .Replace("%PRODUCTDESCRIPTION%", PropString(objProduct, "Description"))
                .Replace("%PRODUCTID%", PropString(objProduct, "ID"));

            LineOut(strOutput);
        }

        // =========================================================================
        // 注册表（StdRegProv）
        // =========================================================================

        /// <summary>对应 SetRegistryStr。</summary>
        private uint SetRegistryStr(uint hKey, string strKeyPath, string strValueName, string strValue)
        {
            try
            {
                ManagementBaseObject inParams = _registry.GetMethodParameters("SetStringValue");
                inParams["hDefKey"] = hKey;
                inParams["sSubKeyName"] = strKeyPath;
                inParams["sValueName"] = strValueName;
                inParams["sValue"] = strValue;

                ManagementBaseObject outParams = _registry.InvokeMethod("SetStringValue", inParams, null);
                return PropUInt(outParams, "ReturnValue");
            }
            catch (Exception ex)
            {
                return unchecked((uint)GetHResult(ex));
            }
        }

        /// <summary>对应 DeleteRegistryValue。</summary>
        private uint DeleteRegistryValue(uint hKey, string strKeyPath, string strValueName)
        {
            try
            {
                ManagementBaseObject inParams = _registry.GetMethodParameters("DeleteValue");
                inParams["hDefKey"] = hKey;
                inParams["sSubKeyName"] = strKeyPath;
                inParams["sValueName"] = strValueName;

                ManagementBaseObject outParams = _registry.InvokeMethod("DeleteValue", inParams, null);
                return PropUInt(outParams, "ReturnValue");
            }
            catch (Exception ex)
            {
                return unchecked((uint)GetHResult(ex));
            }
        }

        /// <summary>对应 ExistsRegistryKey：只关心键是否存在，不关心实际权限。</summary>
        private bool ExistsRegistryKey(uint hKey, string strKeyPath)
        {
            try
            {
                ManagementBaseObject inParams = _registry.GetMethodParameters("CheckAccess");
                inParams["hDefKey"] = hKey;
                inParams["sSubKeyName"] = strKeyPath;
                inParams["uRequired"] = (uint)1; // KEY_QUERY_VALUE

                ManagementBaseObject outParams = _registry.InvokeMethod("CheckAccess", inParams, null);
                return PropUInt(outParams, "ReturnValue") != 2;
            }
            catch
            {
                return false;
            }
        }

        // =========================================================================
        // 其它
        // =========================================================================

        /// <summary>对应 GetDaysFromMins：按天向上取整。</summary>
        internal static long GetDaysFromMins(long iMins)
        {
            const long iMinsInADay = 24 * 60;
            return (iMins + iMinsInADay - 1) / iMinsInADay;
        }

        /// <summary>对应 GuidToString：AD 属性里的 16 字节 GUID 转 {....} 形式。</summary>
        internal static string GuidToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 16)
            {
                return "";
            }

            byte[] guidBytes = new byte[16];
            Array.Copy(bytes, guidBytes, 16);
            return new Guid(guidBytes).ToString("B").ToUpperInvariant();
        }

        public void Dispose()
        {
            for (int i = _disposables.Count - 1; i >= 0; i--)
            {
                try
                {
                    _disposables[i].Dispose();
                }
                catch
                {
                    // 忽略清理异常
                }
            }
            _disposables.Clear();
        }
    }
}
