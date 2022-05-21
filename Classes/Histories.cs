using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 聚合搜索
{
    public class ViewHistory
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTime Time { get; set; }

        public SuggestionItem ToControl()
        {
            var notes = new List<string>();
            string echoUrl = Url;
            if (Url.Contains("/"))
            {
                if (Url.Split("/").Count() > 2)
                {
                    echoUrl = Url.Split("/")[2];
                }
            }
            notes.Add(echoUrl);
            notes.Add(Time.ToString());
            return new SuggestionItem { Title = Title, Notes = notes, Source = this };
        }
    }
    public class SearchHistory
    {
        public string Text { get; set; }
        public DateTime Time { get; set; }

        public SuggestionItem ToControl()
        {
            var notes = new List<string>();
            notes.Add(Time.ToString());
            return new SuggestionItem { Title = Text, Notes = notes, Source = this };
        }
    }
}
