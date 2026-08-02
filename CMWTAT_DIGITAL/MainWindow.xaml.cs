using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using OSVersionInfoClass;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ContentDialog = iNKORE.UI.WPF.Modern.Controls.ContentDialog;
using ContentDialogButtonClickEventArgs = iNKORE.UI.WPF.Modern.Controls.ContentDialogButtonClickEventArgs;

namespace CMWTAT_DIGITAL
{

    static class Constants
    {
        public const string DefaultLang = "en"; // 缺省语言
    }

    /// <summary>
    /// RunAct / RunInstall 的运行参数。
    /// 由调用方（UI 层）在调用前一次性收集，使逻辑本身不需要读取任何控件。
    /// </summary>
    public class LicenseTaskOptions
    {
        /// <summary>是否为自动模式，false 为手动输入密钥模式。</summary>
        public bool IsAuto { get; set; }

        /// <summary>自动模式下选择的系统版本（对应 SystemEditionText 的文本）。</summary>
        public string SystemEdition { get; set; }

        /// <summary>手动模式下输入的产品密钥（对应 SystemEditionTextInput 的文本）。</summary>
        public string ManualKey { get; set; }
    }

    /// <summary>
    /// RunAct / RunInstall 的执行结果。
    /// 只描述“发生了什么”，由调用方决定如何呈现（对话框、气泡提示或命令行输出）。
    /// </summary>
    public class LicenseTaskResult
    {
        public LicenseTaskResult(string code) : this(code, null)
        {
        }

        public LicenseTaskResult(string code, string systemMessage)
        {
            Code = code;
            SystemMessage = systemMessage;
        }

        /// <summary>
        /// 结果代码，"200" 为成功，其余为错误代码。
        /// 错误提示文本约定保存在语言资源 "ErrorMsg" + Code 中（如 "ErrorMsg-1.1"）。
        /// </summary>
        public string Code { get; private set; }

        /// <summary>附加的系统输出，目前仅错误代码 "-4" 会带上 slmgr 的原始输出。</summary>
        public string SystemMessage { get; private set; }

        /// <summary>是否因为未连接激活服务器而将在下次联网时自动激活。</summary>
        public bool WillActivateLater { get; set; }

