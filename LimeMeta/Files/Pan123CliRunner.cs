using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LimeMeta.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimeMeta.Files;

/// <summary>
/// pan123 CLI 调用器。
/// </summary>
internal sealed class Pan123CliRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly LimeMetaConfiguration _config;
    private readonly ILogger<Pan123CliRunner> _logger;

    /// <summary>
    /// Pan123CliRunner
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public Pan123CliRunner(IOptions<LimeMetaConfiguration> options, ILogger<Pan123CliRunner> logger)
    {
        _config = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 执行 pan123 命令。
    /// </summary>
    /// <param name="args"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<JsonElement> RunAsync(IEnumerable<string> args, CancellationToken ct)
    {
        var cli = _config.FileStore.Pan123Cli;
        var psi = new ProcessStartInfo
        {
            FileName = cli.Command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.ArgumentList.Add("--json");
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };

        _logger.LogInformation("执行 pan123: {command} {args}", cli.Command, string.Join(" ", psi.ArgumentList.Select(EscapeArg)));

        if (!process.Start())
        {
            throw new InvalidOperationException("启动 pan123 失败。");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct);

        var stdoutText = stdout.ToString().Trim();
        var stderrText = stderr.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(stderrText))
        {
            _logger.LogInformation("pan123 stderr: {stderr}", stderrText);
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"pan123 执行失败，ExitCode={process.ExitCode}，错误：{stderrText}");
        }

        if (string.IsNullOrWhiteSpace(stdoutText))
        {
            throw new InvalidOperationException("pan123 未返回 JSON。");
        }

        using var doc = JsonDocument.Parse(stdoutText);
        var root = doc.RootElement.Clone();

        var ok = root.TryGetProperty("ok", out var okElement) && okElement.ValueKind == JsonValueKind.True;
        if (!ok)
        {
            var message = "pan123 执行失败。";
            if (root.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var messageElement))
            {
                message = messageElement.GetString() ?? message;
            }

            throw new InvalidOperationException(message);
        }

        return root.TryGetProperty("data", out var data) ? data.Clone() : root;
    }

    private static string EscapeArg(string arg)
    {
        return arg.Contains(' ') ? $"\"{arg}\"" : arg;
    }
}
