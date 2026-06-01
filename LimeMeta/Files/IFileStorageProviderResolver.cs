namespace LimeMeta.Files;

/// <summary>
/// 文件存储服务解析器。
/// </summary>
public interface IFileStorageProviderResolver
{
    /// <summary>
    /// 当前配置使用的文件存储服务。
    /// </summary>
    IFileStorageProvider Current { get; }

    /// <summary>
    /// 根据名称获取文件存储服务。
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IFileStorageProvider Get(string? name);
}

/// <summary>
/// 文件存储服务解析器。
/// </summary>
public sealed class FileStorageProviderResolver : IFileStorageProviderResolver
{
    private readonly Dictionary<string, IFileStorageProvider> _providers;
    private readonly string _currentProvider;

    /// <summary>
    /// FileStorageProviderResolver
    /// </summary>
    /// <param name="providers"></param>
    /// <param name="config"></param>
    public FileStorageProviderResolver(IEnumerable<IFileStorageProvider> providers, Configurations.LimeMetaConfiguration config)
    {
        _providers = providers.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        _currentProvider = string.IsNullOrWhiteSpace(config.FileStore.Provider)
            ? LocalFileStorageProvider.ProviderName
            : config.FileStore.Provider;
    }

    /// <inheritdoc />
    public IFileStorageProvider Current => Get(_currentProvider);

    /// <inheritdoc />
    public IFileStorageProvider Get(string? name)
    {
        name = string.IsNullOrWhiteSpace(name) ? LocalFileStorageProvider.ProviderName : name;
        if (_providers.TryGetValue(name, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException($"未注册文件存储服务：{name}");
    }
}