        /// <summary>是否执行成功。</summary>
        public bool Succeeded
        {
            get { return Code == "200"; }
        }
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int PrivateExtractIcons(string lpszFile, int nIconIndex, int cxIcon, int cyIcon, IntPtr[] phicon, IntPtr[] piconid, int nIcons, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

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
            if (App.log2file == true)
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

        string tempfile = Path.GetTempPath() + @"CMWTAT_DIGITAL\";

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
                langRd = System.Windows.Application.LoadComponent(new Uri(@"/CMWTAT_DIGITAL;component/Lang\" + LangName + ".xaml", UriKind.Relative)) as ResourceDictionary;
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
                langRd = System.Windows.Application.LoadComponent(new Uri(@"/CMWTAT_DIGITAL;component/Lang\" + Constants.DefaultLang + ".xaml", UriKind.Relative)) as ResourceDictionary;
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

            UpdateThemeSwitchButton(); // 语言变了，标题栏主题按钮的工具提示要跟着刷新
            BuildLangMenu();           // 重建语言菜单：勾选项要指向新语言
        }

        private static List<string> availableLangs = null; // 编译期就定死了，检测一次即可

        /// <summary>
        /// 动态检测程序里编译进了哪些语言：直接枚举 .g.resources 里的 lang/*.baml。
        /// 这样以后往 Lang\ 里加一个 xx.xaml 就自动出现在菜单里，不用改代码。
        /// </summary>
        private static List<string> GetAvailableLangs()
        {
            if (availableLangs != null)
            {
                return availableLangs;
            }

            List<string> langs = new List<string>();

            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                System.Resources.ResourceManager rm =
                    new System.Resources.ResourceManager(asm.GetName().Name + ".g", asm);
                System.Resources.ResourceSet res = rm.GetResourceSet(CultureInfo.InvariantCulture, true, true);

                if (res != null)
                {
                    const string prefix = "lang/"; // WPF 会把资源键统一转成小写、并用正斜杠
                    const string suffix = ".baml";

                    // 只取键不碰值：读 Value 会把每个 BAML 流都实例化出来，没必要。
                    // 另外这里刻意不 Dispose：这个 ResourceSet 归我们自己 new 的 ResourceManager 管，
                    // 但不值得为了省一个流去冒动到 WPF 资源加载的风险。
                    System.Collections.IDictionaryEnumerator en = res.GetEnumerator();
                    while (en.MoveNext())
                    {
                        string key = en.Key as string;
                        if (key != null && key.StartsWith(prefix) && key.EndsWith(suffix))
                        {
                            langs.Add(key.Substring(prefix.Length, key.Length - prefix.Length - suffix.Length));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ConsoleLog("Failed to enumerate languages: " + e.Message);
            }

            if (langs.Count == 0)
            {
                langs.Add(Constants.DefaultLang); // 兜底，至少别弹出一个空菜单
            }

            langs.Sort(StringComparer.OrdinalIgnoreCase);
            availableLangs = langs;
            return langs;
        }

        /// <summary>
        /// 读取某个语言包自报的名字（LanguageName），例如 zh -> 简体中文。
        /// 语言包里没写就退回显示语言代码。
        /// </summary>
        private static string GetLangDisplayName(string langCode)
        {
            try
            {
                ResourceDictionary rd = System.Windows.Application.LoadComponent(
                    new Uri(@"/CMWTAT_DIGITAL;component/Lang\" + langCode + ".xaml", UriKind.Relative)) as ResourceDictionary;

                string name = rd == null ? null : rd["LanguageName"] as string;
                return string.IsNullOrEmpty(name) ? langCode : name;
            }
            catch
            {
                return langCode;
            }
        }

        /// <summary>
        /// 重建标题栏的语言菜单，当前语言那一项用 CheckMark 图标标出。
        /// 放在 LoadLang() 里调用：语言列表和勾选项只可能在这个时机变化，
        /// 而且不用去和 FlyoutService 自己挂的 Click 处理器抢执行顺序。
        /// </summary>
        private void BuildLangMenu()
        {
            if (langMenuFlyout == null)
            {
                return; // InitializeComponent() 之前
            }

            langMenuFlyout.Items.Clear();

            foreach (string code in GetAvailableLangs())
            {
                // 这个库里没有 MenuFlyoutItem 这个类型，MenuFlyoutPresenter 本身就是个 ContextMenu，
                // 所以菜单项用 WPF 原生的 MenuItem。
                // 注意必须写全名：本文件同时 using 了 System.Windows.Forms，MenuItem 是有歧义的。
                System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem
                {
                    Header = GetLangDisplayName(code),
                    Tag = code
                };

                if (string.Equals(code, NowLang, StringComparison.OrdinalIgnoreCase))
                {
                    // MenuItem.Icon 是 object，直接塞一个 FontIcon 进去即可
                    item.Icon = new FontIcon { Icon = SegoeFluentIcons.CheckMark, FontSize = 14 };
                }

                item.Click += LangMenuItem_Click;
                langMenuFlyout.Items.Add(item);
            }
        }

        /// <summary>
        /// 语言菜单项被点击：热切到对应语言。
        /// </summary>
        private void LangMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem item = sender as System.Windows.Controls.MenuItem;
            string code = item == null ? null : item.Tag as string;

            if (string.IsNullOrEmpty(code) || string.Equals(code, NowLang, StringComparison.OrdinalIgnoreCase))
            {
                return; // 点的就是当前语言，不用折腾
            }

            ConsoleLog("Language switched to: " + code);
            LoadLang(code); // 内部会刷新标题栏按钮的工具提示并重建本菜单
        }

        /// <summary>
        /// 当前主题模式：null = 跟随系统，Dark = 深色，Light = 亮色。默认跟随系统。
        /// </summary>
        private ApplicationTheme? themeMode = null;

        /// <summary>
        /// 应用当前主题模式（iNKORE.UI.WPF.Modern / Fluent Design）。
        /// ApplicationTheme 置为 null 时，框架会自动读取系统主题，
        /// 并在系统主题发生变化时实时跟随，无需自行轮询。
        /// </summary>
        private void ApplyTheme()
        {
            ThemeManager.Current.ApplicationTheme = themeMode;
            UpdateThemeSwitchButton();
        }

        /// <summary>
        /// 让标题栏按钮的图标和工具提示与当前主题模式保持一致。
        /// 切换主题之后、以及切换语言（LoadLang）之后都需要调用。
        /// </summary>
        private void UpdateThemeSwitchButton()
        {
            if (themeSwitchBtn == null || themeSwitchIcon == null)
            {
                return; // InitializeComponent() 之前
            }

            string modeKey;

            if (themeMode == ApplicationTheme.Dark)
            {
                themeSwitchIcon.Icon = SegoeFluentIcons.QuietHours;  // 月亮：深色
                modeKey = "ThemeMode_Dark";
            }
            else if (themeMode == ApplicationTheme.Light)
            {
                themeSwitchIcon.Icon = SegoeFluentIcons.Brightness;  // 太阳：亮色
                modeKey = "ThemeMode_Light";
            }
            else
            {
                themeSwitchIcon.Icon = SegoeFluentIcons.System;      // 跟随系统
                modeKey = "ThemeMode_System";
            }

            // 语言资源可能还没载入（Window_Activated 早于 Window_Loaded），
            // 此时先跳过，LoadLang() 载入语言之后会再刷新一次。
            string tipFormat = this.Resources["ThemeSwitchToolTip"] as string;
            string modeName = this.Resources[modeKey] as string;

            if (tipFormat != null && modeName != null)
            {
                themeSwitchBtn.ToolTip = string.Format(tipFormat, modeName); // 切换主题 (当前：跟随系统)
            }
        }

        /// <summary>
        /// 标题栏主题切换按钮：在 系统 -> 深色 -> 亮色 -> 系统 三态之间循环。
        /// </summary>
        private void themeSwitchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (themeMode == null)
            {
                themeMode = ApplicationTheme.Dark;
            }
            else if (themeMode == ApplicationTheme.Dark)
            {
                themeMode = ApplicationTheme.Light;
            }
            else
            {
                themeMode = null;
            }

            ConsoleLog("Theme Mode switched to: " + (themeMode == null ? "System" : themeMode.ToString()));

            ApplyTheme();
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
            Attribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute));
            AssemblyDescriptionAttribute description = (AssemblyDescriptionAttribute)
            Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute));

            ProductVersion = assembly.GetName().Version.ToString();

            ConsoleLog("AppDescription: " + description.Description);
            ConsoleLog("AppCopyright: " + copyright.Copyright);
            ConsoleLog("AppProductVersion: " + ProductVersion);
        }

        public string SystemEdition = OSVersionInfo.Edition;

        // 仅用于日志诊断；实际配色由 ApplyTheme() / ThemeManager 负责
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
                WindowsTheme = "Light";
            }
            else
            {
                WindowsTheme = "Dark";
            }

            //ConsoleLog("Windows Theme Background is: " + Wcolor);
            //ConsoleLog("Windows Theme Brightness is: " + lightness);
            //ConsoleLog("Windows Theme Mode is: " + WindowsTheme);
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadTitleBarIcon();
        }

