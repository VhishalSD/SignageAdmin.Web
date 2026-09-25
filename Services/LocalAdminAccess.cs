using System.Net;

namespace SignageApp.Services;

public sealed class LocalAdminAccess(RequestDelegate next, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address?.IsIPv4MappedToIPv6 == true) address = address.MapToIPv4();

        if ((address is not null && IPAddress.IsLoopback(address)) || IsPublicRead(context.Request))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsync("Beheer is alleen bereikbaar vanaf de computer waarop Signage Beheer draait (loopback).");
    }

    private bool IsPublicRead(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) return false;

        var path = request.Path;
        var exactPath = path.Value?.TrimEnd('/');
        return string.Equals(exactPath, "/Live", StringComparison.OrdinalIgnoreCase)
            || string.Equals(exactPath, "/api/admin/status", StringComparison.OrdinalIgnoreCase)
            || string.Equals(exactPath, "/favicon.ico", StringComparison.OrdinalIgnoreCase)
            || (exactPath?.StartsWith("/SignageAdmin.Web.", StringComparison.OrdinalIgnoreCase) == true
                && exactPath.EndsWith(".styles.css", StringComparison.OrdinalIgnoreCase))
            || path.StartsWithSegments("/preview")
            || path.StartsWithSegments("/data")
            || path.StartsWithSegments("/uploads")
            // Shared layout dependencies of the public /Live page.
            || path.StartsWithSegments("/css")
            || path.StartsWithSegments("/js")
            || path.StartsWithSegments("/lib")
            || IsExistingMediaFile(path);
    }

    private bool IsExistingMediaFile(PathString path)
    {
        var extension = Path.GetExtension(path.Value ?? "").ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".svg" or ".webp" or ".pdf"))
            return false;
        var file = environment.WebRootFileProvider.GetFileInfo(path.Value ?? "");
        return file.Exists && !file.IsDirectory;
    }
}
