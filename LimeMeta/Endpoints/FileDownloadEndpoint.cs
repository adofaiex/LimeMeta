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
    /// <summary>
    /// Configure
    /// </summary>
    public override void Configure()
    {
        Get("/api/file/download");
    }

    /// <summary>
    /// HandleAsync
    /// </summary>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public override async Task HandleAsync(FileDownloadRequest req, CancellationToken ct)
    {
        // var cliam = User.Claims.First(r => r.Type == UserLogic.ClaimUserId);
        // var userId = Guid.Parse(cliam.Value);

        var meta = Resolve<ILimeMeta>();
        var info = meta.Query<ModelFileInfo>().FirstOrDefault(r => r.Id == req.Id);
        if (info == null)
        {
            await Send.NotFoundAsync(ct);
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

/// <summary>
/// FileDownloadRequest
/// </summary>
internal sealed class FileDownloadRequest
{
    /// <summary>
    /// Id
    /// </summary>
    public required Guid Id { get; set; }
}
