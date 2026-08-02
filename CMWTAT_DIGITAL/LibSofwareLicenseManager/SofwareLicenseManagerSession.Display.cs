//
// SofwareLicenseManagerSession.Display.cs -- slmgr.vbs 的 C# 复刻：/dli 与 /dlv 的信息展示。
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;

namespace CMWTAT_DIGITAL.LibSofwareLicenseManager
{
    internal sealed partial class SofwareLicenseManagerSession
    {
        // =====================================================================
        // KMS 主机信息
        // =====================================================================
        private void DisplayKMSInformation(ManagementObject objService, ManagementObject objProduct)
        {
            ManagementObject objProductKMSValues = GetProductObject(
                "IsKeyManagementServiceMachine, KeyManagementServiceCurrentCount, " +
                "KeyManagementServiceTotalRequests, KeyManagementServiceFailedRequests, " +
                "KeyManagementServiceUnlicensedRequests, KeyManagementServiceLicensedRequests, " +
                "KeyManagementServiceOOBGraceRequests, KeyManagementServiceOOTGraceRequests, " +
                "KeyManagementServiceNonGenuineGraceRequests, KeyManagementServiceNotificationRequests",
                "id = '" + PropString(objProduct, "ID") + "'");

            if (PropUInt(objProductKMSValues, "IsKeyManagementServiceMachine") == 0)
            {
                return;
            }

            LineOut("");
            LineOut(SofwareLicenseManagerMessages.L_MsgKmsEnabled);
            LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsCurrentCount +
                    PropString(objProductKMSValues, "KeyManagementServiceCurrentCount"));

