//
// SofwareLicenseManagerSession.Ad.cs -- slmgr.vbs 的 C# 复刻：Active Directory 激活。
//
// 对应：/ad-activation-online /ad-activation-get-iid /ad-activation-apply-cid /ao-list /del-ao
//

using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Globalization;
using System.Management;

namespace CMWTAT_DIGITAL.LibSofwareLicenseManager
{
    internal sealed partial class SofwareLicenseManagerSession
    {
        // ---------- AD 相关常量（对应原脚本同名 const） ----------
        private const string ADLdapProviderPrefix = "LDAP://";
        private const string ADRootDSE = "rootDSE";
        private const string ADConfigurationNC = "configurationNamingContext";
        private const string ADActObjContainer = "CN=Activation Objects,CN=Microsoft SPP,CN=Services,";
        private const string ADActObjClass = "msSPP-ActivationObject";
        private const string ADActObjAttribSkuId = "msSPP-CSVLKSkuId";
        private const string ADActObjAttribPid = "msSPP-CSVLKPid";
        private const string ADActObjAttribPartialPkey = "msSPP-CSVLKPartialProductKey";
        private const string ADActObjDisplayName = "displayName";
        private const string ADActObjAttribDN = "distinguishedName";

        private const AuthenticationTypes ADS_READONLY_SERVER = AuthenticationTypes.ReadonlyServer;

        /// <summary>对应 IsADActivated。</summary>
        internal static bool IsADActivated(ManagementObject objProduct)
        {
            return PropUInt(objProduct, "VLActivationType") == 1;
        }

        // =====================================================================
        // /ad-activation-online
        // =====================================================================
        internal void ADActivateOnline(string strProductKey, string strActivationObjectName)
        {
            FailRemoteExec();

            ManagementObject objService = GetServiceObject("Version");

            try
            {
                Invoke(objService, "DoActiveDirectoryOnlineActivation",
                       "ProductKey", strProductKey,
                       "ActivationObjectName", strActivationObjectName);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgActivated);
        }

