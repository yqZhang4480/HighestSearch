using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace 聚合搜索
{
    public sealed partial class SuggestionItem : UserControl
    {
        public string Title { get; set; }
        public List<string> Notes { get; set; }
        public string IconText { get; set; }
        public object Source { get; set; }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            TitleTextBlock.SetBinding(TextBlock.TextProperty,
                                      new Binding
                                      {
                                          Source = Title,
                                          Mode = BindingMode.OneWay
                                      });
            NoteTextBlock.SetBinding(TextBlock.TextProperty,
                                     new Binding
                                     {
                                         Source = NotesToString(),
                                         Mode = BindingMode.OneWay
                                     });
            IconTextBlock.SetBinding(TextBlock.TextProperty,
                                     new Binding
                                     {
                                         Source = IconText,
                                         Mode = BindingMode.OneWay
                                     });
        }
        public SuggestionItem()
        {
            this.InitializeComponent();

        }

        private string NotesToString()
        {
            if (Notes == null)
            {
                return "";
            }
            string s = "";
            for (int i = 0; i < Notes.Count; i++)
            {
                if (i != 0)
                {
                    s += " ⋅ ";
                }
                string note = Notes[i];
                s += note;
            }
            return s;
        }
    }
}
