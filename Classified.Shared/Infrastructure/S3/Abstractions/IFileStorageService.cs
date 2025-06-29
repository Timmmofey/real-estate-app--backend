namespace Classified.Shared.Infrastructure.S3.Abstractions
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName, string id, string fileType);
        Task DeleteFileAsync(string fileUrl);
    }
}
