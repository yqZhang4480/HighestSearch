using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace 聚合搜索
{
    public class CannotDeleteEntranceException : Exception
    { }

    public enum IconType
    {
        FontIcon,
        EmojiIcon
    }

    public class Icon
    {
        public string ImageUrl { get; set; }

        public Icon()
        {
            ImageUrl = "";
        }
    }

    public class Entrance
    {
        public string Name { get; set; } = AppResources.GetString("Default_NewEntrance");
        public string UrlWithArg { get; set; } = "about:blank";
        public string UrlWithoutArg { get; set; } = "about:blank";
        public string IconText { get; set; } = "";

        public Entrance()
        { }
        public Entrance(Entrance en)
        {
            Name = en.Name;
            UrlWithArg = en.UrlWithArg;
            UrlWithoutArg = en.UrlWithoutArg;
            IconText = en.IconText;
        }

        public SuggestionItem ToControl()
        {
            string echoUrl = UrlWithoutArg;
            if (UrlWithoutArg.Contains("/"))
            {
                if (UrlWithoutArg.Split("/").Count() > 2)
                {
                    echoUrl = UrlWithoutArg.Split("/")[2];
                }
            }
            List<string> notes = new List<string>
            {
                echoUrl
            };
            return new SuggestionItem
            {
                Title = Name,
                Notes = notes,
                IconText = IconText,
                Margin = new Thickness(0, 0, 0, 0),
                Source = this
            };
        }
    }

    public class EntranceSet
    {
        public string Name { get; set; } = AppResources.GetString("Default_NewEntranceSet");
        public string Description { get; set; } = "";
        public List<Entrance> Entrances { get; set; } = new List<Entrance>();
        public string IconText { get; set; } = "";

        public EntranceSet()
        {
            //Entrances.Add(new Entrance());
        }

        public SuggestionItem ToControl()
        {
            List<string> notes = new List<string>
            {
                //string s;
                //switch (Entrances.Count)
                //{
                //    case 1:
                //        s = " Item";
                //        break;
                //    default:
                //        s = " Items";
                //        break;
                //}
                //notes.Add($"{Entrances.Count}" + s);
                Description
            };
            return new SuggestionItem
            {
                Title = Name,
                Notes = notes,
                IconText = IconText,
                //Margin = new Thickness(0, 5, 0, 5),
                Source = this
            };
        }

        public void DeleteEntrance(int index)
        {
            if (Entrances.Count == 0)
            {
                Entrances.Add(new Entrance());
            }
            if (Entrances.Count == 1)
            {
                throw new CannotDeleteEntranceException();
            }
            Entrances.RemoveAt(index);
        }
    }
}
