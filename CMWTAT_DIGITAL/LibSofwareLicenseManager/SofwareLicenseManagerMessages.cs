//
// SofwareLicenseManagerMessages.cs -- slmgr.vbs 内置英文资源字符串的 C# 复刻。
//
// 本文件由 Res\slmgr.vbs 中的 `private const L_xxx = "..."` 定义直接提取生成，
// 保证与原脚本逐字节一致。
//
// 为什么用英文：
//   slmgr.vbs 的 GetResource() 优先从 <脚本目录>\slmgr\<语言ID>\slmgr.ini 读取本地化字符串，
//   找不到时回退到脚本内置的英文常量。本项目历来把 slmgr.vbs 复制到不包含
//   slmgr\<语言ID>\slmgr.ini 的临时目录再执行，因此实际拿到的始终是英文输出；
//   MainWindow 中的 EndsWith("successfully.") / Contains("activated") 等判断也因此成立。
//   本复刻保持同样的英文输出，以免破坏现有调用方的字符串匹配。
//

using System.Collections.Generic;

namespace CMWTAT_DIGITAL.LibSofwareLicenseManager
{
    /// <summary>
    /// slmgr.vbs 内置（英文）资源字符串。名称与原脚本一一对应。
    /// </summary>
    internal static class SofwareLicenseManagerMessages
    {
        internal const string L_optInstallProductKey = @"ipk";
        internal const string L_optInstallProductKeyUsage = @"Install product key (replaces existing key)";
        internal const string L_optUninstallProductKey = @"upk";
        internal const string L_optUninstallProductKeyUsage = @"Uninstall product key";
        internal const string L_optActivateProduct = @"ato";
        internal const string L_optActivateProductUsage = @"Activate Windows";
        internal const string L_optDisplayInformation = @"dli";
        internal const string L_optDisplayInformationUsage = @"Display license information (default: current license)";
        internal const string L_optDisplayInformationVerbose = @"dlv";
        internal const string L_optDisplayInformationUsageVerbose = @"Display detailed license information (default: current license)";
        internal const string L_optExpirationDatime = @"xpr";
        internal const string L_optExpirationDatimeUsage = @"Expiration date for current license state";
        internal const string L_optClearPKeyFromRegistry = @"cpky";
        internal const string L_optClearPKeyFromRegistryUsage = @"Clear product key from the registry (prevents disclosure attacks)";
        internal const string L_optInstallLicense = @"ilc";
        internal const string L_optInstallLicenseUsage = @"Install license";
        internal const string L_optReinstallLicenses = @"rilc";
        internal const string L_optReinstallLicensesUsage = @"Re-install system license files";
        internal const string L_optDisplayIID = @"dti";
        internal const string L_optDisplayIIDUsage = @"Display Installation ID for offline activation";
        internal const string L_optPhoneActivateProduct = @"atp";
        internal const string L_optPhoneActivateProductUsage = @"Activate product with user-provided Confirmation ID";
        internal const string L_optReArmWindows = @"rearm";
        internal const string L_optReArmWindowsUsage = @"Reset the licensing status of the machine";
        internal const string L_optReArmApplication = @"rearm-app";
        internal const string L_optReArmApplicationUsage = @"Reset the licensing status of the given app";
        internal const string L_optReArmSku = @"rearm-sku";
        internal const string L_optReArmSkuUsage = @"Reset the licensing status of the given sku";
        internal const string L_optSetKmsName = @"skms";
        internal const string L_optSetKmsNameUsage = @"Set the name and/or the port for the KMS computer this machine will use. IPv6 address must be specified in the format [hostname]:port";
        internal const string L_optClearKmsName = @"ckms";
        internal const string L_optClearKmsNameUsage = @"Clear name of KMS computer used (sets the port to the default)";
        internal const string L_optSetKmsLookupDomain = @"skms-domain";
        internal const string L_optSetKmsLookupDomainUsage = @"Set the specific DNS domain in which all KMS SRV records can be found. This setting has no effect if the specific single KMS host is set via /skms option.";
        internal const string L_optClearKmsLookupDomain = @"ckms-domain";
        internal const string L_optClearKmsLookupDomainUsage = @"Clear the specific DNS domain in which all KMS SRV records can be found. The specific KMS host will be used if set via /skms. Otherwise default KMS auto-discovery will be used.";
        internal const string L_optSetKmsHostCaching = @"skhc";
        internal const string L_optSetKmsHostCachingUsage = @"Enable KMS host caching";
        internal const string L_optClearKmsHostCaching = @"ckhc";
        internal const string L_optClearKmsHostCachingUsage = @"Disable KMS host caching";
        internal const string L_optSetActivationInterval = @"sai";
        internal const string L_optSetActivationIntervalUsage = @"Set interval (minutes) for unactivated clients to attempt KMS connection. The activation interval must be between 15 minutes (min) and 30 days (max) although the default (2 hours) is recommended.";
        internal const string L_optSetRenewalInterval = @"sri";
        internal const string L_optSetRenewalIntervalUsage = @"Set renewal interval (minutes) for activated clients to attempt KMS connection. The renewal interval must be between 15 minutes (min) and 30 days (max) although the default (7 days) is recommended.";
        internal const string L_optSetKmsListenPort = @"sprt";
        internal const string L_optSetKmsListenPortUsage = @"Set TCP port KMS will use to communicate with clients";
        internal const string L_optSetDNS = @"sdns";
        internal const string L_optSetDNSUsage = @"Enable DNS publishing by KMS (default)";
        internal const string L_optClearDNS = @"cdns";
        internal const string L_optClearDNSUsage = @"Disable DNS publishing by KMS";
        internal const string L_optSetNormalPriority = @"spri";
        internal const string L_optSetNormalPriorityUsage = @"Set KMS priority to normal (default)";
        internal const string L_optClearNormalPriority = @"cpri";
        internal const string L_optClearNormalPriorityUsage = @"Set KMS priority to low";
        internal const string L_optSetVLActivationType = @"act-type";
        internal const string L_optSetVLActivationTypeUsage = @"Set activation type to 1 (for AD) or 2 (for KMS) or 3 (for Token) or 0 (for all).";
        internal const string L_optListInstalledILs = @"lil";
        internal const string L_optListInstalledILsUsage = @"List installed Token-based Activation Issuance Licenses";
        internal const string L_optRemoveInstalledIL = @"ril";
        internal const string L_optRemoveInstalledILUsage = @"Remove installed Token-based Activation Issuance License";
        internal const string L_optListTkaCerts = @"ltc";
        internal const string L_optListTkaCertsUsage = @"List Token-based Activation Certificates";
        internal const string L_optForceTkaActivation = @"fta";
        internal const string L_optForceTkaActivationUsage = @"Force Token-based Activation";
        internal const string L_optADActivate = @"ad-activation-online";
        internal const string L_optADActivateUsage = @"Activate AD (Active Directory) forest with user-provided product key";
        internal const string L_optADGetIID = @"ad-activation-get-iid";
        internal const string L_optADGetIIDUsage = @"Display Installation ID for AD (Active Directory) forest";
        internal const string L_optADApplyCID = @"ad-activation-apply-cid";
        internal const string L_optADApplyCIDUsage = @"Activate AD (Active Directory) forest with user-provided product key and Confirmation ID";
        internal const string L_optADListAOs = @"ao-list";
        internal const string L_optADListAOsUsage = @"Display Activation Objects in AD (Active Directory)";
        internal const string L_optADDeleteAO = @"del-ao";
        internal const string L_optADDeleteAOsUsage = @"Delete Activation Objects in AD (Active Directory) for user-provided Activation Object";
        internal const string L_ParamsActivationID = @"<Activation ID>";
        internal const string L_ParamsActivationIDOptional = @"[Activation ID]";
        internal const string L_ParamsActIDOptional = @"[Activation ID | All]";
        internal const string L_ParamsApplicationID = @"<Application ID>";
        internal const string L_ParamsProductKey = @"<Product Key>";
        internal const string L_ParamsLicenseFile = @"<License file>";
        internal const string L_ParamsPhoneActivate = @"<Confirmation ID>";
        internal const string L_ParamsSetKms = @"<Name[:Port] | : port>";
        internal const string L_ParamsSetKmsLookupDomain = @"<FQDN>";
        internal const string L_ParamsSetListenKmsPort = @"<Port>";
        internal const string L_ParamsSetActivationInterval = @"<Activation Interval>";
        internal const string L_ParamsSetRenewalInterval = @"<Renewal Interval>";
        internal const string L_ParamsVLActivationTypeOptional = @"[Activation-Type]";
        internal const string L_ParamsRemoveInstalledIL = @"<ILID> <ILvID>";
        internal const string L_ParamsForceTkaActivation = @"<Certificate Thumbprint> [<PIN>]";
        internal const string L_ParamsAONameOptional = @"[Activation Object name]";
        internal const string L_ParamsAODistinguishedName = @"<Activation Object DN | Activation Object RDN>";
        internal const string L_MsgHelp_1 = @"Windows Software Licensing Management Tool";
        internal const string L_MsgHelp_2 = @"Usage: slmgr.vbs [MachineName [User Password]] [<Option>]";
        internal const string L_MsgHelp_3 = @"MachineName: Name of remote machine (default is local machine)";
        internal const string L_MsgHelp_4 = @"User:        Account with required privilege on remote machine";
        internal const string L_MsgHelp_5 = @"Password:    password for the previous account";
        internal const string L_MsgGlobalOptions = @"Global Options:";
        internal const string L_MsgAdvancedOptions = @"Advanced Options:";
        internal const string L_MsgKmsClientOptions = @"Volume Licensing: Key Management Service (KMS) Client Options:";
        internal const string L_MsgKmsOptions = @"Volume Licensing: Key Management Service (KMS) Options:";
        internal const string L_MsgADOptions = @"Volume Licensing: Active Directory (AD) Activation Options:";
        internal const string L_MsgTkaClientOptions = @"Volume Licensing: Token-based Activation Options:";
        internal const string L_MsgInvalidOptions = @"Invalid combination of command parameters.";
        internal const string L_MsgUnrecognizedOption = @"Unrecognized option: ";
        internal const string L_MsgErrorProductNotFound = @"Error: product not found.";
        internal const string L_MsgClearedPKey = @"Product key from registry cleared successfully.";
        internal const string L_MsgInstalledPKey = @"Installed product key %PKEY% successfully.";
        internal const string L_MsgUninstalledPKey = @"Uninstalled product key successfully.";
        internal const string L_MsgErrorPKey = @"Error: product key not found.";
        internal const string L_MsgInstallationID = @"Installation ID: ";
        internal const string L_MsgPhoneNumbers = @"Product activation telephone numbers can be obtained by searching the phone.inf file for the appropriate phone number for your location/country. You can open the phone.inf file from a Command Prompt or the Start Menu by running: notepad %systemroot%\system32\sppui\phone.inf";
        internal const string L_MsgActivating = @"Activating %PRODUCTNAME% (%PRODUCTID%) ...";
        internal const string L_MsgActivated = @"Product activated successfully.";
        internal const string L_MsgActivated_Failed = @"Error: Product activation failed.";
        internal const string L_MsgConfID = @"Confirmation ID for product %ACTID% deposited successfully.";
        internal const string L_MsgErrorLocalWMI = @"Error 0x%ERRCODE% occurred in connecting to the local WMI provider.";
        internal const string L_MsgErrorLocalRegistry = @"Error 0x%ERRCODE% occurred in connecting to the local registry.";
        internal const string L_MsgErrorConnection = @"Error 0x%ERRCODE% occurred in connecting to server %COMPUTERNAME%.";
        internal const string L_MsgInfoRemoteConnection = @"Connected to server %COMPUTERNAME%.";
        internal const string L_MsgErrorConnectionRegistry = @"Error 0x%ERRCODE% occurred in connecting to the registry on server %COMPUTERNAME%.";
        internal const string L_MsgErrorImpersonation = @"Error 0x%ERRCODE% occurred in setting impersonation level.";
        internal const string L_MsgErrorAuthenticationLevel = @"Error 0x%ERRCODE% occurred in setting authentication level.";
        internal const string L_MsgErrorWMI = @"Error 0x%ERRCODE% occurred in creating a locator object.";
        internal const string L_MsgErrorText_6 = @"On a computer running Microsoft Windows non-core edition, run 'slui.exe 0x2a 0x%ERRCODE%' to display the error text.";
        internal const string L_MsgErrorText_8 = @"Error: ";
        internal const string L_MsgErrorText_9 = @"Error: option %OPTION% needs %PARAM%";
        internal const string L_MsgErrorText_11 = @"The machine is running within the non-genuine grace period. Run 'slui.exe' to go online and make the machine genuine.";
        internal const string L_MsgErrorText_12 = @"Windows is running within the non-genuine notification period. Run 'slui.exe' to go online and validate Windows.";
        internal const string L_MsgLicenseFile = @"License file %LICENSEFILE% installed successfully.";
        internal const string L_MsgKmsPriSetToLow = @"KMS priority set to Low";
        internal const string L_MsgKmsPriSetToNormal = @"KMS priority set to Normal";
        internal const string L_MsgWarningKmsPri = @"Warning: Priority can only be set on a KMS machine that is also activated.";
        internal const string L_MsgKmsDnsPublishingDisabled = @"DNS publishing disabled";
        internal const string L_MsgKmsDnsPublishingEnabled = @"DNS publishing enabled";
        internal const string L_MsgKmsDnsPublishingWarning = @"Warning: DNS Publishing can only be set on a KMS machine that is also activated.";
        internal const string L_MsgKmsPortSet = @"KMS port set to %PORT% successfully.";
        internal const string L_MsgWarningKmsReboot = @"Warning: a KMS reboot is needed for this setting to take effect.";
        internal const string L_MsgWarningKmsPort = @"Warning: KMS port can only be set on a KMS machine that is also activated.";
        internal const string L_MsgRenewalSet = @"Volume renewal interval set to %RENEWAL% minutes successfully.";
        internal const string L_MsgWarningRenewal = @"Warning: Volume renewal interval can only be set on a KMS machine that is also activated.";
        internal const string L_MsgActivationSet = @"Volume activation interval set to %ACTIVATION% minutes successfully.";
        internal const string L_MsgWarningActivation = @"Warning: Volume activation interval can only be set on a KMS machine that is also activated.";
        internal const string L_MsgKmsNameSet = @"Key Management Service machine name set to %KMS% successfully.";
        internal const string L_MsgKmsNameCleared = @"Key Management Service machine name cleared successfully.";
        internal const string L_MsgKmsLookupDomainSet = @"Key Management Service lookup domain set to %FQDN% successfully.";
        internal const string L_MsgKmsLookupDomainCleared = @"Key Management Service lookup domain cleared successfully.";
        internal const string L_MsgKmsUseMachineNameOverrides = @"Warning: /skms setting overrides the /skms-domain setting. %KMS% will be used for activation.";
        internal const string L_MsgKmsUseMachineName = @"Warning: /skms setting is in effect. %KMS% will be used for activation.";
        internal const string L_MsgKmsUseLookupDomain = @"Warning: /skms-domain setting is in effect. %FQDN% will be used for DNS SRV record lookup.";
        internal const string L_MsgKmsHostCachingDisabled = @"KMS host caching is disabled";
        internal const string L_MsgKmsHostCachingEnabled = @"KMS host caching is enabled";
        internal const string L_MsgErrorActivationID = @"Error: Activation ID (%ActID%) not found.";
        internal const string L_MsgVLActivationTypeSet = @"Volume activation type set successfully.";
        internal const string L_MsgRearm_1 = @"Command completed successfully.";
        internal const string L_MsgRearm_2 = @"Please restart the system for the changes to take effect.";
        internal const string L_MsgRemainingWindowsRearmCount = @"Remaining Windows rearm count: %COUNT%";
        internal const string L_MsgRemainingSkuRearmCount = @"Remaining SKU rearm count: %COUNT%";
        internal const string L_MsgRemainingAppRearmCount = @"Remaining App rearm count: %COUNT%";
        internal const string L_MsgLicenseStatusUnlicensed = @"Unlicensed";
        internal const string L_MsgLicenseStatusVL = @"Volume activation will expire %ENDDATE%";
        internal const string L_MsgLicenseStatusTBL = @"Timebased activation will expire %ENDDATE%";
        internal const string L_MsgLicenseStatusAVMA = @"Automatic VM activation will expire %ENDDATE%";
        internal const string L_MsgLicenseStatusLicensed = @"The machine is permanently activated.";
        internal const string L_MsgLicenseStatusInitialGrace = @"Initial grace period ends %ENDDATE%";
        internal const string L_MsgLicenseStatusAdditionalGrace = @"Additional grace period ends %ENDDATE%";
        internal const string L_MsgLicenseStatusNonGenuineGrace = @"Non-genuine grace period ends %ENDDATE%";
        internal const string L_MsgLicenseStatusNotification = @"Windows is in Notification mode";
        internal const string L_MsgLicenseStatusExtendedGrace = @"Extended grace period ends %ENDDATE%";
        internal const string L_MsgLicenseStatusUnlicensed_1 = @"License Status: Unlicensed";
        internal const string L_MsgLicenseStatusLicensed_1 = @"License Status: Licensed";
        internal const string L_MsgLicenseStatusVL_1 = @"Volume activation expiration: %MINUTE% minute(s) (%DAY% day(s))";
        internal const string L_MsgLicenseStatusTBL_1 = @"Timebased activation expiration: %MINUTE% minute(s) (%DAY% day(s))";
        internal const string L_MsgLicenseStatusAVMA_1 = @"Automatic VM activation expiration: %MINUTE% minute(s) (%DAY% day(s))";
        internal const string L_MsgLicenseStatusInitialGrace_1 = @"License Status: Initial grace period";
        internal const string L_MsgLicenseStatusAdditionalGrace_1 = @"License Status: Additional grace period (KMS license expired or hardware out of tolerance)";
        internal const string L_MsgLicenseStatusNonGenuineGrace_1 = @"License Status: Non-genuine grace period.";
        internal const string L_MsgLicenseStatusNotification_1 = @"License Status: Notification";
        internal const string L_MsgLicenseStatusExtendedGrace_1 = @"License Status: Extended grace period";
        internal const string L_MsgNotificationErrorReasonNonGenuine = @"Notification Reason: 0x%ERRCODE% (non-genuine).";
        internal const string L_MsgNotificationErrorReasonExpiration = @"Notification Reason: 0x%ERRCODE% (grace time expired).";
        internal const string L_MsgNotificationErrorReasonOther = @"Notification Reason: 0x%ERRCODE%.";
        internal const string L_MsgLicenseStatusTimeRemaining = @"Time remaining: %MINUTE% minute(s) (%DAY% day(s))";
        internal const string L_MsgLicenseStatusUnknown = @"License Status: Unknown";
        internal const string L_MsgLicenseStatusEvalEndData = @"Evaluation End Date: ";
        internal const string L_MsgReinstallingLicenses = @"Re-installing license files ...";
        internal const string L_MsgLicensesReinstalled = @"License files re-installed successfully.";
        internal const string L_MsgServiceVersion = @"Software licensing service version: ";
        internal const string L_MsgProductName = @"Name: ";
        internal const string L_MsgProductDesc = @"Description: ";
        internal const string L_MsgActID = @"Activation ID: ";
        internal const string L_MsgAppID = @"Application ID: ";
        internal const string L_MsgPID4 = @"Extended PID: ";
        internal const string L_MsgChannel = @"Product Key Channel: ";
        internal const string L_MsgProcessorCertUrl = @"Processor Certificate URL: ";
        internal const string L_MsgMachineCertUrl = @"Machine Certificate URL: ";
        internal const string L_MsgUseLicenseCertUrl = @"Use License URL: ";
        internal const string L_MsgPKeyCertUrl = @"Product Key Certificate URL: ";
        internal const string L_MsgValidationUrl = @"Validation URL: ";
        internal const string L_MsgPartialPKey = @"Partial Product Key: ";
        internal const string L_MsgErrorLicenseNotInUse = @"This license is not in use.";
        internal const string L_MsgKmsInfo = @"Key Management Service client information";
        internal const string L_MsgCmid = @"Client Machine ID (CMID): ";
        internal const string L_MsgRegisteredKmsName = @"Registered KMS machine name: ";
        internal const string L_MsgKmsLookupDomain = @"Registered KMS SRV record lookup domain: ";
        internal const string L_MsgKmsFromDnsUnavailable = @"DNS auto-discovery: KMS name not available";
        internal const string L_MsgKmsFromDns = @"KMS machine name from DNS: ";
        internal const string L_MsgKmsIpAddress = @"KMS machine IP address: ";
        internal const string L_MsgKmsIpAddressUnavailable = @"KMS machine IP address: not available";
        internal const string L_MsgKmsPID4 = @"KMS machine extended PID: ";
        internal const string L_MsgActivationInterval = @"Activation interval: %INTERVAL% minutes";
        internal const string L_MsgRenewalInterval = @"Renewal interval: %INTERVAL% minutes";
        internal const string L_MsgKmsEnabled = @"Key Management Service is enabled on this machine";
        internal const string L_MsgKmsCurrentCount = @"Current count: ";
        internal const string L_MsgKmsListeningOnPort = @"Listening on Port: ";
        internal const string L_MsgKmsPriNormal = @"KMS priority: Normal";
        internal const string L_MsgKmsPriLow = @"KMS priority: Low";
        internal const string L_MsgVLActivationTypeAll = @"Configured Activation Type: All";
        internal const string L_MsgVLActivationTypeAD = @"Configured Activation Type: AD";
        internal const string L_MsgVLActivationTypeKMS = @"Configured Activation Type: KMS";
        internal const string L_MsgVLActivationTypeToken = @"Configured Activation Type: Token";
        internal const string L_MsgVLMostRecentActivationInfo = @"Most recent activation information:";
        internal const string L_MsgInvalidDataError = @"Error: The data is invalid";
        internal const string L_MsgUndeterminedPrimaryKey = @"Warning: SLMGR was not able to validate the current product key for Windows. Please upgrade to the latest service pack.";
        internal const string L_MsgUndeterminedPrimaryKeyOperation = @"Warning: This operation may affect more than one target license.  Please verify the results.";
        internal const string L_MsgUndeterminedOperationFormat = @"Processing the license for %PRODUCTDESCRIPTION% (%PRODUCTID%).";
        internal const string L_MsgPleaseActivateRefreshKMSInfo = @"Please use slmgr.vbs /ato to activate and update KMS client information in order to update values.";
        internal const string L_MsgTokenBasedActivationMustBeDone = @"This system is configured for Token-based activation only. Use slmgr.vbs /fta to initiate Token-based activation, or slmgr.vbs /act-type to change the activation type setting.";
        internal const string L_MsgKmsCumulativeRequestsFromClients = @"Key Management Service cumulative requests received from clients";
        internal const string L_MsgKmsTotalRequestsRecieved = @"Total requests received: ";
        internal const string L_MsgKmsFailedRequestsReceived = @"Failed requests received: ";
        internal const string L_MsgKmsRequestsWithStatusUnlicensed = @"Requests with License Status Unlicensed: ";
        internal const string L_MsgKmsRequestsWithStatusLicensed = @"Requests with License Status Licensed: ";
        internal const string L_MsgKmsRequestsWithStatusInitialGrace = @"Requests with License Status Initial grace period: ";
        internal const string L_MsgKmsRequestsWithStatusLicenseExpiredOrHwidOot = @"Requests with License Status License expired or Hardware out of tolerance: ";
        internal const string L_MsgKmsRequestsWithStatusNonGenuineGrace = @"Requests with License Status Non-genuine grace period: ";
        internal const string L_MsgKmsRequestsWithStatusNotification = @"Requests with License Status Notification: ";
        internal const string L_MsgRemoteWmiVersionMismatch = @"The remote machine does not support this version of SLMgr.vbs";
        internal const string L_MsgRemoteExecNotSupported = @"This command of SLMgr.vbs is not supported for remote execution";
        internal const string L_MsgTkaLicenses = @"Token-based Activation Issuance Licenses:";
        internal const string L_MsgTkaLicenseHeader = @"%ILID%    %ILVID%";
        internal const string L_MsgTkaLicenseILID = @"License ID (ILID): %ILID%";
        internal const string L_MsgTkaLicenseILVID = @"Version ID (ILvID): %ILVID%";
        internal const string L_MsgTkaLicenseExpiration = @"Valid to: %TODATE%";
        internal const string L_MsgTkaLicenseAdditionalInfo = @"Additional Information: %MOREINFO%";
        internal const string L_MsgTkaLicenseAuthZStatus = @"Error: 0x%ERRCODE%";
        internal const string L_MsgTkaLicenseDescr = @"Description: %DESC%";
        internal const string L_MsgTkaLicenseNone = @"No licenses found.";
        internal const string L_MsgTkaRemoving = @"Removing Token-based Activation License ...";
        internal const string L_MsgTkaRemovedItem = @"Removed license with SLID=%SLID%.";
        internal const string L_MsgTkaRemovedNone = @"No licenses found.";
        internal const string L_MsgTkaInfoAdditionalInfo = @"Additional Information: %MOREINFO%";
        internal const string L_MsgTkaInfo = @"Token-based Activation information";
        internal const string L_MsgTkaInfoILID = @"License ID (ILID): %ILID%";
        internal const string L_MsgTkaInfoILVID = @"Version ID (ILvID): %ILVID%";
        internal const string L_MsgTkaInfoGrantNo = @"Grant Number: %GRANTNO%";
        internal const string L_MsgTkaInfoThumbprint = @"Certificate Thumbprint: %THUMBPRINT%";
        internal const string L_MsgTkaCertThumbprint = @"Thumbprint: %THUMBPRINT%";
        internal const string L_MsgTkaCertSubject = @"Subject: %SUBJECT%";
        internal const string L_MsgTkaCertIssuer = @"Issuer: %ISSUER%";
        internal const string L_MsgTkaCertValidFrom = @"Valid from: %FROMDATE%";
        internal const string L_MsgTkaCertValidTo = @"Valid to: %TODATE%";
        internal const string L_MsgADInfo = @"AD Activation client information";
        internal const string L_MsgADInfoAOName = @"Activation Object name: ";
        internal const string L_MsgADInfoAODN = @"AO DN: ";
        internal const string L_MsgADInfoExtendedPid = @"AO extended PID: ";
        internal const string L_MsgADInfoActID = @"AO activation ID: ";
        internal const string L_MsgActObjAvailable = @"Activation Objects";
        internal const string L_MsgActObjNoneFound = @"No objects found";
        internal const string L_MsgSucess = @"Operation completed successfully.";
        internal const string L_MsgADSchemaNotSupported = @"Active Directory-Based Activation is not supported in the current Active Directory schema.";
        internal const string L_MsgAVMAInfo = @"Automatic VM Activation client information";
        internal const string L_MsgAVMAID = @"Guest IAID: ";
        internal const string L_MsgAVMAHostMachineName = @"Host machine name: ";
        internal const string L_MsgAVMALastActTime = @"Activation time: ";
        internal const string L_MsgAVMAHostPid2 = @"Host Digital PID2: ";
        internal const string L_MsgNotAvailable = @"Not Available";
        internal const string L_MsgCurrentTrustedTime = @"Trusted time: ";
        internal const string L_MsgError_C004C001 = @"The activation server determined the specified product key is invalid";
        internal const string L_MsgError_C004C003 = @"The activation server determined the specified product key is blocked";
        internal const string L_MsgError_C004C017 = @"The activation server determined the specified product key has been blocked for this geographic location.";
        internal const string L_MsgError_C004B100 = @"The activation server determined that the computer could not be activated";
        internal const string L_MsgError_C004C008 = @"The activation server determined that the specified product key could not be used";
        internal const string L_MsgError_C004C020 = @"The activation server reported that the Multiple Activation Key has exceeded its limit";
        internal const string L_MsgError_C004C021 = @"The activation server reported that the Multiple Activation Key extension limit has been exceeded";
        internal const string L_MsgError_C004D307 = @"The maximum allowed number of re-arms has been exceeded. You must re-install the OS before trying to re-arm again";
        internal const string L_MsgError_C004F009 = @"The software Licensing Service reported that the grace period expired";
        internal const string L_MsgError_C004F00F = @"The Software Licensing Server reported that the hardware ID binding is beyond level of tolerance";
        internal const string L_MsgError_C004F014 = @"The Software Licensing Service reported that the product key is not available";
        internal const string L_MsgError_C004F025 = @"Access denied: the requested action requires elevated privileges";
        internal const string L_MsgError_C004F02C = @"The software Licensing Service reported that the format for the offline activation data is incorrect";
        internal const string L_MsgError_C004F035 = @"The software Licensing Service reported that the computer could not be activated with a Volume license product key. Volume licensed systems require upgrading from a qualified operating system. Please contact your system administrator or use a different type of key";
        internal const string L_MsgError_C004F038 = @"The software Licensing Service reported that the computer could not be activated. The count reported by your Key Management Service (KMS) is insufficient. Please contact your system administrator";
        internal const string L_MsgError_C004F039 = @"The software Licensing Service reported that the computer could not be activated. The Key Management Service (KMS) is not enabled";
        internal const string L_MsgError_C004F041 = @"The software Licensing Service determined that the Key Management Server (KMS) is not activated. KMS needs to be activated";
        internal const string L_MsgError_C004F042 = @"The software Licensing Service determined that the specified Key Management Service (KMS) cannot be used";
        internal const string L_MsgError_C004F050 = @"The Software Licensing Service reported that the product key is invalid";
        internal const string L_MsgError_C004F051 = @"The software Licensing Service reported that the product key is blocked";
        internal const string L_MsgError_C004F064 = @"The software Licensing Service reported that the non-Genuine grace period expired";
        internal const string L_MsgError_C004F065 = @"The software Licensing Service reported that the application is running within the valid non-genuine period";
        internal const string L_MsgError_C004F066 = @"The Software Licensing Service reported that the product SKU is not found";
        internal const string L_MsgError_C004F06B = @"The software Licensing Service determined that it is running in a virtual machine. The Key Management Service (KMS) is not supported in this mode";
        internal const string L_MsgError_C004F074 = @"The Software Licensing Service reported that the computer could not be activated. No Key Management Service (KMS) could be contacted. Please see the Application Event Log for additional information.";
        internal const string L_MsgError_C004F075 = @"The Software Licensing Service reported that the operation cannot be completed because the service is stopping";
        internal const string L_MsgError_C004F304 = @"The Software Licensing Service reported that required license could not be found.";
        internal const string L_MsgError_C004F305 = @"The Software Licensing Service reported that there are no certificates found in the system that could activate the product.";
        internal const string L_MsgError_C004F30A = @"The Software Licensing Service reported that the computer could not be activated. The certificate does not match the conditions in the license.";
        internal const string L_MsgError_C004F30D = @"The Software Licensing Service reported that the computer could not be activated. The thumbprint is invalid.";
        internal const string L_MsgError_C004F30E = @"The Software Licensing Service reported that the computer could not be activated. A certificate for the thumbprint could not be found.";
        internal const string L_MsgError_C004F30F = @"The Software Licensing Service reported that the computer could not be activated. The certificate does not match the criteria specified in the issuance license.";
        internal const string L_MsgError_C004F310 = @"The Software Licensing Service reported that the computer could not be activated. The certificate does not match the trust point identifier (TPID) specified in the issuance license.";
        internal const string L_MsgError_C004F311 = @"The Software Licensing Service reported that the computer could not be activated. A soft token cannot be used for activation.";
        internal const string L_MsgError_C004F312 = @"The Software Licensing Service reported that the computer could not be activated. The certificate cannot be used because its private key is exportable.";
        internal const string L_MsgError_5 = @"Access denied: the requested action requires elevated privileges";
        internal const string L_MsgError_80070005 = @"Access denied: the requested action requires elevated privileges";
        internal const string L_MsgError_80070057 = @"The parameter is incorrect";
        internal const string L_MsgError_8007232A = @"DNS server failure";
        internal const string L_MsgError_8007232B = @"DNS name does not exist";
        internal const string L_MsgError_800706BA = @"The RPC server is unavailable";
        internal const string L_MsgError_8007251D = @"No records found for DNS query";

