using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace AF.Umbraco.Azure.Blob.Media.Storage.Middlewares
{
    /// <summary>
    /// Exposes opt-in diagnostic endpoints for liveness and basic media storage I/O checks.
    /// Active only when registered by composer with <c>AF_SMOKE_TESTS=1</c>.
    /// </summary>
    public class AzureBlobSmokeTestsMiddleware(IAzureBlobFileSystemProvider fileSystemProvider, ILogger<AzureBlobSmokeTestsMiddleware> logger) : IMiddleware
    {
        private const string LogPrefix = "[AFUABMS]";

        /// <summary>
        /// Handles smoke endpoints and delegates non-smoke requests to the next middleware.
        /// </summary>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (HttpMethods.IsGet(context.Request.Method) && context.Request.Path == "/smoke/health")
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"status\":\"ok\"}");
                return;
            }

            if (HttpMethods.IsGet(context.Request.Method) && context.Request.Path == "/smoke/debug-test")
            {
                try
                {
                    IAzureBlobFileSystem fileSystem = fileSystemProvider.GetFileSystem(AzureBlobFileSystemOptions.MediaFileSystemName);
                    string path = $"/smoke/debug/{Guid.NewGuid():N}.txt";
                    bool existsBefore = fileSystem.FileExists(path);

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{{\"status\":\"ok\",\"path\":\"{path}\",\"existsBefore\":{existsBefore.ToString().ToLowerInvariant()}}}");
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{LogPrefix} Smoke debug test failed.", LogPrefix);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Smoke debug test failed.");
                    return;
                }
            }

            if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path == "/smoke/media-upload")
            {
                try
                {
                    IAzureBlobFileSystem fileSystem = fileSystemProvider.GetFileSystem(AzureBlobFileSystemOptions.MediaFileSystemName);
                    string path = $"/smoke/{Guid.NewGuid():N}.txt";

                    using var payload = new MemoryStream(Encoding.UTF8.GetBytes("smoke-upload"));
                    fileSystem.AddFile(path, payload, true);

                    if (!fileSystem.FileExists(path))
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("Uploaded file was not found in media storage.");
                        return;
                    }

                    using Stream stream = fileSystem.OpenFile(path);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    string content = await reader.ReadToEndAsync();

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{{\"status\":\"ok\",\"exists\":true,\"content\":\"{content}\"}}");
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{LogPrefix} Smoke test media upload failed.", LogPrefix);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Smoke test media upload failed.");
                    return;
                }
            }

            await next(context);
        }
    }
}
