using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;


namespace WinFolderAlias
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            _vm = new MainViewModel();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
            DataContext = _vm;
        }

        private MainViewModel _vm;
        public MainViewModel Vm { get => _vm; set => _vm = value; }

        private void DragFolderEnter(object sender, DragEventArgs e)
        {
            // 检查拖拽的数据是否是文件夹，如果是，则将文件夹路径复制到vm中的Folder.Path
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths.Length > 0 && System.IO.Directory.Exists(paths[0]))
                {
                    // 创建实例
                    _vm.Folder = new(paths[0]);
                    // 更改鼠标图标
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    // 更改鼠标图标
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                // 更改鼠标图标
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
        private void DragFolderLeave(object sender, DragEventArgs e)
        {
            _vm.Folder = null;

            e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine("输入框已失焦。");
#endif
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            // 按下enter后，控件失焦
            if (e.Key == Key.Enter)
            {
                var textBox = (TextBox)sender;
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(textBox), null);

                Keyboard.ClearFocus();

#if DEBUG
                Debug.WriteLineIf(textBox.IsFocused, "输入框未失焦。");
#endif
                e.Handled = true;
            }
        }

        private void ApplicationButtonClick(object sender, RoutedEventArgs e)
        {
            if (Vm.Folder != null)
            {
                Vm.Folder.Alias = _vm.TextBoxModel.AliasText;
            }
        }
        private void TextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            // 判断是否为默认文本
            if (sender is TextBox textBox)
            {
                textBox.Focus();
                if (_vm.TextBoxModel.IsAliasTextBoxDefault)
                {
                    
                    // 将输入指针指向最左边
                    textBox.CaretIndex = 0;
                    Debug.WriteLine("输入框输入指针已指向最左边。");
                }
                else
                {
                    // 选择文本框内所有文本
                    textBox.SelectAll();
                }
            }
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 打开文件管理器，并选中文件夹
            using (var folderDialog = new CommonOpenFileDialog())
            {
                folderDialog.IsFolderPicker = true;
                folderDialog.Title = "选择文件夹";

                if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string folderPath = folderDialog.FileName;
#if DEBUG
                    Debug.WriteLine("选择的文件夹路径：" + folderPath);
#endif
                    _vm.Folder = new(folderPath);
                }
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {

        }
    }
}