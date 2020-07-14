using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
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
    public struct Option
    {
        public string name;
        public string home;
        public string url1;
        public string url2;
    }

    public struct UA
    {
        public string name;
        public string ua;
    }

    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Option[] options;
        private UA[] uas;
        private int optionNum = 18;
        private int uaNum = 5;

        private void HideTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        private void WV_GotoPage(Uri uri)
        {
            loadPR.IsActive = true;
            var tb = (TextBlock)TabBar.SelectedItem;
            Title.Text = $"聚合搜索 - {tb.Text} - 正在连接……";
            Windows.Web.Http.HttpRequestMessage req = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, uri);
            req.Headers.Referer = uri;
            req.Headers.Add("User-Agent", uas[UACB.SelectedIndex].ua);

            Back.IsEnabled = true;
            Forward.IsEnabled = false;
            WV.NavigateWithHttpRequestMessage(req);
        }

        private async void OpenFile()
        {
            //Windows.Storage.StorageFolder storageFolder =
            //    Windows.Storage.ApplicationData.Current.LocalFolder;
            //Windows.Storage.StorageFile sampleFile =
            //    await storageFolder.CreateFileAsync("UserOption.dat",
            //        Windows.Storage.CreationCollisionOption.OpenIfExists);
            options = new Option[optionNum];

            options[0].name = "百度";
            options[0].home = "https://www.baidu.com/";
            options[0].url1 = "https://www.baidu.com/s?ie=UTF-8&wd=";
            options[0].url2 = "";

            options[1].name = "搜狗";
            options[1].home = "https://www.sogou.com/";
            options[1].url1 = "https://www.sogou.com/web?query=";
            options[1].url2 = "";

            options[2].name = "360 搜索";
            options[2].home = "https://www.so.com/";
            options[2].url1 = "https://www.so.com/s?ie=utf-8&fr=none&src=360sou_newhome&q=";
            options[2].url2 = "";

            options[3].name = "Bing 国内版";
            options[3].home = "https://cn.bing.com/";
            options[3].url1 = "https://cn.bing.com/search?q=";
            options[3].url2 = "";

            options[4].name = "Google";
            options[4].home = "https://www.google.com/";
            options[4].url1 = "https://www.google.com/search?q=";
            options[4].url2 = "";

            options[5].name = "搜狗·微信";
            options[5].home = "https://weixin.sogou.com/";
            options[5].url1 = "https://weixin.sogou.com/weixin?type=2&query=";
            options[5].url2 = "";

            options[6].name = "哔哩哔哩";
            options[6].home = "https://www.bilibili.com/";
            options[6].url1 = "https://search.bilibili.com/all?keyword=";
            options[6].url2 = "";

            options[7].name = "知乎";
            options[7].home = "https://www.zhihu.com/";
            options[7].url1 = "https://www.zhihu.com/search?q=";
            options[7].url2 = "";

            options[8].name = "少数派";
            options[8].home = "https://sspai.com/";
            options[8].url1 = "https://sspai.com/search/post/";
            options[8].url2 = "";

            options[9].name = "36氪";
            options[9].home = "https://36kr.com/";
            options[9].url1 = "https://36kr.com/search/articles/";
            options[9].url2 = "";

            options[10].name = "简书";
            options[10].home = "https://www.jianshu.com/";
            options[10].url1 = "https://www.jianshu.com/search?q=";
            options[10].url2 = "";

            options[11].name = "微博";
            options[11].home = "https://www.weibo.com/";
            options[11].url1 = "https://s.weibo.com/weibo?q=";
            options[11].url2 = "";

            options[12].name = "今日头条";
            options[12].home = "https://www.toutiao.com/";
            options[12].url1 = "https://www.toutiao.com/search/?keyword=";
            options[12].url2 = "";

            options[13].name = "天涯社区";
            options[13].home = "https://bbs.tianya.cn/";
            options[13].url1 = "https://search.tianya.cn/bbs?q=";
            options[13].url2 = "";

            options[14].name = "CSDN";
            options[14].home = "https://www.csdn.net/";
            options[14].url1 = "https://so.csdn.net/so/search/s.do?q=";
            options[14].url2 = "";

            options[15].name = "博客园";
            options[15].home = "https://www.cnblogs.com/";
            options[15].url1 = "https://zzk.cnblogs.com/s?t=b&w=";
            options[15].url2 = "";

            options[16].name = "Github";
            options[16].home = "https://github.com/";
            options[16].url1 = "https://github.com/search?q=";
            options[16].url2 = "";

            options[17].name = "Stack Overflow";
            options[17].home = "https://stackoverflow.com/";
            options[17].url1 = "https://stackoverflow.com/search?q=";
            options[17].url2 = "";

            for (int i = 0; i < optionNum; i++)
            {
                var tb = new TextBlock();
                tb.Text = options[i].name;
                TabBar.Items.Add(tb);
            }

            uas = new UA[uaNum];

            uas[0].name = "电脑（Chrome）";
            uas[0].ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36 Edg/83.0.478.61";

            uas[1].name = "电脑（IE）";
            uas[1].ua = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0)";

            uas[2].name = "手机";
            uas[2].ua = "Mozilla/5.0 (Linux; Android 7.0; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/48.0.2564.116 Mobile Safari/537.36 T7/10.3 SearchCraft/2.6.2 (Baidu; P1 7.0)";

            uas[3].name = "手机（WAP）";
            uas[3].ua = "Mozilla/5.0 (Symbian/3; Series60/5.2 NokiaN8-00/012.002; Profile/MIDP-2.1 Configuration/CLDC-1.1 ) AppleWebKit/533.4 (KHTML, like Gecko) NokiaBrowser/7.3.0 Mobile Safari/533.4 3gpp-gba";
            
            uas[4].name = "手机（QQ浏览器）";
            uas[4].ua = "MQQBrowser/26 Mozilla/5.0 (Linux; U; Android 2.3.7; zh-cn; MB200 Build/GRJ22; CyanogenMod-7) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1";

            for (int i = 0; i < uaNum; i++)
            {
                var tb = new TextBlock();
                tb.Text = uas[i].name;
                UACB.Items.Add(tb);
            }
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.OpenFile();

            Back.IsEnabled = false;
            Forward.IsEnabled = false;
            this.HideTitleBar();
            SettingGrid.Visibility = Visibility.Collapsed;
            TabBar.SelectedIndex = 0;
            UACB.SelectedIndex = 0;
            SearchBar.Text = "";
        }

        private void Search()
        {
            Uri uri;
            if (SearchBar.Text.Equals(""))
            {
                uri = new Uri(options[TabBar.SelectedIndex].home);
            }
            else
            {
                uri = new Uri(
                    options[TabBar.SelectedIndex].url1 +
                    SearchBar.Text +
                    options[TabBar.SelectedIndex].url2
                );
            }
            WV_GotoPage(uri);
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
            WV_GotoPage(argss.Uri);
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

        private void History_Click(object sender, RoutedEventArgs e)
        {
            ;
        }
    }
}