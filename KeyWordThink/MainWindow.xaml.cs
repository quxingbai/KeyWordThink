using keyWordThink.Utils;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KeyWordThink
{

    public class EditAddData
    {
        public enum EditType
        {
            AddNext, Update, AddRight
        }
        public EditType EType { get; set; }
        public ThinkNode EditNode { get; set; }
        public bool IsNowEdit { get; set; } = false;
        public String EditFlag { get => EType == EditType.Update ? "修改" : EType == EditType.AddNext ? "本层" : EType == EditType.AddRight ? "下层" : "未知" + EType; }
        public EditAddData NextAddMode(ThinkNode node)
        {
            IsNowEdit = true;
            this.EditNode = node;
            this.EType = EditType.AddNext; return this;
        }
        public EditAddData RightAddMode(ThinkNode node)
        {
            IsNowEdit = true;
            this.EditNode = node;
            this.EType = EditType.AddRight; return this;
        }
        public EditAddData UpdateMode(ThinkNode node)
        {
            IsNowEdit = true;
            this.EditNode = node;
            this.EType = EditType.Update; return this;
        }
        public void EndEdit()
        {
            IsNowEdit = false;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ThinkKeyWordManager manager = new ThinkKeyWordManager("./ThinkDatas.json");
        private Point LastWindowPoint = new Point();
        private TimeSpan Second3 = new TimeSpan(0, 0, 3);
        private DateTime? MessageLife = DateTime.Now;


        private Button LastDownEditButton { get; set; }
        private EditAddData EditData = new EditAddData();
        private bool NeedTextInputToRightSelect = false;
        private Task QueryTask = null;
        private string LastQueryText = null;



        public bool IsNowLoading
        {
            get { return (bool)GetValue(IsNowLoadingProperty); }
            set { SetValue(IsNowLoadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsNowLoading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsNowLoadingProperty =
            DependencyProperty.Register("IsNowLoading", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));




        public MainWindow()
        {
            InitializeComponent();
            POPUP_Think.PreviewMouseDown += (ss, ee) =>
            {
                this.Activate();
            };

            
            CompositionTarget.Rendering += CompositionTarget_Rendering;

        }
        new public void Show()
        {
            base.Show();
            WindowLocationChange();
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            if (MessageLife != null && DateTime.Now > MessageLife)
            {
                HideMessage();
            }
        }

        public override void EndInit()
        {
            LastWindowPoint = new Point(this.Left, this.Top);
            base.EndInit();
        }

        private void TEXT_Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            WindowLocationChange();
            POPUP_Think.IsOpen = TEXT_Input.Text != "";
            if (TEXT_Input.Text == "") return;
            Query(TEXT_Input.Text);
            if (NeedTextInputToRightSelect)
            {
                TEXT_Input.CaretIndex = TEXT_Input.Text.Length;
            }
        }
        public void Query(String text)
        {
            if (QueryTask != null)
            {
                LastQueryText = text;
                return;
            }
            Dispatcher.Invoke(() => IsNowLoading = true);
            QueryTask = Task.Run(() =>
            {
                //Dispatcher.Invoke(() => LIST_Thinks.Items.Clear());
                var q = manager.ThinkNodeQuery(text);
                int count = 0;
                int nowTotal = LIST_Thinks.Items.Count;
                int delay = 10;
                int addcount = 0;
                int adddelay = delay;
                int delayChangeSize = 1;
                foreach (var i in q)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (nowTotal - 1 >= count)
                        {
                            LIST_Thinks.Items[count] = i;
                        }
                        else
                        {
                            LIST_Thinks.Items.Add(i);
                        }
                    });
                    count += 1;
                    addcount++;
                    if (addcount == 3)
                    {
                        addcount = 0;
                        adddelay -= delayChangeSize;
                    }
                    if (adddelay > 0)
                        Thread.Sleep(adddelay);
                }
                nowTotal = LIST_Thinks.Items.Count;
                int remcount = 0;
                if (nowTotal > count)
                {
                    while (LIST_Thinks.Items.Count > count)
                    {
                        Dispatcher.Invoke(() => LIST_Thinks.Items.RemoveAt(count));
                        remcount += 1;
                        if (remcount == 3)
                        {
                            remcount = 0;
                            delay -= delayChangeSize;
                        }
                        if (delay > 0)
                            Thread.Sleep(delay);
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    if (q.Count() > 0)
                    {
                        LIST_Thinks.SelectedIndex = 0;
                    }
                });
                //Dispatcher.Invoke(() =>
                //{
                //    LIST_Thinks.ItemsSource = q;
                //    if (q.Count() > 0)
                //    {
                //        LIST_Thinks.SelectedIndex = 0;
                //    }
                //});
                QueryTask = null;
                if (LastQueryText != null)
                {
                    Query(LastQueryText);
                    LastQueryText = null;
                }
                else
                {
                    Dispatcher.Invoke(() => IsNowLoading = false);
                }
            });
        }
        public void SaveAndReload()
        {
            manager.Save();
            Query(TEXT_Input.Text);
        }
        protected override void OnLocationChanged(EventArgs e)
        {
            WindowLocationChange();
            LastWindowPoint = new Point(Left, Top);
            base.OnLocationChanged(e);
        }
        public void WindowLocationChange()
        {
            WindowLocationChange(Left - LastWindowPoint.X, Top - LastWindowPoint.Y);
        }
        public void WindowLocationChange(double XChange, double YChange)
        {
            POPUP_Think.HorizontalOffset = XChange;
            POPUP_Think.VerticalOffset = YChange;
        }
        public void SelectedThink(ThinkNode node)
        {
            if (node.IsGroupNode)
            {
                NodeMoveRight(node);
                return;
            }
            Clipboard.SetText(node.Value);
            ShowMessage("数据已复制到剪切板...");
            LIST_Thinks.SelectedIndex = -1;
            TEXT_Input.Focus();
            TEXT_Input.Text = "";
        }
        public void ShowMessage(string message, TimeSpan? time = null)
        {
            time = time ?? Second3;
            TEXT_Message.Text = message;
            this.MessageLife = DateTime.Now.Add(time.Value);
            TEXT_Message.Visibility = Visibility.Visible;
        }
        public void HideMessage()
        {
            TEXT_Message.Visibility = Visibility.Collapsed;
        }
        private void CONTROL_ThinkItem_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((e.OriginalSource as dynamic).DataContext is ThinkNode)
            {
                var data = (sender as ContentControl).DataContext as ThinkNode;
                SelectedThink(data);
            }
        }
        public void NodeMoveRight(ThinkNode node)
        {

            if (node.Right != null)
            {
                NeedTextInputToRightSelect = true;
                TEXT_Input.Text = ThinkKeyWordManager.NodeToLeftsKeyword(node) + ":";
            }
            //TEXT_Input.SelectionStart = TEXT_Input.Text.Length;
            //TEXT_Input.SelectionLength = 0;
        }
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }
        public void NodeMoveLeft(ThinkNode node)
        {
            if ((node).GetLeft() == null)
            {
                NeedTextInputToRightSelect = true;
                TEXT_Input.Text = "*";
            }
            else
            {
                //TEXT_Input.SelectionStart = TEXT_Input.Text.Length;
                //TEXT_Input.SelectionLength = 0;
                NeedTextInputToRightSelect = true;
                TEXT_Input.Text = ThinkKeyWordManager.NodeToLeftsKeyword(node, false);
            }
        }
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Escape)
            {
                if (EditData.IsNowEdit)
                {
                }
                else
                {
                    switch (e.Key)
                    {
                        case Key.Enter: if (LIST_Thinks.SelectedItem != null) SelectedThink(LIST_Thinks.SelectedItem as ThinkNode); break;
                        case Key.Up: if (LIST_Thinks.Items.Count > 0 && LIST_Thinks.SelectedIndex > -1) LIST_Thinks.SelectedIndex = LIST_Thinks.SelectedIndex == 0 ? LIST_Thinks.Items.Count - 1 : LIST_Thinks.SelectedIndex - 1; break;
                        case Key.Down: if (LIST_Thinks.Items.Count > 0) LIST_Thinks.SelectedIndex = (LIST_Thinks.SelectedIndex == LIST_Thinks.Items.Count - 1 ? 0 : LIST_Thinks.SelectedIndex + 1); break;
                        case Key.Left: if (LIST_Thinks.SelectedItem != null && LIST_Thinks.Items.Count > 0 && (LIST_Thinks.SelectedItem as ThinkNode).GetLeft() != null) NodeMoveLeft((LIST_Thinks.SelectedItem as ThinkNode).GetLeft()); break;
                        case Key.Right: if (LIST_Thinks.SelectedItem != null && LIST_Thinks.Items.Count > 0 && (LIST_Thinks.SelectedItem as ThinkNode).Right != null) NodeMoveRight(LIST_Thinks.SelectedItem as ThinkNode); break;
                        case Key.Escape: TEXT_Input.Text = "*"; TEXT_Input.Focus(); break;
                    }
                    if (e.Key == Key.Up || e.Key == Key.Down)
                    {
                        LIST_Thinks.ScrollIntoView(LIST_Thinks.SelectedItem);
                    }
                }

            }
            else if (e.Key == Key.F1 || e.Key == Key.F2 || e.Key == Key.F3 || e.Key == Key.F4 && LIST_Thinks.SelectedItem != null)
            {
                if (LIST_Thinks.SelectedItem == null) return;
                ListBoxItem selectedItem = LIST_Thinks.ItemContainerGenerator.ContainerFromItem(LIST_Thinks.SelectedItem) as ListBoxItem;
                var BT_ADDThink = FindChildElementByName(selectedItem, "BT_ADDThink") as Button;
                var BT_ADDThinkRight = FindChildElementByName(selectedItem, "BT_ADDThinkRight") as Button;
                var BT_UpdateThink = FindChildElementByName(selectedItem, "BT_UpdateThink") as Button;
                var BT_DeleteThink = FindChildElementByName(selectedItem, "BT_DeleteThink") as Button;
                switch (e.Key)
                {
                    case Key.F1: BT_ADDThink.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); ; break;
                    case Key.F2: BT_ADDThinkRight.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); ; break;
                    case Key.F3: BT_UpdateThink.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); ; break;
                    case Key.F4: BT_DeleteThink.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); ; break;
                }
                DependencyObject FindChildElementByName(DependencyObject parent, string name)
                {
                    if (parent == null) return null;
                    int childCount = VisualTreeHelper.GetChildrenCount(parent);

                    for (int i = 0; i < childCount; i++)
                    {
                        var child = VisualTreeHelper.GetChild(parent, i);

                        if (child is FrameworkElement element && element.Name == name)
                        {
                            return element;
                        }

                        var foundChild = FindChildElementByName(child, name);

                        if (foundChild != null)
                        {
                            return foundChild;
                        }
                    }

                    return null;
                }
            }

            else if (e.Key == Key.F12)
            {
                MessageBox.Show("输入关键字用:分割 其中星号*作为万能关键字使用\n然后使用小键盘上下键进行选择 也可以使用左右返回左右节点 然后回车键或者双击鼠标进行确认就会把数据复制到剪切板\n操作上f1~f4可以快捷点击编辑按钮 ");
            }
            else if (BD_EditItems.Visibility == Visibility.Collapsed)
            {
                TEXT_Input.Focus();
            }
            base.OnPreviewKeyDown(e);
        }
        private void BT_ADDThink_Click(object sender, RoutedEventArgs e1)
        {
            bool isNowLineModChange = LastDownEditButton?.DataContext == (sender as Button)?.DataContext && LastDownEditButton != sender;
            this.LastDownEditButton = sender as Button;
            ThinkNode node = (sender as Button).DataContext as ThinkNode;
            Grid BD = (sender as Button).CommandParameter as Grid;
            String Tag = (sender as Button).Tag as String;
            var edits = BD_EditItems;
            TEXT_EditInfo.Text = node.Info;
            TEXT_EditKey.Text = node.Key;
            TEXT_EditValue.Text = node.Value;
            switch (Tag)
            {
                case "Update": EditData.UpdateMode(node); break;
                case "Next": EditData.NextAddMode(node); break;
                case "Right": EditData.RightAddMode(node); break;
            }
            TEXT_EditType.Text = EditData.EditFlag;
            if (!isNowLineModChange)
            {
                if (edits.Parent == BD)
                {
                    edits.Visibility = Visibility.Collapsed;
                    BD.Visibility = Visibility.Collapsed;
                    (edits.Parent as Panel)?.Children.Remove(edits);
                    GRID.Children.Add(edits);
                    EditData.EndEdit();
                }
                else
                {
                    (edits.Parent as Panel)?.Children.Remove(edits);
                    BD.Children.Add(BD_EditItems);
                    BD.Visibility = Visibility.Visible;
                    BD_EditItems.Visibility = Visibility.Visible;
                }
            }
            TEXT_EditKey.Focus();
        }
        private void BT_DeleteThink_Click(object sender, RoutedEventArgs e)
        {
            ThinkNode node = (sender as Button).DataContext as ThinkNode;
            if (node.IsFirstTree())
            {
                MessageBox.Show("根节点无法删除");
                return;
            }
            if (MessageBox.Show("删除 [" + node.Key + "] 及以下的子节点？", "删除确认", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                manager.RemoveNode(node);
            }

            SaveAndReload();
        }

        private void BT_Edited_Click(object sender, RoutedEventArgs e1)
        {
            var key = TEXT_EditKey.Text;
            var value = TEXT_EditValue.Text;
            var info = TEXT_EditInfo.Text;
            ThinkNode node = EditData.EditNode;
            var source = new ThinkNodeDataSource() { Key = key, Value = value, Info = info };
            switch (EditData.EType)
            {
                case EditAddData.EditType.AddNext:
                    node.InsertToNext(source);
                    break;
                case EditAddData.EditType.Update:
                    node.SetSource(source);
                    break;
                case EditAddData.EditType.AddRight:
                    if (node.Right == null)
                        node.CreateRightNode(source);
                    else
                        node.Right.AppendNext(source);
                    break;
                default:
                    break;
            }
            SaveAndReload();
            EditData.EndEdit();
            TEXT_Input.Focus();
        }

        private void TEXT_Input_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TEXT_Input.Text == "")
                TEXT_Input.Text = "*";
        }

        private void EditInput_TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Escape) && EditData.IsNowEdit)
            {
                e.Handled = true;
                switch (e.Key)
                {
                    case Key.Enter:
                        if (sender == TEXT_EditKey)
                        {
                            TEXT_EditValue.Focus();
                        }
                        else if (sender == TEXT_EditValue)
                        {
                            TEXT_EditInfo.Focus();
                        }
                        else if (sender == TEXT_EditInfo)
                        {
                            BT_Edited_Click(BT_Edited, null);
                            TEXT_Input.Focus();
                        }
                        ; break;
                    case Key.Escape:
                        BT_ADDThink_Click(LastDownEditButton, null);
                        ; break;
                }
            }
        }

        private void EditInputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox text = sender as TextBox;
            text.SelectionLength = text.Text.Length;
            text.SelectionStart = 0;

        }

        private void TEXT_Input_TextInput(object sender, TextCompositionEventArgs e)
        {
            int i = 1;
        }

        private void TEXT_Input_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Left || e.Key == Key.Right) && NeedTextInputToRightSelect)
            {
                NeedTextInputToRightSelect = false;
                e.Handled = true;
            }
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == IsNowLoadingProperty)
            {
            }
            else if(e.Property== WindowStateProperty)
            {
                switch (WindowState)
                {
                    case WindowState.Normal:
                        Top += 0.1;
                        break;
                    case WindowState.Minimized:
                        Top += 0.1;
                        break;
                    case WindowState.Maximized:
                        WindowLocationChange();
                        break;
                    default:
                        break;
                }
                //WindowLocationChange(Left - LastWindowPoint.X, Top - LastWindowPoint.Y);
            }
            base.OnPropertyChanged(e);
        }
    }
}
