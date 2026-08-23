using Microsoft.Extensions.Options;

namespace PharmaCare.Api.Services;

public sealed class PrescriptionFileStorage : IPrescriptionFileStorage
{
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    private readonly string _rootDirectory;
    private readonly long _maxFileSize;

    public PrescriptionFileStorage(
        IHostEnvironment environment,
        IOptions<PrescriptionStorageSettings> settings)
    {
        _rootDirectory = Path.GetFullPath(
            Path.Combine(environment.ContentRootPath, settings.Value.Directory));
        _maxFileSize = settings.Value.MaxFileSizeBytes;
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > _maxFileSize)
        {
            throw new PrescriptionFileException(
                $"Ảnh đơn thuốc phải có dung lượng từ 1 byte đến {_maxFileSize / 1024 / 1024} MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(extension, out var contentType) ||
            !string.Equals(file.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new PrescriptionFileException("Chỉ chấp nhận ảnh JPEG, PNG hoặc WebP.");
        }

        await using var input = file.OpenReadStream();
        if (!await HasValidSignature(input, extension, cancellationToken))
        {
            throw new PrescriptionFileException("Nội dung file không khớp với định dạng ảnh.");
        }
        input.Position = 0;

        Directory.CreateDirectory(_rootDirectory);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = ResolvePath(storedName);
        await using var output = new FileStream(
            path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await input.CopyToAsync(output, cancellationToken);
        return storedName;
    }

    public Task<PrescriptionFile?> OpenReadAsync(
        string storedName,
        CancellationToken cancellationToken)
    {
        var path = ResolvePath(storedName);
        if (!File.Exists(path))
        {
            return Task.FromResult<PrescriptionFile?>(null);
        }

        var extension = Path.GetExtension(storedName);
        if (!AllowedTypes.TryGetValue(extension, out var contentType))
        {
            return Task.FromResult<PrescriptionFile?>(null);
        }

        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<PrescriptionFile?>(
            new PrescriptionFile(stream, contentType, $"prescription{extension}"));
    }

    public Task DeleteAsync(string storedName)
    {
        var path = ResolvePath(storedName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private string ResolvePath(string storedName)
    {
        var fileName = Path.GetFileName(storedName);
        if (!string.Equals(fileName, storedName, StringComparison.Ordinal))
        {
            throw new PrescriptionFileException("Tên file đơn thuốc không hợp lệ.");
        }

        var path = Path.GetFullPath(Path.Combine(_rootDirectory, fileName));
        if (!path.StartsWith(_rootDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new PrescriptionFileException("Đường dẫn file đơn thuốc không hợp lệ.");
        }
        return path;
    }

    private static async Task<bool> HasValidSignature(
        Stream stream,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header, cancellationToken);
        return extension switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3 &&
                header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 &&
                header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => bytesRead >= 12 &&
                header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
