using System.Diagnostics;
using System.Security.Cryptography;
using PCL.Core.App;
using PCL.Core.IO;

namespace PCL;

/// <summary>管理随启动器分发的 McTunnel 工具。</summary>
internal static class BundledMcTunnel
{
    private const string ResourcePath = "Resources/McTunnel.exe";
    private const string ProcessName = "McTunnel";
    private const string FileName = "McTunnel.exe";

    /// <summary>释放并启动内嵌的 McTunnel；已有实例运行时不会重复启动。</summary>
    public static void Start()
    {
        if (!OperatingSystem.IsWindows())
            return;

        if (_IsRunning())
        {
            LauncherCommon.Log("[Bundled] McTunnel 已在运行，跳过自动启动");
            return;
        }

        var toolsDirectory = Path.Combine(Paths.SharedLocalData, "Tools");
        Directory.CreateDirectory(toolsDirectory);
        var targetPath = Path.Combine(toolsDirectory, FileName);

        using var resource = Basics.GetResourceStream(ResourcePath)
            ?? throw new FileNotFoundException("未找到内嵌的 McTunnel 资源", ResourcePath);
        var expectedHash = _ComputeHash(resource);
        if (resource.CanSeek)
            resource.Position = 0;

        if (!File.Exists(targetPath)
            || !string.Equals(
                Files.GetFileSha256(targetPath),
                expectedHash,
                StringComparison.OrdinalIgnoreCase))
        {
            _WriteResource(resource, targetPath, expectedHash);
            LauncherCommon.Log($"[Bundled] 已释放 {FileName} 到 {targetPath}");
        }

        using var process = Process.Start(
            new ProcessStartInfo(targetPath)
            {
                UseShellExecute = true,
                WorkingDirectory = toolsDirectory
            }) ?? throw new InvalidOperationException("无法启动 McTunnel");

        LauncherCommon.Log($"[Bundled] 已启动 McTunnel，进程 ID：{process.Id}");
    }

    private static bool _IsRunning()
    {
        var processes = Process.GetProcessesByName(ProcessName);
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
                process.Dispose();
        }
    }

    private static string _ComputeHash(Stream stream)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(stream)).ToLowerInvariant();
    }

    private static void _WriteResource(Stream resource, string targetPath, string expectedHash)
    {
        var temporaryPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Files.WriteAllStream(temporaryPath, resource);
            var actualHash = Files.GetFileSha256(temporaryPath);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("释放的 McTunnel 文件校验失败");

            File.Move(temporaryPath, targetPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
