using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OSVersionInfoClass;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace CMWTAT_DIGITAL
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("Kernel32.dll")]
        private static extern bool Wow64EnableWow64FsRedirection(bool Wow64FsEnableRedirection);//重定向
        public struct Frequency
        {
            public int ID { get; set; }
            public string DisplayOS { get; set; }
        }
        //public string SystemEdition = OSVersionInfo.Name + " " + OSVersionInfo.Edition;
        public string SystemEdition = OSVersionInfo.Edition;
        public MainWindow()
        {
            InitializeComponent();
            //MessageBox.Show(@"reg add ""HKLM\SYSTEM\Tokens\"" /v ""Channel"" /t REG_SZ /d ""Retail"" /f");
            DialogWait.IsOpen = true;
            try
            {
                RegistryKey pRegKey = Registry.LocalMachine;
                pRegKey = pRegKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                SystemEdition = pRegKey.GetValue("EditionID").ToString();
            }
            catch
            {
                SystemEdition = OSVersionInfo.Edition;
            }

            //SystemEditionText.Text = SystemEdition;
            Thread loadthread = new Thread(LoadOSList);
            loadthread.Start();
        }
        JArray ositems;
        int now_os_index = 0;
        string checked_os = "unknow";
        private void InvokeTest()
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                LoadOSList();
            }));
        }
        private void LoadOSList()
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                DialogWait.IsOpen = true;
            }));
            try
            {
                string json = GetHttpWebRequest("https://kms.kumo.moe/api/digital?list=1");
                JObject jsonobj = JObject.Parse(json);
                List<Frequency> list = new List<Frequency>();
                Frequency freq = new Frequency();
                ositems = (JArray)jsonobj["OS"];
                for (int i = 0; i < ositems.Count(); i++)
                {
                    freq.ID = i;
                    freq.DisplayOS = jsonobj["OS"][i].ToString();
                    if (jsonobj["OS"][i].ToString() == SystemEdition)
                    {
                        now_os_index = i;
                        checked_os = SystemEdition;
                    }
                    if (jsonobj["OS"][i].ToString() == SystemEdition + OSVersionInfo.BuildVersion)
                    {
                        now_os_index = i;
                        checked_os = SystemEdition + OSVersionInfo.BuildVersion;
                    }
                    list.Add(freq);
                }

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.SystemEditionText.ItemsSource = list;//控件的ID

                    if (checked_os == "unknow")
                    {
                        this.SystemEditionText.SelectedIndex = 0;
                        this.DialogWithOKToCloseDialogTitle.Text = "Attention";
                        this.DialogWithOKToCloseDialogText.Text = "Unable to correctly identify your operating system, may be not be supported.\r\n(System edition: " + SystemEdition + OSVersionInfo.BuildVersion + ")";
                        this.DialogWithOKToCloseDialog.IsOpen = true;
                    }
                    else
                    {
                        this.SystemEditionText.SelectedIndex = now_os_index;
                    }
                }));

                //this.SystemEditionText.SelectedIndex = now_os_index;
                // 在此点之下插入创建对象所需的代码。
            }
            catch
            {
                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    DialogWithExit.IsOpen = true;
                }));
            }
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                DialogWait.IsOpen = false;
            }));

        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Thread actthread = new Thread(RunAct);
            actthread.Start();

            //RunAct();
            //LoadOSList();

            //MessageBox.Show(json);
            //MessageBox.Show(rss["OS"][0].ToString());
            //MessageBox.Show(SystemEdition);
        }
        private string GetHttpWebRequest(string url)
        {
            Uri uri = new Uri(url);
            HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(uri);
            myReq.UserAgent = "User-Agent:Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705";
            myReq.Accept = "*/*";
            myReq.KeepAlive = true;
            myReq.Headers.Add("Accept-Language", "zh-cn,en-us;q=0.5");
            HttpWebResponse result = (HttpWebResponse)myReq.GetResponse();
            Stream receviceStream = result.GetResponseStream();
            StreamReader readerOfStream = new StreamReader(receviceStream, System.Text.Encoding.GetEncoding("utf-8"));
            string strHTML = readerOfStream.ReadToEnd();
            readerOfStream.Close();
            receviceStream.Close();
            result.Close();
            return strHTML;
        }

        private void Exit_Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void RunAct()
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.DialogActProg.IsOpen = true;
                this.activatingtext.Text = "Activating";
            }));

            Wow64EnableWow64FsRedirection(false);//关闭文件重定向

            string code = "-0";
            string key = "00000-00000-00000-00000-00000";
            string sku = "0";
            string msg = "Unknow Error!";
            string system = "";
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                system = this.SystemEditionText.Text;
            }));


            string slmgr = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) + "\\slmgr.vbs";
            string slmgr_self = System.AppDomain.CurrentDomain.BaseDirectory + "slmgr.vbs";

            string changepk = Environment.SystemDirectory + "\\changepk.exe";

            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.activatingtext.Text = "Getting Key";
            }));

            //获取密钥和SKU
            try
            {

                string json = GetHttpWebRequest("https://kms.kumo.moe/api/digital?list=0");
                JObject jsonobj = JObject.Parse(json);
                List<Frequency> list = new List<Frequency>();
                ositems = (JArray)jsonobj["OS"];
                key = jsonobj[system]["key"].ToString();
                sku = jsonobj[system]["sku"].ToString();
                Console.WriteLine("Edition:" + system + "\r\nSKU:" + key + "\r\nSKU:" + sku);

            }
            catch
            {
                code = "-0";
                msg = "激活Windows10需要网络获取产品密钥 :) \nActivate Windows 10 requires a network to gets the product key :)";
                goto EndLine;
            }
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.activatingtext.Text = "Uninstalling old Key";
            }));
            //卸载
            string runend = RunCScript(slmgr_self, "-upk").Trim();
            Console.WriteLine(runend);
            if (runend.EndsWith("successfully.") || runend.EndsWith("not found."))
            {

                RunCScript(slmgr_self, "-ckms").Trim();

                //写入Win7特征
                //ChangePKAction(changepk + " /ProductKey " + key);

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = "Writing feature of old Windows version";
                }));

                RunCMD(@"reg add ""HKLM\SYSTEM\Tokens"" /v ""Channel"" /t REG_SZ /d ""Retail"" /f");
                RunCMD(@"reg add ""HKLM\SYSTEM\Tokens\Kernel"" /v ""Kernel-ProductInfo"" /t REG_DWORD /d " + sku + " /f");
                RunCMD(@"reg add ""HKLM\SYSTEM\Tokens\Kernel"" /v ""Security-SPP-GenuineLocalStatus"" /t REG_DWORD /d 1 /f");
                RunCMD(@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"" /v ""C:\gatherosstate.exe"" /d ""~ WIN7RTM"" /f");

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = "Installing Key";
                }));

                //安装数字权利升级密钥
                if (RunCScript(slmgr_self, "-ipk " + key).Trim().EndsWith("successfully."))
                {

                    actbtn.Dispatcher.Invoke(new Action(() =>
                    {
                        this.activatingtext.Text = "Getting free upgrade permissions";
                    }));

                    RunCMD(System.AppDomain.CurrentDomain.BaseDirectory + "gatherosstate.exe");

                    for (int i = 0; i < 3 || !File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "GenuineTicket.xml"); i++)
                    {
                        Thread.Sleep(3000);
                    }

                    if (File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "GenuineTicket.xml"))
                    {

                        actbtn.Dispatcher.Invoke(new Action(() =>
                        {
                            this.activatingtext.Text = "Cleaning changes";
                        }));

                        RunCMD(@"reg delete ""HKLM\SYSTEM\Tokens"" /f");
                        RunCMD(@"reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"" /v ""C:\gatherosstate.exe"" /f");

                        actbtn.Dispatcher.Invoke(new Action(() =>
                        {
                            this.activatingtext.Text = "Getting digital license";
                        }));

                        Wow64EnableWow64FsRedirection(false);//关闭文件重定向
                        RunCMD(Environment.SystemDirectory + @"\ClipUp.exe -v -o -altto " + System.AppDomain.CurrentDomain.BaseDirectory);

                        actbtn.Dispatcher.Invoke(new Action(() =>
                        {
                            this.activatingtext.Text = "Activating";
                        }));

                        runend = RunCScript(slmgr_self, "-ato").Trim();
                        if (runend.EndsWith("060' to display the error text.") || runend.EndsWith("successfully.") || runend.EndsWith("to display the error text."))
                        {
                            code = "200";
                        }
                        else
                        {
                            code = "-4";
                            msg = "激活失败 :(\nActivation Failed. :(";
                        }
                    }
                    else
                    {
                        code = "-3";
                        msg = "执行超时，可能没有选择正确的版本 :(\nTime out, may be you choose a incorrect version. :(";
                    }
                }
                else
                {
                    code = "-2";
                    msg = "无法安装密钥，可能没有选择正确的版本 :(\nCannot to install key, may be you choose a incorrect version. :(";
                }
            }
            else
            {
                code = "-1";
                msg = "无法卸载旧密钥 :(\nCannot to uninstall old key. :(";
            }
            //string runend = RunCScript(slmgr_self, "-upk").Trim();
            EndLine:;
            if (code != "200")
            {
                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.DialogActProg.IsOpen = false;
                    this.activatingtext.Text = "Activating";
                    this.DialogWithOKToCloseDialog.IsOpen = true;
                    this.DialogWithOKToCloseDialogTitle.Text = "Error";
                    this.DialogWithOKToCloseDialogText.Text = msg + "\r\nCode:" + code;
                }));
                //MessageBox.Show(msg + "\r\nCode:" + code);
            }
            else
            {
                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.DialogActProg.IsOpen = false;
                    this.activatingtext.Text = "Activating";
                    this.DialogWithOKToCloseDialogDonate.IsOpen = true;
                    //this.DialogWithOKToCloseDialogDonateTitle.Text = "Complete";
                    //this.DialogWithOKToCloseDialogDonateText.Text = "Congratulation!";
                }));
                //MessageBox.Show("Congratulation!");
            }
        }

        private void RunCMD(string var)
        {
            Wow64EnableWow64FsRedirection(false);//关闭文件重定向
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";//要执行的程序名称 
            p.StartInfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;//可能接受来自调用程序的输入信息 
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息 
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口 
            p.Start();//启动程序 
                      //向CMD窗口发送输入信息： 
            p.StandardInput.WriteLine(var); //10秒后重启（C#中可不好做哦）
            Console.WriteLine(var);
            //Wow64EnableWow64FsRedirection(false);//关闭文件重定向
            //System.Diagnostics.Process.Start(var);
        }

        public static string RunCScript(string path, string var = "")
        {
            Wow64EnableWow64FsRedirection(false);//关闭文件重定向
            //执行命令行函数
            try
            {
                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo myProcessStartInfo = new System.Diagnostics.ProcessStartInfo("CScript", "//Nologo \"" + path + "\" " + var);
                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.CreateNoWindow = true;
                //myProcessStartInfo.Arguments = "/c chcp 65001 > nul && cmd /c \"" + PHPRuntimePath + "\" \"" + path + "\" " + var;
                //myProcessStartInfo.Arguments = "/c " & Commands
                myProcessStartInfo.StandardOutputEncoding = Encoding.UTF8;
                myProcess.StartInfo = myProcessStartInfo;
                myProcess.Start();
                myProcess.WaitForExit(120 * 1000);
                System.IO.StreamReader myStreamReader = myProcess.StandardOutput;
                string myString = myStreamReader.ReadToEnd();
                myProcess.Close();
                return myString;
            }
            catch
            {
                return "Error";
            }
        }

        private void Donate_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://waxel.cloudmoe.com/donate/");
            this.DialogWithOKToCloseDialogDonate.IsOpen = false;
        }
    }
}
