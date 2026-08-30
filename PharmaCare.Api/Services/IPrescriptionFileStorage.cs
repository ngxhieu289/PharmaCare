using Microsoft.AspNetCore.Http;

namespace PharmaCare.Api.Services;

public interface IPrescriptionFileStorage
{
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken);
    Task<PrescriptionFile?> OpenReadAsync(string storedName, CancellationToken cancellationToken);
    Task DeleteAsync(string storedName);
}

public sealed record PrescriptionFile(Stream Stream, string ContentType, string DownloadName);

public sealed class PrescriptionFileException : Exception
{
    public PrescriptionFileException(string message) : base(message)
    {
    }
}
