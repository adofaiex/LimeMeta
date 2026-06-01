using System.Text.Json;
using LimeMeta.Configurations;
using Microsoft.Extensions.Options;
using FileInfo = LimeMeta.Models.FileInfo;

namespace LimeMeta.Files;

/// <summary>
/// 123 云盘 CLI 文件存储服务。
/// </summary>
public sealed class Pan123CliFileStorageProvider : IFileStorageProvider
{
    /// <summary>
    /// ProviderName
    /// </summary>
    public const string ProviderName = "Pan123Cli";

    private readonly LimeMetaConfiguration _config;
    private readonly Pan123CliRunner _runner;

    /// <summary>
    /// Pan123CliFileStorageProvider
    /// </summary>
    /// <param name="options"></param>
    /// <param name="runner"></param>
    public Pan123CliFileStorageProvider(IOptions<LimeMetaConfiguration> options, Pan123CliRunner runner)
    {
        _config = options.Value;
        _runner = runner;
    }

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public async Task<FileStorageSaveResult> SaveAsync(Stream stream, string fileName, string? contentType, long size, CancellationToken ct)
    {
        var options = _config.FileStore.Pan123Cli;
        Directory.CreateDirectory(options.TempPath);

        var safeName = Path.GetFileName(fileName);
        var tempPath = Path.Combine(options.TempPath, $"{Guid.NewGuid():N}_{safeName}");
        try
        {
            await using (var fs = File.Create(tempPath))
            {
                await stream.CopyToAsync(fs, ct);
            }

            string hash;
            await using (var fs = File.OpenRead(tempPath))
            {
                hash = fs.GetMD5();
            }

            var args = new List<string>
            {
                "upload",
                tempPath,
                "--parent",
                options.ParentFileId.ToString(),
                "--name",
                safeName
            };

            if (options.Overwrite)
            {
                args.Add("--overwrite");
            }

            var data = await _runner.RunAsync(args, ct);
            var providerId = GetString(data, "fileID") ?? GetString(data, "fileId") ?? GetString(data, "id");
            if (string.IsNullOrWhiteSpace(providerId))
            {
                throw new InvalidOperationException("pan123 上传成功但未返回 fileID。");
            }

            return new FileStorageSaveResult
            {
                Provider = ProviderName,
                Store = 0,
                Real = safeName,
                Size = new System.IO.FileInfo(tempPath).Length,
                Hash = hash,
                ProviderId = providerId,
                ProviderPath = options.ParentFileId.ToString(),
                Meta = data.GetRawText()
            };
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <inheritdoc />
    public async Task<FileStorageOpenResult> OpenAsync(FileInfo info, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(info.ProviderId))
        {
            throw new InvalidOperationException("123 云盘文件缺少 ProviderId。");
        }

        var options = _config.FileStore.Pan123Cli;
        if (options.UseDirectLink)
        {
            var data = await _runner.RunAsync(["direct-link", "url", info.ProviderId], ct);
            var url = GetString(data, "url");
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("pan123 未返回直链 URL。");
            }

            return new FileStorageOpenResult
            {
                RedirectUrl = url
            };
        }

        Directory.CreateDirectory(options.TempPath);
        var dataDownload = await _runner.RunAsync(["download", info.ProviderId, "--out", options.TempPath], ct);
        var path = GetString(dataDownload, "path");
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("pan123 下载完成但未返回本地路径。");
        }

        return new FileStorageOpenResult
        {
            FilePath = path
        };
    }

    /// <inheritdoc />
    public Task DeleteAsync(FileInfo info, CancellationToken ct)
    {
        // 123PanCLi 当前没有删除命令。这里只删除数据库记录，不删除云盘文件。
        return Task.CompletedTask;
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.ToString(),
                _ => property.Value.GetRawText()
            };
        }

        return null;
    }
}