        /// <summary>
        /// HRESULT -> 错误描述。对应原脚本的 GetResource("L_MsgError_" &amp; Hex(err))。
        /// 键为 VBScript Hex() 风格：大写、无前导 0x、不补零。
        /// </summary>
        private static readonly Dictionary<string, string> ErrorTable = new Dictionary<string, string>
        {
            { "C004C001", L_MsgError_C004C001 },
            { "C004C003", L_MsgError_C004C003 },
            { "C004C017", L_MsgError_C004C017 },
            { "C004B100", L_MsgError_C004B100 },
            { "C004C008", L_MsgError_C004C008 },
            { "C004C020", L_MsgError_C004C020 },
            { "C004C021", L_MsgError_C004C021 },
            { "C004D307", L_MsgError_C004D307 },
            { "C004F009", L_MsgError_C004F009 },
            { "C004F00F", L_MsgError_C004F00F },
            { "C004F014", L_MsgError_C004F014 },
            { "C004F025", L_MsgError_C004F025 },
            { "C004F02C", L_MsgError_C004F02C },
            { "C004F035", L_MsgError_C004F035 },
            { "C004F038", L_MsgError_C004F038 },
            { "C004F039", L_MsgError_C004F039 },
            { "C004F041", L_MsgError_C004F041 },
            { "C004F042", L_MsgError_C004F042 },
            { "C004F050", L_MsgError_C004F050 },
            { "C004F051", L_MsgError_C004F051 },
            { "C004F064", L_MsgError_C004F064 },
            { "C004F065", L_MsgError_C004F065 },
            { "C004F066", L_MsgError_C004F066 },
            { "C004F06B", L_MsgError_C004F06B },
            { "C004F074", L_MsgError_C004F074 },
            { "C004F075", L_MsgError_C004F075 },
            { "C004F304", L_MsgError_C004F304 },
            { "C004F305", L_MsgError_C004F305 },
            { "C004F30A", L_MsgError_C004F30A },
            { "C004F30D", L_MsgError_C004F30D },
            { "C004F30E", L_MsgError_C004F30E },
            { "C004F30F", L_MsgError_C004F30F },
            { "C004F310", L_MsgError_C004F310 },
            { "C004F311", L_MsgError_C004F311 },
            { "C004F312", L_MsgError_C004F312 },
            { "5", L_MsgError_5 },
            { "80070005", L_MsgError_80070005 },
            { "80070057", L_MsgError_80070057 },
            { "8007232A", L_MsgError_8007232A },
            { "8007232B", L_MsgError_8007232B },
            { "800706BA", L_MsgError_800706BA },
            { "8007251D", L_MsgError_8007251D },
        };

        /// <summary>
        /// 取错误码对应的描述文本；未收录时返回空字符串（与原脚本行为一致）。
        /// </summary>
        internal static string GetErrorMessage(string hexCode)
        {
            string value;
            return ErrorTable.TryGetValue(hexCode, out value) ? value : "";
        }
    }
}
