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
    /// <param name="stream"></param>
    /// <param name="fileName"></param>
    /// <param name="contentType"></param>
    /// <param name="size"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<FileStorageSaveResult> SaveAsync(Stream stream, string fileName, string? contentType, long size, CancellationToken ct);

    /// <summary>
    /// 打开文件。
    /// </summary>
    /// <param name="info"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<FileStorageOpenResult> OpenAsync(FileInfo info, CancellationToken ct);

    /// <summary>
    /// 删除文件。
    /// </summary>
    /// <param name="info"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task DeleteAsync(FileInfo info, CancellationToken ct);
}

/// <summary>
/// 文件保存结果。
/// </summary>
public sealed class FileStorageSaveResult
{
    /// <summary>
    /// 存储服务名称。
    /// </summary>
    public required string Provider { get; set; }

    /// <summary>
    /// 本地存储桶编号。
    /// </summary>
    public int Store { get; set; }

    /// <summary>
    /// 实际文件名。
    /// </summary>
    public string? Real { get; set; }

    /// <summary>
    /// 文件大小。
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 文件哈希。
    /// </summary>
    public required string Hash { get; set; }

    /// <summary>
    /// 第三方文件 ID。
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// 第三方路径或父目录 ID。
    /// </summary>
    public string? ProviderPath { get; set; }

    /// <summary>
    /// 外部访问地址。
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 原始元数据。
    /// </summary>
    public string? Meta { get; set; }
}

/// <summary>
/// 文件打开结果。
/// </summary>
public sealed class FileStorageOpenResult
{
    /// <summary>
    /// 本地文件路径。
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 重定向地址。
    /// </summary>
    public string? RedirectUrl { get; set; }
}