        // =====================================================================
        // /ad-activation-get-iid
        // =====================================================================
        internal void ADGetIID(string strProductKey)
        {
            FailRemoteExec();

            ManagementObject objService = GetServiceObject("Version");

            string strIID = "";

            try
            {
                ManagementBaseObject outParams = Invoke(objService,
                    "GenerateActiveDirectoryOfflineActivationId", "ProductKey", strProductKey);
                strIID = Convert.ToString(GetOutParam(outParams, "InstallationId"),
                                          CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgInstallationID + strIID);
            LineOut("");
            LineOut(SofwareLicenseManagerMessages.L_MsgPhoneNumbers);
        }

        // =====================================================================
        // /ad-activation-apply-cid
        // =====================================================================
        internal void ADActivatePhone(string strProductKey, string strCID, string strActivationObjectName)
        {
            FailRemoteExec();

            ManagementObject objService = GetServiceObject("Version");

            try
            {
                Invoke(objService, "DepositActiveDirectoryOfflineActivationConfirmation",
                       "ProductKey", strProductKey,
                       "ConfirmationId", strCID,
                       "ActivationObjectName", strActivationObjectName);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgActivated);
        }

        // =====================================================================
        // /ao-list
        // =====================================================================
        internal void ADListActivationObjects()
        {
            FailRemoteExec();

            //
            // 必须使用「计算机」所属域来查询激活对象，
            // 避免落到当前用户所在的（可能属于其它林的）域上。
            //
            string machineDomain = GetMachineDomain();

            string configurationNC = "";
            DirectoryEntry container = null;

            try
            {
                using (DirectoryEntry rootDSE = new DirectoryEntry(
                           ADLdapProviderPrefix + machineDomain + ADRootDSE,
                           null, null, ADS_READONLY_SERVER))
                {
                    configurationNC = Convert.ToString(
                        rootDSE.Properties[ADConfigurationNC].Value, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            try
            {
                container = new DirectoryEntry(
                    ADLdapProviderPrefix + machineDomain + ADActObjContainer + configurationNC,
                    null, null, ADS_READONLY_SERVER);

                // 触发绑定，以便捕获「对象不存在」
                string ignored = container.SchemaClassName;
                GC.KeepAlive(ignored);
            }
            catch (Exception ex)
            {
                if (GetHResult(ex) == HR_ERROR_DS_NO_SUCH_OBJECT)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgADSchemaNotSupported);
                    return;
                }

                Quit(ex);
            }

            using (container)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgActObjAvailable);

                bool found = false;

                foreach (DirectoryEntry child in container.Children)
                {
                    using (child)
                    {
                        if (!string.Equals(child.SchemaClassName, ADActObjClass, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        found = true;

                        LineOut("    " + SofwareLicenseManagerMessages.L_MsgADInfoAOName + GetAdString(child, ADActObjDisplayName));
                        LineOut("    " + "    " + SofwareLicenseManagerMessages.L_MsgActID +
                                GuidToString(GetAdBytes(child, ADActObjAttribSkuId)));
                        LineOut("    " + "    " + SofwareLicenseManagerMessages.L_MsgPartialPKey +
                                GetAdString(child, ADActObjAttribPartialPkey));
                        LineOut("    " + "    " + SofwareLicenseManagerMessages.L_MsgADInfoExtendedPid +
                                GetAdString(child, ADActObjAttribPid));
                        LineOut("    " + "    " + SofwareLicenseManagerMessages.L_MsgADInfoAODN +
                                GetAdString(child, ADActObjAttribDN));
                        LineOut("");
                    }
                }

                if (!found)
                {
                    LineOut("    " + SofwareLicenseManagerMessages.L_MsgActObjNoneFound);
                }
            }
        }

        // =====================================================================
        // /del-ao
        // =====================================================================
        internal void ADDeleteActivationObjects(string strName)
        {
            FailRemoteExec();

            string machineDomain = GetMachineDomain();

            string configurationNC = "";

            try
            {
                using (DirectoryEntry rootDSE = new DirectoryEntry(
                           ADLdapProviderPrefix + machineDomain + ADRootDSE))
                {
                    configurationNC = Convert.ToString(
                        rootDSE.Properties[ADConfigurationNC].Value, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            //
            // 检查 AD 架构是否支持激活对象容器
            //
            try
            {
                using (DirectoryEntry container = new DirectoryEntry(
                           ADLdapProviderPrefix + machineDomain + ADActObjContainer + configurationNC,
                           null, null, ADS_READONLY_SERVER))
                {
                    string ignored = container.SchemaClassName;
                    GC.KeepAlive(ignored);
                }
            }
            catch (Exception ex)
            {
                if (GetHResult(ex) == HR_ERROR_DS_NO_SUCH_OBJECT)
                {
                    LineOut(SofwareLicenseManagerMessages.L_MsgADSchemaNotSupported);
                    return;
                }

                Quit(ex);
            }

            string strDN;

            if (strName.IndexOf(",cn=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                strDN = strName;
            }
            else
            {
                // 提供的是 RDN，用计算机所属域补成完整 DN
                if (strName.StartsWith("cn=", StringComparison.OrdinalIgnoreCase))
                {
                    strDN = strName + "," + ADActObjContainer + configurationNC;
                }
                else
                {
                    strDN = "CN=" + strName + "," + ADActObjContainer + configurationNC;
                }

                LineOut("    " + SofwareLicenseManagerMessages.L_MsgADInfoAODN + strDN);
                LineOut("");
            }

            try
            {
                using (DirectoryEntry target = new DirectoryEntry(ADLdapProviderPrefix + strDN))
                {
                    string schemaClass = target.SchemaClassName;

                    using (DirectoryEntry parent = target.Parent)
                    {
                        if (string.Equals(schemaClass, ADActObjClass, StringComparison.OrdinalIgnoreCase))
                        {
                            parent.Children.Remove(target);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgSucess);
        }

        // =====================================================================
        // 辅助
        // =====================================================================

        /// <summary>对应 GetMachineDomain：返回「域 DNS 名 + /」。</summary>
        private string GetMachineDomain()
        {
            string machineDomain = "";

            try
            {
                Type t = Type.GetTypeFromProgID("ADSystemInfo", true);
                object adSystemInfo = Activator.CreateInstance(t);

                object value = t.InvokeMember("DomainDNSName",
                                              System.Reflection.BindingFlags.GetProperty,
                                              null, adSystemInfo, null);

                machineDomain = Convert.ToString(value, CultureInfo.InvariantCulture) + "/";
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            return machineDomain;
        }

        private static string GetAdString(DirectoryEntry entry, string attribute)
        {
            try
            {
                object v = entry.Properties[attribute].Value;
                return v == null ? "" : Convert.ToString(v, CultureInfo.InvariantCulture);
            }
            catch
            {
                return "";
            }
        }

        private static byte[] GetAdBytes(DirectoryEntry entry, string attribute)
        {
            try
            {
                return entry.Properties[attribute].Value as byte[];
            }
            catch
            {
                return null;
            }
        }
    }
}
