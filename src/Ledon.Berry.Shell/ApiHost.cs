using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Ledon.Berry.Shell
{
    /// <summary>
    /// 负责启动/停止随 WPF 一同分发的 Ledon.BerryShare.Api.exe。
    /// 保证单例，避免重复进程。
    /// </summary>
    public sealed class ApiHost
    {
        private static readonly Lazy<ApiHost> _lazy = new(() => new ApiHost());
        public static ApiHost Instance => _lazy.Value;

        private Process? _process;
        private readonly object _lock = new();

        private ApiHost() { }

        /// <summary>
        /// 启动 API，如果已在运行则返回 true。
        /// </summary>
        public int? ListeningPort { get; private set; }

        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public bool TryStart(out string? error)
        {
            lock (_lock)
            {
                error = null;
                if (_process != null && !_process.HasExited)
                    return true;

                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string exePath = Path.Combine(baseDir, "assert", "Ledon.BerryShare.Api.exe");
                    if (!File.Exists(exePath))
                    {
                        error = $"未找到 API 可执行文件: {exePath}";
                        return false;
                    }

                    // 动态申请端口并通过 --port 传递
                    ListeningPort = GetFreeTcpPort();

                    // 若 API 目录下缺少 wwwroot/index.html 而 assert 下有前端资源，则复制以便同源访问
                    try
                    {
                        string apiDir = Path.GetDirectoryName(exePath)!;
                        string sourceWwwroot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assert", "wwwroot");
                        string destWwwroot = Path.Combine(apiDir, "wwwroot");
                        string destIndex = Path.Combine(destWwwroot, "index.html");
                        string sourceIndex = Path.Combine(sourceWwwroot, "index.html");
                        if (File.Exists(sourceIndex) && !File.Exists(destIndex))
                        {
                            CopyDirectory(sourceWwwroot, destWwwroot);
                        }
                    }
                    catch (Exception copyEx)
                    {
                        Trace.WriteLine("[API] 复制前端静态资源失败: " + copyEx.Message);
                    }

                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath)!,
                        Arguments = $"--port {ListeningPort}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                    _process.OutputDataReceived += (_, e) => { if (e.Data != null) Trace.WriteLine($"[API] {e.Data}"); };
                    _process.ErrorDataReceived += (_, e) => { if (e.Data != null) Trace.WriteLine($"[API-ERR] {e.Data}"); };
                    _process.Exited += (_, _) => Trace.WriteLine($"[API] 已退出 (Code={_process?.ExitCode})");

                    if (_process.Start())
                    {
                        _process.BeginOutputReadLine();
                        _process.BeginErrorReadLine();
                        return true;
                    }
                    error = "进程启动失败";
                    return false;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }
        }

        public void TryStop()
        {
            lock (_lock)
            {
                if (_process == null) return;
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.CloseMainWindow();
                        if (!_process.WaitForExit(1500))
                        {
                            _process.Kill(true);
                        }
                    }
                }
                catch { /* 忽略关闭异常 */ }
                finally
                {
                    _process.Dispose();
                    _process = null;
                }
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDir, file);
                var targetPath = Path.Combine(destDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(file, targetPath, true);
            }
        }
    }
}
