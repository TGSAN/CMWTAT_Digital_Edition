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
using CMWTAT_DIGITAL.Domain;
using System.Text.RegularExpressions;
using System.Windows.Forms;

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

        string tempfile = System.IO.Path.GetTempPath() + @"CMWTAT_DIGITAL\";

        public void DelectTempFile()
        {
            //string tempfile = System.IO.Path.GetTempPath() + @"CMWTAT_DIGITAL\";
            if (Directory.Exists(tempfile))
            {
                try
                {
                    //DirectoryInfo dir = new DirectoryInfo(srcPath);
                    //FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                    //foreach (FileSystemInfo i in fileinfo)
                    //{
                    //    if (i is DirectoryInfo)            //判断是否文件夹
                    //    {
                    //        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                    //        subdir.Delete(true);          //删除子目录和文件
                    //    }
                    //    else
                    //    {
                    //        File.Delete(i.FullName);      //删除指定文件
                    //    }
                    //}
                    FileAttributes attr = File.GetAttributes(tempfile);
                    if (attr == FileAttributes.Directory)
                    {
                        Directory.Delete(tempfile, true);
                    }
                    else
                    {
                        File.Delete(tempfile);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("DelectTempFile:" + e.Message);
                }
            }
        }

        public void ExportTempFile()
        {
            //string tempfile = System.IO.Path.GetTempPath() + @"CMWTAT_DIGITAL\";

            //if (tempfile.EndsWith(@"\"))
            //{
            //    tempfile = tempfile.Remove(tempfile.Length - 1, 1);
            //}

            if (!Directory.Exists(tempfile))
            {
                Directory.CreateDirectory(tempfile);
            }

            byte[] temp;
            System.IO.FileStream fileStream;

            temp = CMWTAT_DIGITAL.Properties.Resources.gatherosstate;
            fileStream = new System.IO.FileStream(tempfile + "gatherosstate" + ".exe", System.IO.FileMode.CreateNew);
            fileStream.Write(temp, 0, (int)(temp.Length));
            fileStream.Close();

            temp = CMWTAT_DIGITAL.Properties.Resources.slc;
            fileStream = new System.IO.FileStream(tempfile + "slc" + ".dll", System.IO.FileMode.CreateNew);
            fileStream.Write(temp, 0, (int)(temp.Length));
            fileStream.Close();

            temp = CMWTAT_DIGITAL.Properties.Resources.slmgr;
            fileStream = new System.IO.FileStream(tempfile + "slmgr" + ".vbs", System.IO.FileMode.CreateNew);
            fileStream.Write(temp, 0, (int)(temp.Length));
            fileStream.Close();
        }

        bool autoact = false;
        bool hiderun = false;
        bool expact = false;
        bool showhelp = false;

        //public string SystemEdition = OSVersionInfo.Name + " " + OSVersionInfo.Edition;
        public string SystemEdition = OSVersionInfo.Edition;

        NotifyIcon notifyIcon;

        public MainWindow()
        {

            autoact = Program.autoact;
            hiderun = Program.hiderun;
            expact = Program.expact;
            showhelp = Program.showhelp;

            //MessageBox.Show("A:" + autoact.ToString() + ";H:" + hiderun.ToString());

            InitializeComponent();

            if (showhelp == true)
            {
                DialogHelp.IsOpen = true;
            }

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Text = "CloudMoe Windows 10 Activation Toolkit V2";
            notifyIcon.Icon = ((System.Drawing.Icon)(CMWTAT_DIGITAL.Properties.Resources.CMWTAT_ICON));

            if (hiderun == true && autoact == true)
            {
                this.Hide();
                //notifyIcon.BalloonTipText = "The app has been minimised. Click the tray icon to show.";
                //notifyIcon.BalloonTipTitle = "The App";

                //notifyIcon.Icon = new System.Drawing.Icon("TheAppIcon.ico");

                //notifyIcon.Click += new EventHandler(notifyIcon_Click);

                notifyIcon.Visible = true;

                //打开菜单项
                //System.Windows.Forms.MenuItem open = new System.Windows.Forms.MenuItem("Open");
                //open.Click += new EventHandler((o, e) =>
                //{
                //    this.Show();
                //});

                //退出菜单项
                System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("Exit");
                exit.Click += new EventHandler(Exit_Button_Click);

                //关联托盘控件
                //System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { open, exit };

                System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { exit };

                notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);

                //this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler((o, e) =>
                //{
                //    if (e.Button == MouseButtons.Left) this.Show();
                //});

                int tipShowMilliseconds = 0;
                string tipTitle = "CloudMoe Windows 10 Activation Toolkit V2";
                string tipContent = "Running.";
                ToolTipIcon tipType = ToolTipIcon.None;
                notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);

                //notifyIcon.BalloonTipClicked += new EventHandler((o, e) =>
                //{
                //    //System.Windows.MessageBox.Show(System.Windows.Forms.Control.MouseButtons.ToString());
                //    if (System.Windows.Forms.Control.MouseButtons == MouseButtons.None) //左键返回不是Right是None
                //    {
                //        System.Windows.MessageBox.Show("Hello");
                //    };
                //});
            }

            //初始化动态表单数据绑定
            DataContext = new ViewModel();

            this.DialogHostGrid.Visibility = Visibility.Visible;

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

        bool is_auto = true; //是否为自动模式，false为手动

        private void InvokeTest()
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                LoadOSList();
            }));
        }
        private void LoadOSList()
        {

            int is_selected = 0; //是否已经自动选择,0未选择，1普通模式，2实验模式

            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                DialogWait.IsOpen = true;
            }));
            try
            {
                string json = GetHttpWebRequest("https://kms.kumo.moe/api/digital?list=1&ver=2");
                JObject jsonobj = JObject.Parse(json);
                List<Frequency> list = new List<Frequency>();
                Frequency freq = new Frequency();
                ositems = (JArray)jsonobj["OS"];

                for (int i = 0; i < ositems.Count(); i++)
                {
                    freq.ID = i;
                    freq.DisplayOS = jsonobj["OS"][i].ToString();

                    //按照优先级判断，如果已经自动选择则忽略新的
                    if (String.Equals(jsonobj["OS"][i].ToString(), SystemEdition + OSVersionInfo.BuildVersion, StringComparison.CurrentCultureIgnoreCase) && is_selected == 0)//jsonobj["OS"][i].ToString() == SystemEdition + OSVersionInfo.BuildVersion
                    {
                        now_os_index = i;
                        checked_os = SystemEdition + OSVersionInfo.BuildVersion;
                        is_selected = 1;
                    }

                    if (String.Equals(jsonobj["OS"][i].ToString(), SystemEdition, StringComparison.CurrentCultureIgnoreCase) && is_selected == 0)//jsonobj["OS"][i].ToString() == SystemEdition
                    {
                        now_os_index = i;
                        checked_os = SystemEdition;
                        is_selected = 1;
                    }

                    if (String.Equals(jsonobj["OS"][i].ToString(), "(Experimental) " + SystemEdition, StringComparison.CurrentCultureIgnoreCase) && is_selected == 0)//旧的方法：jsonobj["OS"][i].ToString() == "(Experimental) " + SystemEdition，新方法忽略大小写并提升效率
                    {
                        now_os_index = i;
                        checked_os = "(Experimental) " + SystemEdition;
                        is_selected = 2;
                    }
                    list.Add(freq);
                }

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.SystemEditionText.ItemsSource = list;//控件的ID

                    if (is_selected == 0)//没有匹配
                    {
                        this.SystemEditionText.SelectedIndex = 0;
                        this.DialogWithOKToCloseDialogTitle.Text = "Attention";
                        this.DialogWithOKToCloseDialogText.Text = "Unable to correctly identify your operating system edition, may be not be supported.\r\n(System edition: " + SystemEdition + OSVersionInfo.BuildVersion + ")";
                        this.DialogWithOKToCloseDialog.IsOpen = true;
                    }
                    else if (is_selected == 2)//只找到实验性
                    {
                        this.SystemEditionText.SelectedIndex = 0;
                        this.DialogWithOKToCloseDialogTitle.Text = "Attention";
                        this.DialogWithOKToCloseDialogText.Text = "Only find experimental options that can be used with this operating system edition, little hope of activation success.\r\n(System edition: " + SystemEdition + OSVersionInfo.BuildVersion + ")";
                        this.DialogWithOKToCloseDialog.IsOpen = true;
                    }
                    else
                    {
                        this.SystemEditionText.SelectedIndex = now_os_index;
                    }
                }));

                //this.SystemEditionText.SelectedIndex = now_os_index;

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    DialogWait.IsOpen = false;
                }));

                if (autoact == true)//自动激活
                {
                    Thread actthread = new Thread(RunAct);
                    switch (is_selected)
                    {
                        case 1: //正常
                            actthread.Start();
                            break;
                        case 2: //实验性
                            if (expact == true)
                            {
                                actbtn.Dispatcher.Invoke(new Action(() =>
                                {
                                    DialogWithOKToCloseDialog.IsOpen = false;
                                }));
                                actthread.Start();
                            }
                            else
                            {
                                if (hiderun == true)
                                {
                                    int tipShowMilliseconds = 0;
                                    string tipTitle = "CloudMoe Windows 10 Activation Toolkit V2";
                                    string tipContent = "Your system edition may not be supported, program will exit. you can try add --expact or -e to startup.";
                                    ToolTipIcon tipType = ToolTipIcon.None;
                                    notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                                    Exit_Button_Click(null, null);//退出
                                }
                            }
                            break;
                        default:
                            if (hiderun == true)
                            {
                                int tipShowMilliseconds = 0;
                                string tipTitle = "CloudMoe Windows 10 Activation Toolkit V2";
                                string tipContent = "Your system edition may not be supported, program will exit.";
                                ToolTipIcon tipType = ToolTipIcon.None;
                                notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                                Exit_Button_Click(null, null);//退出
                            }
                            break;
                    }
                }
            }
            catch
            {
                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    DialogWait.IsOpen = false;
                }));

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    DialogWithExit.IsOpen = true;
                }));

                if (hiderun == true && autoact == true)
                {
                    int tipShowMilliseconds = 0;
                    string tipTitle = "CloudMoe Windows 10 Activation Toolkit V2";
                    string tipContent = "Unable to connect to server, program will exit.";
                    ToolTipIcon tipType = ToolTipIcon.None;
                    notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                    Exit_Button_Click(null, null);//退出
                }
            }
        }
        private void Activate_Button_Click(object sender, RoutedEventArgs e)
        {
            Thread actthread = new Thread(RunAct);
            actthread.Start();

            //RunAct();
            //LoadOSList();

            //MessageBox.Show(json);
            //MessageBox.Show(rss["OS"][0].ToString());
            //MessageBox.Show(SystemEdition);
        }

        private void installbtn_Click(object sender, RoutedEventArgs e)
        {
            Thread installthread = new Thread(RunInstall);
            installthread.Start();
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

        private void Exit_Button_Click(object sender, EventArgs e)
        {
            DelectTempFile();
            notifyIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        }

        private void RunInstall()
        {
            ExportTempFile();
            //释放文件
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.DialogActProg.IsOpen = true;
                this.activatingtext.Text = "Converting";
            }));

            Wow64EnableWow64FsRedirection(false);//关闭文件重定向

            string code = "-0";
            string key = "00000-00000-00000-00000-00000";
            string sku = "0";
            string msg = "Unknow Error!";
            string system = "";

            string slmgr = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) + "\\slmgr.vbs";

            string slmgr_self = tempfile + "slmgr.vbs";

            //旧的位置
            //string slmgr_self = System.AppDomain.CurrentDomain.BaseDirectory + "slmgr.vbs";

            string changepk = Environment.SystemDirectory + "\\changepk.exe";

            if (is_auto == true)
            {
                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    system = this.SystemEditionText.Text;
                }));

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = "Getting Key";
                }));

                //获取密钥和SKU
                try
                {

                    string json = GetHttpWebRequest("https://kms.kumo.moe/api/digital?list=0&ver=2");
                    JObject jsonobj = JObject.Parse(json);
                    List<Frequency> list = new List<Frequency>();
                    ositems = (JArray)jsonobj["OS"];
                    key = jsonobj[system]["key"].ToString();
                    sku = jsonobj[system]["sku"].ToString();
                    Console.WriteLine("Edition:" + system + "\r\nKEY:" + key + "\r\nSKU:" + sku);

                }
                catch
                {
                    code = "-0";
                    msg = "激活Windows10需要网络获取产品密钥 :) \nActivate Windows 10 requires a network to gets the product key :)";
                    goto EndLine;
                }
            }
            else
            {

                //手动密钥

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    key = this.SystemEditionTextInput.Text;
                }));

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

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = "Installing Key";
                }));

                //安装数字权利升级密钥
                if (RunCScript(slmgr_self, "-ipk " + key).Trim().EndsWith("successfully."))
                {
                    code = "200";
                }
                else
                {
                    code = "-2";
                    msg = "无法安装密钥，可能没有选择或输入正确的版本 :(\nCannot to install key, may be you choose or enter a incorrect version. :(";
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
                    this.activatingtext.Text = "Converting";
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
                    this.activatingtext.Text = "Converting";
                    this.DialogWithOKToCloseDialogDonate.IsOpen = true;
                    this.DialogWithOKToCloseDialogDonateTitle.Text = "Complete";
                    this.DialogWithOKToCloseDialogDonateText.Text = "\nCongratulation! \n\nWindows 10 has been successful converted.\n";
                }));
                //MessageBox.Show("Congratulation!");
            }
            DelectTempFile();
            //清理文件
        }

        private void ShowBallSameDig()
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                if (hiderun == true && autoact == true)
                {
                    int tipShowMilliseconds = 0;
                    string tipTitle = "CloudMoe Windows 10 Activation Toolkit V2";
                    string tipContent = this.activatingtext.Text;
                    ToolTipIcon tipType = ToolTipIcon.None;
                    notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                }
            }));
        }

        private void RunAct()
        {
            ExportTempFile();
            //释放文件
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.DialogActProg.IsOpen = true;
                this.activatingtext.Text = "Activating";
                ShowBallSameDig();
            }));

            Wow64EnableWow64FsRedirection(false);//关闭文件重定向

            string code = "-0";
            string key = "00000-00000-00000-00000-00000";
            string sku = "0";
            string msg = "Unknow Error!";
            string system = "";
            string mode = "1"; //1：普通（SYS、SKU、KEY完全）；2.需要获取SKU（SYS、KEY）；3.手动输入KEY

            string slmgr = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) + "\\slmgr.vbs";

            string slmgr_self = tempfile + "slmgr.vbs";

            //旧的位置
            //string slmgr_self = System.AppDomain.CurrentDomain.BaseDirectory + "slmgr.vbs";

            string changepk = Environment.SystemDirectory + "\\changepk.exe";

            if (is_auto == true)
            {

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    system = this.SystemEditionText.Text;
                }));

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = "Getting Key";
                    ShowBallSameDig();
                }));

                //获取密钥和SKU
                try
                {

                    string json = GetHttpWebRequest("https://kms.kumo.moe/api/digital?list=0&ver=2");
                    JObject jsonobj = JObject.Parse(json);
                    List<Frequency> list = new List<Frequency>();
                    ositems = (JArray)jsonobj["OS"];
                    key = jsonobj[system]["key"].ToString();
                    sku = jsonobj[system]["sku"].ToString();
                    Console.WriteLine("Edition:" + system + "\r\nKEY:" + key + "\r\nSKU:" + sku);

                    if (sku == "unknow")
                    {
                        mode = "2";
                    }

                }
                catch
                {
                    code = "-0";
                    msg = "激活Windows10需要网络获取产品密钥 :) \nActivate Windows 10 requires a network to gets the product key :)";
                    goto EndLine;
                }

            }
            else
            {

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    key = this.SystemEditionTextInput.Text;
                }));
                mode = "3";

            }

            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.activatingtext.Text = "Uninstalling old Key";
                ShowBallSameDig();
            }));

            //卸载
            string runend = RunCScript(slmgr_self, "-upk").Trim();
            Console.WriteLine(runend);
            if (runend.EndsWith("successfully.") || runend.EndsWith("not found."))
            {

                RunCScript(slmgr_self, "-ckms").Trim();

                if (mode == "2" || mode == "3")
                {

                    actbtn.Dispatcher.Invoke(new Action(() =>
                    {
                        this.activatingtext.Text = "Getting edition code (Experimental)";
                        ShowBallSameDig();
                    }));

                    //安装转换密钥
                    runend = RunCScript(slmgr_self, "-ipk " + key);
                    Console.WriteLine(slmgr_self + " -ipk " + key);
                    Console.WriteLine(runend);
                    if (runend.Trim().EndsWith("successfully."))
                    {
                        Thread.Sleep(6000); //等待6秒，确保SKU生效
                        sku = GetSKU(); //获取SKU
                        if (sku != "Error")
                        {
                            actbtn.Dispatcher.Invoke(new Action(() =>
                            {
                                this.activatingtext.Text = "Uninstalling old Key (Experimental)";
                                ShowBallSameDig();
                            }));

                            runend = RunCScript(slmgr_self, "-upk").Trim();
                            Console.WriteLine(runend);
                            if (runend.EndsWith("successfully.") || runend.EndsWith("not found."))
                            {
                                actbtn.Dispatcher.Invoke(new Action(() =>
                                {
                                    this.activatingtext.Text = "Prepare for the next step (Experimental)";
                                    ShowBallSameDig();
                                }));
                            }
                        }
                        else
                        {
                            code = "-1.2";
                            msg = "无法获取版本代号 :(\nCannot to get edition code. :(";
                            goto EndLine;
                        }
                    }
                    else
                    {
                        code = "-1.1";
                        msg = "无法安装密钥，可能没有选择或输入正确的版本 :(\nCannot to install key, may be you choose or enter a incorrect version. :(";
                        goto EndLine;
                    }
                }

                //写入Win7特征
                //ChangePKAction(changepk + " /ProductKey " + key);

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = "Writing feature of old Windows version";
                    ShowBallSameDig();
                }));

                RunCMD(@"reg add ""HKLM\SYSTEM\Tokens"" /v ""Channel"" /t REG_SZ /d ""Retail"" /f");
                RunCMD(@"reg add ""HKLM\SYSTEM\Tokens\Kernel"" /v ""Kernel-ProductInfo"" /t REG_DWORD /d " + sku + " /f");
                RunCMD(@"reg add ""HKLM\SYSTEM\Tokens\Kernel"" /v ""Security-SPP-GenuineLocalStatus"" /t REG_DWORD /d 1 /f");
                RunCMD(@"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"" /v ""C:\gatherosstate.exe"" /d ""~ WIN7RTM"" /f");

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = "Installing Key";
                    ShowBallSameDig();
                }));

                //安装数字权利升级密钥
                runend = RunCScript(slmgr_self, "-ipk " + key);
                Console.WriteLine(slmgr_self + " -ipk " + key);
                Console.WriteLine(runend);
                if (runend.Trim().EndsWith("successfully."))
                {

                    actbtn.Dispatcher.Invoke(new Action(() =>
                    {
                        this.activatingtext.Text = "Getting free upgrade permissions";
                        ShowBallSameDig();
                    }));

                    RunCMD(tempfile + "gatherosstate.exe");

                    //旧的位置
                    //RunCMD(System.AppDomain.CurrentDomain.BaseDirectory + "gatherosstate.exe"); tempfile

                    for (int i = 0; i < 3 || !File.Exists(tempfile + "GenuineTicket.xml"); i++) //旧的位置： for (int i = 0; i < 3 || !File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "GenuineTicket.xml"); i++)
                    {
                        Thread.Sleep(3000);
                    }

                    if (File.Exists(tempfile + "GenuineTicket.xml")) //旧的位置： if (File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "GenuineTicket.xml"))
                    {

                        actbtn.Dispatcher.Invoke(new Action(() =>
                        {
                            this.activatingtext.Text = "Cleaning changes";
                            ShowBallSameDig();
                        }));

                        RunCMD(@"reg delete ""HKLM\SYSTEM\Tokens"" /f");
                        RunCMD(@"reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"" /v ""C:\gatherosstate.exe"" /f");

                        actbtn.Dispatcher.Invoke(new Action(() =>
                        {
                            this.activatingtext.Text = "Getting digital license";
                            ShowBallSameDig();
                        }));

                        Wow64EnableWow64FsRedirection(false);//关闭文件重定向
                        RunCMD(Environment.SystemDirectory + @"\ClipUp.exe -v -o -altto " + tempfile);

                        actbtn.Dispatcher.Invoke(new Action(() =>
                        {
                            this.activatingtext.Text = "Activating";
                            ShowBallSameDig();
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
                        msg = "执行超时，可能没有选择正确或输入的版本 :(\nTime out, may be you choose or enter a incorrect version. :(";
                    }
                }
                else
                {
                    code = "-2";
                    msg = "无法安装密钥，可能没有选择或输入正确的版本 :(\nCannot to install key, may be you choose or enter a incorrect version. :(";
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
                    if (hiderun == true && autoact == true)
                    {
                        int tipShowMilliseconds = 0;
                        string tipTitle = "CloudMoe Windows 10 Activation Toolkit V2";
                        string tipContent = msg;
                        ToolTipIcon tipType = ToolTipIcon.None;
                        notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                        Exit_Button_Click(null, null);
                    }
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
                    this.DialogWithOKToCloseDialogDonateTitle.Text = "Complete";
                    this.DialogWithOKToCloseDialogDonateText.Text = "\nCongratulation! \n\nWindows 10 has been successful activated.\n";
                    if (hiderun == true && autoact == true)
                    {
                        int tipShowMilliseconds = 0;
                        string tipTitle = "CloudMoe Windows 10 Activation Toolkit V2";
                        string tipContent = "Congratulation!\nWindows 10 has been successful activated.";
                        ToolTipIcon tipType = ToolTipIcon.None;
                        notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                        Exit_Button_Click(null, null);
                    }
                }));
                //MessageBox.Show("Congratulation!");
            }
            DelectTempFile();
            //清理文件
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
            p.StandardInput.WriteLine(var);
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

        public static string GetSKU()
        {
            Wow64EnableWow64FsRedirection(false);//关闭文件重定向
                                                 //执行命令行函数
            try
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = "cmd.exe";//要执行的程序名称 
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.Arguments = "/c wmic os get OperatingSystemSKU";
                //myProcessStartInfo.Arguments = "/c chcp 65001 > nul && cmd /c \"" + PHPRuntimePath + "\" \"" + path + "\" " + var;
                //myProcessStartInfo.Arguments = "/c " & Commands
                p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                p.Start();
                p.WaitForExit(120 * 1000);
                System.IO.StreamReader myStreamReader = p.StandardOutput;
                string myString = myStreamReader.ReadToEnd();
                p.Close();
                myString = Regex.Replace(myString, @"[^0-9]+", "");
                Console.WriteLine("Get SKU:\"" + myString + "\"");
                return myString; //只保留数字SKU
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

        string last_key = "";

        private void SystemEditionTextInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SystemEditionTextInput.Text != last_key)
            {
                int selectlen = SystemEditionTextInput.SelectionStart;
                string temp = SystemEditionTextInput.Text;
                temp = Regex.Replace(temp, @"[^a-zA-Z0-9]+", "");//XAML禁用输入法，并替换可能粘贴进的意外字符
                temp = Regex.Replace(temp, @"([a-zA-Z0-9]{5}(?!$))", "$1-");
                //temp = string.Join("-", Regex.Matches(temp, @".....").Cast<Match>().ToList());
                SystemEditionTextInput.Text = temp.ToUpper();
                last_key = SystemEditionTextInput.Text;
                SystemEditionTextInput.SelectionStart = SystemEditionTextInput.Text.Length;
            }
        }

        private void A_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SystemEditionText.Visibility = Visibility.Visible;
            SystemEditionTextInput.Visibility = Visibility.Hidden;
            is_auto = true;
        }

        private void M_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SystemEditionText.Visibility = Visibility.Hidden;
            SystemEditionTextInput.Visibility = Visibility.Visible;
            is_auto = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DelectTempFile();
            notifyIcon.Visible = false;
        }
    }
}
