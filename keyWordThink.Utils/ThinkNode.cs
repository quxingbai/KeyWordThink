using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyWordThink.Utils
{
    public enum ThinkQueryType
    {
        All, Any
    }
    public class ThinkNode : ThinkNodeDataBase, IDisposable,INotifyPropertyChanged
    {
        private ThinkNode? LastRight { get; set; }
        private ThinkNode? LastNext { get; set; }
        private ThinkNode? Parent { get; set; }
        private ThinkNode? First { get; set; }
        private ThinkNode? Left { get; set; }
        private int Deep { get; set; } = 0;
        public bool IsGroupNode { get; private set; } = false;

        private String _GroupValue = null, _GroupInfo = null;
        public String GroupValue { get { GetNodeGroupInfo?.Invoke(); return _GroupValue; } set => _GroupValue = value; } 
        public String GroupInfo { get { GetNodeGroupInfo?.Invoke();return _GroupInfo; } set => _GroupInfo = value; }

        /// <summary>
        /// 标签
        /// </summary>
        new public ThinkNode? Right { get; set; }
        /// <summary>
        /// 同等级
        /// </summary>
        new public ThinkNode? Next { get; set; }

        /// <summary>
        /// 将可保存数据源序列化为ThinkNode
        /// </summary>
        public static ThinkNode SerializeNode(ThinkNodeDataBase NodeBase)
        {
            return CreateNext(null, NodeBase);
            ThinkNode CreateNext(ThinkNode node, ThinkNodeDataBase source)
            {
                var next = node == null ? new ThinkNode(null, null, source) : node.CreateNextNode(source);
                if (source.Right != null)
                    CreateRight(next, source.Right);
                if (source.Next != null)
                    CreateNext(next, source.Next);
                return next;
            }
            ThinkNode CreateRight(ThinkNode node, ThinkNodeDataBase source)
            {
                var right = node.CreateRightNode(source.Key, source.Value);
                if (source.Next != null)
                    CreateNext(right, source.Next);
                if (source.Right != null)
                    CreateRight(right, source.Right);
                return right;
            }
        }
        public static ThinkNode CreateFirst(string key)
        {
            return new ThinkNode(null, null, new ThinkNodeDataSource() { Key = key });
        }
        public ThinkNode(ThinkNode? Parent, ThinkNode? First)
        {
            this.Parent = Parent;
            this.First = First;
            Init();
        }
        public ThinkNode(ThinkNode? Parent, ThinkNode? First, ThinkNodeDataSource source)
        {
            this.Parent = Parent;
            SetSource(source);
            this.First = First;
            Init();
        }
        public ThinkNode(ThinkNode? Parent, ThinkNode? First, ThinkNode? Left, ThinkNodeDataSource source)
        {
            this.Parent = Parent;
            SetSource(source);
            this.First = First;
            this.Left = Left;
            Init();
        }
        public void NodeIsNotGroup()
        {
            this.GroupInfo = this.GroupValue = null;
            this.IsGroupNode = false;
        }
        private Action GetNodeGroupInfo = null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void NodeIsGroup()
        {
            this.IsGroupNode = true;
            GetNodeGroupInfo = () =>
            {

                StringBuilder newVal = new StringBuilder();
                int count = 0;
                if (Right != null)
                {
                    Right.EachAllNexts(next =>
                    {
                        count += 1;
                        if (count < 4)
                        {
                            if (count != 1)
                                newVal.Append(",");
                            newVal.Append(next.Key);
                        }
                        return true;
                    }, true);
                }
                if (count > 3)
                {
                    newVal.Append("...");
                }
                this.GroupValue = newVal.ToString();
                this.GroupInfo = Key + "中共" + count + "项子节点";
            };
        }
        private void Init()
        {
            if (First == null)
            {
                First = Parent == null ? this : Parent.First;
            }
            Deep = First == null ? 0 : First.Deep + 1;
        }
        public bool IsMainTree()
        {
            return Parent == null;
        }
        public bool IsFirstTree()
        {
            return First == this;
        }
        public ThinkNode CreateRightNode(ThinkNodeDataSource source)
        {
            return (Right = new ThinkNode(this, First, this, source));
        }
        public ThinkNode CreateNextNode(ThinkNodeDataSource source)
        {
            var n = new ThinkNode(this, First, this.Left, source);
            Next = n;
            return n;
        }

        public ThinkNode CreateRightNode(string key, string value = "", string info = null) => CreateRightNode(new ThinkNodeDataSource() { Key = key, Value = value, Info = info });
        public ThinkNode CreateNextNode(string key, string value = "", string info = null) => CreateNextNode(new ThinkNodeDataSource() { Key = key, Value = value, Info = info });
        public ThinkNode AppendNext(ThinkNodeDataSource source) => GetLastNext().CreateNextNode(source);
        public ThinkNode AppendRight(ThinkNodeDataSource source) => GetLastRight().CreateRightNode(source);
        public ThinkNode AppendNext(string key, string value = "", string info = null) => AppendNext(new ThinkNodeDataSource() { Key = key, Value = value, Info = info });
        public ThinkNode AppendRight(string key, string value = "", string info = null) => AppendRight(new ThinkNodeDataSource() { Key = key, Value = value, Info = info });
        public ThinkNode GetLeft() => Left;
        public ThinkNode GetParent() => Parent;
        public ThinkNode GetLastNext()
        {
            if (Next == null) return this;
            var next = Next;
            while (next.Next != null)
            {
                next = next.Next;
            }
            return next;
        }
        public ThinkNode GetLastRight()
        {
            if (Right == null) return this;
            var right = Right;
            while (right.Right != null)
            {
                right = right.Right;
            }
            return right;
        }

        public void EachParents(Action<ThinkNode> parentAct)
        {
            if (Parent == null) return;
            ThinkNode p = Parent;
            while (p != null)
            {
                parentAct(p);
                p = p.Parent;
            }
        }
        public void EachLefts(Func<ThinkNode, bool> each,bool withThis=false)
        {
            if (Left == null) return;
            if (withThis)
            {
                each.Invoke(this);
            }
            ThinkNode node = Left;
            while (node != null)
            {
                if (!each.Invoke(node))
                {
                    break;
                }
                node = node.Left;
            }
        }
        public void SetParent(ThinkNode parent)
        {
            this.Parent = parent;
        }

        public bool IsKey(String key, ThinkQueryType type)
        {
            return key == "*" || (type == ThinkQueryType.All ? Key == key : type == ThinkQueryType.Any ? Key.IndexOf(key) != -1 : false);
        }
        /// <summary>
        /// 插队添加一个节点
        /// </summary>
        /// <returns>新节点</returns>
        public ThinkNode InsertToParent(ThinkNodeDataSource source)
        {
            ThinkNode nodeValues = new ThinkNode(null, First, source);
            nodeValues.Parent = Parent;
            nodeValues.Right = Right;
            nodeValues.Next = this;
            nodeValues.SetSource(source);

            this.Parent = nodeValues;
            this.Right = null;
            return nodeValues;
        }
        /// <summary>
        /// 插队添加到下一个节点
        /// </summary>
        /// <returns>新节点</returns>
        public ThinkNode InsertToNext(ThinkNodeDataSource source)
        {
            ThinkNode node = new ThinkNode(null, First, Left, source);
            node.SetSource(source);
            node.Next = Next;
            node.Parent = this;

            if (Next != null)
                this.Next.Parent = node;
            this.Next = node;
            return node;
        }


        /// <summary>
        /// 移除当前节点
        /// </summary>
        /// <returns>返回下一个节点</returns>
        public ThinkNode Remove()
        {
            ThinkNode res = null;
            if (Parent == null && IsFirstTree())//主节点
            {
                return res;
            }
            else if (this.Left!=null&&this.Parent==this.Left)//如果是第一个子节点
            {
                this.Parent.Right = Next;
                if (Next != null)
                    this.Next.Parent = this.Parent;
            }
            else
            {
                this.Parent.Next = Next;
                if (Next != null)
                {
                    res = Parent;
                    this.Next.Parent = Parent;
                    this.Next = null;
                    this.Parent = null;
                }
            }

            Dispose();
            return res;
        }
        public void EachAllRights(Action<ThinkNode> FindNodeAction)
        {
            var node = Right;
            while (node != null)
            {
                FindNodeAction(node);
                node = node.Right;
            }
        }
        public void EachAllNexts(Func<ThinkNode, bool> FindNodeAction, bool WithThis = false)
        {
            var node = WithThis ? this : Next;
            while (node != null)
            {
                if (!FindNodeAction(node)) return;
                node = node.Next;
            }
        }
        public void QueryAllRightWithNext(List<ThinkNode> outPut)
        {
            if (outPut == null) throw new Exception("返回值集合不能为Null");
            EachAllRights(right =>
            {
                outPut.Add(Right);
                right.EachAllNexts(next =>
                {
                    outPut.Add(next);
                    next.QueryAllRightWithNext(outPut);
                    return true;
                });
            });
        }

        public void Dispose()
        {
            //Parent?.Dispose();
            Right?.Dispose();
            //Next?.Dispose();
        }
    }
}
