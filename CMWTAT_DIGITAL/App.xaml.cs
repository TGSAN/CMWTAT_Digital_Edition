using System;
using System.Windows;

namespace CMWTAT_DIGITAL
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static bool autoact = false;
        public static bool hiderun = false;
        public static bool expact = false;
        public static bool log2file = false;
        public static bool showhelp = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            foreach (string arg in e.Args)
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
                if (arg == "-l" || arg == "--log")
                {
                    Console.WriteLine("LOG: True");
                    log2file = true;
                }
                if (arg == "-?" || arg == "--help")
                {
                    Console.WriteLine("SHOWHELP: True");
                    showhelp = true;
                }
            }
            base.OnStartup(e);
        }
    }
}
