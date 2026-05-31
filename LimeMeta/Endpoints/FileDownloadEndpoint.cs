using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastEndpoints;
using LimeMeta.Data;
using LimeMeta.Logics;
using Microsoft.AspNetCore.StaticFiles;

namespace LimeMeta.Endpoints;

/// <summary>
/// FileDownloadEndpoint
/// </summary>
public class FileDownloadEndpoint : Endpoint<FileDownloadRequest>
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
    public override Task HandleAsync(FileDownloadRequest req, CancellationToken ct)
    {
        // var cliam = User.Claims.First(r => r.Type == UserLogic.ClaimUserId);
        // var userId = Guid.Parse(cliam.Value);

        var meta = Resolve<ILimeMeta>();
        var (info, path) = FileInfoLogic.Find(meta, req.Id);
        if (info == null || path == null)
        {
            return Send.NotFoundAsync(ct);
        }

        var file = new FileInfo(path);
        var contentType = "application/octet-stream";
        if (info.Type != null)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(info.Type, out contentType))
            {
                contentType = "application/octet-stream";
            }
        }

        return Send.FileAsync(file, contentType, null, true, ct);
    }
}

/// <summary>
/// FileDownloadRequest
/// </summary>
public class FileDownloadRequest
{
    /// <summary>
    /// Id
    /// </summary>
    public required Guid Id { get; set; }
}
