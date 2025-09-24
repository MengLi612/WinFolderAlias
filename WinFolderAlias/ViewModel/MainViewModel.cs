using System.ComponentModel;

namespace WinFolderAlias
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            _textBoxModel = new();
            _buttonModel = new()
            {
                Folder = _folder
            };
        }

        private Folder? _folder;

        private TextBoxModel _textBoxModel;
        private ButtonModel _buttonModel;

        public Folder? Folder
        {
            get => _folder;
            set
            {
                if (_folder != value)
                {
                    _folder = value;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Folder)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_buttonModel.ButtonText)));
                    TextBoxModel.AliasText = _folder?.Alias ?? "";
                }
            }
        }

        public TextBoxModel TextBoxModel { get => _textBoxModel; set => _textBoxModel = value; }
        public ButtonModel ButtonModel { get => _buttonModel; set => _buttonModel = value; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class ButtonModel : INotifyPropertyChanged
    {
        private Folder? _folder;

        #region UI元素属性
        private const string _buttonText = "点击选择或拖拽文件夹到此处";
        #endregion
        public string ButtonText
        {
            get
            {
                // 先判断folder是否为null
                if (Folder == null || string.IsNullOrEmpty(Folder.Name))
                {
                    return _buttonText;
                }
                return Folder.Name;
            }
        }

        public Folder? Folder { get => _folder; set => _folder = value; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class TextBoxModel : INotifyPropertyChanged
    {
        private const string _defaultAliasText = "输入别名";
        private const string _defaultAliasTextColor = "#909090";
        private const string _AliasTextColor = "#000000";

        // UI显示的暂存别名文本
        private string _aliasText = "";
        private string _aliasTextColor = _AliasTextColor;
        private bool _isAliasTextBoxDefault = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string AliasText
        {
            get
            {
                if (string.IsNullOrEmpty(_aliasText))
                {
                    _isAliasTextBoxDefault = true;
                    AliasTextColor = _defaultAliasTextColor;
                    return _defaultAliasText;
                }
                _isAliasTextBoxDefault = false;
                AliasTextColor = _AliasTextColor;
                return _aliasText;
            }
            set
            {
                if (_isAliasTextBoxDefault)
                {
                    // 移除默认文本
                    value = value.Replace(_defaultAliasText, "");
                }
                if (_aliasText != value)
                {
                    _aliasText = value;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AliasText)));
                }
            }
        }

        public string AliasTextColor
        {
            get
            {
                return _aliasTextColor;
            }
            set
            {
                if (_aliasTextColor != value)
                {
                    _aliasTextColor = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AliasTextColor)));
                }
            }
        }

        public bool IsAliasTextBoxDefault { get => _isAliasTextBoxDefault; set => _isAliasTextBoxDefault = value; }
    }
}