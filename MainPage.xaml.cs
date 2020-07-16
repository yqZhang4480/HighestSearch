using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

namespace 聚合搜索
{
    #region classes
    public struct Tab
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
    public struct ViewHistory
    {
        public string title;
        public Uri uri;
    }
    #endregion

    public sealed partial class MainPage : Page
    {
        #region vars
        private Stack<Tab> tabs = new Stack<Tab>();
        private UA[] uas;
        private readonly int optionNum = 18;
        private readonly int uaNum = 5;
        private Stack<ViewHistory> viewHistory = new Stack<ViewHistory>();
        private Stack<string> searchHistory = new Stack<string>();

        private Windows.Storage.StorageFile searchHistoryFile;
        private Windows.Storage.StorageFile viewHistoryFile;
        private Windows.Storage.StorageFile TabFile;
        #endregion

        #region init
        private void HideTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.Red;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
        private async void OpenFile()
        {
            #region Open File
            Windows.Storage.StorageFolder storageFolder =
                Windows.Storage.ApplicationData.Current.LocalFolder;
            searchHistoryFile =
                await storageFolder.CreateFileAsync("SearchHistory.dat",
                    Windows.Storage.CreationCollisionOption.OpenIfExists);
            viewHistoryFile =
                await storageFolder.CreateFileAsync("ViewHistory.dat",
                    Windows.Storage.CreationCollisionOption.OpenIfExists);
            TabFile =
                await storageFolder.CreateFileAsync("Tabs.dat",
                    Windows.Storage.CreationCollisionOption.OpenIfExists);
            #endregion

            #region Search History
            string shText = await Windows.Storage.FileIO.ReadTextAsync(searchHistoryFile);
            searchHistory = new Stack<string>(shText.Split("\n"));
            searchHistory = new Stack<string>(searchHistory.ToArray().Where(p => !p.Equals("")));
            #endregion

            #region View History
            string vhText = await Windows.Storage.FileIO.ReadTextAsync(viewHistoryFile);
            string[] vhTextArray = vhText.Split("\n");
            if (vhTextArray.Count() > 1)
            {
                foreach (string vht in vhTextArray)
                {
                    string[] vhTextAA = vht.Split("\t");
                    if (vhTextAA.Count() != 2)
                    {
                        continue;
                    }
                    viewHistory.Push(new ViewHistory { title = vhTextAA[0], uri = new Uri(vhTextAA[1]) });
                }
                viewHistory = new Stack<ViewHistory>(viewHistory.ToArray());
            }
            #endregion

            #region History too much
            if (viewHistory.Count() + searchHistory.Count() < 20000)
            {
                HistoryGrid.Visibility = Visibility.Collapsed;
                HistoryNoticeTB.Visibility = Visibility.Collapsed;
            }
            else
            {
                SearchHistoryRB.IsChecked = true;
            }
            #endregion

            #region tabs
            string tabsText = await Windows.Storage.FileIO.ReadTextAsync(TabFile);
            string[] tabsTextArray = tabsText.Split("\n");

            if (tabsTextArray.Count() > 1)
            {
                foreach (string tabst in tabsTextArray)
                {
                    string[] tabsTextAA = tabst.Split("\t");
                    if (tabsTextAA.Count() != 4)
                    {
                        continue;
                    }

                    tabs.Push(new Tab { name = tabsTextAA[0], home = tabsTextAA[1], url1 = tabsTextAA[2], url2 = tabsTextAA[3] });
                }
                tabs = new Stack<Tab>(tabs.ToArray());
                PickTabItems();
            }
            #endregion

            #region UAs
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
                var tb = new TextBlock
                {
                    Text = uas[i].name
                };
                UACB.Items.Add(tb);
            }
            #endregion
        }
        public MainPage()
        {
            this.OpenFile();
            this.InitializeComponent();
            this.HideTitleBar();

            Back.IsEnabled = false;
            Forward.IsEnabled = false;
            SettingGrid.Visibility = Visibility.Collapsed;
            TabManageGrid.Visibility = Visibility.Collapsed;
            UACB.SelectedIndex = 0;
            TabBar.SelectedIndex = 0;
            SearchBar.Text = "";
        }
        #endregion

        #region Save Files
        private async Task SaveViewHistory()
        {
            string vhToString = "";
            foreach (ViewHistory vh in viewHistory.ToArray())
            {
                vhToString += vh.title + "\t" + vh.uri.AbsoluteUri + "\n";
            }
            await Windows.Storage.FileIO.WriteTextAsync(viewHistoryFile, vhToString);
        }
        private async Task SaveSearchHistory()
        {
            string shToString = "";
            foreach (string sh in searchHistory.ToArray())
            {
                shToString += sh + "\n";
            }
            await Windows.Storage.FileIO.WriteTextAsync(searchHistoryFile, shToString);
        }
        private async Task SaveTabs()
        {
            string tabsToString = "";
            foreach (Tab tab in tabs.ToArray())
            {
                tabsToString += tab.name + "\t" + tab.home + "\t" + tab.url1 + "\t" + tab.url2 + "\n";
            }
            await Windows.Storage.FileIO.WriteTextAsync(TabFile, tabsToString);
        }
        #endregion

