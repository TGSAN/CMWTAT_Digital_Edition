//
// SofwareLicenseManager.cs -- slmgr.vbs 的 C# 复刻：命令行解析、用法说明与对外入口。
//
// 用法（进程内调用，替代 cscript slmgr.vbs）：
//     string output = CMWTAT_DIGITAL.LibSofwareLicenseManager.SofwareLicenseManager.Run("-upk");
//     string output = CMWTAT_DIGITAL.LibSofwareLicenseManager.SofwareLicenseManager.Run("-ipk " + key);
//
// 输出文本与 slmgr.vbs 在「找不到本地化资源」时的内建英文输出保持一致，
// 因此依赖 EndsWith("successfully.") 之类判断的调用方无需修改。
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CMWTAT_DIGITAL.LibSofwareLicenseManager
{
    /// <summary>一次 slmgr 调用的结果。</summary>
    public sealed class SofwareLicenseManagerResult
    {
        private readonly string _output;
        private readonly int _exitCode;

        internal SofwareLicenseManagerResult(string output, int exitCode)
        {
            _output = output == null ? "" : output;
            _exitCode = exitCode;
        }

        /// <summary>完整输出文本（含换行）。</summary>
        public string Output
        {
            get { return _output; }
        }

        /// <summary>退出码。0 表示成功。</summary>
        public int ExitCode
        {
            get { return _exitCode; }
        }

        public override string ToString()
        {
            return _output;
        }
    }

    /// <summary>slmgr.vbs 的 C# 实现入口。</summary>
    public static class SofwareLicenseManager
    {
        internal const int IntKnownOption = 0;
        internal const int IntUnknownOption = 1;

        /// <summary>
        /// 以单个命令行字符串执行（形如 "-ipk XXXXX-XXXXX-..."），返回去除首尾空白的输出。
        /// 这是 cscript //Nologo slmgr.vbs [参数] 的进程内等价物。
        /// </summary>
        public static string Run(string commandLine)
        {
            return Execute(SplitCommandLine(commandLine)).Output.Trim();
        }

        /// <summary>以参数数组执行，返回完整结果。</summary>
        public static SofwareLicenseManagerResult Execute(params string[] args)
        {
            if (args == null)
            {
                args = new string[0];
            }

            using (SofwareLicenseManagerSession session = new SofwareLicenseManagerSession())
            {
                int exitCode = 0;

                try
                {
                    session.ExecCommandLine(args);
                    session.FlushPending();
                }
                catch (SofwareLicenseManagerQuitException quit)
                {
                    exitCode = quit.ExitCode;
                }

                return new SofwareLicenseManagerResult(session.GetOutput(), exitCode);
            }
        }

        /// <summary>按 Windows 命令行规则（引号包裹）拆分参数。</summary>
        internal static string[] SplitCommandLine(string commandLine)
        {
            List<string> args = new List<string>();

            if (string.IsNullOrEmpty(commandLine))
            {
                return args.ToArray();
            }

            StringBuilder current = new StringBuilder();
            bool inQuotes = false;
            bool hasToken = false;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    hasToken = true;
                    continue;
                }

                if (!inQuotes && (c == ' ' || c == '\t'))
                {
                    if (hasToken)
                    {
                        args.Add(current.ToString());
                        current.Length = 0;
                        hasToken = false;
                    }
                    continue;
                }

                current.Append(c);
                hasToken = true;
            }

            if (hasToken)
            {
                args.Add(current.ToString());
            }

            return args.ToArray();
        }
    }

    internal sealed partial class SofwareLicenseManagerSession
    {
        private string[] _args = new string[0];

        private int ArgCount
        {
            get { return _args.Length; }
        }

        private string Arg(int index)
        {
            return (index >= 0 && index < _args.Length) ? _args[index] : "";
        }

        // =====================================================================
        // 对应 ExecCommandLine
        // =====================================================================
        internal void ExecCommandLine(string[] args)
        {
            _args = args == null ? new string[0] : args;

            string[] remoteInfo = new string[4];
            remoteInfo[0] = ".";
            remoteInfo[1] = null;
            remoteInfo[2] = null;
            remoteInfo[3] = null;

            int intOption = SofwareLicenseManager.IntUnknownOption;
            int indexOption;

            //
            // "/" 或 "-" 之前的前三个参数可能是远程连接信息
            //
            for (indexOption = 0; indexOption <= 3; indexOption++)
            {
                if (indexOption >= ArgCount)
                {
                    break;
                }

                string strOption = Arg(indexOption);
                char chOpt = strOption.Length > 0 ? strOption[0] : '\0';

                if (chOpt == '/' || chOpt == '-')
                {
                    intOption = SofwareLicenseManager.IntKnownOption;
                    break;
                }

                remoteInfo[indexOption] = strOption;
            }

            //
            // 只有语法基本正确时才连接远程
            //
            if (intOption == SofwareLicenseManager.IntUnknownOption || indexOption == 2)
            {
                Connect(".", null, null);
                intOption = SofwareLicenseManager.IntUnknownOption;
            }
            else
            {
                Connect(remoteInfo[0], remoteInfo[1], remoteInfo[2]);
            }

            if (intOption == SofwareLicenseManager.IntUnknownOption)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgInvalidOptions);
                LineOut("");
                DisplayUsage();
            }

            intOption = ParseCommandLine(indexOption);

            if (intOption == SofwareLicenseManager.IntUnknownOption)
            {
                LineOut(SofwareLicenseManagerMessages.L_MsgUnrecognizedOption + Arg(indexOption));
                LineOut("");
                DisplayUsage();
            }
        }

        // =====================================================================
        // 对应 HandleOptionParam
        // =====================================================================
        private bool HandleOptionParam(int cParam, bool mustProvide, string opt, string param)
        {
            if (ArgCount > cParam)
            {
                return true;
            }

            if (mustProvide)
            {
                LineOut("");
                LineOut(SofwareLicenseManagerMessages.L_MsgErrorText_9
                        .Replace("%OPTION%", opt)
                        .Replace("%PARAM%", param));
                DisplayUsage();
            }

            return false;
        }

        // =====================================================================
        // 对应 ParseCommandLine
        // =====================================================================
        private int ParseCommandLine(int index)
        {
            string strOption = Arg(index).ToLowerInvariant();

            char chOpt = strOption.Length > 0 ? strOption[0] : '\0';

            if (chOpt != '-' && chOpt != '/')
            {
                return SofwareLicenseManager.IntUnknownOption;
            }

            strOption = strOption.Substring(1);

            if (strOption == SofwareLicenseManagerMessages.L_optInstallLicense)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optInstallLicense,
                                      SofwareLicenseManagerMessages.L_ParamsLicenseFile))
                {
                    InstallLicense(Arg(index + 1));
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optInstallProductKey)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optInstallProductKey,
                                      SofwareLicenseManagerMessages.L_ParamsProductKey))
                {
                    InstallProductKey(Arg(index + 1));
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optUninstallProductKey)
            {
                if (HandleOptionParam(index + 1, false, SofwareLicenseManagerMessages.L_optUninstallProductKey,
                                      SofwareLicenseManagerMessages.L_ParamsActivationIDOptional))
                {
                    UninstallProductKey(Arg(index + 1));
                }
                else
                {
                    UninstallProductKey("");
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optDisplayIID)
            {
                if (HandleOptionParam(index + 1, false, SofwareLicenseManagerMessages.L_optDisplayIID,
                                      SofwareLicenseManagerMessages.L_ParamsActivationIDOptional))
                {
                    DisplayIID(Arg(index + 1));
                }
                else
                {
                    DisplayIID("");
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optActivateProduct)
            {
                if (HandleOptionParam(index + 1, false, SofwareLicenseManagerMessages.L_optActivateProduct,
                                      SofwareLicenseManagerMessages.L_ParamsActivationIDOptional))
                {
                    ActivateProduct(Arg(index + 1));
                }
                else
                {
                    ActivateProduct("");
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optPhoneActivateProduct)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optPhoneActivateProduct,
                                      SofwareLicenseManagerMessages.L_ParamsPhoneActivate))
                {
                    if (HandleOptionParam(index + 2, false, SofwareLicenseManagerMessages.L_optPhoneActivateProduct,
                                          SofwareLicenseManagerMessages.L_ParamsActivationIDOptional))
                    {
                        PhoneActivateProduct(Arg(index + 1), Arg(index + 2));
                    }
                    else
                    {
                        PhoneActivateProduct(Arg(index + 1), "");
                    }
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optDisplayInformation)
            {
                if (HandleOptionParam(index + 1, false, SofwareLicenseManagerMessages.L_optDisplayInformation, ""))
                {
                    DisplayAllInformation(Arg(index + 1), false);
                }
                else
                {
                    DisplayAllInformation("", false);
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optDisplayInformationVerbose)
            {
                if (HandleOptionParam(index + 1, false, SofwareLicenseManagerMessages.L_optDisplayInformationVerbose, ""))
                {
                    DisplayAllInformation(Arg(index + 1), true);
                }
                else
                {
                    DisplayAllInformation("", true);
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optClearPKeyFromRegistry)
            {
                ClearPKeyFromRegistry();
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optReinstallLicenses)
            {
                ReinstallLicenses();
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optReArmWindows)
            {
                ReArmWindows();
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optReArmApplication)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optReArmApplication,
                                      SofwareLicenseManagerMessages.L_ParamsApplicationID))
                {
                    ReArmApp(Arg(index + 1));
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optReArmSku)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optReArmSku,
                                      SofwareLicenseManagerMessages.L_ParamsActivationID))
                {
                    ReArmSku(Arg(index + 1));
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optExpirationDatime)
            {
                if (HandleOptionParam(index + 1, false, SofwareLicenseManagerMessages.L_optExpirationDatime,
                                      SofwareLicenseManagerMessages.L_ParamsActivationIDOptional))
                {
                    ExpirationDatime(Arg(index + 1));
                }
                else
                {
                    ExpirationDatime("");
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optSetKmsName)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optSetKmsName,
                                      SofwareLicenseManagerMessages.L_ParamsSetKms))
                {
                    if (HandleOptionParam(index + 2, false, SofwareLicenseManagerMessages.L_optSetKmsName,
                                          SofwareLicenseManagerMessages.L_ParamsActivationIDOptional))
                    {
                        SetKmsMachineName(Arg(index + 1), Arg(index + 2));
                    }
                    else
                    {
                        SetKmsMachineName(Arg(index + 1), "");
                    }
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optClearKmsName)
            {
                if (HandleOptionParam(index + 1, false, SofwareLicenseManagerMessages.L_optClearKmsName,
                                      SofwareLicenseManagerMessages.L_ParamsActivationIDOptional))
                {
                    ClearKms(Arg(index + 1));
                }
                else
                {
                    ClearKms("");
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optSetKmsLookupDomain)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optSetKmsLookupDomain,
                                      SofwareLicenseManagerMessages.L_ParamsSetKmsLookupDomain))
                {
                    if (HandleOptionParam(index + 2, false, SofwareLicenseManagerMessages.L_optSetKmsLookupDomain,
                                          SofwareLicenseManagerMessages.L_ParamsActivationIDOptional))
                    {
                        SetKmsLookupDomain(Arg(index + 1), Arg(index + 2));
                    }
                    else
                    {
                        SetKmsLookupDomain(Arg(index + 1), "");
                    }
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optClearKmsLookupDomain)
            {
                if (HandleOptionParam(index + 1, false, SofwareLicenseManagerMessages.L_optClearKmsLookupDomain,
                                      SofwareLicenseManagerMessages.L_ParamsActivationIDOptional))
                {
                    ClearKmsLookupDomain(Arg(index + 1));
                }
                else
                {
                    ClearKmsLookupDomain("");
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optSetKmsHostCaching)
            {
                SetHostCachingDisable(false);
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optClearKmsHostCaching)
            {
                SetHostCachingDisable(true);
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optSetActivationInterval)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optSetActivationInterval,
                                      SofwareLicenseManagerMessages.L_ParamsSetActivationInterval))
                {
                    SetActivationInterval(Arg(index + 1));
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optSetRenewalInterval)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optSetRenewalInterval,
                                      SofwareLicenseManagerMessages.L_ParamsSetRenewalInterval))
                {
                    SetRenewalInterval(Arg(index + 1));
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optSetKmsListenPort)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optSetKmsListenPort,
                                      SofwareLicenseManagerMessages.L_ParamsSetListenKmsPort))
                {
                    SetKmsListenPort(Arg(index + 1));
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optSetDNS)
            {
                SetDnsPublishingDisabled(false);
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optClearDNS)
            {
                SetDnsPublishingDisabled(true);
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optSetNormalPriority)
            {
                SetKmsLowPriority(false);
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optClearNormalPriority)
            {
                SetKmsLowPriority(true);
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optSetVLActivationType)
            {
                if (HandleOptionParam(index + 1, false, SofwareLicenseManagerMessages.L_optSetVLActivationType,
                                      SofwareLicenseManagerMessages.L_ParamsVLActivationTypeOptional))
                {
                    if (HandleOptionParam(index + 2, false, SofwareLicenseManagerMessages.L_optSetVLActivationType,
                                          SofwareLicenseManagerMessages.L_ParamsActivationIDOptional))
                    {
                        SetVLActivationType(Arg(index + 1), Arg(index + 2));
                    }
                    else
                    {
                        SetVLActivationType(Arg(index + 1), "");
                    }
                }
                else
                {
                    SetVLActivationType(null, "");
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optListInstalledILs)
            {
                TkaListILs();
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optRemoveInstalledIL)
            {
                if (HandleOptionParam(index + 2, true, SofwareLicenseManagerMessages.L_optRemoveInstalledIL,
                                      SofwareLicenseManagerMessages.L_ParamsRemoveInstalledIL))
                {
                    TkaRemoveIL(Arg(index + 1), Arg(index + 2));
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optListTkaCerts)
            {
                TkaListCerts();
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optForceTkaActivation)
            {
                if (HandleOptionParam(index + 2, false, SofwareLicenseManagerMessages.L_optForceTkaActivation,
                                      SofwareLicenseManagerMessages.L_ParamsForceTkaActivation))
                {
                    TkaActivate(Arg(index + 1), Arg(index + 2));
                }
                else if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optForceTkaActivation,
                                           SofwareLicenseManagerMessages.L_ParamsForceTkaActivation))
                {
                    TkaActivate(Arg(index + 1), "");
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optADGetIID)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optADGetIID,
                                      SofwareLicenseManagerMessages.L_ParamsProductKey))
                {
                    ADGetIID(Arg(index + 1));
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optADActivate)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optADActivate,
                                      SofwareLicenseManagerMessages.L_ParamsProductKey))
                {
                    if (HandleOptionParam(index + 2, false, SofwareLicenseManagerMessages.L_optADActivate,
                                          SofwareLicenseManagerMessages.L_ParamsAONameOptional))
                    {
                        ADActivateOnline(Arg(index + 1), Arg(index + 2));
                    }
                    else
                    {
                        ADActivateOnline(Arg(index + 1), "");
                    }
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optADApplyCID)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optADApplyCID,
                                      SofwareLicenseManagerMessages.L_ParamsProductKey))
                {
                    if (HandleOptionParam(index + 2, true, SofwareLicenseManagerMessages.L_optADApplyCID,
                                          SofwareLicenseManagerMessages.L_ParamsPhoneActivate))
                    {
                        if (HandleOptionParam(index + 3, false, SofwareLicenseManagerMessages.L_optADApplyCID,
                                              SofwareLicenseManagerMessages.L_ParamsAONameOptional))
                        {
                            ADActivatePhone(Arg(index + 1), Arg(index + 2), Arg(index + 3));
                        }
                        else
                        {
                            ADActivatePhone(Arg(index + 1), Arg(index + 2), "");
                        }
                    }
                }
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optADListAOs)
            {
                ADListActivationObjects();
            }
            else if (strOption == SofwareLicenseManagerMessages.L_optADDeleteAO)
            {
                if (HandleOptionParam(index + 1, true, SofwareLicenseManagerMessages.L_optADDeleteAO,
                                      SofwareLicenseManagerMessages.L_ParamsAODistinguishedName))
                {
                    ADDeleteActivationObjects(Arg(index + 1));
                }
            }
            else
            {
                return SofwareLicenseManager.IntUnknownOption;
            }

            return SofwareLicenseManager.IntKnownOption;
        }

        // =====================================================================
        // 对应 DisplayUsage / OptLine / OptLine2 / OptLine3
        // =====================================================================
        private void OptLine(string strOption, string strParams, string strUsage)
        {
            LineOut("/" + strOption + " " + strParams);
            LineOut("    " + strUsage);
        }

        private void OptLine2(string strOption, string strParam1, string strParam2, string strUsage)
        {
            LineOut("/" + strOption + " " + strParam1 + " " + strParam2);
            LineOut("    " + strUsage);
        }

        private void OptLine3(string strOption, string strParam1, string strParam2, string strParam3,
                              string strUsage)
        {
            LineOut("/" + strOption + " " + strParam1 + " " + strParam2 + " " + strParam3);
            LineOut("    " + strUsage);
        }

        private void DisplayUsage()
        {
            LineOut(SofwareLicenseManagerMessages.L_MsgHelp_1);
            LineOut(SofwareLicenseManagerMessages.L_MsgHelp_2);
            LineOut("           " + SofwareLicenseManagerMessages.L_MsgHelp_3);
            LineOut("           " + SofwareLicenseManagerMessages.L_MsgHelp_4);
            LineOut("           " + SofwareLicenseManagerMessages.L_MsgHelp_5);
            LineOut("");

            LineOut(SofwareLicenseManagerMessages.L_MsgGlobalOptions);
            OptLine(SofwareLicenseManagerMessages.L_optInstallProductKey, SofwareLicenseManagerMessages.L_ParamsProductKey,
                    SofwareLicenseManagerMessages.L_optInstallProductKeyUsage);
            OptLine(SofwareLicenseManagerMessages.L_optActivateProduct, SofwareLicenseManagerMessages.L_ParamsActivationIDOptional,
                    SofwareLicenseManagerMessages.L_optActivateProductUsage);
            OptLine(SofwareLicenseManagerMessages.L_optDisplayInformation, SofwareLicenseManagerMessages.L_ParamsActIDOptional,
                    SofwareLicenseManagerMessages.L_optDisplayInformationUsage);
            OptLine(SofwareLicenseManagerMessages.L_optDisplayInformationVerbose, SofwareLicenseManagerMessages.L_ParamsActIDOptional,
                    SofwareLicenseManagerMessages.L_optDisplayInformationUsageVerbose);
            OptLine(SofwareLicenseManagerMessages.L_optExpirationDatime, SofwareLicenseManagerMessages.L_ParamsActivationIDOptional,
                    SofwareLicenseManagerMessages.L_optExpirationDatimeUsage);

            LineFlush("");

            LineOut(SofwareLicenseManagerMessages.L_MsgAdvancedOptions);
            OptLine(SofwareLicenseManagerMessages.L_optClearPKeyFromRegistry, "",
                    SofwareLicenseManagerMessages.L_optClearPKeyFromRegistryUsage);
            OptLine(SofwareLicenseManagerMessages.L_optInstallLicense, SofwareLicenseManagerMessages.L_ParamsLicenseFile,
                    SofwareLicenseManagerMessages.L_optInstallLicenseUsage);
            OptLine(SofwareLicenseManagerMessages.L_optReinstallLicenses, "",
                    SofwareLicenseManagerMessages.L_optReinstallLicensesUsage);
            OptLine(SofwareLicenseManagerMessages.L_optReArmWindows, "",
                    SofwareLicenseManagerMessages.L_optReArmWindowsUsage);
            OptLine(SofwareLicenseManagerMessages.L_optReArmApplication, SofwareLicenseManagerMessages.L_ParamsApplicationID,
                    SofwareLicenseManagerMessages.L_optReArmApplicationUsage);
            OptLine(SofwareLicenseManagerMessages.L_optReArmSku, SofwareLicenseManagerMessages.L_ParamsActivationID,
                    SofwareLicenseManagerMessages.L_optReArmSkuUsage);
            OptLine(SofwareLicenseManagerMessages.L_optUninstallProductKey, SofwareLicenseManagerMessages.L_ParamsActivationIDOptional,
                    SofwareLicenseManagerMessages.L_optUninstallProductKeyUsage);

            LineOut("");
            OptLine(SofwareLicenseManagerMessages.L_optDisplayIID, SofwareLicenseManagerMessages.L_ParamsActivationIDOptional,
                    SofwareLicenseManagerMessages.L_optDisplayIIDUsage);
            OptLine2(SofwareLicenseManagerMessages.L_optPhoneActivateProduct, SofwareLicenseManagerMessages.L_ParamsPhoneActivate,
                     SofwareLicenseManagerMessages.L_ParamsActivationIDOptional,
                     SofwareLicenseManagerMessages.L_optPhoneActivateProductUsage);

            LineOut("");
            LineOut(SofwareLicenseManagerMessages.L_MsgKmsClientOptions);
            OptLine2(SofwareLicenseManagerMessages.L_optSetKmsName, SofwareLicenseManagerMessages.L_ParamsSetKms,
                     SofwareLicenseManagerMessages.L_ParamsActivationIDOptional, SofwareLicenseManagerMessages.L_optSetKmsNameUsage);
            OptLine(SofwareLicenseManagerMessages.L_optClearKmsName, SofwareLicenseManagerMessages.L_ParamsActivationIDOptional,
                    SofwareLicenseManagerMessages.L_optClearKmsNameUsage);
            OptLine2(SofwareLicenseManagerMessages.L_optSetKmsLookupDomain, SofwareLicenseManagerMessages.L_ParamsSetKmsLookupDomain,
                     SofwareLicenseManagerMessages.L_ParamsActivationIDOptional, SofwareLicenseManagerMessages.L_optSetKmsLookupDomainUsage);
            OptLine(SofwareLicenseManagerMessages.L_optClearKmsLookupDomain, SofwareLicenseManagerMessages.L_ParamsActivationIDOptional,
                    SofwareLicenseManagerMessages.L_optClearKmsLookupDomainUsage);
            OptLine(SofwareLicenseManagerMessages.L_optSetKmsHostCaching, "", SofwareLicenseManagerMessages.L_optSetKmsHostCachingUsage);
            OptLine(SofwareLicenseManagerMessages.L_optClearKmsHostCaching, "", SofwareLicenseManagerMessages.L_optClearKmsHostCachingUsage);

            LineFlush("");

            LineOut(SofwareLicenseManagerMessages.L_MsgTkaClientOptions);
            OptLine(SofwareLicenseManagerMessages.L_optListInstalledILs, "", SofwareLicenseManagerMessages.L_optListInstalledILsUsage);
            OptLine(SofwareLicenseManagerMessages.L_optRemoveInstalledIL, SofwareLicenseManagerMessages.L_ParamsRemoveInstalledIL,
                    SofwareLicenseManagerMessages.L_optRemoveInstalledILUsage);
            OptLine(SofwareLicenseManagerMessages.L_optListTkaCerts, "", SofwareLicenseManagerMessages.L_optListTkaCertsUsage);
            OptLine(SofwareLicenseManagerMessages.L_optForceTkaActivation, SofwareLicenseManagerMessages.L_ParamsForceTkaActivation,
                    SofwareLicenseManagerMessages.L_optForceTkaActivationUsage);

            LineFlush("");

            LineOut(SofwareLicenseManagerMessages.L_MsgKmsOptions);
            OptLine(SofwareLicenseManagerMessages.L_optSetKmsListenPort, SofwareLicenseManagerMessages.L_ParamsSetListenKmsPort,
                    SofwareLicenseManagerMessages.L_optSetKmsListenPortUsage);
            OptLine(SofwareLicenseManagerMessages.L_optSetActivationInterval, SofwareLicenseManagerMessages.L_ParamsSetActivationInterval,
                    SofwareLicenseManagerMessages.L_optSetActivationIntervalUsage);
            OptLine(SofwareLicenseManagerMessages.L_optSetRenewalInterval, SofwareLicenseManagerMessages.L_ParamsSetRenewalInterval,
                    SofwareLicenseManagerMessages.L_optSetRenewalIntervalUsage);
            OptLine(SofwareLicenseManagerMessages.L_optSetDNS, "", SofwareLicenseManagerMessages.L_optSetDNSUsage);
            OptLine(SofwareLicenseManagerMessages.L_optClearDNS, "", SofwareLicenseManagerMessages.L_optClearDNSUsage);
            OptLine(SofwareLicenseManagerMessages.L_optSetNormalPriority, "", SofwareLicenseManagerMessages.L_optSetNormalPriorityUsage);
            OptLine(SofwareLicenseManagerMessages.L_optClearNormalPriority, "", SofwareLicenseManagerMessages.L_optClearNormalPriorityUsage);
            OptLine2(SofwareLicenseManagerMessages.L_optSetVLActivationType, SofwareLicenseManagerMessages.L_ParamsVLActivationTypeOptional,
                     SofwareLicenseManagerMessages.L_ParamsActivationIDOptional, SofwareLicenseManagerMessages.L_optSetVLActivationTypeUsage);

            LineFlush("");

            LineOut(SofwareLicenseManagerMessages.L_MsgADOptions);
            OptLine2(SofwareLicenseManagerMessages.L_optADActivate, SofwareLicenseManagerMessages.L_ParamsProductKey,
                     SofwareLicenseManagerMessages.L_ParamsAONameOptional, SofwareLicenseManagerMessages.L_optADActivateUsage);
            OptLine(SofwareLicenseManagerMessages.L_optADGetIID, SofwareLicenseManagerMessages.L_ParamsProductKey,
                    SofwareLicenseManagerMessages.L_optADGetIIDUsage);
            OptLine3(SofwareLicenseManagerMessages.L_optADApplyCID, SofwareLicenseManagerMessages.L_ParamsProductKey,
                     SofwareLicenseManagerMessages.L_ParamsPhoneActivate, SofwareLicenseManagerMessages.L_ParamsAONameOptional,
                     SofwareLicenseManagerMessages.L_optADApplyCIDUsage);
            OptLine(SofwareLicenseManagerMessages.L_optADListAOs, "", SofwareLicenseManagerMessages.L_optADListAOsUsage);
            OptLine(SofwareLicenseManagerMessages.L_optADDeleteAO, SofwareLicenseManagerMessages.L_ParamsAODistinguishedName,
                    SofwareLicenseManagerMessages.L_optADDeleteAOsUsage);

            ExitScript(1);
        }
    }
}
