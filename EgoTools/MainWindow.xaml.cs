using H.NotifyIcon;
using Microsoft.UI;
using Microsoft.UI.Windowing;
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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EgoTools
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        //导入Dll
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);
        //

        public MainWindow()
        {
            InitializeComponent();
            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(Views.MainPage));

            // 设置标题栏
            InitializeTitleBar();
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private void InitializeTitleBar()
        {
            AppWindow m_appWindow = GetAppWindowForCurrentWindow();
            m_appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            m_appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            m_appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            m_appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            SetTitleBar(AppTitleBar);
        }

        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).CheckUtilsFiles(this);
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                NavigateToPage(selectedItem.Tag?.ToString());
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                NavigateToPage(item.Tag?.ToString());
            }
        }

        private void NavigateToPage(string? pageTag)
        {
            if (string.IsNullOrEmpty(pageTag)) return;
            switch (pageTag)
            {
                case "MainPage":
                    ContentFrame.Navigate(typeof(Views.MainPage));
                    break;
                case "BatteryControlPage":
                    ContentFrame.Navigate(typeof(Views.BatteryControlPage));
                    break;
                case "AboutPage":
                    ContentFrame.Navigate(typeof(Views.AboutPage));
                    break;
            }
        }


        private const int WM_SETICON = 0x80;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;
    }
}