        private void LoadTitleBarIcon()
        {
            try
            {
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var hIcons = new IntPtr[1];
                PrivateExtractIcons(exePath, 0, 256, 256, hIcons, null, 1, 0);
                if (hIcons[0] != IntPtr.Zero)
                {
                    TitleBarIcon.Source = Imaging.CreateBitmapSourceFromHIcon(
                        hIcons[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    DestroyIcon(hIcons[0]);
                }
            }
            catch { }
        }

        JArray ositems;
        int now_os_index = 0;
        string checked_os = "unknow";

        bool is_auto = true; //是否为自动模式，false为手动

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
                            this.DialogUpdate.IsSecondaryButtonEnabled = false;
                            //System.Windows.MessageBox.Show("必须更新");
                        }
                        else
                        {
                            this.DialogUpdate.IsSecondaryButtonEnabled = true;
                        }
                        this.DialogUpdate.Title = (string)this.Resources["UpdateTitle"];
                        this.DialogUpdateText.Text = (string)this.Resources["UpdateText"] + "\r\n" + (string)this.Resources["CurrentVersion"] + ": " + ProductVersion + "\r\n" + (string)this.Resources["LatestVersion"] + ": " + latest_version.ToString();
                        OpenDialog(this.DialogUpdate);
                    }));
                    //System.Windows.MessageBox.Show("需要更新");
                }
            }
            catch
            {
            }
        }

        public static string StaticServerDomain = "https://uwa-static.cloudmoe.com"; // 静态服务器
        public static string MainServerDomain = "https://cmwtat.cloudmoe.com"; // 主要服务器
        public static string BackupServerDomain = "https://kms.kumo.moe"; // 备用服务器

        private void LoadOSList()
        {

            int is_selected = 0; //是否已经自动选择,0未选择，1普通模式，2实验模式，3离线KMS模式

            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                OpenDialog(DialogWait);
            }));

            try
            {
                string json;
                try
                {
                    json = GetHttpWebRequest(MainServerDomain + "/api/digital?list=1&ver=4"); // 主要服务器
                }
                catch (Exception e)
                {
                    ConsoleLog("MainServer:" + MainServerDomain + " is not working.");
                    ConsoleLog("Error Message:" + e.Message);
                    ConsoleLog("Ready to use BackupServer:" + BackupServerDomain);
                    json = GetHttpWebRequest(BackupServerDomain + "/api/digital?list=1&ver=4"); // 备用服务器
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
                        this.DialogWithOKToCloseDialog.Title = (string)this.Resources["Attention"];
                        this.DialogWithOKToCloseDialogText.Text = (string)this.Resources["May_be_not_be_supported"] + "\r\n(" + (string)this.Resources["System_Edition"] + ": " + SystemEdition + OSVersionInfo.BuildVersion + ")";
                        OpenDialog(this.DialogWithOKToCloseDialog);
                    }
                    else if (is_selected == 2)//只找到实验性
                    {
                        this.SystemEditionText.SelectedIndex = now_os_index;
                        this.DialogWithOKToCloseDialog.Title = (string)this.Resources["Attention"];
                        this.DialogWithOKToCloseDialogText.Text = (string)this.Resources["Only_find_experimental"] + "\r\n(" + (string)this.Resources["System_Edition"] + ": " + SystemEdition + OSVersionInfo.BuildVersion + ")";
                        OpenDialog(this.DialogWithOKToCloseDialog);
                    }
                    else if (is_selected == 3)//只找到长期KMS
                    {
                        this.SystemEditionText.SelectedIndex = now_os_index;
                        this.DialogWithOKToCloseDialog.Title = (string)this.Resources["Attention"];
                        this.DialogWithOKToCloseDialogText.Text = (string)this.Resources["Only_find_ltok"] + "\r\n(" + (string)this.Resources["System_Edition"] + ": " + SystemEdition + OSVersionInfo.BuildVersion + ")";
                        OpenDialog(this.DialogWithOKToCloseDialog);
                    }
                    else
                    {
                        this.SystemEditionText.SelectedIndex = now_os_index;
                    }
                }));

                //this.SystemEditionText.SelectedIndex = now_os_index;

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    DialogWait.Hide();
                }));

                if (App.autoact == true)//自动激活
                {
                    Thread actthread = new Thread(RunActWithUI);
                    switch (is_selected)
                    {
                        case 1: //正常
                            actthread.Start();
                            break;
                        case 2: //实验性
                            if (App.expact == true)
                            {
                                actbtn.Dispatcher.Invoke(new Action(() =>
                                {
                                    DialogWithOKToCloseDialog.Hide();
                                }));
                                actthread.Start();
                            }
                            else
                            {
                                if (App.hiderun == true)
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
                            if (App.hiderun == true)
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
                    DialogWait.Hide();
                }));

                actbtn.Dispatcher.Invoke(new Action(() =>
                {
                    OpenDialog(DialogWithExit);
                }));

                if (App.hiderun == true && App.autoact == true)
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
                this.Height -= 65;
            }
        }

        private void Activate_Button_Click(object sender, RoutedEventArgs e)
        {
            Thread actthread = new Thread(RunActWithUI);
            actthread.Start();

            //RunAct();
            //LoadOSList();

            //MessageBox.Show(json);
            //MessageBox.Show(rss["OS"][0].ToString());
            //MessageBox.Show(SystemEdition);
        }

        private void installbtn_Click(object sender, RoutedEventArgs e)
        {
            Thread installthread = new Thread(RunInstallWithUI);
            installthread.Start();
        }

        private void upgradefullbtn_Click(object sender, RoutedEventArgs e)
        {
            OpenDialog(this.DialogUpgradeFullVersion);
        }

        private void UpgradeFullVersionWindows_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
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

        /// <summary>
        /// 显示 ContentDialog。框架限定同一时间只能打开一个对话框，
        /// 因此先关闭已打开的那个（与旧的 IsOpen = true 行为一致）。
        /// </summary>
        private void OpenDialog(ContentDialog dialog)
        {
            ContentDialog opened = ContentDialog.GetOpenDialog(this);
            if (opened == dialog)
            {
                return;
            }
            if (opened != null)
            {
                opened.Hide();
            }
            dialog.Hide();
            dialog.ShowAsync(ContentDialogPlacement.InPlace); // 不阻塞 UI 线程
        }

        private void Exit_Button_Click(object sender, EventArgs e)
        {
            DelectTempFile();
            notifyIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// 在 UI 线程上一次性收集运行参数，使 RunAct / RunInstall 无需再访问控件。
        /// </summary>
        private LicenseTaskOptions GetLicenseTaskOptions()
        {
            LicenseTaskOptions options = null;
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                options = new LicenseTaskOptions
                {
                    IsAuto = is_auto,
                    SystemEdition = this.SystemEditionText.Text,
                    ManualKey = this.SystemEditionTextInput.Text
                };
            }));
            return options;
        }

        /// <summary>
        /// 把执行结果的错误代码翻译成当前语言的提示文本。
        /// </summary>
        private string GetErrorMessage(LicenseTaskResult result)
        {
            string msg = this.Resources["ErrorMsg" + result.Code] as string ?? "Unknow Error!";
            if (result.SystemMessage != null)
            {
                msg += "\r\n" + (string)this.Resources["SysMsg"] + "\r\n" + result.SystemMessage;
            }
            return msg;
        }

        /// <summary>
        /// 呈现 RunAct / RunInstall 的执行结果：关闭进度对话框并弹出结果对话框。
        /// </summary>
        /// <param name="result">执行结果。</param>
        /// <param name="busyLangKey">复位进度文字所使用的语言资源键。</param>
        /// <param name="doneLangKey">成功时显示内容的语言资源键。</param>
        /// <param name="notifyWhenHideRun">静默模式下是否弹出气泡提示并退出。</param>
        private void ShowTaskResult(LicenseTaskResult result, string busyLangKey, string doneLangKey, bool notifyWhenHideRun)
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.DialogActProg.Hide();
                this.activatingtext.Text = (string)this.Resources[busyLangKey];

                string tipContent;
                if (result.Succeeded == false)
                {
                    tipContent = GetErrorMessage(result);
                    this.DialogWithOKToCloseDialog.Title = (string)this.Resources["ErrorTitle"]; //错误标题
                    this.DialogWithOKToCloseDialogText.Text = tipContent + "\r\n" + (string)this.Resources["ErrorCode"] + result.Code; //错误代码 如：错误信息\r\nCode：000
                    OpenDialog(this.DialogWithOKToCloseDialog);
                }
                else
                {
                    tipContent = (string)this.Resources[doneLangKey];
                    this.DialogWithOKToCloseDialogDonate.Title = (string)this.Resources["CompleteTitle"]; //完成标题
                    this.DialogWithOKToCloseDialogDonateText.Text = tipContent;
                    OpenDialog(this.DialogWithOKToCloseDialogDonate);
                }

                if (notifyWhenHideRun == true && App.hiderun == true && App.autoact == true)
                {
                    int tipShowMilliseconds = 0;
                    string tipTitle = (string)this.Resources["notifyIconTitle"];
                    ToolTipIcon tipType = ToolTipIcon.None;
                    notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                    Exit_Button_Click(null, null);
                }
            }));
        }

        /// <summary>
        /// installbtn 的线程入口：只负责 UI 交互，实际逻辑见 RunInstall。
        /// </summary>
        private void RunInstallWithUI()
        {
            LicenseTaskOptions options = GetLicenseTaskOptions();

            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.activatingtext.Text = (string)this.Resources["RunInstall_Converting"]; //提示转换中
                OpenDialog(this.DialogActProg);
            }));

            LicenseTaskResult result = RunInstall(options, ReportInstallProgress);

            ShowTaskResult(result, "RunInstall_Converting", "DonateTextConverted", false); //完成转换内容
        }

        /// <summary>
        /// RunInstall 的进度回调：更新进度对话框上的提示文字。
        /// </summary>
        /// <param name="langKey">提示文字对应的语言资源键。</param>
        private void ReportInstallProgress(string langKey)
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.activatingtext.Text = (string)this.Resources[langKey];
            }));
        }

        /// <summary>
        /// 安装（转换）产品密钥。不涉及任何 UI 操作，可在任意线程调用。
        /// </summary>
        /// <param name="options">运行参数，由调用方提前收集。</param>
        /// <param name="onProgress">进度回调，参数为语言资源键，可为 null。</param>
        /// <returns>执行结果，由调用方决定如何呈现。</returns>
        public LicenseTaskResult RunInstall(LicenseTaskOptions options, Action<string> onProgress = null)
        {
            ExportTempFile();
            //释放文件
            try
            {
                Wow64EnableWow64FsRedirection(false);//关闭文件重定向

                string key = "00000-00000-00000-00000-00000";
                string sku = "0";

                if (options.IsAuto == true)
                {
                    string system = options.SystemEdition;

                    onProgress?.Invoke("RunInstall_Getting_Key"); //提示正在获取密钥

                    //获取密钥和SKU
                    try
                    {

                        string json;
                        try
                        {
                            json = GetHttpWebRequest(MainServerDomain + "/api/digital?list=0&ver=4"); // 主要服务器
                        }
                        catch (Exception e)
                        {
                            ConsoleLog("MainServer:" + MainServerDomain + " is not working.");
                            ConsoleLog("Error Message:" + e.Message);
                            ConsoleLog("Ready to use BackupServer:" + BackupServerDomain);
                            json = GetHttpWebRequest(BackupServerDomain + "/api/digital?list=0&ver=4"); // 备用服务器
                        }
                        JObject jsonobj = JObject.Parse(json);
                        ositems = (JArray)jsonobj["OS"];
                        key = jsonobj[system]["key"].ToString();
                        sku = jsonobj[system]["sku"].ToString();
                        ConsoleLog("Edition:" + system + "\r\nKEY:" + key + "\r\nSKU:" + sku);

                    }
                    catch
                    {
                        // 激活Windows10需要网络获取产品密钥
                        return new LicenseTaskResult("-0");
                    }
                }
                else
                {

                    //手动密钥

                    key = options.ManualKey;

                }

                onProgress?.Invoke("RunInstall_Uninstalling_old_Key"); //提示正在卸载旧密钥

                //卸载
                string runend = RunSlmgr("-upk").Trim();
                ConsoleLog(runend);
                if (runend.EndsWith("successfully.") == false && runend.EndsWith("not found.") == false)
                {
                    // 无法卸载旧密钥
                    return new LicenseTaskResult("-1");
                }

                onProgress?.Invoke("RunInstall_Installing_Key"); //提示正在安装密钥

                //安装数字权利升级密钥
                if (RunSlmgr("-ipk " + key).Trim().EndsWith("successfully.") == false)
                {
                    // 无法安装密钥，可能没有选择或输入正确的版本
                    return new LicenseTaskResult("-2");
                }

                return new LicenseTaskResult("200");
            }
            finally
            {
                DelectTempFile();
                //清理文件
            }
        }

        private void RunUpgradeFullVersion()
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.activatingtext.Text = (string)this.Resources["RunUpgradeFullVersion_Upgrading"]; //提示升级中
                OpenDialog(this.DialogActProg);
            }));
            RunCMD(@"sc start sppsvc");
            RunCMD(@"sc start wuauserv");
            RunCLI("ChangePK.exe", ".", "/ProductKey VK7JG-NPHTM-C97JM-9MPGT-3V66T"); // Pro
            RunCLI("ChangePK.exe", ".", "/ProductKey 2B87N-8KFHP-DKV6R-Y2C8J-PKCKT"); // Pro N
            RunCLI("ChangePK.exe", ".", "/ProductKey W269N-WFGWX-YVC9B-4J6C9-T83GX"); // KMS Pro
            RunCLI("ChangePK.exe", ".", "/ProductKey MH37W-N47XK-V7XM9-C7227-GCQG9"); // KMS Pro N
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.DialogActProg.Hide();
            }));
        }

        private void ShowBallSameDig()
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                if (App.hiderun == true && App.autoact == true)
                {
                    int tipShowMilliseconds = 0;
                    string tipTitle = (string)this.Resources["notifyIconTitle"];
                    string tipContent = this.activatingtext.Text;
                    ToolTipIcon tipType = ToolTipIcon.None;
                    notifyIcon.ShowBalloonTip(tipShowMilliseconds, tipTitle, tipContent, tipType);
                }
            }));
        }

        /// <summary>
        /// actbtn / 自动激活的线程入口：只负责 UI 交互，实际逻辑见 RunAct。
        /// </summary>
        private void RunActWithUI()
        {
            LicenseTaskOptions options = GetLicenseTaskOptions();

            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.activatingtext.Text = (string)this.Resources["RunAct_Activating"]; //提示激活中
                OpenDialog(this.DialogActProg);
                ShowBallSameDig();
            }));

            LicenseTaskResult result = RunAct(options, ReportActProgress);

            //即将激活内容 / 完成激活内容
            string doneLangKey = result.WillActivateLater ? "DonateTextWillActivated" : "DonateTextActivated";
            ShowTaskResult(result, "RunAct_Activating", doneLangKey, true);
        }

        /// <summary>
        /// RunAct 的进度回调：更新进度对话框上的提示文字，并同步气泡提示。
        /// </summary>
        /// <param name="langKey">提示文字对应的语言资源键。</param>
        private void ReportActProgress(string langKey)
        {
            actbtn.Dispatcher.Invoke(new Action(() =>
            {
                this.activatingtext.Text = (string)this.Resources[langKey];
                ShowBallSameDig();
            }));
        }

        /// <summary>
        /// 激活（获取数字权利）。不涉及任何 UI 操作，可在任意线程调用。
        /// </summary>
        /// <param name="options">运行参数，由调用方提前收集。</param>
        /// <param name="onProgress">进度回调，参数为语言资源键，可为 null。</param>
        /// <returns>执行结果，由调用方决定如何呈现。</returns>
        public LicenseTaskResult RunAct(LicenseTaskOptions options, Action<string> onProgress = null)
        {
            ExportTempFile();
            //释放文件
            try
            {
                Wow64EnableWow64FsRedirection(false);//关闭文件重定向

                string key = "00000-00000-00000-00000-00000";
                string sku = "0";
                string mode = "1"; //1：普通（SYS、SKU、KEY完全）；2.需要获取SKU（SYS、KEY）；3.手动输入KEY；4.普通OfflineKMS（SYS、SKU、KEY完全）

                if (options.IsAuto == true)
                {

                    string system = options.SystemEdition;

                    onProgress?.Invoke("RunAct_Getting_Key"); //提示正在获取密钥

                    //获取密钥和SKU
                    try
                    {

                        string json;
                        try
                        {
                            json = GetHttpWebRequest(MainServerDomain + "/api/digital?list=0&ver=4"); // 主要服务器
                        }
                        catch (Exception e)
                        {
                            ConsoleLog("MainServer:" + MainServerDomain + " is not working.");
                            ConsoleLog("Error Message:" + e.Message);
                            ConsoleLog("Ready to use BackupServer:" + BackupServerDomain);
                            json = GetHttpWebRequest(BackupServerDomain + "/api/digital?list=0&ver=4"); // 备用服务器
                        }
                        JObject jsonobj = JObject.Parse(json);
                        ositems = (JArray)jsonobj["OS"];
                        key = jsonobj[system]["key"].ToString();
                        sku = jsonobj[system]["sku"].ToString();
                        ConsoleLog("Edition:" + system + "\r\nKEY:" + key + "\r\nSKU:" + sku);

                        // 当前选择的版本
                        ConsoleLog("Selected OS: " + system);

                        if (sku == "unknow")
                        {
                            mode = "2";
                        }

                        if (system.ToUpper().StartsWith("(Offline-KMS)".ToUpper()))
                        {
                            ConsoleLog("Switch Mode Offline-KMS");
                            mode = "4";
                        }

                    }
                    catch
                    {
                        // 激活Windows10需要网络获取产品密钥
                        return new LicenseTaskResult("-0");
                    }

                }
                else
                {

                    key = options.ManualKey;
                    mode = "3";
                    sku = "unknow";

                }

                onProgress?.Invoke("RunAct_Uninstalling_old_Key"); //提示正在卸载旧密钥

                //卸载
                string runend = RunSlmgr("-upk").Trim();
                ConsoleLog(runend);
                if (runend.EndsWith("successfully.") == false && runend.EndsWith("not found.") == false)
                {
                    // 无法卸载旧密钥
                    return new LicenseTaskResult("-1");
                }

                RunSlmgr("-ckms").Trim();

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

                    onProgress?.Invoke("RunAct_Getting_edition_code_Exp"); // "Getting edition code (Experimental)";

                    //安装转换密钥
                    runend = RunSlmgr("-ipk " + key);
                    ConsoleLog("slmgr -ipk " + key);
                    ConsoleLog(runend);
                    if (runend.Trim().EndsWith("successfully.") == false)
                    {
                        // 无法安装密钥，可能没有选择或输入正确的版本
                        return new LicenseTaskResult("-1.1");
                    }

                    Thread.Sleep(6000); //等待6秒，确保SKU生效
                    sku = GetSKU(); //获取SKU
                    if (sku == "Error")
                    {
                        // 无法获取版本代号
                        return new LicenseTaskResult("-1.2");
                    }

                    onProgress?.Invoke("RunAct_Uninstalling_old_Key_Exp"); //提示正在卸载旧密钥（实验性）

                    runend = RunSlmgr("-upk").Trim();
                    ConsoleLog(runend);
                    if (runend.EndsWith("successfully.") || runend.EndsWith("not found."))
                    {
                        onProgress?.Invoke("RunAct_Prepare_for_the_next_step_Exp"); // "Prepare for the next step (Experimental)";
                    }
                }

                onProgress?.Invoke("RunAct_Installing_Key"); //提示正在安装密钥

                //安装数字权利升级密钥
                runend = RunSlmgr("-ipk " + key);
                ConsoleLog("slmgr -ipk " + key);
                ConsoleLog(runend);
                if (runend.Trim().EndsWith("successfully.") == false)
                {
                    // 无法安装密钥，可能没有选择或输入正确的版本
                    return new LicenseTaskResult("-2");
                }

                onProgress?.Invoke("RunAct_Getting_free_upgrade_permissions"); // "Getting free upgrade permissions";

                string ticket = null;

                try
                {
                    RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ProductOptions", true);
                    var packageFamilyName = registryKey.GetValue("OSProductPfn").ToString();
                    try
                    {
                        if (mode == "4")
                        {
                            //长期KMS
                            ticket = GetHttpWebRequest(StaticServerDomain + "/Tickets/KMS.xml");
                        }
                        else
                        {
                            ticket = GetHttpWebRequest(StaticServerDomain + "/Tickets/" + packageFamilyName + ".xml");
                        }
                        //System.Windows.MessageBox.Show(ticket);
                    }
                    catch (Exception e)
                    {
                        ConsoleLog("StaticServer:" + StaticServerDomain + " is not working.");
                        ConsoleLog("Error Message:" + e.Message);
                    }
                }
                catch (Exception e)
                {
                    ConsoleLog("Get PackageFamilyName failed.");
                    ConsoleLog("Error Message:" + e.Message);
                }

                File.WriteAllText(tempfile + "GenuineTicketvNext.xml", ticket, Encoding.UTF8);
                ConsoleLog("进入下一步（CUR：VNEXT）");

                var hasvNextTicket = File.Exists(tempfile + "GenuineTicketvNext.xml");

                if (hasvNextTicket == false)
                {
                    // 执行超时，可能没有选择正确或输入的版本
                    return new LicenseTaskResult("-3");
                }

                onProgress?.Invoke("RunAct_Getting_digital_license"); // "Getting digital license";

                RunCMD(@"sc start wuauserv");
                RunCMD(@"sc start clipsvc");

                RunCMD(@"clipup -v -o -altto " + tempfile);
                RunCMD(@"clipup -v -o -altto " + tempfile.TrimEnd('\\')); // 旧版本系统的 ClipUp 路径不能带最后的反斜杠
                if (OSVersionInfo.BuildVersion > 20348)
                {
                    RunCLI(tempfile + "ClipUp.exe", ".", "-v -o -altto " + tempfile); // 固定版本解决 22H2 后 ARM64 许可证接收问题
                    RunCLI(tempfile + "ClipUp.exe", ".", "-v -o -altto " + tempfile.TrimEnd('\\'));
                }

                onProgress?.Invoke("RunAct_Activating"); // 提示激活中

                int try_max_count = 30;
                for (int i = 0; i < try_max_count + 1; i++)
                {
                    if (!File.Exists(tempfile + "GenuineTicketvNext.xml"))
                    {
                        break;
                    }
                    Thread.Sleep(1000);
                    ConsoleLog($"应用许可证 重试 {i}/{try_max_count}");
                }

                runend = RunSlmgr("-ato").Trim();

                ConsoleLog(runend);

                var xprrunend = RunSlmgr("-xpr").Trim();
                var activated = (xprrunend.Contains("activated") || xprrunend.Contains("activation will expire"));

                ConsoleLog(xprrunend);

                if (runend.EndsWith("successfully.") || activated || runend.Contains("0xC004C003")) // Error 0xC004C003: The activation server determined that the specified product key is blocked. 是因为未连接激活服务器，下次连接时会自动激活。
                {
                    // 未连接激活服务器时，下次连接时会自动激活
                    return new LicenseTaskResult("200") { WillActivateLater = runend.Contains("0xC004C003") };
                }

                // 激活失败
                return new LicenseTaskResult("-4", runend);
            }
            finally
            {
                DelectTempFile();
                //清理文件
            }
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
                myProcess.WaitForExit(120 * 1000);
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

        /// <summary>
        /// 执行 slmgr 命令。使用进程内的 C# 复刻实现
        /// （CMWTAT_DIGITAL.LibSofwareLicenseManager.SofwareLicenseManager），
        /// 不再依赖系统的 VBScript 宿主，也不再需要 slmgr.vbs 文件。
        /// </summary>
        /// <param name="var">slmgr 参数，例如 "-upk"、"-ipk XXXXX-..."。</param>
        /// <returns>已 Trim 的输出；执行失败时返回 "Error"。</returns>
        public static string RunSlmgr(string var = "")
        {
            ConsoleLog("Slmgr " + var);
            try
            {
                string myString = CMWTAT_DIGITAL.LibSofwareLicenseManager.SofwareLicenseManager.Run(var);
                ConsoleLog(myString);
                return myString;
            }
            catch (Exception SlmgrExc)
            {
                ConsoleLog("Slmgr has Exception: " + SlmgrExc.Message);
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

        private void Donate_Button_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            OpenDonatePage();
        }

        /// <summary>
        /// 标题栏捐赠按钮：和对话框里的“捐赠”按钮走同一套逻辑。
        /// </summary>
        private void donateSwitchBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenDonatePage();
        }

        /// <summary>
        /// 打开捐赠页面。
        /// </summary>
        private static void OpenDonatePage()
        {
            System.Diagnostics.Process.Start("https://cmwtat.cloudmoe.com/donate"); // 打开捐赠页
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

        private void UpdateBtn_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            System.Diagnostics.Process.Start("https://cmwtat.cloudmoe.com"); // 打开官网
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            CheckWindowsTheme();
            ApplyTheme(); // 应用颜色（保持用户选择的主题模式）
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CheckWindowsTheme();

            ConsoleLog("Windows Theme Mode is: " + WindowsTheme);

            GetEdition(); // 获取程序版本

            //autoact = App.autoact;
            //hiderun = App.hiderun;
            //expact = App.expact;
            //log2file = App.log2file;
            //showhelp = App.showhelp;

            //MessageBox.Show("A:" + autoact.ToString() + ";H:" + hiderun.ToString());

            ApplyTheme();

            string LangName = currentCultureInfo.Name;
            //根据本地语言来进行本地化
            LangName = LangName.Substring(0, LangName.IndexOf("-"));
            //LangName = "ja"; // 如需测试语言，请取消注释此行
            LoadLang(LangName);

            this.titlebarVersion.Text = "V" + ProductVersion;

            //System.Windows.MessageBox.Show((string)this.Resources["HelpText"]);

            if (App.showhelp == true)
            {
                await DialogHelp.ShowAsync(this); // 等待用户关闭帮助后再继续
            }

            notifyIcon = new NotifyIcon
            {
                Text = (string)this.Resources["notifyIconTitle"], //托盘图标标题
                Icon = Properties.Resources.CMWTAT_ICON
            }; // 先初始化托盘图标，以方便语言缺省时提示

            if ((App.hiderun == true && App.autoact == true) || NotSupportLang == true)
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

            if (App.hiderun == true && App.autoact == true)
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
            DataContext = new Domain.ViewModel();

            OpenDialog(DialogWait);
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
    }
}
