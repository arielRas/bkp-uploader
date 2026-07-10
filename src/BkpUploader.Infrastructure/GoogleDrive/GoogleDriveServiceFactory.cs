using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace BkpUploader.Infrastructure.GoogleDrive;

public sealed class GoogleDriveServiceFactory
{
    private readonly IOptions<GoogleDriveOptions> _options;

    public GoogleDriveServiceFactory(IOptions<GoogleDriveOptions> options)
    {
        _options = options;
    }

    public DriveService Create()
    {
        GoogleDriveOptions options = _options.Value;

        GoogleCredential credential = string.IsNullOrWhiteSpace(options.CredentialFilePath)
            ? GoogleCredential.GetApplicationDefault()
            : CredentialFactory.FromFile<ServiceAccountCredential>(options.CredentialFilePath).ToGoogleCredential();

        credential = credential.CreateScoped(DriveService.ScopeConstants.Drive);

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = options.ApplicationName
        });
    }
}
