using FastEndpoints;
using LimeMeta.Data;
using LimeMeta.Files;
using Microsoft.AspNetCore.StaticFiles;
using ModelFileInfo = LimeMeta.Models.FileInfo;
using SystemFileInfo = System.IO.FileInfo;

namespace LimeMeta.Endpoints;

/// <summary>
/// FileDownloadEndpoint
/// </summary>
internal sealed class FileDownloadEndpoint : Endpoint<FileDownloadRequest>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/api/file/download");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(FileDownloadRequest req, CancellationToken ct)
    {
        var meta = Resolve<ILimeMeta>();
        var info = meta.Query<ModelFileInfo>().FirstOrDefault(r => r.Id == req.Id);
        if (info == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var urlResolver = Resolve<FileUrlResolver>();
        var publicUrl = await urlResolver.ResolveAsync(info, persist: true, ct);

        if (req.Redirect == false)
        {
            await Send.OkAsync(new FileDownloadUrlResponse
            {
                Id = info.Id,
                Url = publicUrl ?? $"/api/file/download?id={info.Id}"
            }, ct);
            return;
        }

        var storage = Resolve<IFileStorageProviderResolver>().Get(info.Provider);
        var openResult = await storage.OpenAsync(info, ct);
        if (!string.IsNullOrWhiteSpace(openResult.RedirectUrl))
        {
            HttpContext.Response.Redirect(openResult.RedirectUrl);
            return;
        }

        if (string.IsNullOrWhiteSpace(openResult.FilePath) || !File.Exists(openResult.FilePath))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var file = new SystemFileInfo(openResult.FilePath);
        var contentType = "application/octet-stream";
        if (info.Type != null)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(info.Type, out contentType))
            {
                contentType = "application/octet-stream";
            }
        }

        HttpContext.Response.Headers.ContentDisposition = $"attachment; filename*=UTF-8''{Uri.EscapeDataString(info.Name)}";
        await Send.FileAsync(file, contentType, null, true, ct);
    }
}

internal sealed class FileDownloadRequest
{
    public required Guid Id { get; set; }

    /// <summary>
    /// 是否 HTTP 重定向到公开地址。false 时返回 JSON { id, url }。
    /// </summary>
    public bool? Redirect { get; set; } = true;
}

internal sealed class FileDownloadUrlResponse
{
    public Guid Id { get; set; }

    public required string Url { get; set; }
}
