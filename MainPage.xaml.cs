using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace 聚合搜索
{
    class Option
    {
        private String name;
        private String url1;
        private String url2;

        Option(String name, String url1, String url2)
        {
            this.name = name;
            this.url1 = url1;
            this.url2 = url2;
        }
    }

    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private void HideTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        public MainPage()
        {
            this.InitializeComponent();
            Back.IsEnabled = false;
            Forward.IsEnabled = false;
            this.HideTitleBar();
            SettingGrid.Visibility = Visibility.Collapsed;
            TabBar.SelectedIndex = 0;
            SearchBar.Text = "";
        }

        private void Search()
        {
            loadPR.IsActive = true;
            var tb = (TextBlock)TabBar.SelectedItem;
            Title.Text = $"聚合搜索 - {tb.Text} - 正在连接……";
            if (SearchBar.Text.Equals(""))
            {
                switch (tb.Text)
                {
                    case "百度":
                        WV.Navigate(new Uri($"https://www.baidu.com/"));
                        break;
                    case "搜狗":
                        WV.Navigate(new Uri($"https://www.sogou.com/"));
                        break;
                    case "360 搜索":
                        WV.Navigate(new Uri($"https://www.so.com/"));
                        break;
                    case "Bing 国内版":
                        WV.Navigate(new Uri($"https://cn.bing.com/"));
                        break;
                    case "Google":
                        WV.Navigate(new Uri($"https://www.google.com/"));
                        break;
                    case "搜狗·微信":
                        WV.Navigate(new Uri($"https://weixin.sogou.com/"));
                        break;
                    case "微博":
                        WV.Navigate(new Uri($"https://weibo.com/"));
                        break;
                    case "Stack Overflow":
                        WV.Navigate(new Uri($"https://stackoverflow.com/"));
                        break;
                    case "CSDN":
                        WV.Navigate(new Uri($"https://www.csdn.net/"));
                        break;
                    case "博客园":
                        WV.Navigate(new Uri($"https://www.cnblogs.com/"));
                        break;
                    case "Github":
                        WV.Navigate(new Uri($"https://github.com/"));
                        break;
                    case "知乎":
                        WV.Navigate(new Uri($"https://www.zhihu.com/"));
                        break;
                    case "哔哩哔哩":
                        WV.Navigate(new Uri($"https://www.bilibili.com/"));
                        break;
                    case "豆瓣":
                        WV.Navigate(new Uri($"https://www.douban.com/"));
                        break;
                    case "天涯社区":
                        WV.Navigate(new Uri($"https://bbs.tianya.cn/"));
                        break;
                    case "简书":
                        WV.Navigate(new Uri($"https://www.jianshu.com/"));
                        break;
                    case "今日头条":
                        WV.Navigate(new Uri($"https://www.toutiao.com/"));
                        break;
                    case "36氪":
                        WV.Navigate(new Uri($"https://36kr.com/"));
                        break;
                    case "少数派":
                        WV.Navigate(new Uri($"https://sspai.com/"));
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (tb.Text)
                {
                    case "百度":
                        WV.Navigate(new Uri($"https://www.baidu.com/s?ie=UTF-8&wd={SearchBar.Text}"));
                        break;
                    case "搜狗":
                        WV.Navigate(new Uri($"https://www.sogou.com/web?query={SearchBar.Text}"));
                        break;
                    case "360 搜索":
                        WV.Navigate(new Uri($"https://www.so.com/s?ie=utf-8&fr=none&src=360sou_newhome&q={SearchBar.Text}"));
                        break;
                    case "Bing 国内版":
                        WV.Navigate(new Uri($"https://cn.bing.com/search?q={SearchBar.Text}"));
                        break;
                    case "Google":
                        WV.Navigate(new Uri($"https://www.google.com/search?q={SearchBar.Text}"));
                        break;
                    case "微博":
                        WV.Navigate(new Uri($"https://s.weibo.com/weibo?q={SearchBar.Text}"));
                        break;
                    case "搜狗·微信":
                        WV.Navigate(new Uri($"https://weixin.sogou.com/weixin?type=2&query={SearchBar.Text}"));
                        break;
                    case "Stack Overflow":
                        WV.Navigate(new Uri($"https://stackoverflow.com/search?q={SearchBar.Text}"));
                        break;
                    case "CSDN":
                        WV.Navigate(new Uri($"https://so.csdn.net/so/search/s.do?q={SearchBar.Text}&t=&u="));
                        break;
                    case "博客园":
                        WV.Navigate(new Uri($"https://zzk.cnblogs.com/s?t=b&w={SearchBar.Text}"));
                        break;
                    case "Github":
                        WV.Navigate(new Uri($"https://github.com/search?q={SearchBar.Text}&ref=opensearch"));
                        break;
                    case "知乎":
                        WV.Navigate(new Uri($"https://www.zhihu.com/search?q={SearchBar.Text}"));
                        break;
                    case "哔哩哔哩":
                        WV.Navigate(new Uri($"https://search.bilibili.com/all?keyword={SearchBar.Text}"));
                        break;
                    case "豆瓣":
                        WV.Navigate(new Uri($"https://www.douban.com/search?q={SearchBar.Text}"));
                        break;
                    case "天涯社区":
                        WV.Navigate(new Uri($"https://search.tianya.cn/bbs?q={SearchBar.Text}"));
                        break;
                    case "简书":
                        WV.Navigate(new Uri($"https://www.jianshu.com/search?q={SearchBar.Text}"));
                        break;
                    case "今日头条":
                        WV.Navigate(new Uri($"https://www.toutiao.com/search/?keyword={SearchBar.Text}"));
                        break;
                    case "36氪":
                        WV.Navigate(new Uri($"https://36kr.com/search/articles/{SearchBar.Text}"));
                        break;
                    case "少数派":
                        WV.Navigate(new Uri($"https://sspai.com/search/post/{SearchBar.Text}"));
                        break;
                    default:
                        break;
                }
            }
            Back.IsEnabled = true;
            Forward.IsEnabled = false;
        }

        private void TabBar_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Search();
        }

        private void folder_Click(object sender, RoutedEventArgs e)
        {
            if (TabBar.Visibility == Visibility.Collapsed)
            {
                TabBar.Visibility = Visibility.Visible;
            }
            else
            {
                TabBar.Visibility = Visibility.Collapsed;
            }
        }

        private void WV_LoadCompleted(object sender, NavigationEventArgs e)
        {
            loadPR.IsActive = false;
            var tb = (TextBlock)TabBar.SelectedItem;
            Title.Text = $"聚合搜索 - {tb.Text} - {SearchBar.Text}";
            if (SearchBar.Text.Equals(""))
            {
                Title.Text = $"聚合搜索 - {tb.Text}";
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            WV.Source = WV.Source;
        }

        private void WV_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            if (OpenLinkOutside.IsOn)
            {
                return;
            }
            args.Handled = true;
            WebViewNewWindowRequestedEventArgs argss = args;
            Windows.Web.Http.HttpRequestMessage req = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, argss.Uri);
            req.Headers.Referer = argss.Referrer;
            Back.IsEnabled = true;
            Forward.IsEnabled = false;
            WV.NavigateWithHttpRequestMessage(req);
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (WV.CanGoBack)
            {
                WV.GoBack();
            }
            Forward.IsEnabled = true;
            if (!WV.CanGoBack)
            {
                Back.IsEnabled = false;
            }
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (WV.CanGoForward)
            {
                WV.GoForward();
            }
            Back.IsEnabled = true;
            if (!WV.CanGoForward)
            {
                Forward.IsEnabled = false;
            }
        }

        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                Search();
            }
            if (e.Key == VirtualKey.F5)
            {
                Refresh_Click(null, null);
            }
        }

        private void OpenOutside_Click(object sender, RoutedEventArgs e)
        {
            Windows.System.Launcher.LaunchUriAsync(WV.Source);
        }

        private void OpenOutsideMenuItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Windows.System.Launcher.LaunchUriAsync(WV.Source);
        }

        private void CopyLink_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(WV.Source.ToString());
            Clipboard.SetContent(dataPackage);

            CopyLink.Content = "\uE10B";
        }

        private void CopyLink_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            CopyLink.Content = "\uE16F";
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            SettingGrid.Visibility = Visibility.Visible;
        }

        private void SettingReturn_Click(object sender, RoutedEventArgs e)
        {
            SettingGrid.Visibility = Visibility.Collapsed;
        }
    }
}