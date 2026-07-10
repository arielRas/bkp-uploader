using BkpUploader.Application.Models;

namespace BkpUploader.Application.Abstractions;

public interface IBackupNameParser
{
    bool TryParse(string fileName, out ParsedBackupName? parsedName);
}
