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
    public override void Configure()
    {
        Post("/api/file/upload");
        AllowFileUploads();
    }

    public override async Task HandleAsync(FileUploadRequest req, CancellationToken ct)
    {
        var claim = User.Claims.First(r => r.Type == UserLogic.ClaimUserId);
        var userId = Guid.Parse(claim.Value);

        var meta = Resolve<ILimeMeta>();
        var storage = Resolve<IFileStorageProviderResolver>().Current;
        var urlResolver = Resolve<FileUrlResolver>();

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

            meta.Insert([info], userId);

            // 确保返回稳定公开 URL（Local 依赖 Id；Pan123 可能上传时已写直链）
            info.Url = await urlResolver.ResolveAsync(info, persist: true, ct);

            items.Add(new FileUploadResponseItem
            {
                Id = info.Id,
                Name = file.FileName,
                Provider = info.Provider,
                ProviderId = info.ProviderId,
                Url = info.Url
            });
        }

        await Send.OkAsync(new FileUploadResponse { Items = items }, ct);
    }
}

internal sealed class FileUploadRequest
{
    public List<IFormFile> Files { get; set; } = [];
}

internal sealed class FileUploadResponse
{
    public List<FileUploadResponseItem> Items { get; set; } = [];
}

internal sealed class FileUploadResponseItem
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Provider { get; set; }

    public string? ProviderId { get; set; }

    public string? Url { get; set; }
}
