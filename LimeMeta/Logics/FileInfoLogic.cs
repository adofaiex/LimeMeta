using FastEndpoints;
using LimeMeta.Configurations;
using LimeMeta.Data;
using LimeMeta.Files;
using LimeMeta.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using FileInfo = LimeMeta.Models.FileInfo;

namespace LimeMeta.Logics;

/// <summary>
/// FileInfoLogic
/// </summary>
public class FileInfoLogic : BaseLogic<FileInfo>
{
    public FileInfoLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : base(loggerFactory, scopeFactory)
    {
        AfterDelete += OnAfterDelete;
    }

    /// <summary>
    /// OnAfterDelete
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnAfterDelete(object? sender, AfterDeleteEventArgs<FileInfo> args)
    {
        using var sc = args.LimeMeta.ScopeFactory.CreateScope();
        var storageResolver = sc.ServiceProvider.GetRequiredService<IFileStorageProviderResolver>();

        foreach (var obj in args.Objs)
        {
            storageResolver.Get(obj.Provider).DeleteAsync(obj, CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Create
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static (FileInfo info, string path) Create(ILimeMeta meta, string name)
    {
        using var sc = meta.ScopeFactory.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();

        var maxStore = meta.Query<FileInfo>().Max(r => r.Store);
        var cntStore = meta.Query<FileInfo>().Where(r => r.Store == maxStore && (r.Provider == null || r.Provider == LocalFileStorageProvider.ProviderName)).Count();
        if (cntStore >= LocalFileStorageProvider.GetStoreCount(config))
        {
            maxStore++;
        }

        var path = Path.Combine(LocalFileStorageProvider.GetRootPath(config), $"{maxStore}");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var fileName = Path.GetFileNameWithoutExtension(name);
        var fileExt = Path.GetExtension(name);
        var id = Guid.NewGuid();

        var real = $"{fileName}_{id}{fileExt}";

        var info = new FileInfo
        {
            Store = maxStore,
            Id = id,
            Name = name,
            Real = real,
            Type = fileExt,
            Size = 0,
            Hash = string.Empty,
            Provider = LocalFileStorageProvider.ProviderName
        };

        path = Path.Combine(path, info.Real);
        return (info, path);
    }

    /// <summary>
    /// Find
    /// </summary>
    /// <param name="meta"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static (FileInfo? info, string? path) Find(ILimeMeta meta, Guid id)
    {
        var info = meta.Query<FileInfo>().FirstOrDefault(r => r.Id == id);
        if (info == null) return (null, null);

        using var sc = meta.ScopeFactory.CreateScope();
        var config = sc.ServiceProvider.GetRequiredService<LimeMetaConfiguration>();

        var path = GetStorePath(info, config);
        return (info, path);
    }

    /// <summary>
    /// GetStorePath
    /// </summary>
    /// <param name="info"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static string GetStorePath(FileInfo info, LimeMetaConfiguration config) => LocalFileStorageProvider.GetStorePath(info, config);
}
