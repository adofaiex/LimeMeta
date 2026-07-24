using LimeMeta.Configurations;
using LimeMeta.Data;
using Microsoft.Extensions.Options;
using FileInfo = LimeMeta.Models.FileInfo;

namespace LimeMeta.Files;

/// <summary>
/// 本地文件存储服务。
/// </summary>
internal sealed class LocalFileStorageProvider : IFileStorageProvider
{
    /// <summary>
    /// ProviderName
    /// </summary>
    public const string ProviderName = "Local";

    private readonly ILimeMeta _meta;
    private readonly LimeMetaConfiguration _config;

    /// <summary>
    /// LocalFileStorageProvider
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="options"></param>
    public LocalFileStorageProvider(ILimeMeta meta, IOptions<LimeMetaConfiguration> options)
    {
        _meta = meta;
        _config = options.Value;
    }

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public async Task<FileStorageSaveResult> SaveAsync(Stream stream, string fileName, string? contentType, long size, CancellationToken ct)
    {
        var root = GetRootPath(_config);
        var count = GetStoreCount(_config);

        var maxStore = _meta.Query<FileInfo>().Max(r => r.Store);
        var cntStore = _meta.Query<FileInfo>().Where(r => r.Store == maxStore && (r.Provider == null || r.Provider == ProviderName)).Count();
        if (cntStore >= count)
        {
            maxStore++;
        }

        var storePath = Path.Combine(root, $"{maxStore}");
        Directory.CreateDirectory(storePath);

        var safeName = Path.GetFileName(fileName);
        var name = Path.GetFileNameWithoutExtension(safeName);
        var ext = Path.GetExtension(safeName);
        var real = $"{name}_{Guid.NewGuid()}{ext}";
        var path = Path.Combine(storePath, real);

        await using (var fs = File.Create(path))
        {
            await stream.CopyToAsync(fs, ct);
        }

        string hash;
        await using (var fs = File.OpenRead(path))
        {
            hash = fs.GetMD5();
        }

        var fileSize = new System.IO.FileInfo(path).Length;
        return new FileStorageSaveResult
        {
            Provider = ProviderName,
            Store = maxStore,
            Real = real,
            Size = fileSize,
            Hash = hash
        };
    }

    /// <inheritdoc />
    public Task<FileStorageOpenResult> OpenAsync(FileInfo info, CancellationToken ct)
    {
        var path = GetStorePath(info, _config);
        return Task.FromResult(new FileStorageOpenResult
        {
            FilePath = path
        });
    }

    /// <inheritdoc />
    public Task DeleteAsync(FileInfo info, CancellationToken ct)
    {
        var path = GetStorePath(info, _config);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取本地文件根目录。
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static string GetRootPath(LimeMetaConfiguration config)
    {
        return string.IsNullOrWhiteSpace(config.FileStore.Local.Path)
            ? config.FileStorePath
            : config.FileStore.Local.Path;
    }

    /// <summary>
    /// 获取每个本地子目录最多保存多少个文件。
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static int GetStoreCount(LimeMetaConfiguration config)
    {
        return config.FileStore.Local.Count ?? config.FileStoreCount;
    }

    /// <summary>
    /// 获取本地文件路径。
    /// </summary>
    /// <param name="info"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static string GetStorePath(FileInfo info, LimeMetaConfiguration config)
    {
        return Path.Combine(GetRootPath(config), $"{info.Store}", info.Real);
    }
}