            uint dwValue = PropUInt(objService, "KeyManagementServiceListeningPort");
            if (dwValue == 0)
            {
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsListeningOnPort + DefaultPort);
            }
            else
            {
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsListeningOnPort +
                        dwValue.ToString(CultureInfo.InvariantCulture));
            }

            if (PropBool(objService, "KeyManagementServiceDnsPublishing"))
            {
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsDnsPublishingEnabled);
            }
            else
            {
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsDnsPublishingDisabled);
            }

            if (!PropBool(objService, "KeyManagementServiceLowPriority"))
            {
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsPriNormal);
            }
            else
            {
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsPriLow);
            }

            if (HasValue(objProductKMSValues, "KeyManagementServiceTotalRequests"))
            {
                LineOut("");
                LineOut(SofwareLicenseManagerMessages.L_MsgKmsCumulativeRequestsFromClients);
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsTotalRequestsRecieved +
                        PropString(objProductKMSValues, "KeyManagementServiceTotalRequests"));
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsFailedRequestsReceived +
                        PropString(objProductKMSValues, "KeyManagementServiceFailedRequests"));
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsRequestsWithStatusUnlicensed +
                        PropString(objProductKMSValues, "KeyManagementServiceUnlicensedRequests"));
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsRequestsWithStatusLicensed +
                        PropString(objProductKMSValues, "KeyManagementServiceLicensedRequests"));
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsRequestsWithStatusInitialGrace +
                        PropString(objProductKMSValues, "KeyManagementServiceOOBGraceRequests"));
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsRequestsWithStatusLicenseExpiredOrHwidOot +
                        PropString(objProductKMSValues, "KeyManagementServiceOOTGraceRequests"));
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsRequestsWithStatusNonGenuineGrace +
                        PropString(objProductKMSValues, "KeyManagementServiceNonGenuineGraceRequests"));
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsRequestsWithStatusNotification +
                        PropString(objProductKMSValues, "KeyManagementServiceNotificationRequests"));
            }
        }

        // =====================================================================
        // AD 激活客户端信息
        // =====================================================================
        private void DisplayADClientInformation(ManagementObject objService, ManagementObject objProduct)
        {
            LineOut("");
            LineOut(SofwareLicenseManagerMessages.L_MsgVLMostRecentActivationInfo);
            LineOut(SofwareLicenseManagerMessages.L_MsgADInfo);

            LineOut("    " + SofwareLicenseManagerMessages.L_MsgADInfoAOName + PropString(objProduct, "ADActivationObjectName"));
            LineOut("    " + SofwareLicenseManagerMessages.L_MsgADInfoAODN + PropString(objProduct, "ADActivationObjectDN"));
            LineOut("    " + SofwareLicenseManagerMessages.L_MsgADInfoExtendedPid + PropString(objProduct, "ADActivationCsvlkPid"));
            LineOut("    " + SofwareLicenseManagerMessages.L_MsgADInfoActID + PropString(objProduct, "ADActivationCsvlkSkuId"));
        }

        // =====================================================================
        // 基于令牌的激活客户端信息
        // =====================================================================
        private void DisplayTkaClientInformation(ManagementObject objService, ManagementObject objProduct)
        {
            LineOut("");
            LineOut(SofwareLicenseManagerMessages.L_MsgVLMostRecentActivationInfo);
            LineOut(SofwareLicenseManagerMessages.L_MsgTkaInfo);

            LineOut("    " + SofwareLicenseManagerMessages.L_MsgTkaInfoILID
                    .Replace("%ILID%", PropString(objProduct, "TokenActivationILID")));
            LineOut("    " + SofwareLicenseManagerMessages.L_MsgTkaInfoILVID
                    .Replace("%ILVID%", PropString(objProduct, "TokenActivationILVID")));
            LineOut("    " + SofwareLicenseManagerMessages.L_MsgTkaInfoGrantNo
                    .Replace("%GRANTNO%", PropString(objProduct, "TokenActivationGrantNumber")));
            LineOut("    " + SofwareLicenseManagerMessages.L_MsgTkaInfoThumbprint
                    .Replace("%THUMBPRINT%", PropString(objProduct, "TokenActivationCertificateThumbprint")));
        }

        // =====================================================================
        // KMS 客户端信息
        // =====================================================================
        private void DisplayKMSClientInformation(ManagementObject objService, ManagementObject objProduct)
        {
            uint iVLRenewalInterval = PropUInt(objProduct, "VLRenewalInterval");
            uint iVLActivationInterval = PropUInt(objProduct, "VLActivationInterval");

            bool bFixedKms = false;
            bool bKmsLookupDomain = false;
            string strKms;
            string strPort = "";

            LineOut("");
            LineOut(SofwareLicenseManagerMessages.L_MsgVLMostRecentActivationInfo);
            LineOut(SofwareLicenseManagerMessages.L_MsgKmsInfo);
            LineOut("    " + SofwareLicenseManagerMessages.L_MsgCmid + PropString(objService, "ClientMachineID"));

            string strKmsLookupDomain = PropString(objProduct, "KeyManagementServiceLookupDomain");

            if (strKmsLookupDomain.Length != 0)
            {
                bKmsLookupDomain = true;
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsLookupDomain + strKmsLookupDomain);
            }

            strKms = PropString(objProduct, "KeyManagementServiceMachine");

            if (strKms.Length != 0)
            {
                bFixedKms = true;
                uint nPort = PropUInt(objProduct, "KeyManagementServicePort");
                strPort = nPort == 0 ? DefaultPort : nPort.ToString(CultureInfo.InvariantCulture);
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgRegisteredKmsName + strKms + ":" + strPort);
            }
            else
            {
                strKms = PropString(objProduct, "DiscoveredKeyManagementServiceMachineName");
                uint nPort = PropUInt(objProduct, "DiscoveredKeyManagementServiceMachinePort");
                strPort = nPort.ToString(CultureInfo.InvariantCulture);

                if (strKms.Length == 0 || nPort == 0)
                {
                    LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsFromDnsUnavailable);
                }
                else
                {
                    LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsFromDns + strKms + ":" + strPort);
                }
            }

            string strIpAddress = PropString(objProduct, "DiscoveredKeyManagementServiceMachineIpAddress");

            if (strIpAddress.Length == 0)
            {
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsIpAddressUnavailable);
            }
            else
            {
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsIpAddress + strIpAddress);
            }

            LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsPID4 + PropString(objProduct, "KeyManagementServiceProductKeyID"));
            LineOut("    " + SofwareLicenseManagerMessages.L_MsgActivationInterval
                    .Replace("%INTERVAL%", iVLActivationInterval.ToString(CultureInfo.InvariantCulture)));
            LineOut("    " + SofwareLicenseManagerMessages.L_MsgRenewalInterval
                    .Replace("%INTERVAL%", iVLRenewalInterval.ToString(CultureInfo.InvariantCulture)));

            if (PropBool(objService, "KeyManagementServiceHostCaching"))
            {
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsHostCachingEnabled);
            }
            else
            {
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgKmsHostCachingDisabled);
            }

            if (bKmsLookupDomain && bFixedKms)
            {
                LineOut("");
                LineOut(SofwareLicenseManagerMessages.L_MsgKmsUseMachineNameOverrides.Replace("%KMS%", strKms + ":" + strPort));
            }
        }

        // =====================================================================
        // AVMA 客户端信息
        // =====================================================================
        private void DisplayAVMAClientInformation(ManagementObject objProduct)
        {
            string strHostName = PropString(objProduct, "AutomaticVMActivationHostMachineName");
            bool bHostName = strHostName.Length != 0;

            DateTime displayDate;
            bool bFiletime = TryGetWmiDate(objProduct, "AutomaticVMActivationLastActivationTime", out displayDate);

            string strPid = PropString(objProduct, "AutomaticVMActivationHostDigitalPid2");
            bool bPid = strPid.Length != 0;

            if (!(bHostName || bFiletime || bPid))
            {
                return;
            }

            LineOut("");
            LineOut(SofwareLicenseManagerMessages.L_MsgVLMostRecentActivationInfo);
            LineOut(SofwareLicenseManagerMessages.L_MsgAVMAInfo);

            LineOut("    " + SofwareLicenseManagerMessages.L_MsgAVMAHostMachineName +
                    (bHostName ? strHostName : SofwareLicenseManagerMessages.L_MsgNotAvailable));

            LineOut("    " + SofwareLicenseManagerMessages.L_MsgAVMALastActTime +
                    (bFiletime ? VbDateToString(displayDate) : SofwareLicenseManagerMessages.L_MsgNotAvailable));

            LineOut("    " + SofwareLicenseManagerMessages.L_MsgAVMAHostPid2 +
                    (bPid ? strPid : SofwareLicenseManagerMessages.L_MsgNotAvailable));
        }

        // =====================================================================
        // /dli 与 /dlv
        // =====================================================================
        internal void DisplayAllInformation(string strParm, bool bVerbose)
        {
            strParm = (strParm == null ? "" : strParm).ToLowerInvariant();

            bool productKeyFound = false;

            string strServiceSelectClause =
                "KeyManagementServiceListeningPort, KeyManagementServiceDnsPublishing, " +
                "KeyManagementServiceLowPriority, ClientMachineId, KeyManagementServiceHostCaching, " +
                "Version";

            string strProductSelectClause =
                ProductIsPrimarySkuSelectClause + ", " +
                "ProductKeyID, ProductKeyChannel, OfflineInstallationId, " +
                "ProcessorURL, MachineURL, UseLicenseURL, ProductKeyURL, ValidationURL, " +
                "GracePeriodRemaining, LicenseStatus, LicenseStatusReason, EvaluationEndDate, " +
                "VLRenewalInterval, VLActivationInterval, KeyManagementServiceLookupDomain, KeyManagementServiceMachine, " +
                "KeyManagementServicePort, DiscoveredKeyManagementServiceMachineName, " +
                "DiscoveredKeyManagementServiceMachinePort, DiscoveredKeyManagementServiceMachineIpAddress, KeyManagementServiceProductKeyID," +
                "TokenActivationILID, TokenActivationILVID, TokenActivationGrantNumber," +
                "TokenActivationCertificateThumbprint, TokenActivationAdditionalInfo, TrustedTime," +
                "ADActivationObjectName, ADActivationObjectDN, ADActivationCsvlkPid, ADActivationCsvlkSkuId, VLActivationTypeEnabled, VLActivationType," +
                "IAID, AutomaticVMActivationHostMachineName, AutomaticVMActivationLastActivationTime, AutomaticVMActivationHostDigitalPid2";

            if (bVerbose)
            {
                strServiceSelectClause = "RemainingWindowsReArmCount, " + strServiceSelectClause;
                strProductSelectClause = "RemainingAppReArmCount, RemainingSkuReArmCount, " + strProductSelectClause;
            }

            ManagementObject objService = GetServiceObject(strServiceSelectClause);

            if (bVerbose)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgServiceVersion + PropString(objService, "Version"));
            }

            string strIterSelectClause = (strParm == "all")
                ? strProductSelectClause
                : ProductIsPrimarySkuSelectClause;

            foreach (ManagementObject objProductIter in GetProductCollection(strIterSelectClause, EmptyWhereClause))
            {
                string strSLActID = PropString(objProductIter, "ID");

                int iIsPrimaryWindowsSku = GetIsPrimaryWindowsSKU(objProductIter);
                bool bUseDefault = false;
                bool bShowSkuInformation = false;

                if (strParm.Length == 0 && (iIsPrimaryWindowsSku == 1 || iIsPrimaryWindowsSku == 2))
                {
                    bUseDefault = true;
                    bShowSkuInformation = true;
                }

                if (strParm.Length == 0
                    && PropBool(objProductIter, "LicenseIsAddon")
                    && PropString(objProductIter, "PartialProductKey").Length != 0)
                {
                    bShowSkuInformation = true;
                }

                if (strParm == "all")
                {
                    bShowSkuInformation = true;
                }

                if (strParm == strSLActID.ToLowerInvariant())
                {
                    bShowSkuInformation = true;
                }

                if (!bShowSkuInformation)
                {
                    continue;
                }

                ManagementObject objProduct = (strParm == "all")
                    ? objProductIter
                    : GetProductObject(strProductSelectClause, "id = '" + strSLActID + "'");

                string strDescription = PropString(objProduct, "Description");

                // 未指定参数且无法确认是主 SKU 时给出警告
                if (bUseDefault && iIsPrimaryWindowsSku == 2)
                {
                    OutputIndeterminateOperationWarning(objProduct);
                }

                productKeyFound = true;

                LineOut("");
                LineOut(SofwareLicenseManagerMessages.L_MsgProductName + PropString(objProduct, "Name"));
                LineOut(SofwareLicenseManagerMessages.L_MsgProductDesc + strDescription);

                string strTkaAdditionalInfo = PropString(objProduct, "TokenActivationAdditionalInfo");
                if (strTkaAdditionalInfo.Length != 0)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgTkaInfoAdditionalInfo.Replace("%MOREINFO%", strTkaAdditionalInfo));
                }

                bool bKmsServer = IsKmsServer(strDescription);
                bool bKmsClient = IsKmsClient(strDescription);
                bool bTBL = IsTBL(strDescription);
                bool bAVMA = IsAVMA(strDescription);

                if (bVerbose)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgActID + strSLActID);
                    LineOut(SofwareLicenseManagerMessages.L_MsgAppID + PropString(objProduct, "ApplicationID"));
                    LineOut(SofwareLicenseManagerMessages.L_MsgPID4 + PropString(objProduct, "ProductKeyID"));
                    LineOut(SofwareLicenseManagerMessages.L_MsgChannel + PropString(objProduct, "ProductKeyChannel"));
                    LineOut(SofwareLicenseManagerMessages.L_MsgInstallationID + PropString(objProduct, "OfflineInstallationId"));

                    if (!bKmsClient && !bAVMA)
                    {
                        // 出于向下兼容，UseLicenseURL 同时被用作产品激活 URL
                        string strUrl;

                        strUrl = PropString(objProduct, "ProcessorURL");
                        if (strUrl.Length != 0)
                        {
                            LineOut(SofwareLicenseManagerMessages.L_MsgProcessorCertUrl + strUrl);
                        }

                        strUrl = PropString(objProduct, "MachineURL");
                        if (strUrl.Length != 0)
                        {
                            LineOut(SofwareLicenseManagerMessages.L_MsgMachineCertUrl + strUrl);
                        }

                        strUrl = PropString(objProduct, "UseLicenseURL");
                        if (strUrl.Length != 0)
                        {
                            LineOut(SofwareLicenseManagerMessages.L_MsgUseLicenseCertUrl + strUrl);
                        }

                        strUrl = PropString(objProduct, "ProductKeyURL");
                        if (strUrl.Length != 0)
                        {
                            LineOut(SofwareLicenseManagerMessages.L_MsgPKeyCertUrl + strUrl);
                        }

                        strUrl = PropString(objProduct, "ValidationURL");
                        if (strUrl.Length != 0)
                        {
                            LineOut(SofwareLicenseManagerMessages.L_MsgValidationUrl + strUrl);
                        }
                    }
                }

                string strPartialProductKey = PropString(objProduct, "PartialProductKey");
                if (strPartialProductKey.Length != 0)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgPartialPKey + strPartialProductKey);
                }
                else
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgErrorLicenseNotInUse);
                }

                uint ls = PropUInt(objProduct, "LicenseStatus");
                uint gpMin;
                long gpDay;
                string strOutput;

                if (ls == 0)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusUnlicensed_1);
                }
                else if (ls == 1)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusLicensed_1);
                    gpMin = PropUInt(objProduct, "GracePeriodRemaining");
                    if (gpMin != 0)
                    {
                        gpDay = GetDaysFromMins(gpMin);

                        if (bTBL)
                        {
                            strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusTBL_1;
                        }
                        else if (bAVMA)
                        {
                            strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusAVMA_1;
                        }
                        else
                        {
                            strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusVL_1;
                        }

                        strOutput = strOutput
                            .Replace("%MINUTE%", gpMin.ToString(CultureInfo.InvariantCulture))
                            .Replace("%DAY%", gpDay.ToString(CultureInfo.InvariantCulture));
                        LineOut(strOutput);
                    }
                }
                else if (ls == 2 || ls == 3 || ls == 4 || ls == 6)
                {
                    if (ls == 2)
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusInitialGrace_1);
                    }
                    else if (ls == 3)
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusAdditionalGrace_1);
                    }
                    else if (ls == 4)
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusNonGenuineGrace_1);
                    }
                    else
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusExtendedGrace_1);
                    }

                    gpMin = PropUInt(objProduct, "GracePeriodRemaining");
                    gpDay = GetDaysFromMins(gpMin);
                    strOutput = SofwareLicenseManagerMessages.L_MsgLicenseStatusTimeRemaining
                        .Replace("%MINUTE%", gpMin.ToString(CultureInfo.InvariantCulture))
                        .Replace("%DAY%", gpDay.ToString(CultureInfo.InvariantCulture));
                    LineOut(strOutput);
                }
                else if (ls == 5)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusNotification_1);

                    uint reason = PropUInt(objProduct, "LicenseStatusReason");
                    string strErr = VbHex(unchecked((int)reason));

                    if (reason == HR_SL_E_NOT_GENUINE)
                    {
                        strOutput = SofwareLicenseManagerMessages.L_MsgNotificationErrorReasonNonGenuine;
                    }
                    else if (reason == HR_SL_E_GRACE_TIME_EXPIRED)
                    {
                        strOutput = SofwareLicenseManagerMessages.L_MsgNotificationErrorReasonExpiration;
                    }
                    else
                    {
                        strOutput = SofwareLicenseManagerMessages.L_MsgNotificationErrorReasonOther;
                    }

                    LineOut(strOutput.Replace("%ERRCODE%", strErr));
                }
                else
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusUnknown);
                }

                DateTime displayDate;

                if (ls != 0 && bVerbose)
                {
                    if (TryGetWmiDate(objProduct, "EvaluationEndDate", out displayDate))
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgLicenseStatusEvalEndData + VbDateToString(displayDate));
                    }
                }

                if (bVerbose)
                {
                    if (string.Equals(PropString(objProduct, "ApplicationId"), WindowsAppId,
                                      StringComparison.OrdinalIgnoreCase))
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgRemainingWindowsRearmCount
                                .Replace("%COUNT%", PropString(objService, "RemainingWindowsReArmCount")));
                    }
                    else
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgRemainingAppRearmCount
                                .Replace("%COUNT%", PropString(objProduct, "RemainingAppReArmCount")));
                    }

                    LineOut(SofwareLicenseManagerMessages.L_MsgRemainingSkuRearmCount
                            .Replace("%COUNT%", PropString(objProduct, "RemainingSkuReArmCount")));

                    if (TryGetWmiDate(objProduct, "TrustedTime", out displayDate))
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgCurrentTrustedTime + VbDateToString(displayDate));
                    }
                }

                //
                // KMS 客户端属性
                //
                if (bKmsClient)
                {
                    uint vlType = PropUInt(objProduct, "VLActivationTypeEnabled");

                    if (vlType == 1)
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgVLActivationTypeAD);
                    }
                    else if (vlType == 2)
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgVLActivationTypeKMS);
                    }
                    else if (vlType == 3)
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgVLActivationTypeToken);
                    }
                    else
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgVLActivationTypeAll);
                    }

                    if (IsADActivated(objProduct))
                    {
                        DisplayADClientInformation(objService, objProduct);
                    }
                    else if (IsTokenActivated(objProduct))
                    {
                        DisplayTkaClientInformation(objService, objProduct);
                    }
                    else if (ls != 1)
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgPleaseActivateRefreshKMSInfo);
                    }
                    else
                    {
                        DisplayKMSClientInformation(objService, objProduct);
                    }
                }

                if (bKmsServer || iIsPrimaryWindowsSku == 1 || iIsPrimaryWindowsSku == 2)
                {
                    DisplayKMSInformation(objService, objProduct);
                }

                if (bAVMA)
                {
                    string strAVMAId = PropString(objProduct, "IAID");

                    if (strAVMAId.Length != 0)
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgAVMAID + strAVMAId);
                    }
                    else
                    {
                        LineOut(SofwareLicenseManagerMessages.L_MsgAVMAID + SofwareLicenseManagerMessages.L_MsgNotAvailable);
                    }

                    DisplayAVMAClientInformation(objProduct);
                }

                // 非 all 模式下，若已经处理到指定项就结束
                if (strParm != "all" && strParm == strSLActID.ToLowerInvariant())
                {
                    break;
                }

                LineOut("");
            }

            if (!productKeyFound)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgErrorPKey);
            }
        }
    }
}
