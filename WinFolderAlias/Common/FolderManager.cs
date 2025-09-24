using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WinFolderAlias
{
    public class FolderManager
    {
        #region 引入Windows API
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int LoadString(IntPtr hModule, int uID, System.Text.StringBuilder lpBuffer, int nBufferMax);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
        #endregion


        // 常量定义
        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNE_UPDATEITEM = 0x00002000;
        private const uint SHCNF_PATH = 0x0005;
        private const uint SHCNF_FLUSH = 0x1000;

        /// <summary>
        /// 刷新指定文件夹的图标显示
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        public static void RefreshFolderIcon(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"文件夹不存在: {folderPath}");

            try
            {
                // 方法1: 发送系统通知，强制刷新该文件夹
                SHChangeNotify(SHCNE_UPDATEITEM | SHCNE_ASSOCCHANGED, SHCNF_PATH | SHCNF_FLUSH,
                    Marshal.StringToHGlobalUni(folderPath), IntPtr.Zero);

                Console.WriteLine($"已发送刷新通知: {folderPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API刷新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取文件夹的别名，当没有则返回空，使用GBK编码读取文本
        /// </summary>
        /// <param name="iniPath"></param>
        /// <returns></returns>
        public static string? GetDisplayNameFromDesktopIni(string iniPath)
        {
            if (!string.IsNullOrWhiteSpace(iniPath))
            {
                return null;
            }

            // 读 ini 文件，找 [.ShellClassInfo] 节，然后找 LocalizedResourceName 键
            try
            {
                // 注意：desktop.ini 通常是 ANSI 或 Unicode 的
                var lines = File.ReadAllLines(iniPath, Encoding.GetEncoding("GBK"));
                bool inShellClassInfoSection = false;
                foreach (var raw in lines)
                {
                    string line = raw.Trim();
                    if (line.Length == 0)
                        continue;

                    // Section 开始判断
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        // 判断是不是 ShellClassInfo 节
                        string sectionName = line.Substring(1, line.Length - 2).Trim();
                        inShellClassInfoSection = sectionName.Equals(".ShellClassInfo", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (!inShellClassInfoSection)
                        continue;

                    // 找 LocalizedResourceName= 前缀
                    const string key = "LocalizedResourceName";
                    if (line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        string value = line.Substring(key.Length + 1).Trim();

                        // 有可能值前面有 @ 表示资源文件 + 资源 ID，也可能是普通字符串
                        // 比如：@%SystemRoot%\\system32\\shell32.dll,-21770 或 “我的文档”
                        // 客户端可能就直接返回 value
                        return UnescapeLocalizedResourceNameValue(value);
                    }
                }
            }
            catch (Exception)
            {
                // 读文件出错
                // 根据需要记录日志
                // 这里返回 null
            }

            return null;
        }

        /// <summary>
        /// 更新 desktop.ini 中的 LocalizedResourceName ，使用GBK编码
        /// </summary>
        /// <param name="desktopIniPath"></param>
        /// <param name="newAlias"></param>
        /// <returns></returns>
        public static bool UpdateLocalizedResourceName(string desktopIniPath, string newAlias)
        {
            if (String.IsNullOrEmpty(desktopIniPath))
            {
                return false;
            }
            try
            {
                var lines = File.ReadAllLines(desktopIniPath, Encoding.GetEncoding("GBK"));
                bool inShellClassInfoSection = false;
                bool shellClassInfoExists = false;
                bool localizedResourceNameExists = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    // 判断是否进入 [.ShellClassInfo] 区域
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        if (line.Equals("[.ShellClassInfo]", StringComparison.OrdinalIgnoreCase))
                        {
                            inShellClassInfoSection = true;
                            shellClassInfoExists = true;
                        }
                        else
                        {
                            inShellClassInfoSection = false;
                        }
                        continue;
                    }

                    // 在 .ShellClassInfo 区域内更新 LocalizedResourceName
                    if (inShellClassInfoSection && line.StartsWith("LocalizedResourceName=", StringComparison.OrdinalIgnoreCase))
                    {
                        lines[i] = "LocalizedResourceName=" + newAlias; // 替换别名
                        localizedResourceNameExists = true;
                        File.WriteAllLines(desktopIniPath, lines, Encoding.GetEncoding("GBK"));
                        return true;
                    }
                }

                if (!shellClassInfoExists)
                {
                    File.AppendAllText(desktopIniPath, "\n[.ShellClassInfo]\nLocalizedResourceName=" + newAlias, Encoding.GetEncoding("GBK"));
                    return true;
                }

                // 如果找到了 [.ShellClassInfo] 节但没有 LocalizedResourceName 字段，则在该节下添加
                if (shellClassInfoExists && !localizedResourceNameExists)
                {
                    List<string> newLines = new List<string>();
                    inShellClassInfoSection = false;
                    bool localizedResourceNameAdded = false;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        newLines.Add(lines[i]);

                        // 检查是否进入或离开 [.ShellClassInfo] 区域
                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            if (line.Equals("[.ShellClassInfo]", StringComparison.OrdinalIgnoreCase))
                            {
                                inShellClassInfoSection = true;
                            }
                            else if (inShellClassInfoSection)
                            {
                                // 即将离开 [.ShellClassInfo] 节，在此之前添加 LocalizedResourceName
                                if (!localizedResourceNameAdded)
                                {
                                    newLines.Insert(newLines.Count - 1, "LocalizedResourceName=" + newAlias);
                                    localizedResourceNameAdded = true;
                                }
                                inShellClassInfoSection = false;
                            }
                        }
                    }

                    // 如果 [.ShellClassInfo] 是最后一个节，在文件末尾添加
                    if (inShellClassInfoSection && !localizedResourceNameAdded)
                    {
                        newLines.Add("LocalizedResourceName=" + newAlias);
                    }

                    File.WriteAllLines(desktopIniPath, newLines, Encoding.GetEncoding("GBK"));
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating desktop.ini: {ex.Message}");
                return false;
            }
        }

        private static string UnescapeLocalizedResourceNameValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            // 简单处理：如果以 @ 开头，说明是资源引用
            if (value.StartsWith("@"))
            {
                // 形如 @%SystemRoot%\system32\shell32.dll,-21770
                // 或 @shell32.dll,-21770
                // 对于这种情况，要去加载资源中的字符串
                // 可以通过 Win32 API 来实现，比如 LoadLibrary + LoadString
                // 下面是调用示例

                string modulePart;
                int resourceId;

                // 分割 “,” 最后的部分
                int commaIndex = value.LastIndexOf(',');
                if (commaIndex > 0 && commaIndex < value.Length - 1)
                {
                    string moduleAndPath = value.Substring(1, commaIndex - 1).Trim(); // 去掉 '@'，到逗号为止
                    string idPart = value.Substring(commaIndex + 1).Trim();

                    if (int.TryParse(idPart, out resourceId))
                    {
                        // 解析 module 路径
                        modulePart = ExpandEnvironmentVariables(moduleAndPath);

                        // Load the string resource
                        string loaded = LoadStringFromModule(modulePart, resourceId);
                        if (!string.IsNullOrEmpty(loaded))
                            return loaded;
                        // 如果加载失败，就 fallback 返回原 value
                    }
                }

                // 如果解析失败，就直接返回 value（或者去掉前导 @ ）
                return value.Substring(1);
            }
            else
            {
                // 不是资源引用，可能就是直接写在 ini 的名称
                return value.Trim('"'); // 去掉可能的引号
            }
        }
        private static string ExpandEnvironmentVariables(string s)
        {
            try
            {
                return Environment.ExpandEnvironmentVariables(s);
            }
            catch
            {
                return s;
            }
        }
        private static string LoadStringFromModule(string modulePath, int resourceId)
        {
            // 调用 Win32 API LoadLibrary + LoadString
            IntPtr hModule = IntPtr.Zero;
            try
            {
                hModule = LoadLibrary(modulePath);
                if (hModule == IntPtr.Zero)
                    return String.Empty;

                const int MAX_LOADSTRING = 512;
                var sb = new System.Text.StringBuilder(MAX_LOADSTRING);
                int len = LoadString(hModule, resourceId, sb, sb.Capacity);
                if (len > 0)
                {
                    return sb.ToString();
                }
                return String.Empty;
            }
            finally
            {
                if (hModule != IntPtr.Zero)
                {
                    FreeLibrary(hModule);
                }
            }
        }


    }
}