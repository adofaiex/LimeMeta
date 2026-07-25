using LimeMeta.Files;
using Xunit;
using ModelFileInfo = LimeMeta.Models.FileInfo;

namespace LimeMeta.Tests;

public sealed class FileUrlResolverContractTests
{
    [Fact]
    public async Task LocalProvider_ResolvePublicUrl_UsesDownloadPath()
    {
        var info = new ModelFileInfo
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "a.png",
            Real = "a.png",
            Size = 1,
            Hash = "hash"
        };

        IFileStorageProvider provider = new FakeLocalProvider();
        var url = await provider.ResolvePublicUrlAsync(info, CancellationToken.None);
        Assert.Equal("/api/file/download?id=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", url);
    }

    private sealed class FakeLocalProvider : IFileStorageProvider
    {
        public string Name => "Local";

        public Task DeleteAsync(ModelFileInfo info, CancellationToken ct) => Task.CompletedTask;

        public Task<FileStorageOpenResult> OpenAsync(ModelFileInfo info, CancellationToken ct)
            => Task.FromResult(new FileStorageOpenResult { FilePath = "x" });

        public Task<FileStorageSaveResult> SaveAsync(Stream stream, string fileName, string? contentType, long size, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<string?> ResolvePublicUrlAsync(ModelFileInfo info, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(info.Url))
            {
                return Task.FromResult<string?>(info.Url);
            }

            return Task.FromResult<string?>($"/api/file/download?id={info.Id}");
        }
    }
}
