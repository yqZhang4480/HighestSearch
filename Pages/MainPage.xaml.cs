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
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Text;

namespace 聚合搜索
{
    #region classes
    public struct UA
    {
        public string name;
        public string ua;
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
        private UA[] uas;
        private readonly int uaNum = 2;
        private User user = new User();
        private WebView WV;

        private StorageFile searchHistoryFile;
        private StorageFile viewHistoryFile;
        private StorageFile entranceSetFile;
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
                //TabBar.Visibility = Visibility.Visible;
                TitleGrid.Visibility = Visibility.Visible;
                ExitFullScreenButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                view.TryEnterFullScreenMode();
                //TabBar.Visibility = Visibility.Collapsed;
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
                await storageFolder.CreateFileAsync("SearchHistories.json",
                    CreationCollisionOption.OpenIfExists);
            viewHistoryFile =
                await storageFolder.CreateFileAsync("ViewHistories.json",
                    CreationCollisionOption.OpenIfExists);
            entranceSetFile =
                await storageFolder.CreateFileAsync("EntranceSets.json",
                    CreationCollisionOption.OpenIfExists);
            darkModeJS =
                await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///Assets/dark.css.js"));
            darkCSS =
                await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///Assets/dark.css"));
            #endregion

            #region Search History
            string shText = await FileIO.ReadTextAsync(searchHistoryFile);
            user.SearchHistories = JsonConvert.DeserializeObject<List<SearchHistory>>(shText);
            #endregion

            #region View History
            string vhText = await FileIO.ReadTextAsync(viewHistoryFile);
            user.ViewHistories = JsonConvert.DeserializeObject<List<ViewHistory>>(vhText);
            #endregion

            #region History too much
            if (user.ViewHistories.Count() + user.SearchHistories.Count() < 5000)
            {
                HistoryGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                PutErrorMessage(AppResources.GetString("Message_HistoryTooMuch"));
                SearchHistoryRB.IsChecked = true;
            }
            #endregion

            #region Entrances
            string entrancesText = await FileIO.ReadTextAsync(entranceSetFile);
            if (entrancesText == "")
            {
                var temp = entranceSetFile;
                entranceSetFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/default/tabs.txt"));
                entrancesText = await FileIO.ReadTextAsync(entranceSetFile);
                entranceSetFile = temp;
            }

            user.EntranceSets = JsonConvert.DeserializeObject<List<EntranceSet>>(entrancesText);
            PickEntranceSets();
            SaveEntranceSets();
            SaveSearchHistories();
            SaveViewHistories();

            #endregion

            #region UAs
            uas = new UA[uaNum];

            uas[0].name = "None";
            uas[0].ua = "";

            uas[1].name = "Android";
            uas[1].ua = "Mozilla/5.0 (Linux; Android 7.0; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Mobile Safari/537.36 T7/10.3 SearchCraft/2.6.2 (Baidu; P1 7.0)";

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
            OpenFile();
            InitializeComponent();
            HideTitleBar();
            WV = WV1;
            SettingGrid.Visibility = Visibility.Collapsed;
            TabManageGrid.Visibility = Visibility.Collapsed;
            UACB.SelectedIndex = 0;
            //TabBar.SelectedIndex = 0;
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
        private async Task SaveViewHistories()
        {
            var vhToString = JsonConvert.SerializeObject(user.ViewHistories);
            try
            {
                await Windows.Storage.FileIO.WriteTextAsync(viewHistoryFile, vhToString);
            }
            catch (Exception) { }
        }
        private async Task SaveSearchHistories()
        {
            var shToString = JsonConvert.SerializeObject(user.SearchHistories);
            try
            {
                await Windows.Storage.FileIO.WriteTextAsync(searchHistoryFile, shToString);
            }
            catch (Exception) { }
        }
        private async Task SaveEntranceSets()
        {
            string tabsToString = JsonConvert.SerializeObject(user.EntranceSets);
            try
            {
                await FileIO.WriteTextAsync(entranceSetFile, tabsToString);
            }
            catch (Exception) { }
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
            if (EntranceStackPanel.Visibility == Visibility.Collapsed)
            {
                EntranceStackPanel.Visibility = Visibility.Visible;
            }
            else
            {
                EntranceStackPanel.Visibility = Visibility.Collapsed;
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
            EntranceStackPanel.Visibility = Visibility.Visible;
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
                loadPR.ShowError = true;
            }
        }
        private void LinkBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            GotoButton_Click(null, null);
        }
        #endregion

