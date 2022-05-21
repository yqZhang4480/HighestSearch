using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 聚合搜索
{
    public class User
    {
        public string Name = "LOCAL";
        public List<EntranceSet> EntranceSets { get; set; } = new List<EntranceSet>();
        public List<ViewHistory> ViewHistories { get; set; } = new List<ViewHistory>();
        public List<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
        public Settings Settings { get; set; } = new Settings();

        public User()
        {
            //EntranceSets.Add(new EntranceSet());
        }
    }
}
