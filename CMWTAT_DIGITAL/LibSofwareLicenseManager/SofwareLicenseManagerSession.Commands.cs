//
// SofwareLicenseManagerSession.Commands.cs -- slmgr.vbs 的 C# 复刻：全局选项与高级选项。
//
// 对应：/ipk /upk /dti /ato /atp /ckms(见 Kms 分部) /cpky /ilc /rilc /rearm /rearm-app
//       /rearm-sku /xpr
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Text;

namespace CMWTAT_DIGITAL.LibSofwareLicenseManager
{
    internal sealed partial class SofwareLicenseManagerSession
    {
        // =====================================================================
        // /ipk -- 安装产品密钥
        // =====================================================================
        internal void InstallProductKey(string strProductKey)
        {
            bool bIsKMS = false;

            ManagementObject objService = GetServiceObject("Version");
            string strVersion = PropString(objService, "Version");

            try
            {
                Invoke(objService, "InstallProductKey", "ProductKey", strProductKey);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            // 安装密钥可能改变授权状态，需要让服务重新消费许可证
            try
            {
                Invoke(objService, "RefreshLicenseStatus");
            }
            catch
            {
                // 与原脚本一致：此处不检查错误
            }

            foreach (ManagementObject objProduct in
                     GetProductCollection(ProductIsPrimarySkuSelectClause, PartialProductKeyNonNullWhereClause))
            {
                string strDescription = PropString(objProduct, "Description");

                if (GetIsPrimaryWindowsSKU(objProduct) == 2)
                {
                    OutputIndeterminateOperationWarning(objProduct);
                }

                if (IsKmsServer(strDescription))
                {
                    bIsKMS = true;
                    break;
                }
            }

            uint lRet;

            if (bIsKMS)
            {
                lRet = SetRegistryStr(HKEY_LOCAL_MACHINE, SLKeyPath, "KeyManagementServiceVersion", strVersion);
                if (lRet != 0)
                {
                    QuitWithError(unchecked((int)lRet));
                }

                if (ExistsRegistryKey(HKEY_LOCAL_MACHINE, SLKeyPath32))
                {
                    lRet = SetRegistryStr(HKEY_LOCAL_MACHINE, SLKeyPath32, "KeyManagementServiceVersion", strVersion);
                    if (lRet != 0)
                    {
                        QuitWithError(unchecked((int)lRet));
                    }
                }
            }
            else
            {
                lRet = DeleteRegistryValue(HKEY_LOCAL_MACHINE, SLKeyPath, "KeyManagementServiceVersion");
                if (lRet != 0 && lRet != 2 && lRet != 5)
                {
                    QuitWithError(unchecked((int)lRet));
                }

                lRet = DeleteRegistryValue(HKEY_LOCAL_MACHINE, SLKeyPath32, "KeyManagementServiceVersion");
                if (lRet != 0 && lRet != 2 && lRet != 5)
                {
                    QuitWithError(unchecked((int)lRet));
                }
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgInstalledPKey.Replace("%PKEY%", strProductKey));
        }

        // =====================================================================
        // /upk -- 卸载产品密钥
        // =====================================================================
        internal void UninstallProductKey(string strActivationID)
        {
            strActivationID = (strActivationID == null ? "" : strActivationID).ToLowerInvariant();

            bool kmsServerFound = false;
            bool uninstallDone = false;

            ManagementObject objService = GetServiceObject("Version");
            string strVersion = PropString(objService, "Version");

            foreach (ManagementObject objProduct in
                     GetProductCollection(ProductIsPrimarySkuSelectClause + ", ProductKeyID",
                                          PartialProductKeyNonNullWhereClause))
            {
                string strDescription = PropString(objProduct, "Description");

                if (CheckProductForCommand(objProduct, strActivationID))
                {
                    int iIsPrimaryWindowsSku = GetIsPrimaryWindowsSKU(objProduct);
                    if (strActivationID.Length == 0 && iIsPrimaryWindowsSku == 2)
                    {
                        OutputIndeterminateOperationWarning(objProduct);
                    }

                    try
                    {
                        Invoke(objProduct, "UninstallProductKey");
                    }
                    catch (Exception ex)
                    {
                        Quit(ex);
                    }

                    // 卸载密钥可能改变授权状态，重新消费许可证
                    try
                    {
                        Invoke(objService, "RefreshLicenseStatus");
                    }
                    catch
                    {
                        // 与原脚本一致
                    }

                    if (strActivationID.Length != 0 || iIsPrimaryWindowsSku == 1)
                    {
                        uninstallDone = true;
                    }

                    LineOut(SofwareLicenseManagerMessages.L_MsgUninstalledPKey);
                }
                else if (IsKmsServer(strDescription))
                {
                    kmsServerFound = true;
                }

                if (kmsServerFound && uninstallDone)
                {
                    break;
                }
            }

            uint lRet;

            if (kmsServerFound)
            {
                lRet = SetRegistryStr(HKEY_LOCAL_MACHINE, SLKeyPath, "KeyManagementServiceVersion", strVersion);
                if (lRet != 0)
                {
                    QuitWithError(unchecked((int)lRet));
                }

                lRet = SetRegistryStr(HKEY_LOCAL_MACHINE, SLKeyPath32, "KeyManagementServiceVersion", strVersion);
                if (lRet != 0)
                {
                    QuitWithError(unchecked((int)lRet));
                }
            }
            else
            {
                lRet = DeleteRegistryValue(HKEY_LOCAL_MACHINE, SLKeyPath, "KeyManagementServiceVersion");
                if (lRet != 0 && lRet != 2)
                {
                    QuitWithError(unchecked((int)lRet));
                }

                lRet = DeleteRegistryValue(HKEY_LOCAL_MACHINE, SLKeyPath32, "KeyManagementServiceVersion");
                if (lRet != 0 && lRet != 2)
                {
                    QuitWithError(unchecked((int)lRet));
                }
            }

            if (!uninstallDone)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgErrorPKey);
            }
        }

