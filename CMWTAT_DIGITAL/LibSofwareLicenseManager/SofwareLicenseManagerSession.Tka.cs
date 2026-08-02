//
// SofwareLicenseManagerSession.Tka.cs -- slmgr.vbs 的 C# 复刻：基于令牌的激活（Token-based Activation）。
//
// 对应：/lil /ril /ltc /fta
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.Reflection;

namespace CMWTAT_DIGITAL.LibSofwareLicenseManager
{
    internal sealed partial class SofwareLicenseManagerSession
    {
        /// <summary>对应 IsTokenActivated。</summary>
        internal static bool IsTokenActivated(ManagementObject objProduct)
        {
            if (!HasValue(objProduct, "TokenActivationILVID"))
            {
                return false;
            }

            return PropUInt(objProduct, "TokenActivationILVID") != 0xFFFFFFFF;
        }

        // =====================================================================
        // /lil -- 列出已安装的令牌激活颁发许可证
        // =====================================================================
        internal void TkaListILs()
        {
            LineOut(SofwareLicenseManagerMessages.L_MsgTkaLicenses);
            LineOut("");

            int nListed = 0;

            foreach (ManagementObject objLicense in GetTokenActivationLicenses())
            {
                string strHeader = SofwareLicenseManagerMessages.L_MsgTkaLicenseHeader
                    .Replace("%ILID%", PropString(objLicense, "ILID"))
                    .Replace("%ILVID%", PropString(objLicense, "ILVID"));
                LineOut(strHeader);

                LineOut("    " + SofwareLicenseManagerMessages.L_MsgTkaLicenseILID
                        .Replace("%ILID%", PropString(objLicense, "ILID")));
                LineOut("    " + SofwareLicenseManagerMessages.L_MsgTkaLicenseILVID
                        .Replace("%ILVID%", PropString(objLicense, "ILVID")));

                DateTime expiration;
                if (TryGetWmiDate(objLicense, "ExpirationDate", out expiration))
                {
                    LineOut("    " + SofwareLicenseManagerMessages.L_MsgTkaLicenseExpiration
                            .Replace("%TODATE%", VbDateToString(expiration)));
                }

                if (HasValue(objLicense, "AdditionalInfo"))
                {
                    LineOut("    " + SofwareLicenseManagerMessages.L_MsgTkaLicenseAdditionalInfo
                            .Replace("%MOREINFO%", PropString(objLicense, "AdditionalInfo")));
                }

                if (HasValue(objLicense, "AuthorizationStatus")
                    && PropUInt(objLicense, "AuthorizationStatus") != 0)
                {
                    string strError = VbHex(unchecked((int)PropUInt(objLicense, "AuthorizationStatus")));
                    LineOut("    " + SofwareLicenseManagerMessages.L_MsgTkaLicenseAuthZStatus.Replace("%ERRCODE%", strError));
                }
                else
                {
                    LineOut("    " + SofwareLicenseManagerMessages.L_MsgTkaLicenseDescr
                            .Replace("%DESC%", PropString(objLicense, "Description")));
                }

                LineOut("");
                nListed++;
            }

            if (nListed == 0)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgTkaLicenseNone);
            }
        }

        // =====================================================================
        // /ril -- 删除指定的令牌激活颁发许可证
        // =====================================================================
        internal void TkaRemoveIL(string strILID, string strILVID)
        {
            long nILVID = 0;

            try
            {
                nILVID = long.Parse((strILVID == null ? "" : strILVID).Trim(),
                                    NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            LineOut(SofwareLicenseManagerMessages.L_MsgTkaRemoving);
            LineOut("");

            int nRemoved = 0;

            foreach (ManagementObject objLicense in GetTokenActivationLicenses())
            {
                if (!string.Equals(strILID, PropString(objLicense, "ILID"), StringComparison.Ordinal))
                {
                    continue;
                }

                if (nILVID != (long)PropUInt(objLicense, "ILVID"))
                {
                    continue;
                }

                string strMsg = SofwareLicenseManagerMessages.L_MsgTkaRemovedItem
                    .Replace("%SLID%", PropString(objLicense, "ID"));

                try
                {
                    Invoke(objLicense, "Uninstall");
                }
                catch (Exception ex)
                {
                    Quit(ex);
                }

                LineOut(strMsg);
                nRemoved++;
            }

            if (nRemoved == 0)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgTkaRemovedNone);
            }
        }

        // =====================================================================
        // /ltc -- 列出可用于令牌激活的证书
        // =====================================================================
        internal void TkaListCerts()
        {
            object objSigner = TkaGetSigner();
            ManagementObject objProduct = TkaGetProduct();

            object arrGrants = null;

            try
            {
                ManagementBaseObject outParams = Invoke(objProduct, "GetTokenActivationGrants");
                arrGrants = GetOutParam(outParams, "Grants");
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            object arrThumbprints = null;

            try
            {
                arrThumbprints = InvokeCom(objSigner, "GetCertificateThumbprints", new object[] { arrGrants });
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            IEnumerable list = arrThumbprints as IEnumerable;
            if (list == null)
            {
                return;
            }

            foreach (object item in list)
            {
                TkaPrintCertificate(Convert.ToString(item, CultureInfo.InvariantCulture));
            }
        }

        // =====================================================================
        // /fta -- 使用指定证书强制进行令牌激活
        // =====================================================================
        internal void TkaActivate(string strThumbprint, string strPin)
        {
            object objSigner = TkaGetSigner();
            ManagementObject objProduct = TkaGetProduct();
            ManagementObject objService = TkaGetService();

            DisplayActivatingSku(objProduct);

            string strChallenge = null;

            try
            {
                ManagementBaseObject outParams = Invoke(objProduct, "GenerateTokenActivationChallenge");
                strChallenge = Convert.ToString(GetOutParam(outParams, "Challenge"), CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            string strAuthInfo1 = null;
            string strAuthInfo2 = null;

            try
            {
                object[] args = new object[] { strChallenge, strThumbprint, strPin, null };
                ParameterModifier byRef = new ParameterModifier(4);
                byRef[3] = true;

                object signed = objSigner.GetType().InvokeMember(
                    "Sign",
                    BindingFlags.InvokeMethod,
                    null,
                    objSigner,
                    args,
                    new ParameterModifier[] { byRef },
                    CultureInfo.InvariantCulture,
                    null);

                strAuthInfo1 = Convert.ToString(signed, CultureInfo.InvariantCulture);
                strAuthInfo2 = args[3] == null
                    ? null
                    : Convert.ToString(args[3], CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            try
            {
                Invoke(objProduct, "DepositTokenActivationResponse",
                       "Challenge", strChallenge,
                       "Response", strAuthInfo1,
                       "CertChain", strAuthInfo2);
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
                // 与原脚本一致：此处显式清空错误
            }

            Refresh(objProduct);
            DisplayActivatedStatus(objProduct);
        }

        // =====================================================================
        // 辅助
        // =====================================================================
        private ManagementObject TkaGetService()
        {
            return GetServiceObject("Version");
        }

        private ManagementObject TkaGetProduct()
        {
            return GetProductObject(
                "ID, Name, ApplicationId, PartialProductKey, Description, LicenseIsAddon ",
                "ApplicationId = '" + WindowsAppId + "' " +
                "AND PartialProductKey <> NULL " +
                "AND LicenseIsAddon = FALSE");
        }

        private object TkaGetSigner()
        {
            object signer = null;

            try
            {
                Type t = Type.GetTypeFromProgID("SPPWMI.SppWmiTokenActivationSigner", true);
                signer = Activator.CreateInstance(t);
            }
            catch (Exception ex)
            {
                Quit(ex);
            }

            return signer;
        }

        private static object InvokeCom(object target, string method, object[] args)
        {
            return target.GetType().InvokeMember(
                method,
                BindingFlags.InvokeMethod,
                null,
                target,
                args,
                null,
                CultureInfo.InvariantCulture,
                null);
        }

        private void TkaPrintCertificate(string strThumbprint)
        {
            string[] arrParams = (strThumbprint == null ? "" : strThumbprint).Split('|');

            LineOut("");
            LineOut(SofwareLicenseManagerMessages.L_MsgTkaCertThumbprint.Replace("%THUMBPRINT%", At(arrParams, 0)));
            LineOut(SofwareLicenseManagerMessages.L_MsgTkaCertSubject.Replace("%SUBJECT%", At(arrParams, 1)));
            LineOut(SofwareLicenseManagerMessages.L_MsgTkaCertIssuer.Replace("%ISSUER%", At(arrParams, 2)));
            LineOut(SofwareLicenseManagerMessages.L_MsgTkaCertValidFrom.Replace("%FROMDATE%", ShortDate(At(arrParams, 3))));
            LineOut(SofwareLicenseManagerMessages.L_MsgTkaCertValidTo.Replace("%TODATE%", ShortDate(At(arrParams, 4))));
        }

        private static string At(string[] arr, int index)
        {
            return (arr != null && index < arr.Length) ? arr[index] : "";
        }

        /// <summary>对应 FormatDateTime(CDate(x), vbShortDate)。</summary>
        private static string ShortDate(string value)
        {
            DateTime parsed;

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed.ToString("d", CultureInfo.CurrentCulture);
            }

            return value;
        }
    }
}
