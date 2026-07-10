using BkpUploader.Application.Abstractions;

namespace BkpUploader.Infrastructure.Common;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
