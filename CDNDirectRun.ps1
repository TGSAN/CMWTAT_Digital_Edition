$isAdmin = (New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Start-Process powershell -ArgumentList "irm https://fastly.jsdelivr.net/gh/TGSAN/CMWTAT_Digital_Edition@master/CDNDirectRun.ps1 | iex" -Verb RunAs
    Exit
}

# $exePath = ".\CMWTAT_DIGITAL\bin\Debug\CMWTAT_DIGITAL.exe"
# $exeBytes = [System.IO.File]::ReadAllBytes($exePath)
# $bytes = $exeBytes

$bytes = (Invoke-WebRequest "https://release-download.iloli.tv/TGSAN/CMWTAT_Digital_Edition/releases/download/2.7.2.0/CMWTAT_Digital_Release_2_7_2_0.exe").Content
$assembly = [System.Reflection.Assembly]::Load($bytes)
$entryPointMethod = $assembly.EntryPoint
$entryPointMethod.Invoke($null, @())
