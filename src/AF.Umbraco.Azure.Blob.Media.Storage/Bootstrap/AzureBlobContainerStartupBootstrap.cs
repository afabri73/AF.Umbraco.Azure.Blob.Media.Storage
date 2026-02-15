using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AF.Umbraco.Azure.Blob.Media.Storage.Bootstrap;

/// <summary>
/// Performs startup fail-fast validation for Azure Blob configuration and connectivity.
/// Application startup is blocked when required settings are missing, storage is unreachable,
/// or required containers are unavailable while auto-create is disabled.
/// </summary>
internal sealed class AzureBlobStartupConnectivityHostedService(
    IConfiguration configuration,
    ILogger<AzureBlobStartupConnectivityHostedService> logger) : IHostedService
{
    private const string LogPrefix = "[AFUABMS]";

    /// <summary>
    /// Validates configured Azure Blob sections, tests account connectivity, and ensures media/cache containers.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        AzureBlobSectionConfiguration mediaConfiguration = GetRequiredSectionConfiguration("Media");
        AzureBlobSectionConfiguration imageSharpConfiguration = GetRequiredSectionConfiguration("ImageSharp");

        await EnsureConnectionAsync(mediaConfiguration, cancellationToken);

        if (!string.Equals(
            mediaConfiguration.ConnectionString,
            imageSharpConfiguration.ConnectionString,
            StringComparison.Ordinal))
        {
            await EnsureConnectionAsync(imageSharpConfiguration, cancellationToken);
        }

        await EnsureContainerAsync(mediaConfiguration, cancellationToken);
        await EnsureContainerAsync(imageSharpConfiguration, cancellationToken);
    }

    /// <summary>
    /// No-op stop method required by <see cref="IHostedService"/>.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private AzureBlobSectionConfiguration GetRequiredSectionConfiguration(string sectionName)
    {
        IConfigurationSection section = configuration.GetSection($"Umbraco:Storage:AzureBlob:{sectionName}");
        if (!section.Exists())
        {
            string message = $"{LogPrefix} Missing required configuration section 'Umbraco:Storage:AzureBlob:{sectionName}'. Application startup is blocked.";
            logger.LogCritical(message);
            throw new InvalidOperationException(message);
        }

        string connectionString = section["ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            string message = $"{LogPrefix} Missing required setting 'Umbraco:Storage:AzureBlob:{sectionName}:ConnectionString'. Application startup is blocked.";
            logger.LogCritical(message);
            throw new InvalidOperationException(message);
        }

        string containerName = section["ContainerName"];
        if (string.IsNullOrWhiteSpace(containerName))
        {
            string message = $"{LogPrefix} Missing required setting 'Umbraco:Storage:AzureBlob:{sectionName}:ContainerName'. Application startup is blocked.";
            logger.LogCritical(message);
            throw new InvalidOperationException(message);
        }

        bool createContainerIfNotExists = section.GetValue<bool?>("CreateContainerIfNotExists")
            ?? configuration.GetValue<bool?>("Umbraco:Storage:AzureBlob:CreateContainerIfNotExists")
            ?? true;

        return new AzureBlobSectionConfiguration(
            sectionName,
            connectionString,
            containerName,
            createContainerIfNotExists);
    }

    private async Task EnsureConnectionAsync(AzureBlobSectionConfiguration sectionConfiguration, CancellationToken cancellationToken)
    {
        try
        {
            BlobServiceClient serviceClient = new(sectionConfiguration.ConnectionString);
            await serviceClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            logger.LogInformation(
                "{LogPrefix} Azure Blob connection check passed for section '{SectionName}'.",
                LogPrefix,
                sectionConfiguration.SectionName);
        }
        catch (RequestFailedException ex)
        {
            logger.LogCritical(
                ex,
                "{LogPrefix} Azure Blob connection check failed for section '{SectionName}'. Application startup is blocked.",
                LogPrefix,
                sectionConfiguration.SectionName);
            throw;
        }
    }

    private async Task EnsureContainerAsync(AzureBlobSectionConfiguration sectionConfiguration, CancellationToken cancellationToken)
    {
        try
        {
            BlobContainerClient containerClient = new(
                sectionConfiguration.ConnectionString,
                sectionConfiguration.ContainerName);

            if (sectionConfiguration.CreateContainerIfNotExists)
            {
                await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                logger.LogInformation(
                    "{LogPrefix} Ensured Azure Blob container '{ContainerName}' exists for section '{SectionName}'.",
                    LogPrefix,
                    sectionConfiguration.ContainerName,
                    sectionConfiguration.SectionName);

                return;
            }

            Response<bool> containerExistsResponse = await containerClient.ExistsAsync(cancellationToken);
            if (!containerExistsResponse.Value)
            {
                throw new InvalidOperationException(
                    $"{LogPrefix} Container '{sectionConfiguration.ContainerName}' does not exist for section '{sectionConfiguration.SectionName}' and auto-create is disabled. Application startup is blocked.");
            }

            logger.LogInformation(
                "{LogPrefix} Azure Blob container '{ContainerName}' exists for section '{SectionName}' (auto-create disabled).",
                LogPrefix,
                sectionConfiguration.ContainerName,
                sectionConfiguration.SectionName);
        }
        catch (Exception ex) when (ex is RequestFailedException or InvalidOperationException)
        {
            logger.LogError(
                ex,
                "{LogPrefix} Failed to ensure Azure Blob container '{ContainerName}' for section '{SectionName}'. Application startup is blocked.",
                LogPrefix,
                sectionConfiguration.ContainerName,
                sectionConfiguration.SectionName);
            throw;
        }
    }

    private sealed class AzureBlobSectionConfiguration(
        string sectionName,
        string connectionString,
        string containerName,
        bool createContainerIfNotExists)
    {
        public string SectionName { get; } = sectionName;
        public string ConnectionString { get; } = connectionString;
        public string ContainerName { get; } = containerName;
        public bool CreateContainerIfNotExists { get; } = createContainerIfNotExists;
    }
}
