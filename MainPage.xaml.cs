using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

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

    public static class AppResources
    {
        private static ResourceLoader CurrentResourceLoader
        {
            get { return _loader ?? (_loader = ResourceLoader.GetForCurrentView("Resources")); }
        }

        private static ResourceLoader _loader;
        private static readonly Dictionary<string, string> ResourceCache = new Dictionary<string, string>();

        public static string GetString(string key)
        {
            if (ResourceCache.TryGetValue(key, out string s))
            {
                return s;
            }
            else
            {
                s = CurrentResourceLoader.GetString(key);
                ResourceCache[key] = s;
                return s;
            }
        }
    }
    #endregion

    public sealed partial class MainPage : Page
    {

        #region vars
        private Stack<Tab> tabs = new Stack<Tab>();
        private UA[] uas;
        //private enum Layout { Horizontal, Tile };
        //private Layout layout = Layout.Horizontal;
        private readonly int uaNum = 4;
        private Stack<ViewHistory> viewHistory = new Stack<ViewHistory>();
        private Stack<string> searchHistory = new Stack<string>();
        private WebView WV;

        private StorageFile searchHistoryFile;
        private StorageFile viewHistoryFile;
        private StorageFile tabFile;
        private StorageFile darkModeJS;
        private StorageFile darkCSS;
        #endregion

        #region init
        private void HideTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(loadPR);
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
            titleBar.ButtonForegroundColor = Windows.UI.Colors.Black;
        }
        private void FullScreen()
        {
            ApplicationView view = ApplicationView.GetForCurrentView();

            bool isInFullScreenMode = view.IsFullScreenMode;

            if (isInFullScreenMode)
            {
                view.ExitFullScreenMode();
                TabBar.Visibility = Visibility.Visible;
                TitleGrid.Visibility = Visibility.Visible;
                ExitFullScreenButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                view.TryEnterFullScreenMode();
                TabBar.Visibility = Visibility.Collapsed;
                TitleGrid.Visibility = Visibility.Collapsed;
                ExitFullScreenButton.Visibility = Visibility.Visible;
            }
        }
        private async void OpenFile()
        {
            #region Open File
            StorageFolder storageFolder =
                ApplicationData.Current.LocalFolder;
            searchHistoryFile =
                await storageFolder.CreateFileAsync("SearchHistory.dat",
                    CreationCollisionOption.OpenIfExists);
            viewHistoryFile =
                await storageFolder.CreateFileAsync("ViewHistory.dat",
                    CreationCollisionOption.OpenIfExists);
            tabFile =
                await storageFolder.CreateFileAsync("Tabs.dat",
                    CreationCollisionOption.OpenIfExists);
            darkModeJS =
                await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///Assets/dark.css.js"));
            darkCSS =
                await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///Assets/dark.css"));

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
            if (viewHistory.Count() + searchHistory.Count() < 5000)
            {
                HistoryGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                PutErrorMessage(AppResources.GetString("Message_HistoryTooMuch"));
                SearchHistoryRB.IsChecked = true;
            }
            #endregion

            #region tabs
            string tabsText = await FileIO.ReadTextAsync(tabFile);
            if (tabsText == "")
            {
                tabFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/tabs.zh-cn.txt"));
                tabsText = await FileIO.ReadTextAsync(tabFile);
            }
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

            uas[0].name = "Chrome";
            uas[0].ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/83.0.4103.116";

            uas[1].name = "IE 10";
            uas[1].ua = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0)";

            uas[2].name = "Android";
            uas[2].ua = "Mozilla/5.0 (Linux; Android 7.0; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Mobile Safari/537.36 T7/10.3 SearchCraft/2.6.2 (Baidu; P1 7.0)";

            uas[3].name = "WAP";
            uas[3].ua = "Mozilla/5.0 (Symbian/3; Series60/5.2 NokiaN8-00/012.002; Profile/MIDP-2.1 Configuration/CLDC-1.1 ) AppleWebKit/533.4 (KHTML, like Gecko) NokiaBrowser/7.3.0 Mobile Safari/533.4 3gpp-gba";

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
            WV = WV1;
            SettingGrid.Visibility = Visibility.Collapsed;
            TabManageGrid.Visibility = Visibility.Collapsed;
            UACB.SelectedIndex = 0;
            TabBar.SelectedIndex = 0;
            SearchBar.Text = "";
            ErrorMessageGridRow.Height = new GridLength(0);
            OpenOutside.IsEnabled = false;
            FlyoutOpenOutside.IsEnabled = false;
            Refresh.IsEnabled = false;
            FlyoutRefresh.IsEnabled = false;
            CheckNavigationButtonState();
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
            try
            {
                await Windows.Storage.FileIO.WriteTextAsync(viewHistoryFile, vhToString);
            }
            catch (Exception) { }
        }
        private async Task SaveSearchHistory()
        {
            string shToString = "";
            foreach (string sh in searchHistory.ToArray())
            {
                shToString += sh + "\n";
            }
            try
            {
                await Windows.Storage.FileIO.WriteTextAsync(searchHistoryFile, shToString);
            }
            catch (Exception)
            {
            }
        }
        private async Task SaveTabs()
        {
            string tabsToString = "";
            foreach (Tab tab in tabs.ToArray())
            {
                tabsToString += tab.name + "\t" + tab.home + "\t" + tab.url1 + "\t" + tab.url2 + "\n";
            }
            try
            {
                await Windows.Storage.FileIO.WriteTextAsync(tabFile, tabsToString);
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region Title Buttons
        private void CheckNavigationButtonState()
        {
            if (WV.CanGoBack)
            {
                Back.IsEnabled = true;
                FlyoutBack.IsEnabled = true;
            }
            else
            {
                Back.IsEnabled = false;
                FlyoutBack.IsEnabled = false;
            }
            if (WV.CanGoForward)
            {
                Forward.IsEnabled = true;
                FlyoutForward.IsEnabled = true;
            }
            else
            {
                Forward.IsEnabled = false;
                FlyoutForward.IsEnabled = false;
            }
            LinkBar.Text = WV.Source.ToString();
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
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            WV_GotoPage(WV.Source);
        }
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            FullScreen();
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (WV.CanGoBack)
            {
                WV.GoBack();
            }
            CheckNavigationButtonState();
        }
        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (WV.CanGoForward)
            {
                WV.GoForward();
            }
            CheckNavigationButtonState();
        }
        private void OpenOutside_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(WV.Source);
        }
        private void CopyLink_Click(object sender, RoutedEventArgs e)
        {
            if (LinkBarGrid.Visibility == Visibility.Collapsed)
            {
                LinkBarGrid.Visibility = Visibility.Visible;
            }
            else
            {
                LinkBarGrid.Visibility = Visibility.Collapsed;
            }
        }
        private void ExitFullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            view.ExitFullScreenMode();
            TabBar.Visibility = Visibility.Visible;
            TitleGrid.Visibility = Visibility.Visible;
            ExitFullScreenButton.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Error Message Grid Row
        private void PutErrorMessage(string error)
        {
            ErrorMessageGridRow.Height = new GridLength(1, GridUnitType.Auto);
            ErrorMessage.Text = error;
        }
        private void ErrorMessageButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageGridRow.Height = new GridLength(0);
        }
        #endregion

        #region Link Bar
        private void GotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (LinkBar.Text == "")
            {
                return;
            }
            try
            {
                WV_GotoPage(new Uri(LinkBar.Text));
            }
            catch (Exception)
            {
                PutErrorMessage(AppResources.GetString("Message_CannotResolve_LinkBar"));
            }
        }
        private void LinkBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            GotoButton_Click(null, null);
        }
        #endregion

        #region Search
        private async void Search()
        {
            Uri uri;
            try
            {
                if (!tabs.ElementAt(TabBar.SelectedIndex).url1.Equals("") && !SearchBar.Text.Equals(""))
                {
                    uri = new Uri(
                        tabs.ElementAt(TabBar.SelectedIndex).url1 +
                        System.Web.HttpUtility.UrlEncode(SearchBar.Text) +
                        tabs.ElementAt(TabBar.SelectedIndex).url2
                    );
                }
                else
                {
                    uri = new Uri(tabs.ElementAt(TabBar.SelectedIndex).home);
                }
            }
            catch (UriFormatException)
            {
                PutErrorMessage(AppResources.GetString("Message_CannotResolve_Item"));
                return;
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
        private void SearchBar_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = (string)args.SelectedItem;
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
            OpenOutside.IsEnabled = true;
            FlyoutOpenOutside.IsEnabled = true;
            CopyLink.IsEnabled = true;
            FlyoutCopyLink.IsEnabled = true;
            Refresh.IsEnabled = true;
            FlyoutRefresh.IsEnabled = true;
            LinkBar.Text = WV.Source.ToString();
            var tb = (TextBlock)TabBar.SelectedItem;
            Windows.Web.Http.HttpRequestMessage req = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, uri);
            req.Headers.Referer = uri;
            if (UACB.SelectedIndex >= 0)
            {
                req.Headers.Add("User-Agent", uas[UACB.SelectedIndex].ua);
            }
            try
            {
                WV.NavigateWithHttpRequestMessage(req);
            }
            catch (Exception)
            {
                PutErrorMessage(AppResources.GetString("Message_CannotResolve_Unknown"));
            }
            CheckNavigationButtonState();
        }
        private void WV_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            args.Handled = true;
            WebViewNewWindowRequestedEventArgs argss = args;
            WV_GotoPage(argss.Uri);
        }
        private async void WV_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (!args.IsSuccess)
            {
                PutErrorMessage(AppResources.GetString("Message_AccessFailed") + args.WebErrorStatus.ToString());
            }
            if (WV.Source.Equals(new Uri("about:blank")))
            {
                return;
            }
            CheckNavigationButtonState();
            loadPR.IsActive = false;
            var tb = (TextBlock)TabBar.SelectedItem;

            var h = new ViewHistory
            {
                title = WV.DocumentTitle,
                uri = WV.Source
            };
            if (sender.Source.ToString().StartsWith("http://"))
            {
                PutErrorMessage(AppResources.GetString("Message_Unsafe"));
            }
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
        private async void WV_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            if (DarkModeToggleSwitch.IsOn && darkCSS != null && darkModeJS != null)
            {
                var CSSString = (await FileIO.ReadTextAsync(darkCSS)).Replace("\n", "");
                var JSString = (await FileIO.ReadTextAsync(darkModeJS)).Replace("__DARK_CSS__", CSSString);
                await WV.InvokeScriptAsync("eval", new string[] { JSString });
            }
            CheckNavigationButtonState();
        }      
        private void WV_UnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args)
        {
            PutErrorMessage(AppResources.GetString("Message_Unsupport_Content"));
        }
        private void WV_UnsupportedUriSchemeIdentified(WebView sender, WebViewUnsupportedUriSchemeIdentifiedEventArgs args)
        {
            PutErrorMessage(AppResources.GetString("Message_Unsupport_URI"));
        }
        private void WV_UnsafeContentWarningDisplaying(WebView sender, object args)
        {
            PutErrorMessage(AppResources.GetString("Message_Unsafe"));
        }
        #endregion

        #region Setting
        private void ChangeTheme(FrameworkElement element, ElementTheme theme)
        {
            element.RequestedTheme = theme;
            if (element.GetType() == typeof(Grid))
            {
                foreach (var e in ((Grid)element).Children)
                {
                    ChangeTheme((FrameworkElement)e, theme);
                }
            }
            if (element.GetType() == typeof(StackPanel))
            {
                foreach (var e in ((StackPanel)element).Children)
                {
                    ChangeTheme((FrameworkElement)e, theme);
                }
            }
        }
        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            SettingGrid.Visibility = Visibility.Visible;
        }
        private void SettingReturn_Click(object sender, RoutedEventArgs e)
        {
            SettingGrid.Visibility = Visibility.Collapsed;
        }
        private void Comment_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?productid=9P08CHLDB0Q1"));
        }
        private void About_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri("https://github.com/CS4480/Union-Find-Sets"));
        }
        private async void DarkModeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (DarkModeToggleSwitch.IsOn && darkCSS != null && darkModeJS != null)
            {
                var CSSString = (await FileIO.ReadTextAsync(darkCSS)).Replace("\n", "");
                var JSString = (await FileIO.ReadTextAsync(darkModeJS)).Replace("__DARK_CSS__", CSSString);
                await WV.InvokeScriptAsync("eval", new string[] { JSString });
                ChangeTheme(RootGrid, ElementTheme.Dark);
                ApplicationView.GetForCurrentView().TitleBar.ButtonForegroundColor = Colors.White;

            }
            else
            {
                ChangeTheme(RootGrid, ElementTheme.Light);
                ApplicationView.GetForCurrentView().TitleBar.ButtonForegroundColor = Colors.Black;
            }
        }
        private void EnableSuggest_Toggled(object sender, RoutedEventArgs e)
        {
            // 防止建议框闪烁
            if (((ToggleSwitch)sender).IsOn == false)
            {
                SearchBar.ItemsSource = null;
            }
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
            Stack<TextBlock> textBlocks = new Stack<TextBlock>(s.ToArray().Select(p => new TextBlock { Text = p, TextDecorations = Windows.UI.Text.TextDecorations.Underline, Margin = new Thickness(20, 0, 0, 0), FontSize = 12 }).ToArray().Reverse());

            foreach (TextBlock tb in textBlocks)
            {
                tb.Tapped += new TappedEventHandler(SearchHistoryTextBlocks_Tapped);
                tb.PointerEntered += new PointerEventHandler(this.HistoryTextBlocks_PointerEntered);
                tb.PointerExited += new PointerEventHandler(this.HistoryTextBlocks_PointerExited);
                HistoryLB.Items.Add(tb);
            }
            HistoryNum.Text = HistoryLB.Items.Count + AppResources.GetString("HistoryNumResults");
        }
        private void PickViewHistoryItems()
        {
            HistoryLB.Items.Clear();
            Stack<ViewHistory> s = new Stack<ViewHistory>(viewHistory.ToArray().Where(p => p.title.Contains(HistorySearchBar.Text)).ToArray().Reverse());
            Stack<TextBlock> textBlocks = new Stack<TextBlock>(s.ToArray().Select(p => new TextBlock { Text = p.title, TextDecorations = Windows.UI.Text.TextDecorations.Underline, Margin = new Thickness(20, 0, 0, 0), FontSize = 12 }).ToArray().Reverse());

            foreach (TextBlock tb in textBlocks)
            {
                tb.Tapped += new TappedEventHandler(this.ViewHistoryTextBlocks_Tapped);
                tb.PointerEntered += new PointerEventHandler(this.HistoryTextBlocks_PointerEntered);
                tb.PointerExited += new PointerEventHandler(this.HistoryTextBlocks_PointerExited);
                HistoryLB.Items.Add(tb);
            }

            HistoryNum.Text = HistoryLB.Items.Count + AppResources.GetString("HistoryNumResults");
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
        private void HistoryTextBlocks_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var tb = (TextBlock)sender;
            tb.Foreground = loadPR.Foreground;
        }
        private void HistoryTextBlocks_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var tb = (TextBlock)sender;
            tb.Foreground = textBlock.Foreground;
        }
        #endregion

        #region Tab Manage
        private void PickTabItems()
        {
            TabBar.Items.Clear();
            Stack<Tab> s = tabs;
            Stack<TextBlock> textBlocks = new Stack<TextBlock>(s.ToArray().Select(p => new TextBlock { Text = p.name, FontSize = 13, TextWrapping = TextWrapping.WrapWholeWords }).ToArray().Reverse());

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
        private void TabBar_RightTapped(object sender, RightTappedRoutedEventArgs e)
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
            tabs_.Insert(index, new Tab { name = AppResources.GetString("NewItem"), home = "about:blank", url1 = "about:blank", url2 = "" });
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
        #endregion

        #region WebView Layout
        private void OnlyShowWV_Click(object sender, RoutedEventArgs e)
        {
            WV1PB.Visibility = Visibility.Collapsed;
            WV2PB.Visibility = Visibility.Collapsed;
            if (((MenuFlyoutItem)sender).Equals(OnlyShowWV1) || ((MenuFlyoutItem)sender).Equals(FlyoutOnlyShowWV1))
            {
                OnlyShowWV(WV1);
                OffAllGrid();
                ShowGrid(C0);
            }
            else if (((MenuFlyoutItem)sender).Equals(OnlyShowWV2) || ((MenuFlyoutItem)sender).Equals(FlyoutOnlyShowWV2))
            {
                OnlyShowWV(WV2);
                OffAllGrid();
                ShowGrid(C1);
            }
            else
            {
                throw new Exception();
            }
        }
        private void WVTabToggle_Click(object sender, RoutedEventArgs e)
        {
            if (((MenuFlyoutItem)sender).Equals(WVTab1Toggle))
            {
                if (WVTab1Toggle.IsChecked || WVTab2Toggle.IsChecked)
                {
                    SwitchGrid(C0);
                }
                else
                {
                    WVTab1Toggle.IsChecked = true;
                }
                FlyoutWVTab1Toggle.IsChecked = WVTab1Toggle.IsChecked;
            }
            else if (((MenuFlyoutItem)sender).Equals(FlyoutWVTab1Toggle))
            {
                if (FlyoutWVTab1Toggle.IsChecked || WVTab2Toggle.IsChecked)
                {
                    SwitchGrid(C0);
                }
                else
                {
                    FlyoutWVTab1Toggle.IsChecked = true;
                }
                WVTab1Toggle.IsChecked = FlyoutWVTab1Toggle.IsChecked;
            }
            else if (((MenuFlyoutItem)sender).Equals(WVTab2Toggle))
            {
                if (WVTab2Toggle.IsChecked || WVTab1Toggle.IsChecked)
                {
                    SwitchGrid(C1);
                }
                else
                {
                    WVTab2Toggle.IsChecked = true;
                }
                FlyoutWVTab2Toggle.IsChecked = WVTab2Toggle.IsChecked;
            }
            else if (((MenuFlyoutItem)sender).Equals(FlyoutWVTab2Toggle))
            {
                if (FlyoutWVTab2Toggle.IsChecked || WVTab1Toggle.IsChecked)
                {
                    SwitchGrid(C1);
                }
                else
                {
                    FlyoutWVTab2Toggle.IsChecked = true;
                }
                WVTab2Toggle.IsChecked = FlyoutWVTab2Toggle.IsChecked;
            }
            else
            {
                throw new Exception();
            }
        }
        private void WV_GotFocus(object sender, RoutedEventArgs e)
        {
            WVFocus(sender);
        }

        private void WVFocus(object sender)
        {
            WV = (WebView)sender;
            CheckNavigationButtonState();
            if (WV == WV1)
            {
                WV1PB.Value = 100;
                WV2PB.Value = 0;
            }
            else
            {
                WV2PB.Value = 100;
                WV1PB.Value = 0;
            }
        }

        private void OnlyShowWV(WebView wv)
        {
            WVFocus(wv);
            CheckNavigationButtonState();

            if (WV == WV1)
            {
                WVTab1Toggle.IsChecked = true;
                WVTab2Toggle.IsChecked = false;
                FlyoutWVTab1Toggle.IsChecked = true;
                FlyoutWVTab2Toggle.IsChecked = false;
            }
            else
            {
                WVTab2Toggle.IsChecked = true;
                WVTab1Toggle.IsChecked = false;
                FlyoutWVTab2Toggle.IsChecked = true;
                FlyoutWVTab1Toggle.IsChecked = false;
            }
            //WV1.Visibility = Visibility.Collapsed;
            //WV2.Visibility = Visibility.Collapsed;
            //WV3.Visibility = Visibility.Collapsed;
            //WV4.Visibility = Visibility.Collapsed;
            //WV.Visibility = Visibility.Visible;
        }
        private void ShowGrid(ColumnDefinition C)
        {
            C.Width = new GridLength(1, GridUnitType.Star);
        }
        private void OffGrid(ColumnDefinition C)
        {
            C.Width = new GridLength(0, GridUnitType.Pixel);
        }
        private void OffAllGrid()
        {
            C0.Width = new GridLength(0, GridUnitType.Pixel);
            C1.Width = new GridLength(0, GridUnitType.Pixel);
        }
        private void SwitchGrid(ColumnDefinition C)
        {
            if (C.Width.Equals(new GridLength(1, GridUnitType.Star)))
            {
                OffGrid(C);
                WV1PB.Visibility = Visibility.Collapsed;
                WV2PB.Visibility = Visibility.Collapsed;
                if (C == C1)
                {
                    WVFocus(WV1);
                }
                else
                {
                    WVFocus(WV2);
                }
            }
            else if (C.Width.Equals(new GridLength(0, GridUnitType.Pixel)))
            {
                ShowGrid(C);
                WV1PB.Visibility = Visibility.Visible;
                WV2PB.Visibility = Visibility.Visible;
                if (C == C0)
                {
                    WVFocus(WV1);
                }
                else
                {
                    WVFocus(WV2);
                }
            }
            else
            {
                throw new Exception();
            }
            CheckNavigationButtonState();
        }

        #endregion

        private async void WV_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                WV_GotoPage(await e.DataView.GetWebLinkAsync());
            }
        }
        private void WV_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            e.DragUIOverride.Caption = "拖动链接到此处";
            e.DragUIOverride.IsGlyphVisible = true;
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
        }
        private void TitleGrid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            e.DragUIOverride.Caption = "拖动链接到此处";
            e.DragUIOverride.IsGlyphVisible = true;
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
        }
        private async void TitleGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                WV_GotoPage(await e.DataView.GetWebLinkAsync());
            }
        }

        #region WV_DEBUG
        private async void WV1_ActualThemeChanged(FrameworkElement sender, object args)
        {
            Debug.WriteLine($"ActualThemeChanged {WV1.Source}");
        }
        private void WV1_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            Debug.WriteLine($"ContextRequested {WV1.Source}");
        }
        private void WV1_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Debug.WriteLine($"DataContextChanged {WV1.Source}");
        }
        private void WV1_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            Debug.WriteLine($"DOMContentLoaded {WV1.Source}");
        }
        private void WV1_FrameContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            Debug.WriteLine($"FrameContentLoading {WV1.Source}");
        }
        private void WV1_FrameNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            Debug.WriteLine($"FrameNavigationCompleted {WV1.Source}");
        }
        private void WV1_LoadCompleted(object sender, NavigationEventArgs e)
        {
            Debug.WriteLine($"LoadCompleted {WV1.Source}");
        }
        private void WV1_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Loaded {WV1.Source}");
        }
        private void WV1_Loading(FrameworkElement sender, object args)
        {
            Debug.WriteLine($"Loading {WV1.Source}");
        } 
        #endregion
    }
}