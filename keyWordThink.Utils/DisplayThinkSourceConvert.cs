using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace keyWordThink.Utils
{
    public class DisplayThinkSourceConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text= "";
            ThinkNode node = value as ThinkNode;
            if (node == null) return "ThinkNode To Text 转换错误";
            node.EachLefts(parent =>
            {
                text = parent.Key + " " + text;
                return true;
            });
            return text+" "+node.Key;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
