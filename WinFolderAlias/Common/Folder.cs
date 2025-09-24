using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WinFolderAlias
{
    /// <summary>
    /// 应用中使用的文件夹类
    /// </summary>
    public partial class Folder(string path) : INotifyPropertyChanged
    {
        private string DesktopFilePath => System.IO.Path.Combine(Path, DesktopFileName);
        private const string DesktopFileName = "desktop.ini";
        private const string ProfileSection = ".ShellClassInfo";
        private const string ProfileKeyName = "LocalizedResourceName";


        private string _path = path;

        // 文件夹路径
        public string Path
        {
            get => _path; set
            {
                if (_path != value)
                {
                    _path = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Path)));
                }
            }
        }
        // 文件夹名称
        public string Name
        {
            // 从path中获取文件名
            get => System.IO.Path.GetFileName(_path);
        }

        // 文件夹别名
        public string? Alias
        {
            get
            {
                if (File.Exists(DesktopFilePath))
                {
                    return GetProfileString(ProfileKeyName);
                }
                return null;
            }
            set
            {
                if (!String.IsNullOrWhiteSpace(value))
                {
                    bool ret = SetProfileString(ProfileKeyName, value);

                    if (ret)
                    {
                        _ = new FileInfo(DesktopFilePath)
                        {
                            Attributes = FileAttributes.Hidden
                        };
                        FolderManager.RefreshFolderIcon(_path);
                    }

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Alias)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        #region 获取配置文件值
        private string GetProfileString(string key)
        {
            StringBuilder temp = new(1024);
            GetPrivateProfileString(ProfileSection, key, String.Empty, temp, 1024, DesktopFilePath);
            return temp.ToString();
        }
        #endregion
        #region 设置配置文件值
        private bool SetProfileString(string key, string val)
        {
            long ret = WritePrivateProfileString(ProfileSection, key, val, DesktopFilePath);
            if (ret == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion
        #region 引入Windows API
        [DllImport("kernel32")] // 返回0表示失败，非0为成功
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]//返回取得字符串缓冲区的长度
        private static extern long GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        #endregion
    }
}