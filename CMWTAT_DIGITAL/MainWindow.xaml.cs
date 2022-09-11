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
using System.Globalization;
using System.Reflection;
using MaterialDesignThemes.Wpf;

namespace CMWTAT_DIGITAL
{

    static class Constants
    {
        public const string DefaultLang = "en"; // 缺省语言
    }

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

        public static void ConsoleLog(string log_text = "")
        {
            Console.WriteLine(log_text);
            if (Program.log2file == true)
            {
                WriteLog(log_text);
            }
        }

        public static void WriteLog(string strLog)
        {
            string sFilePath = AppDomain.CurrentDomain.BaseDirectory;
            string sFileName = "CMWTAT-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            sFileName = sFilePath + sFileName; //文件的绝对路径
            if (!Directory.Exists(sFilePath))//验证路径是否存在
            {
                Directory.CreateDirectory(sFilePath);
                //不存在则创建
            }
            FileStream fs;
            StreamWriter sw;
            if (File.Exists(sFileName))
            //验证文件是否存在，有则追加，无则创建
            {
                fs = new FileStream(sFileName, FileMode.Append, FileAccess.Write);
            }
            else
            {
                fs = new FileStream(sFileName, FileMode.Create, FileAccess.Write);
            }
            sw = new StreamWriter(fs);
            sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "   ---   " + strLog);
            sw.Close();
            fs.Close();
        }

        string tempfile = System.IO.Path.GetTempPath() + @"CMWTAT_DIGITAL\";

        public void DelectTempFile()
        {
            if (Directory.Exists(tempfile))
            {
                try
                {
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
                    ConsoleLog("DelectTempFile:" + e.Message);
                }
            }
        }

        public void ExportTempFile()
        {
            if (Directory.Exists(tempfile))
            {
                ConsoleLog("找到已存在的缓存，开始删除");
                DelectTempFile();
                ConsoleLog("删除操作完毕");
                if (Directory.Exists(tempfile))
                {
                    ConsoleLog("[警告] 检测到文件依旧存在");
                }
            }

            ConsoleLog("开始创建缓存目录");
            if (Directory.Exists(tempfile) == false)
            {
                Directory.CreateDirectory(tempfile);
            }
            ConsoleLog("创建缓存目录完毕");

            ConsoleLog("开始写入缓存文件");
            File.WriteAllBytes(tempfile + "ClipUp" + ".exe", Properties.Resources.ClipUp);
            //File.WriteAllBytes(tempfile + "LibHWIDx86" + ".dll", Properties.Resources.LibHWIDx86);
            //File.WriteAllBytes(tempfile + "LibHWIDx64" + ".dll", Properties.Resources.LibHWIDx64);
            File.WriteAllBytes(tempfile + "slmgr" + ".vbs", Properties.Resources.slmgr);
            ConsoleLog("写入缓存文件完毕");
        }

        public static string LocalLang = Constants.DefaultLang;
        public static string NowLang = LocalLang;
        ResourceDictionary langRd = null; //语言资源字典
        public static CultureInfo currentCultureInfo = CultureInfo.CurrentCulture; //获取系统语言
        public static bool NotSupportLang = false;

        /// <summary>
        /// 加载指定语言（支持热加载）
        /// </summary>
        public void LoadLang(string LangName = Constants.DefaultLang)
        {

            //MessageBox.Show(currentCultureInfo.Name);

            try
            {
                //根据名字载入语言文件
                langRd = System.Windows.Application.LoadComponent(new Uri(@"Lang\" + LangName + ".xaml", UriKind.Relative)) as ResourceDictionary;
                NowLang = LangName;
                if (LangName != Constants.DefaultLang)
                {
                    LocalLang = LangName;
                    //btnChangeLang.Tag = LocalLang;
                }
                NotSupportLang = false;
            }
            catch
            {
                NotSupportLang = true;
                //System.Windows.MessageBox.Show("The " + LangName + " language pack was not found and the language was set to English.\nIf you want to use Chinese as the interface language, click the \"Language\" button to switch.");
                langRd = System.Windows.Application.LoadComponent(new Uri(@"Lang\" + Constants.DefaultLang + ".xaml", UriKind.Relative)) as ResourceDictionary;
                NowLang = Constants.DefaultLang;
            }

            if (langRd != null)
            {
                //如果已使用其他语言,先清空
                if (this.Resources.MergedDictionaries.Count > 0)
                {
                    this.Resources.MergedDictionaries.Clear();
                }
                this.Resources.MergedDictionaries.Add(langRd);
            }
        }

        private static void ApplyBase(bool isDark)
        {
            var theme = new PaletteHelper().GetTheme();
            theme.SetBaseTheme(isDark ? Theme.Dark : Theme.Light);
            new PaletteHelper().SetTheme(theme);
        }

        string ProductVersion = "0.0.0.0"; // 存储程序版本

        /// <summary>
        /// 获取当前版本
        /// </summary>
        private void GetEdition()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            ConsoleLog("AppAssemblyFullName: " + assembly.FullName);

            // 获取程序集元数据 
            AssemblyCopyrightAttribute copyright = (AssemblyCopyrightAttribute)
            AssemblyCopyrightAttribute.GetCustomAttribute(Assembly.GetExecutingAssembly(),
            typeof(AssemblyCopyrightAttribute));
            AssemblyDescriptionAttribute description = (AssemblyDescriptionAttribute)
            AssemblyDescriptionAttribute.GetCustomAttribute(System.Reflection.Assembly.GetExecutingAssembly(),
            typeof(AssemblyDescriptionAttribute));

            ProductVersion = System.Windows.Forms.Application.ProductVersion;

            ConsoleLog("AppDescription: " + description.Description);
            ConsoleLog("AppCopyright: " + copyright.Copyright);
            ConsoleLog("AppProductVersion: " + System.Windows.Forms.Application.ProductVersion);
        }

        //static bool autoact = false;
        //static bool hiderun = false;
        //static bool expact = false;
        //static bool log2file = false;
        //static bool showhelp = false;

        //public string SystemEdition = OSVersionInfo.Name + " " + OSVersionInfo.Edition;
        public string SystemEdition = OSVersionInfo.Edition;

        bool isDark = false;

        string WindowsTheme = "Light";

        NotifyIcon notifyIcon;

        public void CheckWindowsTheme()
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            Windows.UI.Color Wcolor = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
            System.Drawing.Color Scolor = System.Drawing.Color.FromArgb(Wcolor.R, Wcolor.G, Wcolor.B);
            float hue = Scolor.GetHue(); // 色调
            float saturation = Scolor.GetSaturation(); // 饱和度
            float lightness = Scolor.GetBrightness(); // 亮度

            if (lightness > 0.75)
            {
                isDark = false;
                WindowsTheme = "Light";
            }
            else
            {
                isDark = true;
                WindowsTheme = "Dark";
            }

            //ConsoleLog("Windows Theme Background is: " + Wcolor);
            //ConsoleLog("Windows Theme Brightness is: " + lightness);
            //ConsoleLog("Windows Theme Mode is: " + WindowsTheme);
        }

        public MainWindow()
        {
            CheckWindowsTheme();

            ConsoleLog("Windows Theme Mode is: " + WindowsTheme);

            GetEdition(); // 获取程序版本

            //autoact = Program.autoact;
            //hiderun = Program.hiderun;
            //expact = Program.expact;
            //log2file = Program.log2file;
            //showhelp = Program.showhelp;

            //MessageBox.Show("A:" + autoact.ToString() + ";H:" + hiderun.ToString());

            InitializeComponent();

            ApplyBase(isDark);

            string LangName = currentCultureInfo.Name;
            //根据本地语言来进行本地化
            LangName = LangName.Substring(0, LangName.IndexOf("-"));
            //LangName = "ja"; // 如需测试语言，请取消注释此行
            LoadLang(LangName);

            this.Title = this.Title + " V" + ProductVersion; // 初始化语言后为标题增加版本号

            //System.Windows.MessageBox.Show((string)this.Resources["HelpText"]);

            if (Program.showhelp == true)
            {
                DialogHelp.IsOpen = true;
            }

            notifyIcon = new System.Windows.Forms.NotifyIcon(); // 先初始化托盘图标，以方便语言缺省时提示
            notifyIcon.Text = (string)this.Resources["notifyIconTitle"]; //托盘图标标题
            notifyIcon.Icon = ((System.Drawing.Icon)(CMWTAT_DIGITAL.Properties.Resources.CMWTAT_ICON));

            if ((Program.hiderun == true && Program.autoact == true) || NotSupportLang == true)
            {

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
            }

            if (NotSupportLang == true)
            {
                int tipShowMilliseconds = 0;
                string tipTitle = (string)this.Resources["notifyIconTitle"];
                string tipContent = "The language pack \"" + LangName + "\" was not found, language has been automatically switched to English. You can submit this language on GitHub."; // 提示不支持语言提示
                ToolTipIcon tipType = ToolTipIcon.None;
                notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
            }

            if (Program.hiderun == true && Program.autoact == true)
            {
                this.Hide();

                int tipShowMilliseconds = 0;
                string tipTitle = (string)this.Resources["notifyIconTitle"]; //通知气泡标题
                string tipContent = (string)this.Resources["Running"]; //提示正在运行
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

            CheckWindowsCore();
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

        /// <summary>
        /// 检查更新
        /// </summary>
        private void CheckUpdate()
        {
            try
            {
                string check_update_json = GetHttpWebRequest("https://cmwtat.cloudmoe.com/api/check_update?version=" + ProductVersion);
                JObject check_update_jsonobj = JObject.Parse(check_update_json);
                List<Frequency> check_update_list = new List<Frequency>();
                JValue latest_version = (JValue)check_update_jsonobj["latest"];
                JValue oldest_version = (JValue)check_update_jsonobj["oldest"];
                //System.Windows.MessageBox.Show(latest_version.ToString());
                Version CurrentVersion = new Version(ProductVersion);
                Version LatestVersion = new Version(latest_version.ToString());
                Version AllowedVersion = new Version(oldest_version.ToString());
                if (CurrentVersion >= LatestVersion) // 当前版本大于等于最新版本
                {
                    //System.Windows.MessageBox.Show("无需更新");
                }
                if (CurrentVersion < LatestVersion) // 当前版本小于最新版本
                {
                    actbtn.Dispatcher.Invoke(new Action(() =>
                    {
                        if (CurrentVersion < AllowedVersion) // 当前版本小于最低允许版本
                        {
                            this.IgnoreUpdate.IsEnabled = false;
                            //System.Windows.MessageBox.Show("必须更新");
                        }
                        else
                        {
                            this.IgnoreUpdate.IsEnabled = true;
                        }
                        this.DialogUpdateTitle.Text = (string)this.Resources["UpdateTitle"];
                        this.DialogUpdateText.Text = (string)this.Resources["UpdateText"] + "\r\n" + (string)this.Resources["CurrentVersion"] + ": " + ProductVersion + "\r\n" + (string)this.Resources["LatestVersion"] + ": " + latest_version.ToString();
                        this.DialogUpdate.IsOpen = true;
                    }));
                    //System.Windows.MessageBox.Show("需要更新");
                }
            }
            catch
            {
            }
        }

        public static string MainServerDomain = "https://cmwtat.cloudmoe.com"; // 主要服务器
        public static string BackupServerDomain = "https://kms.kumo.moe"; // 备用服务器

        private void LoadOSList()
        {

            int is_selected = 0; //是否已经自动选择,0未选择，1普通模式，2实验模式，3离线KMS模式

            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                DialogWait.IsOpen = true;
            }));

            try
            {
                string json;
                try
                {
                    json = GetHttpWebRequest(MainServerDomain + "/api/digital?list=1&ver=3"); // 主要服务器
                }
                catch (Exception e)
                {
                    ConsoleLog("MainServer:" + MainServerDomain + " is not working.");
                    ConsoleLog("Error Message:" + e.Message);
                    ConsoleLog("Ready to use BackupServer:" + BackupServerDomain);
                    json = GetHttpWebRequest(BackupServerDomain + "/api/digital?list=1&ver=3"); // 备用服务器
                }
                JObject jsonobj = JObject.Parse(json);
                List<Frequency> list = new List<Frequency>();
                Frequency freq = new Frequency();
                ositems = (JArray)jsonobj["OS"];

                for (int i = 0; i < ositems.Count(); i++)
                {
                    freq.ID = i;
                    freq.DisplayOS = jsonobj["OS"][i].ToString();

                    //按照优先级判断，如果已经自动选择则忽略新的
                    //选择带版本号
                    if (String.Equals(jsonobj["OS"][i].ToString(), SystemEdition + OSVersionInfo.BuildVersion, StringComparison.CurrentCultureIgnoreCase) && is_selected == 0)//jsonobj["OS"][i].ToString() == SystemEdition + OSVersionInfo.BuildVersion
                    {
                        now_os_index = i;
                        checked_os = SystemEdition + OSVersionInfo.BuildVersion;
                        is_selected = 1;
                    }

                    //选择带版本号Offline-KMS
                    if (String.Equals(jsonobj["OS"][i].ToString(), "(Offline-KMS) " + SystemEdition + OSVersionInfo.BuildVersion, StringComparison.CurrentCultureIgnoreCase) && is_selected == 0)//旧的方法：jsonobj["OS"][i].ToString() == "(Experimental) " + SystemEdition，新方法忽略大小写并提升效率
                    {
                        now_os_index = i;
                        checked_os = "(Offline-KMS) " + SystemEdition + OSVersionInfo.BuildVersion;
                        is_selected = 3;
                    }

                    //选择不带版本号
                    if (String.Equals(jsonobj["OS"][i].ToString(), SystemEdition, StringComparison.CurrentCultureIgnoreCase) && is_selected == 0)//jsonobj["OS"][i].ToString() == SystemEdition
                    {
                        now_os_index = i;
                        checked_os = SystemEdition;
                        is_selected = 1;
                    }

                    //选择不带版本号Offline-KMS
                    if (String.Equals(jsonobj["OS"][i].ToString(), "(Offline-KMS) " + SystemEdition, StringComparison.CurrentCultureIgnoreCase) && is_selected == 0)//旧的方法：jsonobj["OS"][i].ToString() == "(Experimental) " + SystemEdition，新方法忽略大小写并提升效率
                    {
                        now_os_index = i;
                        checked_os = "(Offline-KMS) " + SystemEdition;
                        is_selected = 3;
                    }

                    //选择不带版本号实验
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
                        this.DialogWithOKToCloseDialogTitle.Text = (string)this.Resources["Attention"];
                        this.DialogWithOKToCloseDialogText.Text = (string)this.Resources["May_be_not_be_supported"] + "\r\n(" + (string)this.Resources["System_Edition"] + ": " + SystemEdition + OSVersionInfo.BuildVersion + ")";
                        this.DialogWithOKToCloseDialog.IsOpen = true;
                    }
                    else if (is_selected == 2)//只找到实验性
                    {
                        this.SystemEditionText.SelectedIndex = now_os_index;
                        this.DialogWithOKToCloseDialogTitle.Text = (string)this.Resources["Attention"];
                        this.DialogWithOKToCloseDialogText.Text = (string)this.Resources["Only_find_experimental"] + "\r\n(" + (string)this.Resources["System_Edition"] + ": " + SystemEdition + OSVersionInfo.BuildVersion + ")";
                        this.DialogWithOKToCloseDialog.IsOpen = true;
                    }
                    else if (is_selected == 3)//只找到长期KMS
                    {
                        this.SystemEditionText.SelectedIndex = now_os_index;
                        this.DialogWithOKToCloseDialogTitle.Text = (string)this.Resources["Attention"];
                        this.DialogWithOKToCloseDialogText.Text = (string)this.Resources["Only_find_ltok"] + "\r\n(" + (string)this.Resources["System_Edition"] + ": " + SystemEdition + OSVersionInfo.BuildVersion + ")";
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

                if (Program.autoact == true)//自动激活
                {
                    Thread actthread = new Thread(RunAct);
                    switch (is_selected)
                    {
                        case 1: //正常
                            actthread.Start();
                            break;
                        case 2: //实验性
                            if (Program.expact == true)
                            {
                                actbtn.Dispatcher.Invoke(new Action(() =>
                                {
                                    DialogWithOKToCloseDialog.IsOpen = false;
                                }));
                                actthread.Start();
                            }
                            else
                            {
                                if (Program.hiderun == true)
                                {
                                    int tipShowMilliseconds = 0;
                                    string tipTitle = (string)this.Resources["notifyIconTitle"];
                                    string tipContent = (string)this.Resources["notify_May_be_not_be_supported_try"]; //提示不支持可尝试实验性
                                    ToolTipIcon tipType = ToolTipIcon.None;
                                    notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                                    Exit_Button_Click(null, null);//退出
                                }
                            }
                            break;
                        default:
                            if (Program.hiderun == true)
                            {
                                int tipShowMilliseconds = 0;
                                string tipTitle = (string)this.Resources["notifyIconTitle"];
                                string tipContent = (string)this.Resources["notify_May_be_not_be_supported_exit"]; //提示不支持并退出（实验性开启）
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

                if (Program.hiderun == true && Program.autoact == true)
                {
                    int tipShowMilliseconds = 0;
                    string tipTitle = (string)this.Resources["notifyIconTitle"];
                    string tipContent = (string)this.Resources["notify_Disconnect_to_server_exit"]; //提示无法连接服务器退出
                    ToolTipIcon tipType = ToolTipIcon.None;
                    notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                    Exit_Button_Click(null, null);//退出
                }
            }
            CheckUpdate(); // 检查更新
        }

        private void CheckWindowsCore()
        {
            if (SystemEdition.ToLower().IndexOf("core") == -1)
            {
                upgradefullbtn.IsEnabled = false;
                upgradefullbtn.Visibility = Visibility.Collapsed;
                this.Height -= 95;
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
        
        private void upgradefullbtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogUpgradeFullVersion.IsOpen = true;
        }
        
        private void UpgradeFullVersionWindows_Click(object sender, RoutedEventArgs e)
        {
            this.DialogUpgradeFullVersion.IsOpen = false;
            Thread upgradethread = new Thread(RunUpgradeFullVersion);
            upgradethread.Start();
        }

        private string GetHttpWebRequest(string url, int timeout = 10000, int retry = 2)
        {
            string outex = "UnknowError";
            for (int i = 0; i < retry; i++) // 默认重试2次
            {
                ConsoleLog("GetHttpWebRequest Try: " + i.ToString());
                try
                {
                    Uri uri = new Uri(url);
                    HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(uri);
                    myReq.UserAgent = "User-Agent:Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705";
                    myReq.Accept = "*/*";
                    myReq.KeepAlive = true;
                    myReq.Headers.Add("Accept-Language", "zh-cn,en-us;q=0.5");
                    myReq.Timeout = timeout; // 默认10s超时
                    HttpWebResponse result = null;
                    string strHTML = null;
                    result = (HttpWebResponse)myReq.GetResponse();
                    Stream receviceStream = result.GetResponseStream();
                    StreamReader readerOfStream = new StreamReader(receviceStream, System.Text.Encoding.GetEncoding("utf-8"));
                    strHTML = readerOfStream.ReadToEnd();
                    readerOfStream.Close();
                    receviceStream.Close();
                    result.Close();
                    return strHTML;
                }
                catch (WebException ex)
                {
                    outex = ex.Message;
                    ConsoleLog("GetHttpWebRequest Exception: " + ex.Message);
                    if (ex.Status == WebExceptionStatus.Timeout) // 超时重试
                    {
                        continue;
                    }
                    throw new Exception(ex.Message); // 其他错误抛出
                }
            }
            throw new Exception(outex);
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
                this.activatingtext.Text = (string)this.Resources["RunInstall_Converting"]; //提示转换中
            }));

            Wow64EnableWow64FsRedirection(false);//关闭文件重定向

            string code = "-0";
            string key = "00000-00000-00000-00000-00000";
            string sku = "0";
            string msg = "Unknow Error!";
            string system = "";

            string slmgr = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) + "\\slmgr.vbs";

            string slmgr_self = tempfile + "slmgr.vbs";

            try
            {
                string sourceFile = slmgr;
                string targetFile = slmgr_self;
                bool isrewrite = true; // true=覆盖已存在的同名文件,false则反之
                ConsoleLog("Copy Start: " + sourceFile + " To " + targetFile);
                System.IO.File.Copy(sourceFile, targetFile, isrewrite);
                ConsoleLog("Copy Completed.");
            }
            catch (Exception CopyExc)
            {
                ConsoleLog("Copy has Exception: " + CopyExc.Message);
            }

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
                    this.activatingtext.Text = (string)this.Resources["RunInstall_Getting_Key"]; //提示正在获取密钥
                }));

                //获取密钥和SKU
                try
                {

                    string json;
                    try
                    {
                        json = GetHttpWebRequest(MainServerDomain + "/api/digital?list=0&ver=3"); // 主要服务器
                    }
                    catch (Exception e)
                    {
                        ConsoleLog("MainServer:" + MainServerDomain + " is not working.");
                        ConsoleLog("Error Message:" + e.Message);
                        ConsoleLog("Ready to use BackupServer:" + BackupServerDomain);
                        json = GetHttpWebRequest(BackupServerDomain + "/api/digital?list=0&ver=3"); // 备用服务器
                    }
                    JObject jsonobj = JObject.Parse(json);
                    List<Frequency> list = new List<Frequency>();
                    ositems = (JArray)jsonobj["OS"];
                    key = jsonobj[system]["key"].ToString();
                    sku = jsonobj[system]["sku"].ToString();
                    ConsoleLog("Edition:" + system + "\r\nKEY:" + key + "\r\nSKU:" + sku);

                }
                catch
                {
                    code = "-0";
                    msg = (string)this.Resources["ErrorMsg-0"]; // "激活Windows10需要网络获取产品密钥 :) \nActivate Windows 10 requires a network to gets the product key :)";
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
                this.activatingtext.Text = (string)this.Resources["RunInstall_Uninstalling_old_Key"]; //提示正在卸载旧密钥
            }));
            //卸载
            string runend = RunCScript(slmgr_self, "-upk").Trim();
            //string runend = RunCMD(@"cscript.exe /nologo %systemroot%\system32\slmgr.vbs -upk").Trim();
            ConsoleLog(runend);
            if (runend.EndsWith("successfully.") || runend.EndsWith("not found."))
            {

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = (string)this.Resources["RunInstall_Installing_Key"]; //提示正在安装密钥
                }));

                //安装数字权利升级密钥
                if (RunCScript(slmgr_self, "-ipk " + key).Trim().EndsWith("successfully."))
                //if (RunCMD(@"cscript.exe /nologo %systemroot%\system32\slmgr.vbs -ipk " + key).Trim().EndsWith("successfully."))
                {
                    code = "200";
                }
                else
                {
                    code = "-2";
                    msg = (string)this.Resources["ErrorMsg-2"]; // "无法安装密钥，可能没有选择或输入正确的版本 :(\nCannot to install key, may be you choose or enter a incorrect version. :(";
                }
            }
            else
            {
                code = "-1";
                msg = (string)this.Resources["ErrorMsg-1"]; // "无法卸载旧密钥 :(\nCannot to uninstall old key. :(";
            }
        //string runend = RunCScript(slmgr_self, "-upk").Trim();
        EndLine:;
            if (code != "200")
            {
                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.DialogActProg.IsOpen = false;
                    this.activatingtext.Text = (string)this.Resources["RunInstall_Converting"]; //提示转换中
                    this.DialogWithOKToCloseDialog.IsOpen = true;
                    this.DialogWithOKToCloseDialogTitle.Text = (string)this.Resources["ErrorTitle"]; //错误标题
                    this.DialogWithOKToCloseDialogText.Text = msg + "\r\n" + (string)this.Resources["ErrorCode"] + code; //错误代码 如：错误信息\r\nCode：000
                }));
                //MessageBox.Show(msg + "\r\nCode:" + code);
            }
            else
            {
                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.DialogActProg.IsOpen = false;
                    this.activatingtext.Text = (string)this.Resources["RunInstall_Converting"]; //提示转换中
                    this.DialogWithOKToCloseDialogDonate.IsOpen = true;
                    this.DialogWithOKToCloseDialogDonateTitle.Text = (string)this.Resources["CompleteTitle"]; //完成标题
                    this.DialogWithOKToCloseDialogDonateText.Text = (string)this.Resources["DonateTextConverted"]; //完成转换内容
                }));
                //MessageBox.Show("Congratulation!");
            }
            DelectTempFile();
            //清理文件
        }

        private void RunUpgradeFullVersion()
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.DialogActProg.IsOpen = true;
                this.activatingtext.Text = (string)this.Resources["RunUpgradeFullVersion_Upgrading"]; //提示升级中
            }));
            RunCMD(@"sc start sppsvc");
            RunCMD(@"sc start wuauserv");
            RunCLI("ChangePK.exe", ".", "/ProductKey VK7JG-NPHTM-C97JM-9MPGT-3V66T"); // Pro
            RunCLI("ChangePK.exe", ".", "/ProductKey 2B87N-8KFHP-DKV6R-Y2C8J-PKCKT"); // Pro N
            RunCLI("ChangePK.exe", ".", "/ProductKey W269N-WFGWX-YVC9B-4J6C9-T83GX"); // KMS Pro
            RunCLI("ChangePK.exe", ".", "/ProductKey MH37W-N47XK-V7XM9-C7227-GCQG9"); // KMS Pro N
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.DialogActProg.IsOpen = false;
            }));
        }

        private void ShowBallSameDig()
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                if (Program.hiderun == true && Program.autoact == true)
                {
                    int tipShowMilliseconds = 0;
                    string tipTitle = (string)this.Resources["notifyIconTitle"];
                    string tipContent = this.activatingtext.Text;
                    ToolTipIcon tipType = ToolTipIcon.None;
                    notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                }
            }));
        }

        private void RunAct()
        {
            bool is_not_network_to_act = false; //是否无法联网稍后激活
            ExportTempFile();
            //释放文件
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.DialogActProg.IsOpen = true;
                this.activatingtext.Text = (string)this.Resources["RunAct_Activating"]; //提示激活中
                ShowBallSameDig();
            }));

            Wow64EnableWow64FsRedirection(false);//关闭文件重定向

            string code = "-0";
            string key = "00000-00000-00000-00000-00000";
            string sku = "0";
            string msg = "Unknow Error!";
            string system = "";
            string mode = "1"; //1：普通（SYS、SKU、KEY完全）；2.需要获取SKU（SYS、KEY）；3.手动输入KEY；4.普通OfflineKMS（SYS、SKU、KEY完全）

            string slmgr = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) + "\\slmgr.vbs";

            string slmgr_self = tempfile + "slmgr.vbs";

            try
            {
                string sourceFile = slmgr;
                string targetFile = slmgr_self;
                bool isrewrite = true; // true=覆盖已存在的同名文件,false则反之
                ConsoleLog("Copy Start: " + sourceFile + " To " + targetFile);
                System.IO.File.Copy(sourceFile, targetFile, isrewrite);
                ConsoleLog("Copy Completed.");
            }
            catch (Exception CopyExc)
            {
                ConsoleLog("Copy has Exception: " + CopyExc.Message);
            }

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
                    this.activatingtext.Text = (string)this.Resources["RunAct_Getting_Key"]; //提示正在获取密钥
                    ShowBallSameDig();
                }));

                //获取密钥和SKU
                try
                {

                    string json;
                    try
                    {
                        json = GetHttpWebRequest(MainServerDomain + "/api/digital?list=0&ver=3"); // 主要服务器
                    }
                    catch (Exception e)
                    {
                        ConsoleLog("MainServer:" + MainServerDomain + " is not working.");
                        ConsoleLog("Error Message:" + e.Message);
                        ConsoleLog("Ready to use BackupServer:" + BackupServerDomain);
                        json = GetHttpWebRequest(BackupServerDomain + "/api/digital?list=0&ver=3"); // 备用服务器
                    }
                    JObject jsonobj = JObject.Parse(json);
                    List<Frequency> list = new List<Frequency>();
                    ositems = (JArray)jsonobj["OS"];
                    key = jsonobj[system]["key"].ToString();
                    sku = jsonobj[system]["sku"].ToString();
                    ConsoleLog("Edition:" + system + "\r\nKEY:" + key + "\r\nSKU:" + sku);

                    string selecos = "";
                    // 获取当前选择的选择的文本
                    actbtn.Dispatcher.Invoke(new Action(() =>
                    {
                        selecos = SystemEditionText.Text;
                    }));

                    ConsoleLog("Selected OS: " + selecos);

                    if (sku == "unknow")
                    {
                        mode = "2";
                    }

                    if (selecos.ToUpper().StartsWith("(Offline-KMS)".ToUpper()))
                    {
                        ConsoleLog("Switch Mode Offline-KMS");
                        mode = "4";
                    }

                }
                catch
                {
                    code = "-0";
                    msg = (string)this.Resources["ErrorMsg-0"]; // "激活Windows10需要网络获取产品密钥 :) \nActivate Windows 10 requires a network to gets the product key :)";
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
                sku = "unknow";

            }

            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.activatingtext.Text = (string)this.Resources["RunAct_Uninstalling_old_Key"]; //提示正在卸载旧密钥
                ShowBallSameDig();
            }));

            //卸载
            string runend = RunCScript(slmgr_self, "-upk").Trim();
            //string runend = RunCMD(@"cscript.exe /nologo %systemroot%\system32\slmgr.vbs -upk").Trim();
            ConsoleLog(runend);
            if (runend.EndsWith("successfully.") || runend.EndsWith("not found."))
            {

                RunCScript(slmgr_self, "-ckms").Trim();

                if (mode == "4")
                {
                    //长期KMS
                    RunCMD(@"sc stop sppsvc");

                    RunCMD(@"del /F /Q %systemroot%\system32\spp\store\2.0\tokens.dat");
                    RunCMD(@"del /F /Q %systemroot%\system32\spp\store\2.0\data.dat");
                    RunCMD(@"del /F /Q %systemroot%\system32\spp\store\2.0\cache\cache.dat");

                    RunCMD(@"sc start sppsvc");
                }

                if (sku == "unknow")//if (mode == "2" || mode == "3") //获取SKU
                {

                    actbtn.Dispatcher.Invoke(new Action(() =>
                    {
                        this.activatingtext.Text = (string)this.Resources["RunAct_Getting_edition_code_Exp"]; // "Getting edition code (Experimental)";
                        ShowBallSameDig();
                    }));

                    //安装转换密钥
                    runend = RunCScript(slmgr_self, "-ipk " + key);
                    //runend = RunCMD(@"cscript.exe /nologo %systemroot%\system32\slmgr.vbs -ipk " + key);
                    ConsoleLog(slmgr_self + " -ipk " + key);
                    ConsoleLog(runend);
                    if (runend.Trim().EndsWith("successfully."))
                    {
                        Thread.Sleep(6000); //等待6秒，确保SKU生效
                        sku = GetSKU(); //获取SKU
                        if (sku != "Error")
                        {
                            actbtn.Dispatcher.Invoke(new Action(() =>
                            {
                                this.activatingtext.Text = (string)this.Resources["RunAct_Uninstalling_old_Key_Exp"]; //提示正在卸载旧密钥（实验性）
                                ShowBallSameDig();
                            }));

                            runend = RunCScript(slmgr_self, "-upk").Trim();
                            //runend = RunCMD(@"cscript.exe /nologo %systemroot%\system32\slmgr.vbs -upk").Trim();
                            ConsoleLog(runend);
                            if (runend.EndsWith("successfully.") || runend.EndsWith("not found."))
                            {
                                actbtn.Dispatcher.Invoke(new Action(() =>
                                {
                                    this.activatingtext.Text = (string)this.Resources["RunAct_Prepare_for_the_next_step_Exp"]; // "Prepare for the next step (Experimental)";
                                    ShowBallSameDig();
                                }));
                            }
                        }
                        else
                        {
                            code = "-1.2";
                            msg = (string)this.Resources["ErrorMsg-1.2"]; // "无法获取版本代号 :(\nCannot to get edition code. :(";
                            goto EndLine;
                        }
                    }
                    else
                    {
                        code = "-1.1";
                        msg = (string)this.Resources["ErrorMsg-1.1"]; // "无法安装密钥，可能没有选择或输入正确的版本 :(\nCannot to install key, may be you choose or enter a incorrect version. :(";
                        goto EndLine;
                    }
                }

                //写入Win7特征
                //ChangePKAction(changepk + " /ProductKey " + key);

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = (string)this.Resources["RunAct_Writing_old_OS"]; // "Writing feature of old Windows version";
                    ShowBallSameDig();
                }));

                if (mode == "4")
                {
                    //长期KMS
                    ConsoleLog(RunCScript(slmgr_self, "-skms 1.1.45.14:1919").Trim()); // いいよ、来いよ ｗｗｗ
                    //ConsoleLog(RunCMD(@"cscript.exe /nologo %systemroot%\system32\slmgr.vbs -skms 1.1.45.14:1919").Trim()); // いいよ、来いよ ｗｗｗ
                    //if (runend.EndsWith("successfully."))
                    //{
                    //}
                    RunCMD(@"reg add ""HKLM\SYSTEM\Tokens"" /v ""Channel"" /t REG_SZ /d ""Volume:GVLK"" /f");
                }
                else
                {
                    RunCMD(@"reg add ""HKLM\SYSTEM\Tokens"" /v ""Channel"" /t REG_SZ /d ""Retail"" /f");
                }

                RunCMD(@"reg add ""HKLM\SYSTEM\Tokens\Kernel"" /v ""Kernel-ProductInfo"" /t REG_DWORD /d " + sku + " /f");
                RunCMD(@"reg add ""HKLM\SYSTEM\Tokens\Kernel"" /v ""Security-SPP-GenuineLocalStatus"" /t REG_DWORD /d 1 /f");

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = (string)this.Resources["RunAct_Installing_Key"]; //提示正在安装密钥
                    ShowBallSameDig();
                }));

                //安装数字权利升级密钥
                runend = RunCScript(slmgr_self, "-ipk " + key);
                //runend = RunCMD(@"cscript.exe /nologo %systemroot%\system32\slmgr.vbs -ipk " + key);
                ConsoleLog(slmgr_self + " -ipk " + key);
                ConsoleLog(runend);
                if (runend.Trim().EndsWith("successfully."))
                {

                    actbtn.Dispatcher.Invoke(new Action(() =>
                    {
                        this.activatingtext.Text = (string)this.Resources["RunAct_Getting_free_upgrade_permissions"]; // "Getting free upgrade permissions";
                        ShowBallSameDig();
                    }));

                    //LibGatherOsState.GatherOsState.SetLibHWIDPath(tempfile + "LibHWIDx86.dll", tempfile + "LibHWIDx64.dll");

                    var licenseType = LibGatherOsState.GatherOsState.LicenseType.Retail;

                    if (mode == "4")
                    {
                        //长期KMS
                        licenseType = LibGatherOsState.GatherOsState.LicenseType.GVLK;
                    }
                    
                    var result = LibGatherOsState.GatherOsState.GenActivateLicenseXML(licenseType);

                    if (result.state == LibGatherOsState.GatherOsState.ActivateLicenseXMLResultState.OK)
                    { 
                        File.WriteAllText(tempfile + "GenuineTicket.xml", result.xml, Encoding.UTF8);
                        ConsoleLog("进入下一步");

                        int try_max_count = 30;
                        for (int i = 0; i < try_max_count + 1 && !File.Exists(tempfile + "GenuineTicket.xml"); i++)
                        {
                            Thread.Sleep(1000);
                            ConsoleLog($"检查许可证 重试 {i}/{try_max_count}");
                        }
                    }
                    
                    if (File.Exists(tempfile + "GenuineTicket.xml"))
                    {
                        actbtn.Dispatcher.Invoke(new Action(() =>
                        {
                            this.activatingtext.Text = (string)this.Resources["RunAct_Getting_digital_license"]; // "Getting digital license";
                            ShowBallSameDig();
                        }));

                        RunCMD(@"sc start wuauserv");
                        RunCMD(@"sc start clipsvc");
                        
                        RunCMD(@"clipup -v -o -altto " + tempfile);
                        RunCLI(tempfile + "ClipUp.exe", ".", "-v -o -altto " + tempfile); // 固定版本解决 22H2 后 ARM64 许可证接收问题

                        actbtn.Dispatcher.Invoke(new Action(() =>
                        {
                            this.activatingtext.Text = (string)this.Resources["RunAct_Activating"]; // 提示激活中
                            ShowBallSameDig();
                        }));

                        runend = RunCScript(slmgr_self, "-ato").Trim();
                        
                        ConsoleLog(runend);
                        if (runend.EndsWith("successfully.") || runend.Contains("0xC004F074") || runend.Contains("0xC004C003")) //0xC004F074 是 KMS38 长期激活会出的提示，Error 0xC004C003: The activation server determined that the specified product key is blocked. 是因为未连接激活服务器，下次连接时会自动激活。
                        {
                            if (runend.Contains("0xC004C003"))
                            {
                                is_not_network_to_act = true;
                            }
                            code = "200";
                        }
                        else
                        {
                            code = "-4";
                            msg = (string)this.Resources["ErrorMsg-4"] + "\r\n" + (string)this.Resources["SysMsg"] + "\r\n" + runend; // "激活失败 :(\nActivation Failed. :(";
                        }
                    }
                    else
                    {
                        code = "-3";
                        msg = (string)this.Resources["ErrorMsg-3"]; // "执行超时，可能没有选择正确或输入的版本 :(\nTime out, may be you choose or enter a incorrect version. :(";
                    }
                }
                else
                {
                    code = "-2";
                    msg = (string)this.Resources["ErrorMsg-2"]; // "无法安装密钥，可能没有选择或输入正确的版本 :(\nCannot to install key, may be you choose or enter a incorrect version. :(";
                }
            }
            else
            {
                code = "-1";
                msg = (string)this.Resources["ErrorMsg-1"]; // "无法卸载旧密钥 :(\nCannot to uninstall old key. :(";
            }
        //string runend = RunCScript(slmgr_self, "-upk").Trim();
        EndLine:;
            // 此处确保注册表清理一定进行
            try
            {
                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.activatingtext.Text = (string)this.Resources["RunAct_Cleaning_changes"]; // "Cleaning changes";
                    ShowBallSameDig();
                }));

                RunCMD(@"reg delete ""HKLM\SYSTEM\Tokens"" /f");
            }
            catch
            {
                ConsoleLog("Delete Reg Error");
            }
            if (code != "200")
            {
                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    this.DialogActProg.IsOpen = false;
                    this.activatingtext.Text = (string)this.Resources["RunAct_Activating"]; //提示激活中
                    this.DialogWithOKToCloseDialog.IsOpen = true;
                    this.DialogWithOKToCloseDialogTitle.Text = (string)this.Resources["ErrorTitle"]; //错误标题
                    this.DialogWithOKToCloseDialogText.Text = msg + "\r\n" + (string)this.Resources["ErrorCode"] + code; //错误代码 如：错误信息\r\nCode：000
                    if (Program.hiderun == true && Program.autoact == true)
                    {
                        int tipShowMilliseconds = 0;
                        string tipTitle = (string)this.Resources["notifyIconTitle"];
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
                    this.activatingtext.Text = (string)this.Resources["RunAct_Activating"]; //提示激活中
                    this.DialogWithOKToCloseDialogDonate.IsOpen = true;
                    this.DialogWithOKToCloseDialogDonateTitle.Text = (string)this.Resources["CompleteTitle"]; //完成标题
                    if (is_not_network_to_act == true)
                    {
                        this.DialogWithOKToCloseDialogDonateText.Text = (string)this.Resources["DonateTextWillActivated"]; //即将激活内容
                    }
                    else
                    {
                        this.DialogWithOKToCloseDialogDonateText.Text = (string)this.Resources["DonateTextActivated"]; //完成激活内容
                    }

                    if (Program.hiderun == true && Program.autoact == true)
                    {
                        int tipShowMilliseconds = 0;
                        string tipTitle = (string)this.Resources["notifyIconTitle"];
                        string tipContent = this.DialogWithOKToCloseDialogDonateText.Text;
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

        private void RunCMD_old(string var)
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
            ConsoleLog(var);
            //p.WaitForExit();
            ConsoleLog(p.StandardOutput.ReadToEnd().Trim());
            p.Close();

            //Wow64EnableWow64FsRedirection(false);//关闭文件重定向
            //System.Diagnostics.Process.Start(var);
        }

        public static string RunCLI(string path, string wdPath, string var = "")
        {
            ConsoleLog(path + " " + var);
            Wow64EnableWow64FsRedirection(false);//关闭文件重定向
                                                 //执行命令行函数
            try
            {
                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                myProcess.StartInfo.FileName = path;//要执行的程序名称 
                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.RedirectStandardInput = true;//可能接受来自调用程序的输入信息 
                myProcess.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息 
                myProcess.StartInfo.CreateNoWindow = true;//不显示程序窗口 
                myProcess.StartInfo.Arguments = var;
                myProcess.StartInfo.WorkingDirectory = wdPath;
                //myProcess.Arguments = "/c chcp 65001 > nul && cmd /c \"" + PHPRuntimePath + "\" \"" + path + "\" " + var;
                //myProcess.Arguments = "/c " & Commands
                //myProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                myProcess.Start();
                myProcess.WaitForExit(60 * 1000);
                StreamReader myStreamReader = myProcess.StandardOutput;
                string myString = myStreamReader.ReadToEnd();
                myProcess.Close();
                ConsoleLog(myString.Trim());
                ConsoleLog("执行完毕");
                return myString.Trim();
            }
            catch
            {
                return "Error";
            }
        }

        public static string RunCMD(string var)
        {
            ConsoleLog(var);
            Wow64EnableWow64FsRedirection(false); //关闭文件重定向
            //执行命令行函数
            try
            {
                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                myProcess.StartInfo.FileName = "cmd.exe";//要执行的程序名称 
                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.RedirectStandardInput = true;//可能接受来自调用程序的输入信息 
                myProcess.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息 
                myProcess.StartInfo.CreateNoWindow = true;//不显示程序窗口 
                myProcess.StartInfo.Arguments = "/c chcp 437 > nul && cmd /c \"" + var + "\"";
                //myProcess.Arguments = "/c chcp 65001 > nul && cmd /c \"" + PHPRuntimePath + "\" \"" + path + "\" " + var;
                //myProcess.Arguments = "/c " & Commands
                //myProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                myProcess.Start();
                myProcess.WaitForExit(60 * 1000);
                System.IO.StreamReader myStreamReader = myProcess.StandardOutput;
                string myString = myStreamReader.ReadToEnd();
                myProcess.Close();
                ConsoleLog(myString.Trim());
                return myString.Trim();
            }
            catch
            {
                return "Error";
            }
        }

        public static string RunCScript(string path, string var = "")
        {
            ConsoleLog("CScript" + " " + "//Nologo \"" + path + "\" " + var);
            Wow64EnableWow64FsRedirection(false);//关闭文件重定向
            //执行命令行函数
            try
            {
                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo ProcessStartInfo = new System.Diagnostics.ProcessStartInfo("CScript", "//Nologo \"" + path + "\" " + var);
                ProcessStartInfo.UseShellExecute = false;
                ProcessStartInfo.RedirectStandardOutput = true;
                ProcessStartInfo.CreateNoWindow = true;
                //myProcessStartInfo.Arguments = "/c chcp 65001 > nul && cmd /c \"" + PHPRuntimePath + "\" \"" + path + "\" " + var;
                //myProcessStartInfo.Arguments = "/c " & Commands
                ProcessStartInfo.StandardOutputEncoding = Encoding.UTF8;
                myProcess.StartInfo = ProcessStartInfo;
                myProcess.Start();
                myProcess.WaitForExit(60 * 1000);
                System.IO.StreamReader myStreamReader = myProcess.StandardOutput;
                string myString = myStreamReader.ReadToEnd();
                myProcess.Close();
                ConsoleLog(myString.Trim());
                return myString.Trim();
            }
            catch
            {
                return "Error";
            }
        }

        public static string GetSKU()
        {
            ConsoleLog("Geting SKU");
            Wow64EnableWow64FsRedirection(false);//关闭文件重定向
            //执行命令行函数
            try
            {
                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                myProcess.StartInfo.FileName = "cmd.exe";//要执行的程序名称 
                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.RedirectStandardOutput = true;
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.StartInfo.Arguments = "/c wmic os get OperatingSystemSKU";
                //myProcess.Arguments = "/c chcp 65001 > nul && cmd /c \"" + PHPRuntimePath + "\" \"" + path + "\" " + var;
                //myProcess.Arguments = "/c " & Commands
                myProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                myProcess.Start();
                myProcess.WaitForExit(60 * 1000);
                System.IO.StreamReader myStreamReader = myProcess.StandardOutput;
                string myString = myStreamReader.ReadToEnd();
                myProcess.Close();
                myString = Regex.Replace(myString, @"[^0-9]+", "");
                ConsoleLog("Get SKU:\"" + myString.Trim() + "\"");
                return myString.Trim(); //只保留数字SKU
            }
            catch
            {
                return "Error";
            }
        }

        private void Donate_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://cmwtat.cloudmoe.com/donate"); // 打开捐赠页
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
            UpdateInputMatch(); // 更新按钮启用状态
        }

        /// <summary>  
        /// 验证产品密钥字符串是否匹配正则表达式描述的规则并更新按钮状态（如果自动模式则启用按钮）
        /// </summary>  
        private void UpdateInputMatch()
        {
            //防止初始化前访问null出错
            try
            {
                if (actbtn != null)
                { 
                    string pattern = @"^[a-zA-Z0-9]{5}-[a-zA-Z0-9]{5}-[a-zA-Z0-9]{5}-[a-zA-Z0-9]{5}-[a-zA-Z0-9]{5}$";
                    if (is_auto == false)
                    {
                        if (CMWTAT_DIGITAL.Domain.IsSN.IsMatch((SystemEditionTextInput.Text ?? "").ToString(), pattern))
                        {
                            actbtn.IsEnabled = true;
                            installbtn.IsEnabled = true;
                        }
                        else
                        {
                            actbtn.IsEnabled = false;
                            installbtn.IsEnabled = false;
                        }
                    }
                    else
                    {
                        actbtn.IsEnabled = true;
                        installbtn.IsEnabled = true;
                    }
                }
            }
            catch { }
        }

        private void A_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SystemEditionText.Visibility = Visibility.Visible;
            SystemEditionTextInput.Visibility = Visibility.Hidden;
            is_auto = true;
            UpdateInputMatch(); // 更新按钮启用状态
        }

        private void M_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SystemEditionText.Visibility = Visibility.Hidden;
            SystemEditionTextInput.Visibility = Visibility.Visible;
            is_auto = false;
            UpdateInputMatch(); // 更新按钮启用状态
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DelectTempFile();
            notifyIcon.Visible = false;
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://cmwtat.cloudmoe.com"); // 打开官网
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            CheckWindowsTheme();
            ApplyBase(isDark); // 应用颜色
        }
    }
}
