using LimeMeta.Data;
using FileInfo = LimeMeta.Models.FileInfo;

namespace LimeMeta.Files;

/// <summary>
/// 统一解析并缓存文件公开 URL。
/// </summary>
public sealed class FileUrlResolver(
    ILimeMeta meta,
    IFileStorageProviderResolver storageResolver)
{
    /// <summary>
    /// 按文件 ID 解析公开 URL。
    /// </summary>
    public Task<string?> ResolveAsync(Guid? fileId, bool persist = true, CancellationToken ct = default)
        => ResolveAndCacheAsync(fileId, persist, ct);

    /// <summary>
    /// 解析公开 URL。
    /// </summary>
    public Task<string?> ResolveAsync(FileInfo info, bool persist = true, CancellationToken ct = default)
        => ResolveAndCacheAsync(info, persist, ct);

    /// <summary>
    /// 按文件 ID 解析公开 URL，可选写回 <see cref="FileInfo.Url"/>。
    /// </summary>
    public async Task<string?> ResolveAndCacheAsync(Guid? fileId, bool persist = true, CancellationToken ct = default)
    {
        if (fileId is null)
        {
            return null;
        }

        var info = meta.Query<FileInfo>().FirstOrDefault(x => x.Id == fileId.Value);
        return info is null ? null : await ResolveAndCacheAsync(info, persist, ct);
    }

    /// <summary>
    /// 解析公开 URL，可选写回 <see cref="FileInfo.Url"/>。
    /// </summary>
    public async Task<string?> ResolveAndCacheAsync(FileInfo info, bool persist = true, CancellationToken ct = default)
    {
        var provider = storageResolver.Get(info.Provider);
        var url = await provider.ResolvePublicUrlAsync(info, ct);
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (persist && !string.Equals(info.Url, url, StringComparison.Ordinal))
        {
            info.Url = url;
            meta.Update([info], [nameof(FileInfo.Url)], null, enableLogic: false);
        }

        return url;
    }
}
