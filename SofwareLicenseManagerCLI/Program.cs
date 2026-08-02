//

// Program.cs -- slmgr.vbs 的命令行等价物。

//

// 直接以源码形式引用 ..\CMWTAT_DIGITAL\LibSofwareLicenseManager\*.cs，

// 与 CMWTAT_DIGITAL 共用同一份实现，行为与 `cscript //Nologo slmgr.vbs <args>` 一致：

//

//     SofwareLicenseManagerCLI.exe /dlv

//     SofwareLicenseManagerCLI.exe -ipk XXXXX-XXXXX-XXXXX-XXXXX-XXXXX

//     SofwareLicenseManagerCLI.exe MyServer Administrator P@ssw0rd /dli

//

// 退出码：0 表示成功；其余与 slmgr.vbs 相同（用法错误为 1，SPP 失败为对应 HRESULT）。

//



using System;

using CMWTAT_DIGITAL.LibSofwareLicenseManager;



namespace SofwareLicenseManagerCLI

{

    internal static class Program

    {

        private static int Main(string[] args)

        {

            try

            {

                SofwareLicenseManagerResult result = SofwareLicenseManager.Execute(args);



                // Output 已包含换行，直接原样写出即可与 cscript 的输出一致

                Console.Out.Write(result.Output);

                Console.Out.Flush();



                return result.ExitCode;

            }

            catch (Exception ex)

            {

                // 走到这里说明连 WMI 都没能连上（例如服务被禁用、权限不足）

                Console.Error.WriteLine("Error: " + ex.Message);



                int hr = System.Runtime.InteropServices.Marshal.GetHRForException(ex);

                return hr == 0 ? 1 : hr;

            }

        }

    }

}

