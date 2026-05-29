using System.Diagnostics;
using System.Text;

namespace ContextKeys.Utils;

/// <summary>
/// 简易日志：同时输出到 Debug 和文件。
/// 文件路径：%AppData%/ContextKeys/log.txt
/// </summary>
public static class Logger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ContextKeys",
        "log.txt");

    private static readonly object Lock = new();

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Warn(string message)
    {
        Write("WARN", message);
    }

    public static void Error(string message)
    {
        Write("ERROR", message);
    }

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}";
        Debug.WriteLine(line);

        lock (Lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // 写日志失败不能影响主流程
            }
        }
    }

    public static string GetLogPath() => LogPath;
}
