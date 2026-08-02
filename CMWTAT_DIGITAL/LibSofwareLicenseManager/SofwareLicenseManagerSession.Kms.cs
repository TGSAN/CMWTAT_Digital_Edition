//
// SofwareLicenseManagerSession.Kms.cs -- slmgr.vbs 的 C# 复刻：KMS 客户端与 KMS 主机相关选项。
//
// 对应：/skms /ckms /skms-domain /ckms-domain /skhc /ckhc
//       /sprt /sai /sri /spri /cpri /sdns /cdns /act-type
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;

namespace CMWTAT_DIGITAL.LibSofwareLicenseManager
{
    internal sealed partial class SofwareLicenseManagerSession
    {
        /// <summary>对应 QuitIfErrorRestoreKmsName：出错时先还原 KMS 名称再报错退出。</summary>
        private void QuitIfErrorRestoreKmsName(Exception ex, ManagementObject obj, string strKmsName)
        {
            SofwareLicenseManagerQuitException quit = ex as SofwareLicenseManagerQuitException;
            if (quit != null)
            {
                throw quit;
            }

            SofwareLicenseManagerError err = SofwareLicenseManagerError.FromException(ex);

            try
            {
                if (string.IsNullOrEmpty(strKmsName))
                {
                    Invoke(obj, "ClearKeyManagementServiceMachine");
                }
                else
                {
                    Invoke(obj, "SetKeyManagementServiceMachine", "MachineName", strKmsName);
                }
            }
            catch
            {
                // 还原失败时忽略，仍然报告原始错误
            }

            ShowError(SofwareLicenseManagerMessages.L_MsgErrorText_8, err);
            ExitScript(err.Number);
        }

        /// <summary>对应 GetKmsClientObjectByActivationID：未指定 ActID 时返回服务对象。</summary>
        private ManagementObject GetKmsClientObjectByActivationID(string strActivationID)
        {
            strActivationID = (strActivationID == null ? "" : strActivationID).ToLowerInvariant();

            if (strActivationID.Length == 0)
            {
                return GetServiceObject("Version, " + KMSClientLookupClause);
            }

            foreach (ManagementObject objProduct in
                     GetProductCollection("ID, " + KMSClientLookupClause, EmptyWhereClause))
            {
                if (string.Equals(PropString(objProduct, "ID").ToLowerInvariant(), strActivationID,
                                  StringComparison.Ordinal))
                {
                    return objProduct;
                }
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgErrorActivationID.Replace("%ActID%", strActivationID));
            return null;
        }

        // =====================================================================
        // /skms
        // =====================================================================
        internal void SetKmsMachineName(string strKmsNamePort, string strActivationID)
        {
            string strKmsName;
            string strKmsPort;

            if (strKmsNamePort == null)
            {
                strKmsNamePort = "";
            }

            int nBracketEnd = strKmsNamePort.IndexOf(']') + 1; // 1 基，未找到为 0

            if (strKmsNamePort.StartsWith("[", StringComparison.Ordinal) && nBracketEnd > 1)
            {
                // IPv6 地址
                if (strKmsNamePort.Length == nBracketEnd)
                {
                    strKmsName = strKmsNamePort;
                    strKmsPort = "";
                }
                else
                {
                    strKmsName = strKmsNamePort.Substring(0, nBracketEnd);
                    strKmsPort = strKmsNamePort.Substring(nBracketEnd + 1);
                }
            }
            else
            {
                // IPv4 地址 / 主机名
                int nColon = strKmsNamePort.IndexOf(':') + 1; // 1 基，未找到为 0
                if (nColon != 0)
                {
                    strKmsName = strKmsNamePort.Substring(0, nColon - 1);
                    strKmsPort = strKmsNamePort.Substring(nColon);
                }
                else
                {
                    strKmsName = strKmsNamePort;
                    strKmsPort = "";
                }
            }

            ManagementObject objTarget = GetKmsClientObjectByActivationID(strActivationID);
            if (objTarget == null)
            {
                return;
            }

            string strKmsNamePrev = PropString(objTarget, "KeyManagementServiceMachine");

            if (strKmsName.Length != 0)
            {
                try
                {
                    Invoke(objTarget, "SetKeyManagementServiceMachine", "MachineName", strKmsName);
                }
                catch (Exception ex)
                {
                    Quit(ex);
                }
            }

            if (strKmsPort.Length != 0)
            {
                uint nKmsPort = 0;

                try
                {
                    nKmsPort = uint.Parse(strKmsPort, CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    QuitIfErrorRestoreKmsName(ex, objTarget, strKmsNamePrev);
                }

                try
                {
                    Invoke(objTarget, "SetKeyManagementServicePort", "PortNumber", nKmsPort);
                }
                catch (Exception ex)
                {
                    QuitIfErrorRestoreKmsName(ex, objTarget, strKmsNamePrev);
                }
            }
            else
            {
                try
                {
                    Invoke(objTarget, "ClearKeyManagementServicePort");
                }
                catch (Exception ex)
                {
                    QuitIfErrorRestoreKmsName(ex, objTarget, strKmsNamePrev);
                }
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgKmsNameSet.Replace("%KMS%", strKmsNamePort));

            if (PropString(objTarget, "KeyManagementServiceLookupDomain").Length != 0)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgKmsUseMachineNameOverrides.Replace("%KMS%", strKmsNamePort));
            }
        }

