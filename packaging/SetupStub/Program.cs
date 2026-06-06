using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace ContextKeysSetup;

internal static class Program
{
    private const string MarkerText = "\n--CONTEXTKEYS-PAYLOAD-V1--\n";

    [STAThread]
    private static int Main(string[] args)
    {
        var selfTest = args.Any(arg => string.Equals(arg, "--self-test", StringComparison.OrdinalIgnoreCase));

        try
        {
            var tempDir = ExtractPayload();
            try
            {
                var installScript = Path.Combine(tempDir, "Install-ContextKeys.ps1");
                var appExe = Path.Combine(tempDir, "ContextKeys.exe");
                var icon = Path.Combine(tempDir, "LKey.ico");

                if (!File.Exists(installScript) || !File.Exists(appExe) || !File.Exists(icon))
                    throw new InvalidOperationException("安装包内容不完整。");

                if (selfTest)
                    return 0;

                return RunInstallScript(installScript, tempDir);
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }
        catch (Exception ex)
        {
            if (!selfTest)
                MessageBox(nint.Zero, ex.Message, "ContextKeys 安装失败", 0x00000010);
            return 1;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(nint hWnd, string text, string caption, uint type);

    private static string ExtractPayload()
    {
        var setupPath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("无法定位安装器自身路径。");
        var marker = Encoding.UTF8.GetBytes(MarkerText);
        var setupBytes = File.ReadAllBytes(setupPath);
        var markerIndex = LastIndexOf(setupBytes, marker);
        if (markerIndex < 0)
            throw new InvalidOperationException("安装包缺少内置载荷。");

        var payloadOffset = markerIndex + marker.Length;
        var payloadLength = setupBytes.Length - payloadOffset;
        if (payloadLength <= 0)
            throw new InvalidOperationException("安装包载荷为空。");

        var tempDir = Path.Combine(Path.GetTempPath(), "ContextKeysSetup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var payloadPath = Path.Combine(tempDir, "payload.zip");
        using (var payloadStream = File.Create(payloadPath))
            payloadStream.Write(setupBytes, payloadOffset, payloadLength);

        ZipFile.ExtractToDirectory(payloadPath, tempDir, overwriteFiles: true);
        File.Delete(payloadPath);
        return tempDir;
    }

    private static int LastIndexOf(byte[] source, byte[] pattern)
    {
        for (var index = source.Length - pattern.Length; index >= 0; index--)
        {
            var matched = true;
            for (var patternIndex = 0; patternIndex < pattern.Length; patternIndex++)
            {
                if (source[index + patternIndex] != pattern[patternIndex])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
                return index;
        }

        return -1;
    }

    private static int RunInstallScript(string installScript, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{installScript}\"",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("无法启动安装脚本。");
        process.WaitForExit();
        return process.ExitCode;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Temporary setup files can be cleaned by the OS later.
        }
    }
}
