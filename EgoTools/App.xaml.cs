using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.Json;
using System.Text.Json.Nodes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EgoTools
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        //变量定义
        // --- 电池监控所需变量 ---
        private DispatcherTimer batteryMonitorTimer;
        private int minutesChargingAbove90 = 0;
        private int minutesUnplugged = 0;

        // *** 新增：用于防止滑块任务重叠 ***
        private bool isSliderWorkInProgress = false;
        //
        public static AppConfig AppConfig { get; private set; }

        private Window? _window;

        public static Window? MainWindowInstance { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            MainWindowInstance = _window;
            _window.Activate();

            CheckUtilsFiles(this._window);
            LoadConfig();
        }

        public async void CheckUtilsFiles(Window win)
        {
            string[] requiredFiles =
            {
                "kbd-detach.exe",
                "KeyboardService.dll",
                "qdcm-loader.exe",
                "qdcmlib.dll"
            };
            string utilsPath = Path.Combine(AppContext.BaseDirectory, "utils");
            var missing = requiredFiles.Where(util => !File.Exists(Path.Combine(utilsPath, util))).ToList();
            if (missing.Count > 0)
            {
                string msg = "以下组件缺失：\n" + string.Join("\n", missing) + "\n请确保utils文件夹完整。";
                // 追加实际查找路径
                msg += $"\n\n实际查找路径：\n{utilsPath}";
                var dialog = new ContentDialog
                {
                    Title = "组件缺失",
                    Content = msg,
                    PrimaryButtonText = "退出"
                };
                if (win.Content is FrameworkElement fe)
                {
                    dialog.XamlRoot = fe.XamlRoot;
                }
                await dialog.ShowAsync();
                Application.Current.Exit();
                return;
            }
        }

        //public async void CheckUtilsFiles(Window win)
        //{
        //    string[] requiredFiles = new[]
        //    {
        //        "kbd-detach.exe",
        //        "KeyboardService.dll",
        //        "qdcm-loader.exe",
        //        "qdcmlib.dll"
        //    };
        //    string utilsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "utils");
        //    var missing = requiredFiles.Where(f => !File.Exists(System.IO.Path.Combine(utilsPath, f))).ToList();
        //    if (missing.Count > 0)
        //    {
        //        string msg = "以下组件缺失：\n" + string.Join("\n", missing) + "\n请确保utils文件夹完整。";
        //        // 追加实际查找路径
        //        msg += $"\n\n实际查找路径：\n{utilsPath}";
        //        var dialog = new ContentDialog
        //        {
        //            Title = "组件缺失",
        //            Content = msg,
        //            PrimaryButtonText = "退出"
        //        };
        //        if (win.Content is FrameworkElement fe)
        //        {
        //            dialog.XamlRoot = fe.XamlRoot;
        //        }
        //        await dialog.ShowAsync();
        //        Application.Current.Exit();
        //        return;
        //    }
        //    // 检查AppData中的config.json
        //    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        //    string configDir = Path.Combine(appData, "EgoTools");
        //    string configPath = Path.Combine(configDir, "config.json");
        //    if (!File.Exists(configPath))
        //    {
        //        // 不可取消的初始化进度弹窗
        //        var progressBar = new ProgressBar
        //        {
        //            IsIndeterminate = true,
        //            Width = 350,
        //            Height = 20,
        //            Margin = new Thickness(0, 16, 0, 0)
        //        };
        //        var stack = new StackPanel();
        //        stack.Children.Add(new TextBlock { Text = "首次使用，正在初始化设备...", FontSize = 16, Margin = new Thickness(0,0,0,8) });
        //        stack.Children.Add(progressBar);
        //        var dialog = new ContentDialog
        //        {
        //            Title = "初始化中",
        //            Content = stack
        //        };
        //        if (win.Content is FrameworkElement fe)
        //            dialog.XamlRoot = fe.XamlRoot;
        //        var showTask = dialog.ShowAsync();
        //        bool success = true;
        //        string errorMsg = "";
        //        dialog.Hide();
        //        if (!success)
        //        {
        //            // 错误进度条弹窗，只能退出
        //            var errorBar = new ProgressBar
        //            {
        //                IsIndeterminate = false,
        //                Value = 100,
        //                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
        //                Width = 250,
        //                Height = 20,
        //                Margin = new Thickness(0, 16, 0, 0)
        //            };
        //            var errorStack = new StackPanel();
        //            errorStack.Children.Add(new TextBlock { Text = "初始化失败，程序将退出。", FontSize = 16, Margin = new Thickness(0,0,0,8), Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) });
        //            errorStack.Children.Add(new TextBlock { Text = errorMsg, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
        //            errorStack.Children.Add(errorBar);
        //            var errorDialog = new ContentDialog
        //            {
        //                Title = "错误",
        //                Content = errorStack,
        //                PrimaryButtonText = "退出",
        //                IsPrimaryButtonEnabled = true
        //            };
        //            if (win.Content is FrameworkElement fe2)
        //                errorDialog.XamlRoot = fe2.XamlRoot;
        //            await errorDialog.ShowAsync();
        //            Application.Current.Exit();
        //            return;
        //        }
        //        else
        //        {
        //            var okDialog = new ContentDialog
        //            {
        //                Title = "初始化完成",
        //                Content = "初始化完毕，欢迎使用！",
        //                CloseButtonText = "确定"
        //            };
        //            if (win.Content is FrameworkElement fe3)
        //                okDialog.XamlRoot = fe3.XamlRoot;
        //            await okDialog.ShowAsync();
        //        }
        //    }
        //}

        public static void LoadConfig()
        {
            try
            {
                string configFilePath = ConfigPath.MainConfigFilePath;

                if (File.Exists(configFilePath))
                {
                    // 文件存在，读取并反序列化
                    string jsonString = File.ReadAllText(configFilePath);

                    // 尝试反序列化
                    var loadedConfig = JsonSerializer.Deserialize<AppConfig>(jsonString);

                    if (loadedConfig != null)
                    {
                        AppConfig = loadedConfig;
                        Debug.WriteLine($"配置加载成功: {configFilePath}");
                    }
                    else
                    {
                        // JSON 文件可能已损坏或是空的
                        Debug.WriteLine("配置文件内容为空或已损坏，将创建新配置。");
                        SaveConfig(); // 创建并保存一个新配置
                    }
                }
                else
                {
                    // 文件不存在，按要求立即执行 SaveConfig
                    Debug.WriteLine($"未找到配置文件，将创建默认配置于: {configFilePath}");
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                // 处理加载或反序列化过程中可能发生的任何错误
                Debug.WriteLine($"加载配置时发生严重错误: {ex.Message}");
                // 即使加载失败，AppConfig 依然是 new AppConfig()，程序可以继续运行
            }
        }

        public static void SaveConfig()
        {
            try
            {
                // 1. 确保配置文件夹存在
                ConfigPath.EnsureConfigDirectoryExists();

                string configFilePath = ConfigPath.MainConfigFilePath;

                // 2. 设置JSON序列化选项，使其格式化 (WriteIndented = true)
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                // 3. 将 AppConfig 对象序列化为 JSON 字符串
                string jsonString = JsonSerializer.Serialize(AppConfig, options);

                // 4. 将字符串写入文件
                File.WriteAllText(configFilePath, jsonString);

                Debug.WriteLine($"配置保存成功: {configFilePath}");
            }
            catch (Exception ex)
            {
                // 处理保存过程中可能发生的任何错误
                Debug.WriteLine($"保存配置时发生严重错误: {ex.Message}");
            }
        }

        public static void ApplyBatteryChargeLimit(int percentageLimit)
        {
            Debug.WriteLine($"[WMI] 准备设置电池上限为: {percentageLimit}%");

            try
            {
                // 1. 钳制数值 (与 PS1 脚本逻辑一致)
                int clampedLimit = Math.Max(percentageLimit, 50);
                clampedLimit = Math.Min(clampedLimit, 100);

                // 2. 准备 WMI 请求的字节数组
                byte[] request = new byte[64];
                request[0] = 0x03; // MFID
                request[1] = 0x15; // SFID = SBCM
                request[2] = 0x01; // \SBCM.CHMD
                request[3] = 0x18; // \SBCM.DELY
                request[4] = (byte)(clampedLimit - 5);  // \SBCM.STCP start charge percentage threshold
                request[5] = (byte)clampedLimit;    // \SBCM.SOCP stop charge percentage threshold

                // 3. 查找 WMI 实例
                // (C# 中等效于 Get-WmiObject -Namespace ROOT\WMI -Class OemWMIMethod)
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"ROOT\WMI", "SELECT * FROM OemWMIMethod");

                // 4. 获取第一个实例
                ManagementObject? inst = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

                if (inst != null)
                {
                    // 5. 准备方法参数并调用
                    // (C# 中等效于 $inst.OemWMIfun($request))
                    object[] methodArgs = { request };
                    inst.InvokeMethod("OemWMIfun", methodArgs);

                    Debug.WriteLine($"[WMI] 成功调用 OemWMIfun，设置上限为: {clampedLimit}%");
                }
                else
                {
                    Debug.WriteLine("[WMI] 错误: 未找到 'OemWMIMethod' 实例。");
                    // 可以在此处向用户显示错误（例如，驱动未安装或设备不支持）
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WMI] 调用失败: {ex.Message}");
                Debug.WriteLine("[WMI] 严重错误：请确保应用程序是以管理员权限运行的。");
                // 可以在此处向用户显示权限不足的错误
            }
        }

        public static void UpdateAndApplyChargeLimit(int newLimit)
        {
            // 1. 更新内存中的静态配置
            AppConfig.BatteryControl.ChargeLimit = newLimit;

            // 2. 将新配置保存到 AppConfig.json 文件
            SaveConfig();
            Debug.WriteLine("已保存配置");

            // 3. 立即通过 WMI 应用硬件设置
            // (ChargeLimit 的值会自动传递过去)
            ApplyBatteryChargeLimit(AppConfig.BatteryControl.ChargeLimit);
        }

    }
}
