using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Security.Principal;
using System.Management;
using System.Threading.Tasks;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EgoTools.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BatteryControlPage : Page
    {
        private bool isSliderWorkInProgress = false;
        public BatteryControlPage()
        {
            InitializeComponent();
            
            UacButton.Click += UacButton_Click;

            LoadBatteryControlUI();

            RegisterSettingChargeHandles();


        }

        private void UpdateManualChargeState(bool isEnabled)
        {
            ChargeManualContorlCard.IsEnabled = isEnabled;
            ChargeLimitSlider.IsEnabled = isEnabled;
        }

        private void LoadBatteryControlUI()
        {
            //1. 加载自动充电开关
            bool isAutoChargeOn = App.AppConfig.BatteryControl.AutoCharge;
            AutoChargeToggle.IsOn = isAutoChargeOn;
            Debug.WriteLine($"[UI] AutoChargeToggle 加载为: {isAutoChargeOn}");

            //2. 加载充电上限滑块值
            int chargeLimit = App.AppConfig.BatteryControl.ChargeLimit;
            ChargeLimitSlider.Value = chargeLimit;

            //3. 加载UacButton状态
            if (IsAdministrator())
            {
                UacButton.IsEnabled = false;
                UAC_CheckMark.Visibility = Visibility.Visible;
                BatteryControlCard.IsEnabled = true;
                BatteryControlCard.IsExpanded = true;
            }
            else
            {
                UacButton.IsEnabled = true;
                UAC_CheckMark.Visibility = Visibility.Collapsed;
                BatteryControlCard.IsEnabled = false;
                BatteryControlCard.IsExpanded = false;
            }
            UpdateManualChargeState(!isAutoChargeOn);
        }

        private void AutoChargeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            bool newState = AutoChargeToggle.IsOn;
            Debug.WriteLine($"[UI] 自动充电已切换: {newState}");

            //1. 保存新状态
            App.AppConfig.BatteryControl.AutoCharge = newState;
            App.SaveConfig();

            //2. 更新 UI 启用状态
            UpdateManualChargeState(!newState);

            //3. 如果关闭自动充电，立刻恢复上限
            if (!newState)
            {
                int manualLimit =App.AppConfig.BatteryControl.ChargeLimit;
                Debug.WriteLine($"[UI] 自动充电已关闭。正在恢复手动上限: {manualLimit}%");

                //立即应用手动调整值
                App.ApplyBatteryChargeLimit(manualLimit);
            }
            else
            {
                Debug.WriteLine($"[UI] 自动充电已开启，后台监控接管");
            }
        }

        /// <summary>
        /// 注册UI控制事件，并在改变时保存
        /// </summary>
        private void RegisterSettingChargeHandles()
        {
            //1. 自动充电开关
            AutoChargeToggle.Toggled += AutoChargeToggle_Toggled;

            //2.充电上限滑块
            ChargeLimitSlider.PointerCaptureLost += ChargeLimitSlider_PointerCaptureLost;
        }
        
        private async void ChargeLimitSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (isSliderWorkInProgress)
            {
                Debug.WriteLine("[UI] 滑块任务已在进行中，跳过。");
                return;
            }

            Debug.WriteLine("[UI] ChargeLimitSlider_PointerCaptureLost 触发！");
            int newValue = (int)ChargeLimitSlider.Value;
            Debug.WriteLine($"[UI] 滑块值读取为: {newValue}");

            if (newValue == App.AppConfig.BatteryControl.ChargeLimit)
            {
                Debug.WriteLine($"[UI] 值未改变 (仍为 {newValue})，跳过调用。");
                return;
            }

            isSliderWorkInProgress = true; // 设置标志

            try
            {
                // 2. 显示“工作中”状态
                ChargeLimitSlider.IsEnabled = false; // 禁用滑块
                ChargeLimitCompleteIcon.Visibility = Visibility.Collapsed; // 隐藏✔
                ChargeLimitProgressRing.Visibility = Visibility.Visible; // 显示圆圈
                ChargeLimitProgressRing.IsActive = true;

                // 3. 调用 App.cs 中的组合方法 (WMI)
                Debug.WriteLine($"[UI] 值已改变，正在调用 App.UpdateAndApplyChargeLimit({newValue})...");
                App.UpdateAndApplyChargeLimit(newValue);

                // 4. 强制的最小禁用时间 (模拟工作)
                await Task.Delay(1500); // 禁用 1.5 秒

                // 5. 显示“完成”状态
                ChargeLimitProgressRing.IsActive = false;
                ChargeLimitProgressRing.Visibility = Visibility.Collapsed;
                ChargeLimitCompleteIcon.Visibility = Visibility.Visible; // 显示✔

                // 6. 保持“完成”状态 1 秒
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UI] 滑块任务失败: {ex.Message}");
                // 即使失败也要重置 UI
            }
            finally
            {
                // 7. 重置 UI
                ChargeLimitCompleteIcon.Visibility = Visibility.Collapsed;

                // 仅在手动模式仍然启用时才重新启用滑块
                if (ChargeManualContorlCard.IsEnabled)
                {
                    ChargeLimitSlider.IsEnabled = true;
                }

                isSliderWorkInProgress = false; // 解除标志
                Debug.WriteLine("[UI] 滑块任务完成，UI 已重置。");
            }
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private async System.Threading.Tasks.Task ShowUacWarningDialog()
        {
            var dialog = new ContentDialog
            {
                Title = "需要管理员权限",
                Content = "请点击上方‘获取’按钮，以管理员身份重启应用后再调整电源阈值。",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        //切换图标
        //private void ThresholdSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        //{
        //    int value = App.AppConfig.BatteryControl.ChargeLimit;
        //    if (value <= 60)
        //        BatteryIcon.Glyph = "\xEBAF";
        //    else if (value <= 70)
        //        BatteryIcon.Glyph = "\xEBB0";
        //    else if (value <= 80)
        //        BatteryIcon.Glyph = "\xEBB1";
        //    else if (value <= 90)
        //        BatteryIcon.Glyph = "\xEBB3";
        //    else if (value <= 99)
        //        BatteryIcon.Glyph = "\xEBB4";
        //    else
        //        BatteryIcon.Glyph = "\xEBB5";
        //    App.SaveConfig();
        //}

        private async void UacButton_Click(object sender, RoutedEventArgs e)
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                var dialog = new ContentDialog
                {
                    Title = "无法获取可执行文件路径",
                    Content = "未能获取当前进程的可执行文件路径，无法以管理员身份重启。",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };
            try
            {
                System.Diagnostics.Process.Start(psi);
                Application.Current.Exit();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "提权失败",
                    Content = $"无法以管理员身份重启：{ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
