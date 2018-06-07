using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CMWTAT_DIGITAL
{
    public static class Program
    {

        public static bool autoact = false;
        public static bool hiderun = false;
        public static bool expact = false;
        public static bool showhelp = false;

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]

        public static void Main(string[] startup_args)
        {
            //添加程序集解析事件  
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                String resourceName = "CMWTAT_DIGITAL.Res." +

                new AssemblyName(args.Name).Name + ".dll";

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    Byte[] assemblyData = new Byte[stream.Length];

                    stream.Read(assemblyData, 0, assemblyData.Length);

                    return Assembly.Load(assemblyData);
                }
            };

            //if (startup_args.Length == 0)
            //{
            //    //MessageBox.Show("ARGS NULL");
            //    //Console.WriteLine("CMWTAT Digital Edition V2");
            //    //Console.WriteLine("This application can use console args.");
            //}
            foreach (string arg in startup_args)
            {
                Console.WriteLine("arg: " + arg);
                if (arg == "-a" || arg == "--auto")
                {
                    Console.WriteLine("AUTO: True");
                    autoact = true;
                }
                if (arg == "-h" || arg == "--hide")
                {
                    Console.WriteLine("HIDE: True");
                    hiderun = true;
                }
                if (arg == "-e" || arg == "--expact")
                {
                    Console.WriteLine("EXPACT: True");
                    expact = true;
                }
                if (arg == "-?" || arg == "--help")
                {
                    Console.WriteLine("EXPACT: True");
                    showhelp = true;
                }
            }

            CMWTAT_DIGITAL.App app = new CMWTAT_DIGITAL.App();//WPF项目的Application实例，用来启动WPF项目的
            app.InitializeComponent();
            app.Run();
        }
    }
}
