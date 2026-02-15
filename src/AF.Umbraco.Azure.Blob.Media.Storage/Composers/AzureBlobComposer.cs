using AF.Umbraco.Azure.Blob.Media.Storage.Bootstrap;
using AF.Umbraco.Azure.Blob.Media.Storage.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SixLabors.ImageSharp.Web.Caching;
using System;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace AF.Umbraco.Azure.Blob.Media.Storage.Composers
{
    /// <summary>
    /// Composes the package using official Umbraco Azure Blob storage providers.
    /// </summary>
    [ComposeAfter(typeof(global::Umbraco.Cms.Imaging.ImageSharp.ImageSharpComposer))]
    public class AzureBlobComposer : IComposer
    {
        /// <summary>
        /// Registers package runtime services:
        /// Azure Blob media filesystem, ImageSharp cache, upload-validation middleware,
        /// optional smoke middleware, and startup fail-fast hosted service.
        /// </summary>
        /// <param name="builder">The Umbraco builder.</param>
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AddAzureBlobMediaFileSystem();
            builder.AddAzureBlobFileSystem("ImageSharp");
            builder.AddAzureBlobImageSharpCache("ImageSharp", "cache");

            if (Environment.GetEnvironmentVariable("AF_SMOKE_TESTS") == "1")
            {
                builder.Services.TryAddTransient<AzureBlobSmokeTestsMiddleware>();
            }
            builder.Services.AddHostedService<AzureBlobStartupConnectivityHostedService>();
            builder.Services.AddHostedService<AzureBlobCacheRetentionCleanupHostedService>();
            builder.Services.Configure<UmbracoPipelineOptions>(options =>
            {
                if (Environment.GetEnvironmentVariable("AF_SMOKE_TESTS") == "1")
                {
                    options.AddFilter(new UmbracoPipelineFilter(
                        "AzureBlobSmokeTests",
                        prePipeline: app => app.UseMiddleware<AzureBlobSmokeTestsMiddleware>()));
                }
            });
        }
    }
}
