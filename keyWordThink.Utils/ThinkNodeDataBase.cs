using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyWordThink.Utils
{
    public class ThinkNodeDataSource
    {
        public String Key { get; set; } = "空节点";
        public String Value { get; set; } = "空节点";
        public String Info { get; set; } = "详细信息";
        public ThinkNodeDataSource GetSource()
        {
            return this;
        }
        public void SetSource(ThinkNodeDataSource source)
        {
            this.Key = source.Key;
            this.Value = source.Value;
            this.Info = source.Info;
        }
    }
    public class ThinkNodeDataBase : ThinkNodeDataSource
    {
        public ThinkNodeDataBase? Right { get; set; }
        public ThinkNodeDataBase? Next { get; set; }
    }
}
