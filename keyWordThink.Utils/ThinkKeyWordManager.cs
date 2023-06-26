using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

namespace keyWordThink.Utils
{
    public class ThinkKeyWordManager
    {
        private ThinkNode Nodes = new ThinkNode(null, null) { Key = "默认节点", Value = "..." };
        private readonly string DataFilePath = "";
        private readonly FileInfo DataFile = null;
        public ThinkKeyWordManager(string DataFilePath)
        {
            this.DataFilePath = DataFilePath;
            this.DataFile = new FileInfo(DataFilePath);
            Init();
        }
        private void Init()
        {
            DataFile.Directory.Create();
            if (!DataFile.Exists)
            {
                var w = DataFile.CreateText();
                w.WriteLine(JsonConvert.SerializeObject(Nodes));
                w.Dispose();
            }
            var text = File.ReadAllText(DataFilePath);
            var data = JsonConvert.DeserializeObject<ThinkNodeDataBase>(text);
            if (data == null) return;
            Nodes = ThinkNode.SerializeNode(data);
        }
        public void Save()
        {
            var json = JsonConvert.SerializeObject(Nodes);
            File.WriteAllText(DataFilePath, json);
        }
        public void Clear()
        {
            //Nodes.Dispose();
            Nodes = new ThinkNode(null, null);
        }
        /// <summary>
        /// 根据关键字去查询
        /// </summary>
        /// <param name="Keyword">使用符号:进行分割的关键字集合</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public IEnumerable<ThinkNode> ThinkNodeQuery(String Keyword, ThinkQueryType type = ThinkQueryType.Any)
        {
            var keys = Keyword.Split(':');
            IEnumerable<ThinkNode> QueryResult = null;
            int deep = 0;
            foreach (var k in keys)
            {
                QueryResult = QueryOnce(k, deep, QueryResult);
                deep += 1;
            }
            IEnumerable<ThinkNode> QueryOnce(string keyword, int deep, IEnumerable<ThinkNode> nowSource)
            {
                List<ThinkNode> ns = new List<ThinkNode>();
                if (deep == 0)
                {
                    if (Nodes.IsKey(keyword, type))
                        ns.Add(Nodes);
                    Nodes.EachAllNexts(next =>
                    {
                        if (next.IsKey(keyword, type))
                        {
                            ns.Add(next);
                        }
                        return true;
                    });
                }
                else
                {
                    foreach (var n in nowSource)
                    {
                        if (n.Right != null)
                        {
                            if (n.Right.IsKey(keyword, type))
                            {
                                ns.Add(n.Right);
                            }
                            n.Right.EachAllNexts(next =>
                            {
                                if (next.IsKey(keyword, type))
                                    ns.Add(next);
                                return true;
                            });
                        }
                    }
                }
                foreach (var i in ns)
                {
                    if (i.Right != null)
                    //if ((i.Value == "" || i.Value == null) && i.Right != null)
                    {
                        i.NodeIsGroup();
                    }
                    else
                    {
                        if (i.IsGroupNode && (i.GroupValue != null || i.GroupInfo != null))
                        {
                            i.NodeIsNotGroup();
                        }
                    }
                }
                return ns;
            }
            return QueryResult;
        }
        public void AddNode(ThinkNode node)
        {
            //Nodes.InsertToNext(node);
            node.Next = Nodes.Next;
            Nodes.Next = node;
            node.SetParent(Nodes);
        }
        public bool RemoveNode(ThinkNode node)
        {
            if (node.IsFirstTree())
            {
                return false; ;
            }
            else
            {
                node.Remove();
            }
            return true;
            //var result=node.Remove();
            //if (result==null)
            //{

            //}

        }
        /// <summary>
        /// 从当前节点开始一个一个向前寻找关键字最后组成一整条关键字链
        /// </summary>
        public static string NodeToLeftsKeyword(ThinkNode node,bool WithThis=true)
        {
            StringBuilder sb = new StringBuilder();
            if(WithThis)
            {
                sb.Insert(0, node.Key);
            }
            node.EachLefts(l =>
            {
                sb.Insert(0, ":");
                sb.Insert(0, l.Key);
                return true;
            },false);
            return sb.ToString();
        }
    }
}
