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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EgoTools.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
            this.Loaded += AboutPage_Loaded;
        }

        private void AboutPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = App.LoadConfig();
                var theme = config.AppearanceSettings?.Theme ?? "Default";

                // 先取消订阅，避免设置初始值时触发 ApplyTheme
                ThemeComboBox.SelectionChanged -= ThemeComboBox_SelectionChanged;
                foreach (ComboBoxItem item in ThemeComboBox.Items)
                {
                    if (item.Tag.ToString() == theme)
                    {
                        ThemeComboBox.SelectedItem = item;
                        break;
                    }
                }
                ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
            }
            catch { }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string selectedTheme = item.Tag.ToString()!;
                try
                {
                    var config = App.LoadConfig();
                    if (config.AppearanceSettings == null)
                    {
                        config.AppearanceSettings = new AppearanceSettings();
                    }
                    config.AppearanceSettings.Theme = selectedTheme;
                    App.SaveConfig(config);

                    // Apply the new theme
                    if (selectedTheme == "Light")
                        App.ApplyTheme(ElementTheme.Light);
                    else if (selectedTheme == "Dark")
                        App.ApplyTheme(ElementTheme.Dark);
                    else
                        App.ApplyTheme(ElementTheme.Default);
                }
                catch { }
            }
        }

        private async void OnDantmnfGithubClick(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://github.com/dantmnf");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
        private async void OnAngelaCooljxGithubClick(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://github.com/AngelaCooljx");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
        private async void OnEgoToolsGithubClick(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://github.com/SaKongA/EgoTools");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
        private async void OnGoddiesGithubClick(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://github.com/matebook-e-go/goodies");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}
