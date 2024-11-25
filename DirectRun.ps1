$isAdmin = (New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Start-Process powershell -ArgumentList "irm https://tgsan.github.io/CMWTAT_Digital_Edition/DirectRun.ps1 | iex" -Verb RunAs
    Exit
}

# $exePath = ".\CMWTAT_DIGITAL\bin\Debug\CMWTAT_DIGITAL.exe"
# $exeBytes = [System.IO.File]::ReadAllBytes($exePath)
# $bytes = $exeBytes

$bytes = (Invoke-WebRequest "https://tgsan.github.io/CMWTAT_Digital_Edition/CMWTAT_Digital_Release_2_7_2_0.exe").Content
$assembly = [System.Reflection.Assembly]::Load($bytes)
$entryPointMethod = $assembly.EntryPoint
$entryPointMethod.Invoke($null, @())