        #region Search
        private void Search()
        {
            Uri uri;
            if (null == EntrancesBar.SelectedItem)
            {
                EntrancesBar.SelectedIndex = 0;
            }
            var selectedEntrance = (EntrancesBar.SelectedItem as SuggestionItem).Source as Entrance;
            try
            {
                if (!selectedEntrance.UrlWithArg.Equals("") && !SearchBar.Text.Equals(""))
                {
                    uri = new Uri(
                        selectedEntrance.UrlWithArg.Replace("%%", System.Web.HttpUtility.UrlEncode(SearchBar.Text))
                    );
                }
                else
                {
                    uri = new Uri(selectedEntrance.UrlWithoutArg);
                }
            }
            catch (UriFormatException)
            {
                PutErrorMessage(AppResources.GetString("Message_CannotResolve_Item"));
                loadPR.ShowError = true;
                return;
            }

            WV_GotoPage(uri);
            bool changeflag = true;
            while (changeflag)
            {
                changeflag = false;
                foreach (var sh in user.SearchHistories)
                {

                    if (sh.Text == SearchBar.Text /*&& sh.Time.Date == DateTime.Now.Date*/)
                    {
                        user.SearchHistories.Remove(sh);
                        changeflag = true;
                        break;
                    }
                }
            }
            if (SearchBar.Text != null && SearchBar.Text != "")
            {
                user.SearchHistories.Insert(0, new SearchHistory { Text = SearchBar.Text, Time = DateTime.Now });
            }

            SaveSearchHistories();

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
            if (a.Text == "" || !EnableSuggest.IsOn)
            {
                a.ItemsSource = null;
                return;
            }
            var filteredSearchHistories = user.SearchHistories.Where(p => p.Text.Contains(a.Text) && p.Text != a.Text).Select(p => p.ToControl()).ToArray();
            foreach (var shc in filteredSearchHistories)
            {
                shc.Notes.Insert(0, "搜索记录");
                shc.IconText = "\uE1A3";
            }
            var filteredViewHistories = user.ViewHistories.Where(p => p.Title.Contains(a.Text) || p.Url.Contains(a.Text)).Select(p => p.ToControl()).ToArray();
            foreach (var vhc in filteredViewHistories)
            {
                vhc.Notes.Insert(0, "浏览记录");
                vhc.IconText = "\uE12B";
            }
            a.ItemsSource = filteredSearchHistories.Union(filteredViewHistories);
        }
        private void SearchBar_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if ((args.SelectedItem as SuggestionItem).IconText == "\uE1A3")
            {
                var sh = (args.SelectedItem as SuggestionItem).Source as SearchHistory;
                sender.Text = sh.Text;
            }
            else if ((args.SelectedItem as SuggestionItem).IconText == "\uE12B")
            {
                var sh = (args.SelectedItem as SuggestionItem).Source as ViewHistory;
                sender.Text = sh.Title;
            }
            else
            {
                sender.Text = "";
            }
        }
        private void SearchBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {

            if (args.ChosenSuggestion != null)
            {
                if ((args.ChosenSuggestion as SuggestionItem).IconText == "\uE1A3")
                {
                    Search();
                }
                else if ((args.ChosenSuggestion as SuggestionItem).IconText == "\uE12B")
                {
                    WV_GotoPage(new Uri(((args.ChosenSuggestion as SuggestionItem).Source as ViewHistory).Url));
                }
            }
            else
            {
                Search();
            }
        }
        private void TabBar_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Search();
        }
        private void TabBar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EntrancesBar.SelectedIndex >= 0)
            {
                Entrance entrance = (EntrancesBar.SelectedItem as SuggestionItem).Source as Entrance;
                TabName.Text = entrance.Name;
                TabHome.Text = entrance.UrlWithoutArg;
                TabUrl1.Text = entrance.UrlWithArg;
            }
        }
        #endregion

        #region WebView
        private void WV_GotoPage(Uri uri)
        {
            loadPR.ShowError = false;
            loadPR.ShowPaused = false;
            OpenOutside.IsEnabled = true;
            FlyoutOpenOutside.IsEnabled = true;
            CopyLink.IsEnabled = true;
            FlyoutCopyLink.IsEnabled = true;
            Refresh.IsEnabled = true;
            FlyoutRefresh.IsEnabled = true;
            LinkBar.Text = WV.Source.ToString();
            Windows.Web.Http.HttpRequestMessage req = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, uri);
            req.Headers.Referer = uri;
            if (UACB.SelectedIndex > 0)
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
                loadPR.ShowError = true;
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
                loadPR.ShowError = true;
            }
            if (WV.Source.Equals(new Uri("about:blank")))
            {
                return;
            }
            CheckNavigationButtonState();
            loadPR.ShowPaused = true;
            if (sender.Source.ToString().StartsWith("http://"))
            {
                PutErrorMessage(AppResources.GetString("Message_Unsafe"));
            }

            do
            {
                var h = new ViewHistory
                {
                    Title = WV.DocumentTitle,
                    Url = WV.Source.ToString(),
                    Time = DateTime.Now
                };
                if (h.Title == "" || h.Title == null)
                {
                    break;
                }
                if (h.Title.Contains("\t") || h.Title.Contains("\n"))
                {
                    h.Title.Replace('\t', ' ');
                    h.Title.Replace('\n', ' ');
                }
                foreach (var vh in user.ViewHistories)
                {
                    if (vh.Title == h.Title)
                    {
                        user.ViewHistories.Remove(vh);
                        break;
                    }
                }
                user.ViewHistories.Insert(0, h);
            } while (false);
            SaveViewHistories();
            if (HistoryGrid.Visibility == Visibility.Visible && (bool)ViewHistoryRB.IsChecked)
            {
                PickViewHistoryItems();
            }

            //var h = new ViewHistory
            //{
            //    title = WV.DocumentTitle,
            //    uri = WV.Source
            //};
            //if (sender.Source.ToString().StartsWith("http://"))
            //{
            //    PutErrorMessage(AppResources.GetString("Message_Unsafe"));
            //}
            //if (h.title == "" || h.title == null)
            //{
            //    return;
            //}
            //if (h.title.Contains("\t") || h.title.Contains("\n"))
            //{
            //    h.title.Replace('\t', ' ');
            //    h.title.Replace('\n', ' ');
            //}
            //viewHistory = new Stack<ViewHistory>(viewHistory.ToArray().Where(p => !p.title.Equals(h.title)).ToArray().Reverse());
            //viewHistory.Push(h);
            //await SaveViewHistory();
            //if (HistoryGrid.Visibility == Visibility.Visible && (bool)ViewHistoryRB.IsChecked)
            //{
            //    PickViewHistoryItems();
            //}
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
        private async void SendUserInformationButton_Click(object sender, RoutedEventArgs e)
        {
            var uname = UserNameTextBox.Text;
            var upwd = PasswordTextBox.Text;
            if (uname == "" || upwd == "")
            {
                MessageTextBlock.Text = "请输入内容！";
                return;

            }
            var text = HostTextBox.Text;
            LoginProgressRing.Visibility = Visibility.Visible;
            LoginTextBlock.Visibility = Visibility.Collapsed;
            byte[] output = new byte[256];
            string errorStr = "未知错误。";
            await Task.Run(() =>
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    s.Connect(text, 9999);
                }
                catch (Exception)
                {
                    errorStr = "连接失败。";
                    return;
                }
                var n = new NetworkStream(s);
                var utf8encoder = new UTF8Encoding(false);
                byte[] input = utf8encoder.GetBytes($"UserName={uname}&UserPassWord={upwd}");
                int utflen = input.Length;
                byte a = (byte)((utflen >> 8) & 0xFF);
                byte b = (byte)((utflen >> 0) & 0xFF);
                byte[] bytSendUTF = new byte[utflen + 2];
                bytSendUTF[0] = a;
                bytSendUTF[1] = b;
                Array.Copy(input, 0, bytSendUTF, 2, utflen);
                int iCount = 0;
                Debug.WriteLine(bytSendUTF.Length);
                while (iCount < bytSendUTF.Length)
                {
                    if (iCount + 1024 > bytSendUTF.Length)
                    {
                        n.Write(bytSendUTF, iCount, bytSendUTF.Length - iCount);
                        iCount = bytSendUTF.Length;
                    }
                    else
                    {
                        n.Write(bytSendUTF, iCount, 1024);
                        iCount += 1024;
                    }
                }//while

                //n.Write(input, 0, input.Length);
                n.Flush();
                n.Read(output, 0, 256);
                n.Close();
                s.Close();
            });
            MessageTextBlock.Text = Encoding.UTF8.GetString(output);
            LoginProgressRing.Visibility = Visibility.Collapsed;
            LoginTextBlock.Visibility = Visibility.Visible;
            if (MessageTextBlock != null && MessageTextBlock.Text == "")
            {
                MessageTextBlock.Text = errorStr;
            }

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
            if ((bool)SearchHistoryRB.IsChecked)
            {
                user.SearchHistories.RemoveAll(HistoryLB.SelectedItems.Select(p => (p as SuggestionItem).Source).Contains);
                await SaveSearchHistories();
                PickSearchHistoryItems();
            }
            else if ((bool)ViewHistoryRB.IsChecked)
            {
                user.ViewHistories.RemoveAll(HistoryLB.SelectedItems.Select(p => (p as SuggestionItem).Source).Contains);
                await SaveViewHistories();
                PickViewHistoryItems();
            }

            //foreach (TextBlock tb in HistoryLB.SelectedItems)
            //{
            //    textArray.Push(tb.Text);
            //}
            //if ((bool)SearchHistoryRB.IsChecked)
            //{
            //    searchHistory = new Stack<string>(searchHistory.ToArray().Where(p => !textArray.Contains(p)).ToArray().Reverse());
            //    await SaveSearchHistory();
            //    PickSearchHistoryItems();
            //}
            //else if ((bool)ViewHistoryRB.IsChecked)
            //{
            //    viewHistory = new Stack<ViewHistory>(viewHistory.ToArray().Where(p => !textArray.Contains(p.title)).ToArray().Reverse());
            //    await SaveViewHistory();
            //    PickViewHistoryItems();
            //}
        }
        private async void DeleteUnselectedHistory_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedHistoryFlyout.Hide();
            if ((bool)SearchHistoryRB.IsChecked)
            {
                user.SearchHistories.RemoveAll(q => !HistoryLB.SelectedItems.Select(p => (p as SuggestionItem).Source).Contains(q));
                await SaveSearchHistories();
                PickSearchHistoryItems();
            }
            else if ((bool)ViewHistoryRB.IsChecked)
            {
                user.ViewHistories.RemoveAll(q => !HistoryLB.SelectedItems.Select(p => (p as SuggestionItem).Source).Contains(q));
                await SaveViewHistories();
                PickViewHistoryItems();
            }
            //DeleteUnselectedHistoryFlyout.Hide();
            //Stack<string> textArray = new Stack<string>();
            //foreach (TextBlock tb in HistoryLB.SelectedItems)
            //{
            //    textArray.Push(tb.Text);
            //}
            //if ((bool)SearchHistoryRB.IsChecked)
            //{
            //    searchHistory = new Stack<string>(searchHistory.ToArray().Where(p => textArray.Contains(p)).ToArray().Reverse());
            //    await SaveSearchHistory();
            //    PickSearchHistoryItems();
            //}
            //else if ((bool)ViewHistoryRB.IsChecked)
            //{
            //    viewHistory = new Stack<ViewHistory>(viewHistory.ToArray().Where(p => textArray.Contains(p.title)).ToArray().Reverse());
            //    await SaveViewHistory();
            //    PickViewHistoryItems();
            //}
        }
        private async void DeleteHalfHistory_Click(object sender, RoutedEventArgs e)
        {
            DeleteHalfHistoryFlyout.Hide();
            if ((bool)SearchHistoryRB.IsChecked)
            {

                var searchHistories = user.SearchHistories;
                user.SearchHistories = new List<SearchHistory>(searchHistories.Take(searchHistories.Count / 2));
                await SaveSearchHistories();
                PickSearchHistoryItems();
            }
            else if ((bool)ViewHistoryRB.IsChecked)
            {
                var viewHistories = user.ViewHistories;
                user.ViewHistories = new List<ViewHistory>(viewHistories.Take(viewHistories.Count / 2));
                await SaveViewHistories();
                PickViewHistoryItems();
            }
            //DeleteHalfHistoryFlyout.Hide();
            //if ((bool)SearchHistoryRB.IsChecked)
            //{
            //    searchHistory = new Stack<string>(searchHistory.ToArray().Take(searchHistory.Count / 2).Reverse());
            //    await SaveSearchHistory();
            //    PickSearchHistoryItems();
            //}
            //else if ((bool)ViewHistoryRB.IsChecked)
            //{
            //    viewHistory = new Stack<ViewHistory>(viewHistory.ToArray().Take(viewHistory.Count / 2).Reverse());
            //    await SaveViewHistory();
            //    PickViewHistoryItems();
            //}
        }
        public bool HistoryFilter(SearchHistory sh, string str)
        {
            string text = $"{sh.Text}\n{sh.Time}";
            foreach (var pattern in str.Split(' '))
            {
                if (!text.Contains(pattern))
                {
                    return false;
                }
            }
            return true;
        }
        public bool HistoryFilter(ViewHistory vh, string str)
        {
            string text;
            try
            {
                text = $"{vh.Title}\n{vh.Url.Split('/')[2]}\n{vh.Time}";
            }
            catch (Exception)
            {
                text = $"{vh.Title}\n{vh.Url}\n{vh.Time}";
            }
            foreach (var pattern in str.Split(' '))
            {
                if (!text.Contains(pattern))
                {
                    return false;
                }
            }
            return true;
        }
        private void PickSearchHistoryItems()
        {
            HistoryLB.Items.Clear();
            foreach (var shc in user.SearchHistories.Where(p => HistoryFilter(p, HistorySearchBar.Text)).Select(p => p.ToControl()))
            {
                HistoryLB.Items.Add(shc);
                shc.Tapped += new TappedEventHandler(SearchHistory_Tapped);
                shc.PointerEntered += new PointerEventHandler(History_PointerEntered);
                shc.PointerExited += new PointerEventHandler(History_PointerExited);
            }
            HistoryNum.Text = HistoryLB.Items.Count + AppResources.GetString("HistoryNumResults");

            //HistoryLB.Items.Clear();
            //Stack<string> s = new Stack<string>(searchHistory.ToArray().Where(p => p.Contains(HistorySearchBar.Text)).ToArray().Reverse());
            //Stack<TextBlock> textBlocks = new Stack<TextBlock>(s.ToArray().Select(p => new TextBlock { Text = p, TextDecorations = Windows.UI.Text.TextDecorations.Underline, Margin = new Thickness(20, 0, 0, 0), FontSize = 12 }).ToArray().Reverse());

            //foreach (TextBlock tb in textBlocks)
            //{
            //    tb.Tapped += new TappedEventHandler(SearchHistoryTextBlocks_Tapped);
            //    tb.PointerEntered += new PointerEventHandler(this.HistoryTextBlocks_PointerEntered);
            //    tb.PointerExited += new PointerEventHandler(this.HistoryTextBlocks_PointerExited);
            //    HistoryLB.Items.Add(tb);
            //}
            //HistoryNum.Text = HistoryLB.Items.Count + AppResources.GetString("HistoryNumResults");
        }
        private void PickViewHistoryItems()
        {
            HistoryLB.Items.Clear();
            foreach (var vhc in user.ViewHistories.Where(p => HistoryFilter(p, HistorySearchBar.Text)).Select(p => p.ToControl()))
            {
                HistoryLB.Items.Add(vhc);
                vhc.Tapped += new TappedEventHandler(ViewHistory_Tapped);
                vhc.PointerEntered += new PointerEventHandler(History_PointerEntered);
                vhc.PointerExited += new PointerEventHandler(History_PointerExited);
            }
            HistoryNum.Text = HistoryLB.Items.Count + AppResources.GetString("HistoryNumResults");

            //HistoryLB.Items.Clear();
            //Stack<ViewHistory> s = new Stack<ViewHistory>(viewHistory.ToArray().Where(p => p.title.Contains(HistorySearchBar.Text)).ToArray().Reverse());
            //Stack<TextBlock> textBlocks = new Stack<TextBlock>(s.ToArray().Select(p => new TextBlock { Text = p.title, TextDecorations = Windows.UI.Text.TextDecorations.Underline, Margin = new Thickness(20, 0, 0, 0), FontSize = 12 }).ToArray().Reverse());

            //foreach (TextBlock tb in textBlocks)
            //{
            //    tb.Tapped += new TappedEventHandler(this.ViewHistoryTextBlocks_Tapped);
            //    tb.PointerEntered += new PointerEventHandler(this.HistoryTextBlocks_PointerEntered);
            //    tb.PointerExited += new PointerEventHandler(this.HistoryTextBlocks_PointerExited);
            //    HistoryLB.Items.Add(tb);
            //}

            //HistoryNum.Text = HistoryLB.Items.Count + AppResources.GetString("HistoryNumResults");
        }
        private void SearchHistory_Tapped(object sender, RoutedEventArgs e)
        {
            var tb = (SuggestionItem)sender;
            SearchBar.Text = tb.Title;
            Search();
        }
        private void ViewHistory_Tapped(object sender, RoutedEventArgs e)
        {
            var tb = (SuggestionItem)sender;
            WV_GotoPage(new Uri((tb.Source as ViewHistory).Url));
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
        private void History_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var tb = (SuggestionItem)sender;
            tb.Foreground = loadPR.Foreground;
        }
        private void History_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var tb = (SuggestionItem)sender;
            tb.Foreground = textBlock.Foreground;
        }
        #endregion

        #region Tab Manage
        public void PickEntrances()
        {
            EntrancesBar.Items.Clear();
            if (null == EntranceSetsBar.SelectedItem)
            {
                EntranceSetsBar.SelectedIndex = 0;
            }
            try
            {
                foreach (var e in
                    ((EntranceSetsBar.SelectedItem as SuggestionItem).Source
                    as EntranceSet).Entrances)
                {
                    EntrancesBar.Items.Add(e.ToControl());
                }

                if (EntrancesBar.Items.Count <= 0)
                {
                    ((EntranceSetsBar.SelectedItem as SuggestionItem).Source
                        as EntranceSet).Entrances.Add(new Entrance());
                }
            }
            catch (NullReferenceException) { return; }
        }
        private void EntranceSetsBar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PickEntrances();
            try
            {
                EntranceSetName.Text = ((EntranceSetsBar.SelectedItem as SuggestionItem).Source as EntranceSet).Name;
                EntranceSetDescription.Text = ((EntranceSetsBar.SelectedItem as SuggestionItem).Source as EntranceSet).Description;
            }
            catch (NullReferenceException) { return; }

        }
        private void PickEntranceSets()
        {
            EntranceSetsBar.Items.Clear();
            foreach (var es in user.EntranceSets)
            {
                EntranceSetsBar.Items.Add(es.ToControl());
            }
            if (null == EntranceSetsBar.SelectedItem)
            {
                EntranceSetsBar.SelectedIndex = 0;
            }
            PickEntrances();
        }
        private void TabManage_Click(object sender, RoutedEventArgs e)
        {
            TabManageGrid.Visibility = Visibility.Visible;
            //TabBar.Visibility = Visibility.Visible;
        }
        private void TabBar_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (EntrancesBar.SelectedIndex < 0)
            {
                EntrancesBar.SelectedIndex = 0;
            }
            TabManageGrid.Visibility = Visibility.Visible;
            //TabBar.Visibility = Visibility.Visible;
        }
        private void TabManageReturn_Click(object sender, RoutedEventArgs e)
        {
            TabManageGrid.Visibility = Visibility.Collapsed;
        }

        private void TabAdd_Click(object sender, RoutedEventArgs e)
        {
            if (null == EntrancesBar.SelectedItem)
            {
                EntrancesBar.SelectedIndex = 0;
            }
            int index = EntrancesBar.SelectedIndex;
            ((EntranceSetsBar.SelectedItem as SuggestionItem).Source as EntranceSet).Entrances.Insert(index, new Entrance());
            PickEntrances();
            SaveEntranceSets();
            EntrancesBar.SelectedIndex = index;
            //int index = TabBar.SelectedIndex;
            //List<Tab> tabs_ = tabs.ToList();
            //tabs_.Insert(index, new Tab { name = AppResources.GetString("NewItem"), home = "about:blank", url1 = "about:blank", url2 = "" });
            //tabs = new Stack<Tab>(tabs_.ToArray().Reverse());
            //await SaveEntrances();
            //PickEntranceSets();
            //TabBar.SelectedIndex = index;
        }
        private void AddEntranceSetButton_Click(object sender, RoutedEventArgs e)
        {
            int index = EntranceSetsBar.SelectedIndex;
            if (index < 0)
            {
                index = 0;
            }
            user.EntranceSets.Insert(index, new EntranceSet());
            PickEntranceSets();
            SaveEntranceSets();
            EntranceSetsBar.SelectedIndex = index;
            //int index = TabBar.SelectedIndex;
            //List<Tab> tabs_ = tabs.ToList();
            //tabs_.Insert(index, new Tab { name = AppResources.GetString("NewItem"), home = "about:blank", url1 = "about:blank", url2 = "" });
            //tabs = new Stack<Tab>(tabs_.ToArray().Reverse());
            //await SaveEntrances();
            //PickEntranceSets();
            //TabBar.SelectedIndex = index;
        }

        private void TabSave_Click(object sender, RoutedEventArgs e)
        {
            int index = EntrancesBar.SelectedIndex;
            if (index < 0)
            {
                return;
            }
            ((EntrancesBar.SelectedItem as SuggestionItem).Source as Entrance).Name = TabName.Text;
            ((EntrancesBar.SelectedItem as SuggestionItem).Source as Entrance).UrlWithoutArg = TabHome.Text;
            ((EntrancesBar.SelectedItem as SuggestionItem).Source as Entrance).UrlWithArg = TabUrl1.Text;
            SaveEntranceSets();
            PickEntrances();
            EntrancesBar.SelectedIndex = index;
            //int index = TabBar.SelectedIndex;
            //List<Tab> tabs_ = tabs.ToList();
            //tabs_.RemoveAt(TabBar.SelectedIndex);
            //tabs_.Insert(TabBar.SelectedIndex, new Tab { name = TabName.Text, home = TabHome.Text, url1 = TabUrl1.Text, url2 = TabUrl2.Text });
            //tabs = new Stack<Tab>(tabs_.ToArray().Reverse());
            //await SaveEntrances();
            //PickEntranceSets();
            //TabBar.SelectedIndex = index;
        }
        private void EntranceSetSave_Click(object sender, RoutedEventArgs e)
        {
            int index = EntranceSetsBar.SelectedIndex;
            if (index < 0)
            {
                return;
            }
            ((EntranceSetsBar.SelectedItem as SuggestionItem).Source as EntranceSet).Name = EntranceSetName.Text;
            ((EntranceSetsBar.SelectedItem as SuggestionItem).Source as EntranceSet).Description = EntranceSetDescription.Text;
            EntranceSetsBar.SelectedIndex = index;
            SaveEntranceSets();
            PickEntranceSets();
        }
        private async void TabMoveUp_Click(object sender, RoutedEventArgs e)
        {
            int index = EntrancesBar.SelectedIndex;
            if (index - 1 >= 0)
            {
                List<Entrance> entrances = user.EntranceSets[EntranceSetsBar.SelectedIndex].Entrances;
                Entrance[] tabs_ = entrances.ToArray();
                var temp = new Entrance
                {
                    Name = tabs_[index - 1].Name,
                    UrlWithArg = tabs_[index - 1].UrlWithArg,
                    UrlWithoutArg = tabs_[index - 1].UrlWithoutArg,
                };
                tabs_[index - 1] = tabs_[index];
                tabs_[index] = temp;
                user.EntranceSets[EntranceSetsBar.SelectedIndex].Entrances = tabs_.ToList();

                PickEntrances();
                SaveEntranceSets();
                EntrancesBar.SelectedIndex = index - 1;
            }
        }
        private void EntranceSetMoveUp_Click(object sender, RoutedEventArgs e)
        {
            int index = EntranceSetsBar.SelectedIndex;
            if (index - 1 >= 0)
            {
                EntranceSet[] tabs_ = user.EntranceSets.ToArray();
                var temp = new EntranceSet
                {
                    Name = tabs_[index - 1].Name,
                    Description = tabs_[index - 1].Description,
                    Entrances = tabs_[index - 1].Entrances
                };
                tabs_[index - 1] = tabs_[index];
                tabs_[index] = temp;
                user.EntranceSets = tabs_.ToList();

                PickEntranceSets();
                SaveEntranceSets();
                EntranceSetsBar.SelectedIndex = index - 1;
            }
        }
        private async void TabMoveDown_Click(object sender, RoutedEventArgs e)
        {
            int index = EntrancesBar.SelectedIndex;
            List<Entrance> entrances = user.EntranceSets[EntranceSetsBar.SelectedIndex].Entrances;
            if (index + 1 < entrances.Count())
            {
                var tabs_ = entrances.ToArray();
                var temp = new Entrance
                {
                    Name = tabs_[index].Name,
                    UrlWithArg = tabs_[index].UrlWithArg,
                    UrlWithoutArg = tabs_[index].UrlWithoutArg,
                };
                tabs_[index] = tabs_[index + 1];
                tabs_[index + 1] = temp;
                user.EntranceSets[EntranceSetsBar.SelectedIndex].Entrances = tabs_.ToList();

                PickEntrances();
                SaveEntranceSets();
                EntrancesBar.SelectedIndex = index + 1;
            }
        }
        private void EntranceSetMoveDown_Click(object sender, RoutedEventArgs e)
        {
            int index = EntranceSetsBar.SelectedIndex;
            List<EntranceSet> entrancesets = user.EntranceSets;
            if (index + 1 < entrancesets.Count())
            {
                var tabs_ = entrancesets.ToArray();
                var temp = new EntranceSet
                {
                    Name = tabs_[index].Name,
                    Description = tabs_[index].Description,
                    Entrances = tabs_[index].Entrances
                };
                tabs_[index] = tabs_[index + 1];
                tabs_[index + 1] = temp;
                user.EntranceSets = tabs_.ToList();

                PickEntranceSets();
                SaveEntranceSets();
                EntranceSetsBar.SelectedIndex = index + 1;
            }
        }
        private void TabDelete_Click(object sender, RoutedEventArgs e)
        {
            if (null == EntrancesBar.SelectedItem)
            {
                return;
            }
            int index = EntrancesBar.SelectedIndex;
            try
            {
                ((EntranceSetsBar.SelectedItem as SuggestionItem).Source
                    as EntranceSet).DeleteEntrance(index);
            }
            catch (CannotDeleteEntranceException) { }
            PickEntrances();
            EntrancesBar.SelectedIndex = index <= 0 ? 0 : index - 1;
            SaveEntranceSets();
            //int index = TabBar.SelectedIndex;
            //List<Tab> tabs_ = tabs.ToList();
            //tabs_.RemoveAt(index);
            //tabs = new Stack<Tab>(tabs_.ToArray().Reverse());
            //await SaveEntrances();
            //PickEntranceSets();
            //TabBar.SelectedIndex = index <= 0 ? 0 : index - 1;
        }
        private void EntranceSetDelete_Click(object sender, RoutedEventArgs e)
        {
            int index = EntranceSetsBar.SelectedIndex;
            if (index < 0 || user.EntranceSets.Count <= 1)
            {
                return;
            }
            user.EntranceSets.RemoveAt(index);
            PickEntranceSets();
            SaveEntranceSets();
            EntranceSetsBar.SelectedIndex = index - 1 >= 0 ? index - 1 : 0;
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

        #region Drops
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
        #endregion

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