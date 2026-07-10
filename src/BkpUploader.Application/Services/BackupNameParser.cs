using System.Text.RegularExpressions;
using BkpUploader.Application.Abstractions;
using BkpUploader.Application.Models;

namespace BkpUploader.Application.Services;

public sealed class BackupNameParser : IBackupNameParser
{
    private static readonly Regex BackupNameRegex = new(
        "^(?<database>[A-Za-z0-9_]+)_(?<type>FULL|DIFF|LOG|TRANSAC|TRANSACTIONLOG)_(?<date>\\d{8})_(?<time>\\d{6})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public bool TryParse(string fileName, out ParsedBackupName? parsedName)
    {
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        Match match = BackupNameRegex.Match(nameWithoutExtension);

        if (!match.Success)
        {
            parsedName = null;
            return false;
        }

        string typeToken = match.Groups["type"].Value.ToUpperInvariant();
        if (!TryMapBackupType(typeToken, out BackupType backupType))
        {
            parsedName = null;
            return false;
        }

        string datePart = match.Groups["date"].Value;
        string timePart = match.Groups["time"].Value;
        if (!DateTime.TryParseExact(
                $"{datePart}{timePart}",
                "yyyyMMddHHmmss",
                null,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal,
                out DateTime parsedDateTime))
        {
            parsedName = null;
            return false;
        }

        parsedName = new ParsedBackupName(
            match.Groups["database"].Value,
            backupType,
            new DateTimeOffset(DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Utc)));

        return true;
    }

    private static bool TryMapBackupType(string typeToken, out BackupType backupType)
    {
        switch (typeToken)
        {
            case "FULL":
                backupType = BackupType.Full;
                return true;
            case "DIFF":
                backupType = BackupType.Diff;
                return true;
            case "LOG":
            case "TRANSAC":
            case "TRANSACTIONLOG":
                backupType = BackupType.TransactionLog;
                return true;
            default:
                backupType = default;
                return false;
        }
    }
}
