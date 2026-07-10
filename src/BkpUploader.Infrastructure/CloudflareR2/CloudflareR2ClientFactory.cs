using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace BkpUploader.Infrastructure.CloudflareR2;

public sealed class CloudflareR2ClientFactory
{
    private const string AuthenticationRegion = "auto";

    private readonly CloudflareR2Options _options;

    public CloudflareR2ClientFactory(IOptions<CloudflareR2Options> options)
    {
        _options = options.Value;
    }

    public IAmazonS3 Create()
    {
        string accessKeyId = EnsureConfigured(_options.AccessKeyId, nameof(CloudflareR2Options.AccessKeyId));
        string secretAccessKey = EnsureConfigured(_options.SecretAccessKey, nameof(CloudflareR2Options.SecretAccessKey));
        string accountId = EnsureConfigured(_options.AccountId, nameof(CloudflareR2Options.AccountId));
        string serviceUrl = EnsureConfigured(_options.ServiceUrl, nameof(CloudflareR2Options.ServiceUrl));

        BasicAWSCredentials credentials = new(accessKeyId, secretAccessKey);

        AmazonS3Config config = new()
        {
            ServiceURL = serviceUrl,
            AuthenticationRegion = AuthenticationRegion,
            ForcePathStyle = true
        };

        return new AmazonS3Client(credentials, config);
    }

    private static string EnsureConfigured(string value, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{optionName} is required.");
        }

        return value;
    }
}
