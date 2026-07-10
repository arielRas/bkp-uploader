namespace BkpUploader.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
