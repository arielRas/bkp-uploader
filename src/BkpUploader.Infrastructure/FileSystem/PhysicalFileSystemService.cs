using BkpUploader.Application.Abstractions;

namespace BkpUploader.Infrastructure.FileSystem;

public sealed class PhysicalFileSystemService : IFileSystemService
{
    public async IAsyncEnumerable<string> EnumerateFilesAsync(
        string rootPath,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootPath))
        {
            yield break;
        }

        foreach (string filePath in Directory.EnumerateFiles(rootPath, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return filePath;
            await Task.Yield();
        }
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(path));
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<long> GetLengthAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new FileInfo(path).Length);
    }

    public Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new DateTimeOffset(File.GetLastWriteTimeUtc(path)));
    }
}
