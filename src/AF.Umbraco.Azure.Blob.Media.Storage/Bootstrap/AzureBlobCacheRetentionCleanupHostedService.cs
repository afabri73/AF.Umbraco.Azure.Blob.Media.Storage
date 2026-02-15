using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AF.Umbraco.Azure.Blob.Media.Storage.Bootstrap;

/// <summary>
/// Periodically removes expired ImageSharp cache blobs based on configurable retention settings.
/// </summary>
internal sealed class AzureBlobCacheRetentionCleanupHostedService(
    IConfiguration configuration,
    ILogger<AzureBlobCacheRetentionCleanupHostedService> logger) : BackgroundService
{
    private const string LogPrefix = "[AFUABMS]";
    private const string ImageSharpSectionPath = "Umbraco:Storage:AzureBlob:ImageSharp";
    private const string CacheFolderName = "cache";
    private static readonly TimeSpan DefaultDisabledPoll = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetryOnError = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _retentionSweepLock = new(1, 1);
    private DateTimeOffset _nextRetentionSweepUtc = DateTimeOffset.MinValue;

    /// <summary>
    /// Runs the background cleanup loop until application shutdown.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            CacheRetentionRuntimeSettings settings = ResolveSettings();
            if (!settings.Enabled)
            {
                await Task.Delay(DefaultDisabledPoll, stoppingToken).ConfigureAwait(false);
                continue;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now < _nextRetentionSweepUtc)
            {
                TimeSpan wait = _nextRetentionSweepUtc - now;
                if (wait > TimeSpan.FromMinutes(1))
                {
                    wait = TimeSpan.FromMinutes(1);
                }

                await Task.Delay(wait, stoppingToken).ConfigureAwait(false);
                continue;
            }

            await CleanupExpiredCacheIfNeededAsync(settings, stoppingToken).ConfigureAwait(false);
        }
    }

    private CacheRetentionRuntimeSettings ResolveSettings()
    {
        IConfigurationSection imageSharpSection = configuration.GetSection(ImageSharpSectionPath);
        IConfigurationSection retentionSection = imageSharpSection.GetSection("CacheRetention");

        string connectionString = imageSharpSection["ConnectionString"] ?? string.Empty;
        string containerName = imageSharpSection["ContainerName"] ?? string.Empty;
        string containerRootPath = imageSharpSection["ContainerRootPath"] ?? string.Empty;

        bool testModeEnable = retentionSection.GetValue<bool?>("TestModeEnable") ?? false;
        if (testModeEnable)
        {
            int testModeSweepSeconds = retentionSection.GetValue<int?>("TestModeSweepSeconds") ?? 30;
            int testModeMaxAgeMinutes = retentionSection.GetValue<int?>("TestModeMaxAgeMinutes") ?? 10;

            return new CacheRetentionRuntimeSettings(
                Enabled: true,
                ConnectionString: connectionString,
                ContainerName: containerName,
                ContainerRootPath: containerRootPath,
                MaxAge: TimeSpan.FromMinutes(Math.Max(1, testModeMaxAgeMinutes)),
                SweepInterval: TimeSpan.FromSeconds(Math.Max(5, testModeSweepSeconds)));
        }

        bool enabled = retentionSection.GetValue<bool?>("Enabled") ?? false;
        int numberOfDays = retentionSection.GetValue<int?>("NumberOfDays") ?? 90;

        return new CacheRetentionRuntimeSettings(
            Enabled: enabled,
            ConnectionString: connectionString,
            ContainerName: containerName,
            ContainerRootPath: containerRootPath,
            MaxAge: TimeSpan.FromDays(Math.Max(1, numberOfDays)),
            SweepInterval: TimeSpan.FromHours(12));
    }

    private async Task CleanupExpiredCacheIfNeededAsync(CacheRetentionRuntimeSettings settings, CancellationToken cancellationToken)
    {
        if (!await _retentionSweepLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now < _nextRetentionSweepUtc)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.ConnectionString) || string.IsNullOrWhiteSpace(settings.ContainerName))
            {
                logger.LogWarning(
                    "{LogPrefix} Cache retention cleanup skipped because ImageSharp storage settings are incomplete.",
                    LogPrefix);
                _nextRetentionSweepUtc = DateTimeOffset.UtcNow.Add(RetryOnError);
                return;
            }

            BlobContainerClient containerClient = new(settings.ConnectionString, settings.ContainerName);
            DateTimeOffset cutoff = DateTimeOffset.UtcNow.Subtract(settings.MaxAge);
            IReadOnlyCollection<string> prefixes = BuildPrefixes(settings.ContainerRootPath);

            int deletedCount = 0;
            foreach (string prefix in prefixes)
            {
                await foreach (BlobItem blob in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
                {
                    DateTimeOffset? lastModified = blob.Properties.LastModified;
                    if (!lastModified.HasValue || lastModified.Value >= cutoff)
                    {
                        continue;
                    }

                    Response<bool> result = await containerClient
                        .DeleteBlobIfExistsAsync(blob.Name, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    if (result.Value)
                    {
                        deletedCount++;
                    }
                }
            }

            logger.LogInformation(
                "{LogPrefix} Azure Blob cache retention sweep completed. Container={ContainerName}; Deleted={DeletedCount}; MaxAge={MaxAge}.",
                LogPrefix,
                settings.ContainerName,
                deletedCount,
                settings.MaxAge);

            _nextRetentionSweepUtc = DateTimeOffset.UtcNow.Add(settings.SweepInterval);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "{LogPrefix} Unable to apply Azure Blob cache retention cleanup.",
                LogPrefix);

            _nextRetentionSweepUtc = DateTimeOffset.UtcNow.Add(RetryOnError);
        }
        finally
        {
            _retentionSweepLock.Release();
        }
    }

    private static IReadOnlyCollection<string> BuildPrefixes(string containerRootPath)
    {
        string normalizedRoot = NormalizePath(containerRootPath);
        var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"{CacheFolderName}/"
        };

        if (!string.IsNullOrWhiteSpace(normalizedRoot))
        {
            prefixes.Add($"{normalizedRoot}/{CacheFolderName}/");

            if (normalizedRoot.Equals(CacheFolderName, StringComparison.OrdinalIgnoreCase))
            {
                prefixes.Add($"{CacheFolderName}/{CacheFolderName}/");
            }
        }

        return [.. prefixes];
    }

    private static string NormalizePath(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Trim('/').Replace("\\", "/", StringComparison.Ordinal);

    private sealed record CacheRetentionRuntimeSettings(
        bool Enabled,
        string ConnectionString,
        string ContainerName,
        string ContainerRootPath,
        TimeSpan MaxAge,
        TimeSpan SweepInterval);
}
