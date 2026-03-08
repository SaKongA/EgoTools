using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Threading.Tasks;
using System.Text.Json.Nodes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EgoTools.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ColorManagementPage : Page
    {
        private AppConfig config = default!;
        private string GetCurrentModeDetailText()
        {
            var cm = config?.ColorManagement;
            string mode = cm?.CurrentMode ?? "";
            string profile = cm?.CurrentProfile ?? "";
            string igc = cm?.IgcFile ?? "";
            string lut = cm?._3dlutFile ?? "";
            string text = "";
            if (mode == "Factory")
            {
                text = $"原厂预设：{profile}";
                if (!string.IsNullOrEmpty(igc))
                    text += $"\nIGC文件：{igc}";
                if (!string.IsNullOrEmpty(lut))
                    text += $"\n3DLUT文件：{lut}";
            }
            else if (mode == "Custom")
            {
                text = $"自定义模式";
                if (!string.IsNullOrEmpty(igc))
                    text += $"\nIGC文件：{igc}";
                if (!string.IsNullOrEmpty(lut))
                    text += $"\n3DLUT文件：{lut}";
            }
            return text;
        }
        public ColorManagementPage()
        {
            InitializeComponent();
            config = App.LoadConfig() ?? new AppConfig
            {
                KeyboardSettings = new KeyboardSettings { KeyboardDetachment = false },
                ColorManagement = new ColorManagement { CurrentMode = "Factory", CurrentProfile = "Default", IgcFile = "", _3dlutFile = "" },
                PowerThreshold = new PowerThreshold { ChargeLimit = 80 }
            };
            if (config?.ColorManagement == null)
                config.ColorManagement = new ColorManagement { CurrentMode = "Factory", CurrentProfile = "Default", IgcFile = "", _3dlutFile = "" };
            CurrentModeDetailText.Text = GetCurrentModeDetailText();
            // 绑定下拉框事件，并根据配置设置选中项
            if (ProfileComboBox != null)
            {
                foreach (var item in ProfileComboBox.Items)
                {
                    if (item is ComboBoxItem cbi && cbi.Content?.ToString() == config?.ColorManagement?.CurrentProfile)
                    {
                        ProfileComboBox.SelectedItem = cbi;
                        break;
                    }
                }
                ProfileComboBox.SelectionChanged += ProfileComboBox_SelectionChanged;
            }
        }
        private async void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (ProfileComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "sRGB";
            string exeName = System.IO.Path.Combine(AppContext.BaseDirectory, "utils", "qdcm-loader.exe");
            var progressBar = new ProgressBar
            {
                IsIndeterminate = true,
                Width = 250,
                Height = 20,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = selected == "Default" ? "正在还原出厂色彩设置..." : "正在切换色彩预设...", FontSize = 16, Margin = new Thickness(0,0,0,8) });
            stack.Children.Add(progressBar);
            var runningDialog = new ContentDialog
            {
                RequestedTheme=EgoTools.App.RootTheme,
                Title = "请稍候",
                Content = stack
            };
            runningDialog.XamlRoot = this.XamlRoot;
            var showTask = runningDialog.ShowAsync();
            var minDurationTask = System.Threading.Tasks.Task.Delay(1500);

            string arg = selected == "Default" ? "--reset" : $"--preset {selected}";
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exeName,
                    Arguments = arg,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = AppContext.BaseDirectory
                };
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    await minDurationTask;
                    runningDialog.Hide();
                    if (process.ExitCode != 0)
                    {
                        var dialog = new ContentDialog
                        {
                            RequestedTheme=EgoTools.App.RootTheme,
                            Title = selected == "Default" ? "还原失败" : "色彩预设切换失败",
                            Content = $"命令行错误：{error}",
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot
                        };
                        await dialog.ShowAsync();
                        // 操作失败，不保存json
                        // 恢复下拉框为原配置
                        var cm = config?.ColorManagement;
                        string oldProfile = cm?.CurrentProfile ?? "sRGB";
                        foreach (var item in ProfileComboBox.Items)
                        {
                            if (item is ComboBoxItem cbi && cbi.Content?.ToString() == oldProfile)
                            {
                                ProfileComboBox.SelectedItem = cbi;
                                break;
                            }
                        }
                        return;
                    }
                }
                // 命令执行成功保存json
                if (selected == "Default")
                {
                    if (config == null)
                    {
                        config = new AppConfig
                        {
                            KeyboardSettings = new KeyboardSettings { KeyboardDetachment = false },
                            ColorManagement = new ColorManagement { CurrentMode = "Factory", CurrentProfile = "Default", IgcFile = "", _3dlutFile = "" },
                            PowerThreshold = new PowerThreshold { ChargeLimit = 100 }
                        };
                    }
                    if (config?.ColorManagement == null)
                        config.ColorManagement = new ColorManagement { CurrentMode = "Factory", CurrentProfile = "Default", IgcFile = "", _3dlutFile = "" };
                    if (config?.ColorManagement != null)
                    {
                        config.ColorManagement.CurrentMode = "Factory";
                        config.ColorManagement.CurrentProfile = "Default";
                        config.ColorManagement.IgcFile = "";
                        config.ColorManagement._3dlutFile = "";
                    }
                }
                else
                {
                    if (config?.ColorManagement != null)
                        config.ColorManagement.CurrentProfile = selected;
                }
                if (config != null)
                    App.SaveConfig(config);
                CurrentModeDetailText.Text = GetCurrentModeDetailText();
            }
            catch (Exception ex)
            {
                await minDurationTask;
                runningDialog.Hide();
                var dialog = new ContentDialog
                {
                    RequestedTheme=EgoTools.App.RootTheme,
                    Title = selected == "Default" ? "还原失败" : "调用失败",
                    Content = $"无法调用qdcm-loader.exe：{ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                // 恢复下拉框为原配置
                var cm = config?.ColorManagement;
                string oldProfile = cm?.CurrentProfile ?? "sRGB";
                foreach (var item in ProfileComboBox.Items)
                {
                    if (item is ComboBoxItem cbi && cbi.Content?.ToString() == oldProfile)
                    {
                        ProfileComboBox.SelectedItem = cbi;
                        break;
                    }
                }
            }
        }

        private async Task<string> PickCubeFileAsync()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".cube");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                ContentDialog dialog = new ContentDialog
                {
                    RequestedTheme=EgoTools.App.RootTheme,
                    Title = "已选择文件",
                    Content = $"已选择 {file.Name}，确定要继续使用这个校色文件吗？",
                    PrimaryButtonText = "确定",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                    return file.Path; // 返回绝对路径
                else
                    return null;
            }
            return null;
        }
        private async void OnSelectCubeFile1Click(object sender, RoutedEventArgs e)
        {
            var filePath = await PickCubeFileAsync();
            if (!string.IsNullOrEmpty(filePath))
            {
                // 调用 qdcm-loader.exe --igc <file.cube>
                var progressBar = new ProgressBar
                {
                    IsIndeterminate = true,
                    Width = 250,
                    Height = 20,
                    Margin = new Thickness(0, 16, 0, 0)
                };
                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = "正在加载Gamma校正文件...", FontSize = 16, Margin = new Thickness(0,0,0,8) });
                stack.Children.Add(progressBar);
                var runningDialog = new ContentDialog
                {
                    RequestedTheme=EgoTools.App.RootTheme,
                    Title = "请稍候",
                    Content = stack
                };
                runningDialog.XamlRoot = this.XamlRoot;
                var showTask = runningDialog.ShowAsync();
                var minDurationTask = System.Threading.Tasks.Task.Delay(1500);

                string exeName = System.IO.Path.Combine(AppContext.BaseDirectory, "utils", "qdcm-loader.exe");
                string arg = $"--igc \"{filePath}\"";
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exeName,
                        Arguments = arg,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = AppContext.BaseDirectory
                    };
                    using (var process = System.Diagnostics.Process.Start(psi))
                    {
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();
                        await minDurationTask;
                        runningDialog.Hide();
                        if (process.ExitCode != 0)
                        {
                            var dialog = new ContentDialog
                            {
                                RequestedTheme=EgoTools.App.RootTheme,
                                Title = "Gamma校正加载失败",
                                Content = $"命令行错误：{error}",
                                CloseButtonText = "确定",
                                XamlRoot = this.XamlRoot
                            };
                            await dialog.ShowAsync();
                            // 操作失败，不保存json
                            return;
                        }
                    }
                    // 命令执行成功保存json（只保存文件名）
                    if (config?.ColorManagement != null)
                        config.ColorManagement.IgcFile = System.IO.Path.GetFileName(filePath);
                    if (config != null)
                        App.SaveConfig(config);
                    CurrentModeDetailText.Text = GetCurrentModeDetailText();
                }
                catch (Exception ex)
                {
                    await minDurationTask;
                    runningDialog.Hide();
                    var dialog = new ContentDialog
                    {
                        RequestedTheme=EgoTools.App.RootTheme,
                        Title = "调用失败",
                        Content = $"无法调用qdcm-loader.exe：{ex.Message}",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }
        private async void OnSelectCubeFile2Click(object sender, RoutedEventArgs e)
        {
            var filePath = await PickCubeFileAsync();
            if (!string.IsNullOrEmpty(filePath))
            {
                // 调用 qdcm-loader.exe --3dlut <file.cube>
                var progressBar = new ProgressBar
                {
                    IsIndeterminate = true,
                    Width = 250,
                    Height = 20,
                    Margin = new Thickness(0, 16, 0, 0)
                };
                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = "正在加载3D LUT文件...", FontSize = 16, Margin = new Thickness(0,0,0,8) });
                stack.Children.Add(progressBar);
                var runningDialog = new ContentDialog
                {
                    RequestedTheme=EgoTools.App.RootTheme,
                    Title = "请稍候",
                    Content = stack
                };
                runningDialog.XamlRoot = this.XamlRoot;
                var showTask = runningDialog.ShowAsync();
                var minDurationTask = System.Threading.Tasks.Task.Delay(1500);

                string exeName = System.IO.Path.Combine(AppContext.BaseDirectory, "utils", "qdcm-loader.exe");
                string arg = $"--3dlut \"{filePath}\"";
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exeName,
                        Arguments = arg,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = AppContext.BaseDirectory
                    };
                    using (var process = System.Diagnostics.Process.Start(psi))
                    {
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();
                        await minDurationTask;
                        runningDialog.Hide();
                        if (process.ExitCode != 0)
                        {
                            var dialog = new ContentDialog
                            {
                                RequestedTheme=EgoTools.App.RootTheme,
                                Title = "3D LUT加载失败",
                                Content = $"命令行错误：{error}",
                                CloseButtonText = "确定",
                                XamlRoot = this.XamlRoot
                            };
                            await dialog.ShowAsync();
                            // 操作失败，不保存json
                            return;
                        }
                    }
                    // 命令执行成功保存json（只保存文件名）
                    if (config?.ColorManagement != null)
                        config.ColorManagement._3dlutFile = System.IO.Path.GetFileName(filePath);
                    if (config != null)
                        App.SaveConfig(config);
                    CurrentModeDetailText.Text = GetCurrentModeDetailText();
                }
                catch (Exception ex)
                {
                    await minDurationTask;
                    runningDialog.Hide();
                    var dialog = new ContentDialog
                    {
                        RequestedTheme=EgoTools.App.RootTheme,
                        Title = "调用失败",
                        Content = $"无法调用qdcm-loader.exe：{ex.Message}",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }
        private async void OnResetButtonClick(object sender, RoutedEventArgs e)
        {
            ContentDialog confirmDialog = new ContentDialog
            {
                RequestedTheme=EgoTools.App.RootTheme,
                Title = "重置确认",
                Content = "确定要将色彩管理设置还原为出厂状态吗？",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };
            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;
            var progressBar = new ProgressBar
            {
                IsIndeterminate = true,
                Width = 250,
                Height = 20,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "正在重置色彩管理设置...", FontSize = 16, Margin = new Thickness(0,0,0,8) });
            stack.Children.Add(progressBar);
            var runningDialog = new ContentDialog
            {
                RequestedTheme=EgoTools.App.RootTheme,
                Title = "重置中",
                Content = stack
            };
            runningDialog.XamlRoot = this.XamlRoot;
            var showTask = runningDialog.ShowAsync();
            var minDurationTask = System.Threading.Tasks.Task.Delay(1500);

            // 调用 qdcm-loader.exe --reset
            string exeName = System.IO.Path.Combine(AppContext.BaseDirectory, "utils", "qdcm-loader.exe");
            string arg = "--reset";
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exeName,
                    Arguments = arg,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = AppContext.BaseDirectory
                };
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    await minDurationTask;
                    runningDialog.Hide();
                    if (process.ExitCode != 0)
                    {
                        var dialog = new ContentDialog
                        {
                            RequestedTheme=EgoTools.App.RootTheme,
                            Title = "重置失败",
                            Content = $"命令行错误：{error}",
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot
                        };
                        await dialog.ShowAsync();
                        return;
                    }
                }
                // 命令执行成功后重置json
                if (config == null)
                {
                    config = new AppConfig
                    {
                        KeyboardSettings = new KeyboardSettings { KeyboardDetachment = false },
                        ColorManagement = new ColorManagement { CurrentMode = "Factory", CurrentProfile = "Default", IgcFile = "", _3dlutFile = "" },
                        PowerThreshold = new PowerThreshold { ChargeLimit = 100 }
                    };
                }
                if (config?.ColorManagement == null)
                    config.ColorManagement = new ColorManagement { CurrentMode = "Factory", CurrentProfile = "Default", IgcFile = "", _3dlutFile = "" };
                if (config?.ColorManagement != null)
                {
                    config.ColorManagement.CurrentMode = "Factory";
                    config.ColorManagement.CurrentProfile = "Default";
                    config.ColorManagement.IgcFile = "";
                    config.ColorManagement._3dlutFile = "";
                }
                if (config != null)
                    App.SaveConfig(config);
                // 刷新界面
                if (ProfileComboBox != null)
                {
                    foreach (var item in ProfileComboBox.Items)
                    {
                        if (item is ComboBoxItem cbi && cbi.Content?.ToString() == "Default")
                        {
                            ProfileComboBox.SelectedItem = cbi;
                            break;
                        }
                    }
                }
                CurrentModeDetailText.Text = GetCurrentModeDetailText();
            }
            catch (Exception ex)
            {
                await minDurationTask;
                runningDialog.Hide();
                var dialog = new ContentDialog
                {
                    RequestedTheme=EgoTools.App.RootTheme,
                    Title = "调用失败",
                    Content = $"无法调用qdcm-loader.exe：{ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