        #region Title Buttons
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
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            WV_GotoPage(WV.Source);
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
        #endregion

        #region Search
        private async void Search()
        {
            Uri uri;
            if (SearchBar.Text.Equals(""))
            {
                try 
                { 
                    uri = new Uri(tabs.ElementAt(TabBar.SelectedIndex).home);
                }
                catch (UriFormatException)
                {
                    TabManageGrid.Visibility = Visibility.Visible;
                    TabManageNotice.Visibility = Visibility.Visible;
                    TabManageNotice.Text = "无法解析，请修正网址。";
                    return;
                }
            }
            else
            {
                uri = new Uri(
                    tabs.ElementAt(TabBar.SelectedIndex).url1 +
                    System.Web.HttpUtility.UrlEncode(SearchBar.Text) +
                    tabs.ElementAt(TabBar.SelectedIndex).url2
                );
            }

            WV_GotoPage(uri);

            if (searchHistory.Contains(SearchBar.Text))
            {
                searchHistory = new Stack<string>(searchHistory.ToArray().Where(p => !p.Equals(SearchBar.Text)).ToArray().Reverse());
            }
            if (SearchBar.Text != null && SearchBar.Text != "")
            {
                searchHistory.Push(SearchBar.Text);
            }

            await SaveSearchHistory();

            if (HistoryGrid.Visibility == Visibility.Visible)
            {
                if ((bool)ViewHistoryRB.IsChecked)
                {
                    PickViewHistoryItems();
                }
                else if ((bool)SearchHistoryRB.IsChecked)
                {
                    PickSearchHistoryItems();
                }
            }
        }
        private void SearchBar_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var a = (AutoSuggestBox)sender;
            if (!EnableSuggest.IsOn)
            {
                a.ItemsSource = null;
                return;
            }
            var filtered = searchHistory.ToArray().Where(p => p.Contains(a.Text)).ToArray();
            a.ItemsSource = filtered;
        }
        private void SearchBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            Search();
        }
        private void TabBar_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Search();
        }
        private void TabBar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabBar.SelectedIndex >= 0)
            {
                TabName.Text = tabs.ElementAt(TabBar.SelectedIndex).name;
                TabHome.Text = tabs.ElementAt(TabBar.SelectedIndex).home;
                TabUrl1.Text = tabs.ElementAt(TabBar.SelectedIndex).url1;
                TabUrl2.Text = tabs.ElementAt(TabBar.SelectedIndex).url2;
            }
        }
        #endregion

        #region WebView
        private void WV_GotoPage(Uri uri)
        {
            loadPR.IsActive = true;
            var tb = (TextBlock)TabBar.SelectedItem;
            Title.Text = $"聚合搜索 - {tb.Text} - 正在连接……";
            Windows.Web.Http.HttpRequestMessage req = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, uri);
            req.Headers.Referer = uri;
            if (UACB.SelectedIndex >= 0)
            {
                req.Headers.Add("User-Agent", uas[UACB.SelectedIndex].ua);
            }
            Back.IsEnabled = true;
            Forward.IsEnabled = false;
            try
            {
                WV.NavigateWithHttpRequestMessage(req);
            }
            catch (Exception)
            {
                TabManageGrid.Visibility = Visibility.Visible;
                TabManageNotice.Visibility = Visibility.Visible;
                TabManageNotice.Text = "未知错误，请尝试修正网址。";
            }
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
        private async void WV_LoadCompleted(object sender, NavigationEventArgs e)
        {
            loadPR.IsActive = false;
            var tb = (TextBlock)TabBar.SelectedItem;
            Title.Text = $"聚合搜索 - {tb.Text} - {SearchBar.Text}";
            if (SearchBar.Text.Equals(""))
            {
                Title.Text = $"聚合搜索 - {tb.Text}";
            }

            var h = new ViewHistory
            {
                title = WV.DocumentTitle,
                uri = WV.Source
            };
            if (h.title == "" || h.title == null)
            {
                return;
            }
            if (h.title.Contains("\t") || h.title.Contains("\n"))
            {
                h.title.Replace('\t', ' ');
                h.title.Replace('\n', ' ');
            }
            viewHistory = new Stack<ViewHistory>(viewHistory.ToArray().Where(p => !p.title.Equals(h.title)).ToArray().Reverse());
            viewHistory.Push(h);
            await SaveViewHistory();
            if (HistoryGrid.Visibility == Visibility.Visible && (bool)ViewHistoryRB.IsChecked)
            {
                PickViewHistoryItems();
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
        #endregion

        #region Setting
        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            SettingGrid.Visibility = Visibility.Visible;
        }
        private void SettingReturn_Click(object sender, RoutedEventArgs e)
        {
            SettingGrid.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region History
        private void History_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)SearchHistoryRB.IsChecked || !(bool)ViewHistoryRB.IsChecked)
            {
                SearchHistoryRB.IsChecked = true;
            }
            HistoryGrid.Visibility = Visibility.Visible;
        }
        private void HistoryReturn_Click(object sender, RoutedEventArgs e)
        {
            HistoryGrid.Visibility = Visibility.Collapsed;
        }
        private void SearchHistoryRB_Checked(object sender, RoutedEventArgs e)
        {
            PickSearchHistoryItems();
        }
        private void ViewHistoryRB_Checked(object sender, RoutedEventArgs e)
        {
            PickViewHistoryItems();
        }
        private async void DeleteSelectedHistory_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedHistoryFlyout.Hide();
            Stack<string> textArray = new Stack<string>();
            foreach (TextBlock tb in HistoryLB.SelectedItems)
            {
                textArray.Push(tb.Text);
            }
            if ((bool)SearchHistoryRB.IsChecked)
            {
                searchHistory = new Stack<string>(searchHistory.ToArray().Where(p => !textArray.Contains(p)).ToArray().Reverse());
                await SaveSearchHistory();
                PickSearchHistoryItems();
            }
            else if ((bool)ViewHistoryRB.IsChecked)
            {
                viewHistory = new Stack<ViewHistory>(viewHistory.ToArray().Where(p => !textArray.Contains(p.title)).ToArray().Reverse());
                await SaveViewHistory();
                PickViewHistoryItems();
            }
        }
        private async void DeleteUnselectedHistory_Click(object sender, RoutedEventArgs e)
        {
            DeleteUnselectedHistoryFlyout.Hide();
            Stack<string> textArray = new Stack<string>();
            foreach (TextBlock tb in HistoryLB.SelectedItems)
            {
                textArray.Push(tb.Text);
            }
            if ((bool)SearchHistoryRB.IsChecked)
            {
                searchHistory = new Stack<string>(searchHistory.ToArray().Where(p => textArray.Contains(p)).ToArray().Reverse());
                await SaveSearchHistory();
                PickSearchHistoryItems();
            }
            else if ((bool)ViewHistoryRB.IsChecked)
            {
                viewHistory = new Stack<ViewHistory>(viewHistory.ToArray().Where(p => textArray.Contains(p.title)).ToArray().Reverse());
                await SaveViewHistory();
                PickViewHistoryItems();
            }
        }
        private async void DeleteHalfHistory_Click(object sender, RoutedEventArgs e)
        {
            DeleteHalfHistoryFlyout.Hide();
            if ((bool)SearchHistoryRB.IsChecked)
            {
                searchHistory = new Stack<string>(searchHistory.ToArray().Take(searchHistory.Count / 2).Reverse());
                await SaveSearchHistory();
                PickSearchHistoryItems();
            }
            else if ((bool)ViewHistoryRB.IsChecked)
            {
                viewHistory = new Stack<ViewHistory>(viewHistory.ToArray().Take(viewHistory.Count / 2).Reverse());
                await SaveViewHistory();
                PickViewHistoryItems();
            }
        }
        private void PickSearchHistoryItems()
        {
            HistoryLB.Items.Clear();
            Stack<string> s = new Stack<string>(searchHistory.ToArray().Where(p => p.Contains(HistorySearchBar.Text)).ToArray().Reverse());
            Stack<TextBlock> textBlocks = new Stack<TextBlock>(s.ToArray().Select(p => new TextBlock { Text = p, TextDecorations = Windows.UI.Text.TextDecorations.Underline }).ToArray().Reverse());

            foreach (TextBlock tb in textBlocks)
            {
                tb.Tapped += new TappedEventHandler(SearchHistoryTextBlocks_Tapped);
                HistoryLB.Items.Add(tb);
            }
            HistoryNum.Text = "共" + HistoryLB.Items.Count + "条符合条件的记录。";
        }
        private void PickViewHistoryItems()
        {
            HistoryLB.Items.Clear();
            Stack<ViewHistory> s = new Stack<ViewHistory>(viewHistory.ToArray().Where(p => p.title.Contains(HistorySearchBar.Text)).ToArray().Reverse());
            Stack<TextBlock> textBlocks = new Stack<TextBlock>(s.ToArray().Select(p => new TextBlock { Text = p.title, TextDecorations = Windows.UI.Text.TextDecorations.Underline }).ToArray().Reverse());

            foreach (TextBlock tb in textBlocks)
            {
                tb.Tapped += new TappedEventHandler(this.ViewHistoryTextBlocks_Tapped);
                HistoryLB.Items.Add(tb);
            }

            HistoryNum.Text = "共" + HistoryLB.Items.Count + "条符合条件的记录。";
        }
        private void SearchHistoryTextBlocks_Tapped(object sender, RoutedEventArgs e)
        {
            var tb = (TextBlock)sender;
            SearchBar.Text = tb.Text;
            Search();
        }
        private void ViewHistoryTextBlocks_Tapped(object sender, RoutedEventArgs e)
        {
            var tb = (TextBlock)sender;
            foreach (ViewHistory vh in viewHistory)
            {
                if (vh.title == tb.Text)
                {
                    WV_GotoPage(vh.uri);
                    break;
                }
            }
        }
        private void HistorySearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((bool)ViewHistoryRB.IsChecked)
            {
                PickViewHistoryItems();
            }
            else if ((bool)SearchHistoryRB.IsChecked)
            {
                PickSearchHistoryItems();
            }
        }
        #endregion

        #region Tab Manage
        private void PickTabItems()
        {
            TabBar.Items.Clear();
            Stack<Tab> s = tabs;
            Stack<TextBlock> textBlocks = new Stack<TextBlock>(s.ToArray().Select(p => new TextBlock { Text = p.name }).ToArray().Reverse());

            foreach (TextBlock tb in textBlocks)
            {
                TabBar.Items.Add(tb);
            }
        }
        private void TabManage_Click(object sender, RoutedEventArgs e)
        {
            TabManageGrid.Visibility = Visibility.Visible;
            TabBar.Visibility = Visibility.Visible;
        }
        private void TabManageReturn_Click(object sender, RoutedEventArgs e)
        {
            TabManageGrid.Visibility = Visibility.Collapsed;
        }
        private async void TabAdd_Click(object sender, RoutedEventArgs e)
        {
            int index = TabBar.SelectedIndex;
            List<Tab> tabs_ = tabs.ToList();
            tabs_.Insert(index, new Tab { name = "新建搜索项", home = "about:blank", url1 = "about:blank", url2 = "" });
            tabs = new Stack<Tab>(tabs_.ToArray().Reverse());
            await SaveTabs();
            PickTabItems();
            TabBar.SelectedIndex = index;
        }
        private async void TabSave_Click(object sender, RoutedEventArgs e)
        {
            int index = TabBar.SelectedIndex;
            List<Tab> tabs_ = tabs.ToList();
            tabs_.RemoveAt(TabBar.SelectedIndex);
            tabs_.Insert(TabBar.SelectedIndex, new Tab { name = TabName.Text, home = TabHome.Text, url1 = TabUrl1.Text, url2 = TabUrl2.Text });
            tabs = new Stack<Tab>(tabs_.ToArray().Reverse());
            await SaveTabs();
            PickTabItems();
            TabBar.SelectedIndex = index;
        }
        private async void TabDelete_Click(object sender, RoutedEventArgs e)
        {
            int index = TabBar.SelectedIndex;
            List<Tab> tabs_ = tabs.ToList();
            tabs_.RemoveAt(index);
            tabs = new Stack<Tab>(tabs_.ToArray().Reverse());
            await SaveTabs();
            PickTabItems();
            TabBar.SelectedIndex = index <= 0 ? 0 : index - 1;
        }
        private async void TabMoveUp_Click(object sender, RoutedEventArgs e)
        {
            int index = TabBar.SelectedIndex;
            if (index - 1 >= 0)
            {
                var tabs_ = tabs.ToArray();
                var temp = new Tab
                {
                    name = tabs_[index - 1].name,
                    home = tabs_[index - 1].home,
                    url1 = tabs_[index - 1].url1,
                    url2 = tabs_[index - 1].url2
                };
                tabs_[index - 1] = tabs_[index];
                tabs_[index] = temp;
                tabs = new Stack<Tab>(tabs_.ToArray().Reverse());

                await SaveTabs();
                PickTabItems();
                TabBar.SelectedIndex = index - 1;
            }
        }
        private async void TabMoveDown_Click(object sender, RoutedEventArgs e)
        {
            int index = TabBar.SelectedIndex;
            if (index + 1 < tabs.Count())
            {
                var tabs_ = tabs.ToArray();
                var temp = new Tab
                {
                    name = tabs_[index].name,
                    home = tabs_[index].home,
                    url1 = tabs_[index].url1,
                    url2 = tabs_[index].url2
                };
                tabs_[index] = tabs_[index + 1];
                tabs_[index + 1] = temp;
                tabs = new Stack<Tab>(tabs_.ToArray().Reverse());

                await SaveTabs();
                PickTabItems();
                TabBar.SelectedIndex = index + 1;
            }
        }
        private void TabManageGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TabManageNotice.Visibility = Visibility.Collapsed;
        }
        #endregion

    }
}