        // =====================================================================
        // /dti -- 显示安装 ID
        // =====================================================================
        internal void DisplayIID(string strActivationID)
        {
            strActivationID = (strActivationID == null ? "" : strActivationID).ToLowerInvariant();

            bool bFoundAtLeastOneKey = false;

            foreach (ManagementObject objProduct in
                     GetProductCollection(ProductIsPrimarySkuSelectClause + ", OfflineInstallationId",
                                          PartialProductKeyNonNullWhereClause))
            {
                if (CheckProductForCommand(objProduct, strActivationID))
                {
                    int iIsPrimaryWindowsSku = GetIsPrimaryWindowsSKU(objProduct);
                    if (strActivationID.Length == 0 && iIsPrimaryWindowsSku == 2)
                    {
                        OutputIndeterminateOperationWarning(objProduct);
                    }

                    LineOut(SofwareLicenseManagerMessages.L_MsgInstallationID + PropString(objProduct, "OfflineInstallationId"));
                    bFoundAtLeastOneKey = true;

                    if (strActivationID.Length != 0 || iIsPrimaryWindowsSku == 1)
                    {
                        return;
                    }
                }
            }

            if (bFoundAtLeastOneKey)
            {
                LineOut("");
                LineOut(SofwareLicenseManagerMessages.L_MsgPhoneNumbers);
            }
            else
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgErrorProductNotFound);
            }
        }

        // =====================================================================
        // 激活状态输出
        // =====================================================================
        private void DisplayActivatingSku(ManagementObject objProduct)
        {
            string strOutput = SofwareLicenseManagerMessages.L_MsgActivating
                .Replace("%PRODUCTNAME%", PropString(objProduct, "Name"))
                .Replace("%PRODUCTID%", PropString(objProduct, "ID"));

            LineFlush(strOutput);
        }

        private void DisplayActivatedStatus(ManagementObject objProduct)
        {
            uint ls = PropUInt(objProduct, "LicenseStatus");

            if (ls == 1)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgActivated);
            }
            else if (ls == 4)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgErrorText_8 + SofwareLicenseManagerMessages.L_MsgErrorText_11);
            }
            else if (ls == 5 && PropUInt(objProduct, "LicenseStatusReason") == HR_SL_E_NOT_GENUINE)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgErrorText_8 + SofwareLicenseManagerMessages.L_MsgErrorText_12);
            }
            else if (ls == 6)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgActivated);
                LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusExtendedGrace_1);
            }
            else
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgActivated_Failed);
            }
        }

        // =====================================================================
        // /ato -- 联机激活
        // =====================================================================
        internal void ActivateProduct(string strActivationID)
        {
            strActivationID = (strActivationID == null ? "" : strActivationID).ToLowerInvariant();

            bool bFoundAtLeastOneKey = false;

            ManagementObject objService = GetServiceObject("Version");

            foreach (ManagementObject objProduct in
                     GetProductCollection(ProductIsPrimarySkuSelectClause + ", LicenseStatus, VLActivationTypeEnabled",
                                          PartialProductKeyNonNullWhereClause))
            {
                if (!CheckProductForCommand(objProduct, strActivationID))
                {
                    continue;
                }

                int iIsPrimaryWindowsSku = GetIsPrimaryWindowsSKU(objProduct);
                if (strActivationID.Length == 0 && iIsPrimaryWindowsSku == 2)
                {
                    OutputIndeterminateOperationWarning(objProduct);
                }

                // 本流程不做基于令牌的激活；若已配置为 TA，提示用户
                if (PropUInt(objProduct, "VLActivationTypeEnabled") == 3)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgTokenBasedActivationMustBeDone);
                    return;
                }

                string strOutput = SofwareLicenseManagerMessages.L_MsgActivating
                    .Replace("%PRODUCTNAME%", PropString(objProduct, "Name"))
                    .Replace("%PRODUCTID%", PropString(objProduct, "ID"));
                LineOut(strOutput);

                // 避免不必要地消耗 MAK 激活次数
                if (!IsMAK(PropString(objProduct, "Description")) || PropUInt(objProduct, "LicenseStatus") != 1)
                {
                    try
                    {
                        Invoke(objProduct, "Activate");
                    }
                    catch (Exception ex)
                    {
                        Quit(ex);
                    }

                    try
                    {
                        Invoke(objService, "RefreshLicenseStatus");
                    }
                    catch
                    {
                        // 与原脚本一致
                    }

                    Refresh(objProduct);
                }

                DisplayActivatedStatus(objProduct);

                bFoundAtLeastOneKey = true;
                if (strActivationID.Length != 0 || iIsPrimaryWindowsSku == 1)
                {
                    return;
                }
            }

            if (bFoundAtLeastOneKey)
            {
                return;
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgErrorProductNotFound);
        }

        // =====================================================================
        // /atp -- 用确认 ID 激活
        // =====================================================================
        internal void PhoneActivateProduct(string strCID, string strActivationID)
        {
            strActivationID = (strActivationID == null ? "" : strActivationID).ToLowerInvariant();

            bool bFoundAtLeastOneKey = false;
            ManagementObject objService = GetServiceObject("Version");

            foreach (ManagementObject objProduct in
                     GetProductCollection(ProductIsPrimarySkuSelectClause +
                                          ", OfflineInstallationId, LicenseStatus, LicenseStatusReason",
                                          PartialProductKeyNonNullWhereClause))
            {
                if (!CheckProductForCommand(objProduct, strActivationID))
                {
                    continue;
                }

                int iIsPrimaryWindowsSku = GetIsPrimaryWindowsSKU(objProduct);
                if (strActivationID.Length == 0 && iIsPrimaryWindowsSku == 2)
                {
                    OutputIndeterminateOperationWarning(objProduct);
                }

                try
                {
                    Invoke(objProduct, "DepositOfflineConfirmationId",
                           "InstallationId", PropString(objProduct, "OfflineInstallationId"),
                           "ConfirmationId", strCID);
                }
                catch (Exception ex)
                {
                    Quit(ex);
                }

                try
                {
                    Invoke(objService, "RefreshLicenseStatus");
                }
                catch
                {
                    // 与原脚本一致
                }

                Refresh(objProduct);

                uint ls = PropUInt(objProduct, "LicenseStatus");

                if (ls == 1)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgConfID.Replace("%ACTID%", PropString(objProduct, "ID")));
                }
                else if (ls == 4)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgErrorText_8 + SofwareLicenseManagerMessages.L_MsgErrorText_11);
                }
                else if (ls == 5 && PropUInt(objProduct, "LicenseStatusReason") == HR_SL_E_NOT_GENUINE)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgErrorText_8 + SofwareLicenseManagerMessages.L_MsgErrorText_12);
                }
                else if (ls == 6)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgActivated);
                    LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusExtendedGrace_1);
                }
                else
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgActivated_Failed);
                }

                bFoundAtLeastOneKey = true;
                if (strActivationID.Length != 0 || iIsPrimaryWindowsSku == 1)
                {
                    return;
                }
            }

            if (bFoundAtLeastOneKey)
            {
                return;
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgErrorProductNotFound);
        }

        // =====================================================================
        // /cpky -- 从注册表清除产品密钥
        // =====================================================================
        internal void ClearPKeyFromRegistry()
        {
            ManagementObject objService = GetServiceObject("Version");

            try
            {
                Invoke(objService, "ClearProductKeyFromRegistry");
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgClearedPKey);
        }

        // =====================================================================
        // /ilc -- 安装许可证文件
        // =====================================================================
        internal void InstallLicense(string licFile)
        {
            string licenseData = null;

            try
            {
                licenseData = ReadAllTextFile(licFile);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            ManagementObject objService = GetServiceObject("Version");

            try
            {
                Invoke(objService, "InstallLicense", "License", licenseData);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgLicenseFile.Replace("%LICENSEFILE%", licFile));
            LineOut("");
        }

        /// <summary>
        /// 对应 ReadAllTextFile：支持 ASCII / UTF-16LE / UTF-8（含 BOM 检测）。
        /// </summary>
        private static string ReadAllTextFile(string strFileName)
        {
            byte[] raw = File.ReadAllBytes(strFileName);

            if (raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE)
            {
                return new UnicodeEncoding(false, true).GetString(raw, 2, raw.Length - 2);
            }

            if (raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF)
            {
                return new UTF8Encoding(false).GetString(raw, 3, raw.Length - 3);
            }

            // 其余按 ASCII 处理（与 ADODB.Stream 的 "ascii" 行为一致）
            return Encoding.ASCII.GetString(raw);
        }

        // =====================================================================
        // /rilc -- 重新安装全部许可证文件
        // =====================================================================
        internal void ReinstallLicenses()
        {
            string systemRoot = Environment.ExpandEnvironmentVariables("%SystemRoot%");
            string strOemFolder = systemRoot + @"\system32\oem";
            string strSppTokensFolder = systemRoot + @"\system32\spp\tokens";

            LineOut(SofwareLicenseManagerMessages.L_MsgReinstallingLicenses);

            try
            {
                foreach (string subFolder in Directory.GetDirectories(strSppTokensFolder))
                {
                    InstallLicenseFiles(subFolder);
                }
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            if (Directory.Exists(strOemFolder))
            {
                InstallLicenseFiles(strOemFolder);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgLicensesReinstalled);
        }

        private void InstallLicenseFiles(string strParentDirectory)
        {
            foreach (string file in Directory.GetFiles(strParentDirectory))
            {
                if (file.EndsWith(".xrm-ms", StringComparison.OrdinalIgnoreCase))
                {
                    InstallLicense(file);
                }
            }

            foreach (string subFolder in Directory.GetDirectories(strParentDirectory))
            {
                InstallLicenseFiles(subFolder);
            }
        }

        // =====================================================================
        // /rearm 系列
        // =====================================================================
        internal void ReArmWindows()
        {
            ManagementObject objService = GetServiceObject("Version");

            try
            {
                Invoke(objService, "ReArmWindows");
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgRearm_1);
            LineOut(SofwareLicenseManagerMessages.L_MsgRearm_2);
        }

        internal void ReArmApp(string strSLID)
        {
            ManagementObject objService = GetServiceObject("Version");

            try
            {
                Invoke(objService, "ReArmApp", "ApplicationId", strSLID);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgRearm_1);
        }

        internal void ReArmSku(string strSLID)
        {
            strSLID = (strSLID == null ? "" : strSLID).ToLowerInvariant();

            bool bSkuFound = false;

            foreach (ManagementObject objProductIter in
                     GetProductCollection("ID", "ID = '" + strSLID + "'"))
            {
                string strSLActID = PropString(objProductIter, "ID");

                if (string.Equals(strSLID, strSLActID.ToLowerInvariant(), StringComparison.Ordinal))
                {
                    bSkuFound = true;

                    try
                    {
                        Invoke(objProductIter, "ReArmSku");
                    }
                    catch (Exception ex)
                    {
                        Quit(ex);
                    }

                    LineOut(SofwareLicenseManagerMessages.L_MsgRearm_1);
                    break;
                }
            }

            if (!bSkuFound)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgErrorProductNotFound);
            }
        }

        // =====================================================================
        // /xpr -- 到期日期
        // =====================================================================
        internal void ExpirationDatime(string strActivationID)
        {
            strActivationID = (strActivationID == null ? "" : strActivationID).ToLowerInvariant();

            bool bFound = false;
            string strWhereClause;

            if (strActivationID.Length == 0)
            {
                strWhereClause = "ApplicationId = '" + WindowsAppId + "'";
            }
            else
            {
                strWhereClause = "ID = '" + strActivationID.Replace("'", "") + "'";
            }

            strWhereClause = strWhereClause + " AND " + PartialProductKeyNonNullWhereClause;

            foreach (ManagementObject objProduct in
                     GetProductCollection(ProductIsPrimarySkuSelectClause + ", LicenseStatus, GracePeriodRemaining",
                                          strWhereClause))
            {
                uint ls = PropUInt(objProduct, "LicenseStatus");
                uint graceRemaining = PropUInt(objProduct, "GracePeriodRemaining");
                string strEnds = VbDateToString(DateTime.Now.AddMinutes(graceRemaining));

                bFound = true;

                int iIsPrimaryWindowsSku = GetIsPrimaryWindowsSKU(objProduct);
                if (strActivationID.Length == 0 && iIsPrimaryWindowsSku == 2)
                {
                    OutputIndeterminateOperationWarning(objProduct);
                }

                string strOutput = "";

                if (ls == 0)
                {
                    strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusUnlicensed;
                }
                else if (ls == 1)
                {
                    if (graceRemaining != 0)
                    {
                        string strDescription = PropString(objProduct, "Description");

                        if (IsTBL(strDescription))
                        {
                            strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusTBL.Replace("%ENDDATE%", strEnds);
                        }
                        else if (IsAVMA(strDescription))
                        {
                            strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusAVMA.Replace("%ENDDATE%", strEnds);
                        }
                        else
                        {
                            strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusVL.Replace("%ENDDATE%", strEnds);
                        }
                    }
                    else
                    {
                        strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusLicensed;
                    }
                }
                else if (ls == 2)
                {
                    strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusInitialGrace.Replace("%ENDDATE%", strEnds);
                }
                else if (ls == 3)
                {
                    strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusAdditionalGrace.Replace("%ENDDATE%", strEnds);
                }
                else if (ls == 4)
                {
                    strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusNonGenuineGrace.Replace("%ENDDATE%", strEnds);
                }
                else if (ls == 5)
                {
                    strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusNotification;
                }
                else if (ls == 6)
                {
                    strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusExtendedGrace.Replace("%ENDDATE%", strEnds);
                }

                if (strOutput.Length != 0)
                {
                    LineOut(PropString(objProduct, "Name") + ":");
                    LineOut("    " + strOutput);
                }
            }

            if (!bFound)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgErrorPKey);
            }
        }
    }
}
