using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace keyWordThink.Utils
{
    public class DisplayThinkSourceShortInfo : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ThinkNode node = value as ThinkNode;
            if (node == null) return "";
            if (node.IsGroupNode)return node.GroupInfo;
            return node.Key + "（"+node.Info+"）";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