        // =====================================================================
        // /ckms
        // =====================================================================
        internal void ClearKms(string strActivationID)
        {
            ManagementObject objTarget = GetKmsClientObjectByActivationID(strActivationID);
            if (objTarget == null)
            {
                return;
            }

            try
            {
                Invoke(objTarget, "ClearKeyManagementServiceMachine");
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            try
            {
                Invoke(objTarget, "ClearKeyManagementServicePort");
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgKmsNameCleared);

            string strLookupDomain = PropString(objTarget, "KeyManagementServiceLookupDomain");
            if (strLookupDomain.Length != 0)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgKmsUseLookupDomain.Replace("%FQDN%", strLookupDomain));
            }
        }

        // =====================================================================
        // /skms-domain
        // =====================================================================
        internal void SetKmsLookupDomain(string strKmsLookupDomain, string strActivationID)
        {
            ManagementObject objTarget = GetKmsClientObjectByActivationID(strActivationID);
            if (objTarget == null)
            {
                return;
            }

            try
            {
                Invoke(objTarget, "SetKeyManagementServiceLookupDomain",
                       "LookupDomain", strKmsLookupDomain);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgKmsLookupDomainSet.Replace("%FQDN%", strKmsLookupDomain));

            string strKms = PropString(objTarget, "KeyManagementServiceMachine");
            if (strKms.Length != 0)
            {
                string nPort = PropString(objTarget, "KeyManagementServicePort");
                LineOut(SofwareLicenseManagerMessages.L_MsgKmsUseMachineNameOverrides.Replace("%KMS%", strKms + ":" + nPort));
            }
        }

        // =====================================================================
        // /ckms-domain
        // =====================================================================
        internal void ClearKmsLookupDomain(string strActivationID)
        {
            ManagementObject objTarget = GetKmsClientObjectByActivationID(strActivationID);
            if (objTarget == null)
            {
                return;
            }

            try
            {
                Invoke(objTarget, "ClearKeyManagementServiceLookupDomain");
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgKmsLookupDomainCleared);

            string strKms = PropString(objTarget, "KeyManagementServiceMachine");
            if (strKms.Length != 0)
            {
                string nPort = PropString(objTarget, "KeyManagementServicePort");
                LineOut(SofwareLicenseManagerMessages.L_MsgKmsUseMachineName.Replace("%KMS%", strKms + ":" + nPort));
            }
        }

        // =====================================================================
        // /skhc 与 /ckhc
        // =====================================================================
        internal void SetHostCachingDisable(bool boolHostCaching)
        {
            ManagementObject objService = GetServiceObject("Version");

            try
            {
                Invoke(objService, "DisableKeyManagementServiceHostCaching",
                       "Disable", boolHostCaching);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            if (boolHostCaching)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgKmsHostCachingDisabled);
            }
            else
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgKmsHostCachingEnabled);
            }
        }

        // =====================================================================
        // /sai
        // =====================================================================
        internal void SetActivationInterval(string strInterval)
        {
            long intInterval;

            if (!TryParseInterval(strInterval, out intInterval) || intInterval < 0)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgInvalidDataError);
                return;
            }

            ManagementObject objService = GetServiceObject("Version");

            uint kmsFlag = 0;

            foreach (ManagementObject objProduct in
                     GetProductCollection("ID, IsKeyManagementServiceMachine", PartialProductKeyNonNullWhereClause))
            {
                kmsFlag = PropUInt(objProduct, "IsKeyManagementServiceMachine");
                if (kmsFlag == 1)
                {
                    try
                    {
                        Invoke(objService, "SetVLActivationInterval",
                               "ActivationInterval", (uint)intInterval);
                    }
                    catch (Exception ex)
                    {
                        Quit(ex);
                    }

                    LineOut(SofwareLicenseManagerMessages.L_MsgActivationSet
                            .Replace("%ACTIVATION%", intInterval.ToString(CultureInfo.InvariantCulture)));
                    LineOut(SofwareLicenseManagerMessages.L_MsgWarningKmsReboot);
                    break;
                }
            }

            if (kmsFlag != 1)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgWarningActivation);
            }
        }

        // =====================================================================
        // /sri
        // =====================================================================
        internal void SetRenewalInterval(string strInterval)
        {
            long intInterval;

            if (!TryParseInterval(strInterval, out intInterval) || intInterval < 0)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgInvalidDataError);
                return;
            }

            ManagementObject objService = GetServiceObject("Version");

            uint kmsFlag = 0;

            foreach (ManagementObject objProduct in
                     GetProductCollection("ID, IsKeyManagementServiceMachine", PartialProductKeyNonNullWhereClause))
            {
                kmsFlag = PropUInt(objProduct, "IsKeyManagementServiceMachine");
                if (kmsFlag != 0)
                {
                    try
                    {
                        Invoke(objService, "SetVLRenewalInterval",
                               "RenewalInterval", (uint)intInterval);
                    }
                    catch (Exception ex)
                    {
                        Quit(ex);
                    }

                    LineOut(SofwareLicenseManagerMessages.L_MsgRenewalSet
                            .Replace("%RENEWAL%", intInterval.ToString(CultureInfo.InvariantCulture)));
                    LineOut(SofwareLicenseManagerMessages.L_MsgWarningKmsReboot);
                    break;
                }
            }

            if (kmsFlag != 1)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgWarningRenewal);
            }
        }

        // =====================================================================
        // /sprt
        // =====================================================================
        internal void SetKmsListenPort(string strPort)
        {
            ManagementObject objService = GetServiceObject("Version");

            uint kmsFlag = 0;

            foreach (ManagementObject objProduct in
                     GetProductCollection("ID, IsKeyManagementServiceMachine", PartialProductKeyNonNullWhereClause))
            {
                kmsFlag = PropUInt(objProduct, "IsKeyManagementServiceMachine");
                if (kmsFlag != 0)
                {
                    uint nPort = 0;

                    try
                    {
                        nPort = uint.Parse(strPort, CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        Quit(ex);
                    }

                    try
                    {
                        Invoke(objService, "SetKeyManagementServiceListeningPort", "PortNumber", nPort);
                    }
                    catch (Exception ex)
                    {
                        Quit(ex);
                    }

                    LineOut(SofwareLicenseManagerMessages.L_MsgKmsPortSet.Replace("%PORT%", strPort));
                    LineOut(SofwareLicenseManagerMessages.L_MsgWarningKmsReboot);
                    break;
                }
            }

            if (kmsFlag != 1)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgWarningKmsPort);
            }
        }

        // =====================================================================
        // /sdns 与 /cdns
        // =====================================================================
        internal void SetDnsPublishingDisabled(bool bDisable)
        {
            ManagementObject objService = GetServiceObject("Version");

            uint kmsFlag = 0;

            foreach (ManagementObject objProduct in
                     GetProductCollection("ID, IsKeyManagementServiceMachine", PartialProductKeyNonNullWhereClause))
            {
                kmsFlag = PropUInt(objProduct, "IsKeyManagementServiceMachine");
                if (kmsFlag != 0)
                {
                    try
                    {
                        Invoke(objService, "DisableKeyManagementServiceDnsPublishing", "Disable", bDisable);
                    }
                    catch (Exception ex)
                    {
                        Quit(ex);
                    }

                    if (bDisable)
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgKmsDnsPublishingDisabled);
                    }
                    else
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgKmsDnsPublishingEnabled);
                    }

                    LineOut(SofwareLicenseManagerMessages.L_MsgWarningKmsReboot);
                    break;
                }
            }

            if (kmsFlag != 1)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgKmsDnsPublishingWarning);
            }
        }

        // =====================================================================
        // /spri 与 /cpri
        // 说明：原脚本此处的 Exit For 位于 If 之外，因此只检查第一个产品。此行为被原样保留。
        // =====================================================================
        internal void SetKmsLowPriority(bool bLow)
        {
            ManagementObject objService = GetServiceObject("Version");

            uint kmsFlag = 0;

            foreach (ManagementObject objProduct in
                     GetProductCollection("ID, IsKeyManagementServiceMachine", PartialProductKeyNonNullWhereClause))
            {
                kmsFlag = PropUInt(objProduct, "IsKeyManagementServiceMachine");
                if (kmsFlag != 0)
                {
                    try
                    {
                        Invoke(objService, "EnableKeyManagementServiceLowPriority", "Enable", bLow);
                    }
                    catch (Exception ex)
                    {
                        Quit(ex);
                    }

                    if (bLow)
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgKmsPriSetToLow);
                    }
                    else
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgKmsPriSetToNormal);
                    }

                    LineOut(SofwareLicenseManagerMessages.L_MsgWarningKmsReboot);
                }

                break;
            }

            if (kmsFlag != 1)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgWarningKmsPri);
            }
        }

        // =====================================================================
        // /act-type
        // =====================================================================
        internal void SetVLActivationType(string strType, string strActivationID)
        {
            long intType;

            if (strType == null)
            {
                intType = 0;
            }
            else if (!TryParseInterval(strType, out intType))
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgInvalidDataError);
                return;
            }

            if (intType < 0 || intType > 3)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgInvalidDataError);
                return;
            }

            ManagementObject objTarget = GetKmsClientObjectByActivationID(strActivationID);
            if (objTarget == null)
            {
                return;
            }

            try
            {
                if (intType != 0)
                {
                    Invoke(objTarget, "SetVLActivationTypeEnabled", "ActivationType", (uint)intType);
                }
                else
                {
                    Invoke(objTarget, "ClearVLActivationTypeEnabled");
                }
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgVLActivationTypeSet);
        }

        private static bool TryParseInterval(string value, out long result)
        {
            result = 0;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return long.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
    }
}
