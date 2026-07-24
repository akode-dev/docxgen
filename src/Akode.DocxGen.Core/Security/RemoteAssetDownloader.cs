using System.Collections.Frozen;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Security;

internal static class RemoteAssetDownloader
{
    private static readonly FrozenSet<string> AllowedImageMediaTypes =
        new[]
        {
            "image/png",
            "image/jpeg",
            "image/gif",
            "image/bmp",
            "image/svg+xml",
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HttpClient Client = CreateClient();

    public static ResolvedAsset Resolve(
        Uri uri,
        AssetKind kind,
        Limits limits)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(limits);
        if (!uri.IsAbsoluteUri || uri.Scheme is not ("http" or "https"))
        {
            throw Failure(
                uri.ToString(),
                "Remote assets must use an absolute HTTP or HTTPS URL.");
        }

        if (kind != AssetKind.Image)
        {
            throw Failure(
                uri.AbsoluteUri,
                "Only remote image assets are supported.");
        }

        EnsurePublicHost(uri);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
            using var response = Client.Send(
                request,
                HttpCompletionOption.ResponseHeadersRead);
            if ((int)response.StatusCode is >= 300 and < 400)
            {
                throw Failure(
                    uri.AbsoluteUri,
                    "Remote image redirects are not followed.");
            }

            response.EnsureSuccessStatusCode();
            var declaredLength = response.Content.Headers.ContentLength;
            if (declaredLength > limits.MaxAssetBytes)
            {
                throw TooLarge(uri.AbsoluteUri, declaredLength.Value, limits);
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType
                ?? InferMediaType(uri.AbsolutePath);
            if (!AllowedImageMediaTypes.Contains(mediaType))
            {
                throw Failure(
                    uri.AbsoluteUri,
                    $"Remote content type '{mediaType}' is not an approved image type.");
            }

            using var input = response.Content.ReadAsStream();
            using var output = new MemoryStream();
            var buffer = new byte[81_920];
            while (true)
            {
                var count = input.Read(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    break;
                }

                if (output.Length + count > limits.MaxAssetBytes)
                {
                    throw TooLarge(
                        uri.AbsoluteUri,
                        output.Length + count,
                        limits);
                }

                output.Write(buffer, 0, count);
            }

            return new ResolvedAsset(
                uri.AbsoluteUri,
                kind,
                mediaType,
                output.ToArray());
        }
        catch (AssetResolutionException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or IOException
            or SocketException
            or TaskCanceledException)
        {
            throw Failure(
                uri.AbsoluteUri,
                $"Remote image download failed: {exception.Message}",
                exception);
        }
    }

    private static void EnsurePublicHost(Uri uri)
    {
        IPAddress[] addresses;
        try
        {
            addresses = Dns.GetHostAddresses(uri.DnsSafeHost);
        }
        catch (SocketException exception)
        {
            throw Failure(
                uri.AbsoluteUri,
                $"Remote image host could not be resolved: {exception.Message}",
                exception);
        }

        if (addresses.Length == 0 || addresses.Any(IsNonPublic))
        {
            throw Failure(
                uri.AbsoluteUri,
                "Remote image hosts must resolve only to public IP addresses.");
        }
    }

    private static bool IsNonPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return IPAddress.IsLoopback(address)
                || address.IsIPv6LinkLocal
                || address.IsIPv6Multicast
                || address.IsIPv6SiteLocal;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] is 0 or 10 or 127
            || bytes[0] >= 224
            || bytes[0] == 169 && bytes[1] == 254
            || bytes[0] == 172 && bytes[1] is >= 16 and <= 31
            || bytes[0] == 192 && bytes[1] == 168;
    }

    private static string InferMediaType(string path) =>
        Path.GetExtension(path).ToUpperInvariant() switch
        {
            ".PNG" => "image/png",
            ".JPG" or ".JPEG" => "image/jpeg",
            ".GIF" => "image/gif",
            ".BMP" => "image/bmp",
            ".SVG" => "image/svg+xml",
            _ => "application/octet-stream",
        };

    private static AssetResolutionException TooLarge(
        string path,
        long length,
        Limits limits) =>
        new(
            DiagnosticRegistry.Create(
                DiagnosticCode.AssetTooLarge,
                path,
                $"Remote image is at least {length} bytes; the limit is "
                + $"{limits.MaxAssetBytes} bytes."));

    private static AssetResolutionException Failure(
        string path,
        string message,
        Exception? innerException = null) =>
        new(
            DiagnosticRegistry.Create(
                DiagnosticCode.RemoteImageDownloadFailed,
                path,
                message),
            innerException);

    private static HttpClient CreateClient()
    {
#pragma warning disable CA2000 // The returned HttpClient owns and disposes this handler.
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromSeconds(10),
        };
#pragma warning restore CA2000
        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
    }
}
