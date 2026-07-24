#pragma warning disable CA2007 // Console file I/O does not require synchronization-context capture.
using System.Text;

namespace Akode.DocxGen.Cli.Output;

internal static class AtomicFileWriter
{
    public static async Task WriteStreamAsync(
        string path,
        Stream source,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(source);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new IOException($"Output '{fullPath}' has no parent directory.");
        Directory.CreateDirectory(directory);
        if (!overwrite && File.Exists(fullPath))
        {
            throw new IOException(
                $"Output '{fullPath}' already exists. Use --overwrite or choose another path.");
        }

        var temporary = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            if (source.CanSeek)
            {
                source.Position = 0;
            }

            await using (var destination = new FileStream(
                             temporary,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 81_920,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await source.CopyToAsync(
                    destination,
                    cancellationToken).ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporary, fullPath, overwrite);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    public static Task WriteTextAsync(
        string path,
        string content,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        var bytes = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true).GetBytes(content);
        return WriteStreamAsync(
            path,
            new MemoryStream(bytes, writable: false),
            overwrite,
            cancellationToken);
    }
}
#pragma warning restore CA2007
