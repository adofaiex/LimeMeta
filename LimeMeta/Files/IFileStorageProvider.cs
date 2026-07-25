using FileInfo = LimeMeta.Models.FileInfo;

namespace LimeMeta.Files;

/// <summary>
/// 文件存储服务。
/// </summary>
public interface IFileStorageProvider
{
    /// <summary>
    /// 存储服务名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 保存文件。
    /// </summary>
    Task<FileStorageSaveResult> SaveAsync(Stream stream, string fileName, string? contentType, long size, CancellationToken ct);

    /// <summary>
    /// 打开文件。
    /// </summary>
    Task<FileStorageOpenResult> OpenAsync(FileInfo info, CancellationToken ct);

    /// <summary>
    /// 删除文件。
    /// </summary>
    Task DeleteAsync(FileInfo info, CancellationToken ct);

    /// <summary>
    /// 解析可供前端直接访问的公开 URL。
    /// 默认：已有 Url 则返回；否则尝试 OpenAsync 的 RedirectUrl；再否则回退到下载接口。
    /// </summary>
    async Task<string?> ResolvePublicUrlAsync(FileInfo info, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(info.Url))
        {
            return info.Url;
        }

        var open = await OpenAsync(info, ct);
        if (!string.IsNullOrWhiteSpace(open.RedirectUrl))
        {
            return open.RedirectUrl;
        }

        return $"/api/file/download?id={info.Id}";
    }
}

/// <summary>
/// 文件保存结果。
/// </summary>
public sealed class FileStorageSaveResult
{
    public required string Provider { get; set; }

    public int Store { get; set; }

    public string? Real { get; set; }

    public long Size { get; set; }

    public required string Hash { get; set; }

    public string? ProviderId { get; set; }

    public string? ProviderPath { get; set; }

    public string? Url { get; set; }

    public string? Meta { get; set; }
}

/// <summary>
/// 文件打开结果。
/// </summary>
public sealed class FileStorageOpenResult
{
    public string? FilePath { get; set; }

    public string? RedirectUrl { get; set; }
}
