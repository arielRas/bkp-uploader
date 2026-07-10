namespace BkpUploader.Application.Abstractions;

public interface IFileSystemService
{
    IAsyncEnumerable<string> EnumerateFilesAsync(string rootPath, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<long> GetLengthAsync(string path, CancellationToken cancellationToken = default);

    Task<DateTimeOffset> GetLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default);
}
