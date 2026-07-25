using FastEndpoints;
using LimeMeta.Data;
using LimeMeta.Files;
using LimeMeta.Logics;
using Microsoft.AspNetCore.Http;
using FileInfo = LimeMeta.Models.FileInfo;

namespace LimeMeta.Endpoints;

/// <summary>
/// FileUploadEndpoint
/// </summary>
internal sealed class FileUploadEndpoint : Endpoint<FileUploadRequest, FileUploadResponse>
{
    /// <summary>
    /// Configure
    /// </summary>
    public override void Configure()
    {
        Post("/api/file/upload");
        AllowFileUploads(); // 核心：必须调用此方法以告知 Swagger 这是个文件上传接口
    }

    /// <summary>
    /// HandleAsync
    /// </summary>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public override async Task HandleAsync(FileUploadRequest req, CancellationToken ct)
    {
        var cliam = User.Claims.First(r => r.Type == UserLogic.ClaimUserId);
        var userId = Guid.Parse(cliam.Value);

        var meta = Resolve<ILimeMeta>();
        var storage = Resolve<IFileStorageProviderResolver>().Current;

        var items = new List<FileUploadResponseItem>();
        foreach (var file in req.Files)
        {
            if (file is null || file.Length == 0) continue;

            await using var stream = file.OpenReadStream();
            var saveResult = await storage.SaveAsync(stream, file.FileName, file.ContentType, file.Length, ct);
            var info = new FileInfo
            {
                Id = Guid.NewGuid(),
                Name = file.FileName,
                Real = saveResult.Real ?? string.Empty,
                Type = Path.GetExtension(file.FileName),
                Size = saveResult.Size,
                Hash = saveResult.Hash,
                Store = saveResult.Store,
                Provider = saveResult.Provider,
                ProviderId = saveResult.ProviderId,
                ProviderPath = saveResult.ProviderPath,
                Url = saveResult.Url,
                Meta = saveResult.Meta
            };

            meta.Insert(new[] { info }, userId);

            var item = new FileUploadResponseItem
            {
                Id = info.Id,
                Name = file.FileName,
                Provider = info.Provider,
                ProviderId = info.ProviderId
            };
            items.Add(item);
        }

        await Send.OkAsync(new FileUploadResponse { Items = items }, ct);
    }
}

/// <summary>
/// FileUploadRequest
/// </summary>
internal sealed class FileUploadRequest
{
    /// <summary>
    /// Files
    /// </summary>
    public List<IFormFile> Files { get; set; } = [];
}

/// <summary>
/// FileUploadResponse
/// </summary>
internal sealed class FileUploadResponse
{
    /// <summary>
    /// Files
    /// </summary>
    public List<FileUploadResponseItem> Items { get; set; } = [];
}

/// <summary>
/// FileUploadResponseItem
/// </summary>
internal sealed class FileUploadResponseItem
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Provider
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// ProviderId
    /// </summary>
    public string? ProviderId { get; set; }
}
