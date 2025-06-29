using Amazon.S3;
using Amazon.S3.Model;
using Classified.Shared.Infrastructure.S3.Abstractions;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Classified.Shared.Infrastructure.S3.Service
{
    public class SufyStorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _serviceURL = "https://mos.us-south-1.sufybkt.com";
        private readonly string _bucketName = "classified";
        private readonly string _signatureVersion = "2";
        private readonly bool _forcePathStyle = true;
        private readonly bool _useHttp = true;

        private readonly IConfiguration _config = LibraryConfiguration.BuildConfiguration();

        // Поддерживаемые форматы изображений
        private readonly string[] _imageExtensions = {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif",
            ".webp", ".svg", ".heif", ".heic", ".raw", ".ico"
        };

        public SufyStorageService()
        {
            var s3Config = new AmazonS3Config
            {
                ServiceURL = _serviceURL,
                ForcePathStyle = _forcePathStyle,
                UseHttp = _useHttp,
                SignatureVersion = _signatureVersion
            };

            _s3Client = new AmazonS3Client(_config["S3:accessKey"], _config["S3:secretKey"], s3Config);
        }

        private static readonly Dictionary<string, string[]> AllowedExtensions = new()
        {
            ["image"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" },
            ["video"] = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" },
            ["document"] = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" },
        };

        public async Task<string> UploadFileAsync(
            Stream fileStream,
            string fileName,
            string containerName,
            string id,
            string expectedFileType) // "image", "video", "document"
        {
            // Получаем расширение файла
            string fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

            // Проверяем, что расширение соответствует ожидаемому типу
            if (!AllowedExtensions.TryGetValue(expectedFileType, out var validExtensions))
                throw new ArgumentException($"Unsupported file type: {expectedFileType}");

            if (!validExtensions.Contains(fileExtension))
                throw new ArgumentException(
                    $"File extension '{fileExtension}' is not allowed for type '{expectedFileType}'. " +
                    $"Allowed: {string.Join(", ", validExtensions)}");

            // Обработка изображений (если тип = "image")
            if (expectedFileType == "image")
            {
                fileStream = await ConvertToWebPAsync(fileStream);
                fileExtension = ".webp"; // Теперь файл будет в формате WebP
            }

            // Формируем ключ для S3 (например: "photos/user123.webp")
            string key = $"{containerName}/{id}{fileExtension}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                AutoCloseStream = true
            };

            await _s3Client.PutObjectAsync(request);

            return $"{_serviceURL}/{_bucketName}/{key}";
        }

        private async Task<Stream> ConvertToWebPAsync(Stream imageStream)
        {
            var outputStream = new MemoryStream();

            using (var image = await Image.LoadAsync(imageStream))
            {
                // Оптимизация размера (макс. 1920x1080)
                if (image.Width > 1920 || image.Height > 1080)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(1920, 1080),
                        Mode = ResizeMode.Max
                    }));
                }

                // Сохраняем в WebP с качеством 80%
                await image.SaveAsync(outputStream, new WebpEncoder { Quality = 80 });
            }

            outputStream.Position = 0;
            return outputStream;
        }

        private async Task<Stream> ProcessImageAsync(Stream imageStream)
        {
            // Оптимальные настройки для WebP
            var encoder = new WebpEncoder
            {
                Quality = 80, // Качество 80% - хороший баланс между качеством и размером
                Method = WebpEncodingMethod.Default
            };

            // Оптимальные размеры для изображений
            const int maxWidth = 1920;
            const int maxHeight = 1080;

            var outputStream = new MemoryStream();

            try
            {
                using (var image = await Image.LoadAsync(imageStream))
                {
                    // Изменяем размер, если изображение слишком большое
                    if (image.Width > maxWidth || image.Height > maxHeight)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(maxWidth, maxHeight),
                            Mode = ResizeMode.Max
                        }));
                    }

                    // Сохраняем в WebP формате
                    await image.SaveAsync(outputStream, encoder);
                }

                outputStream.Position = 0; // Сбрасываем позицию для чтения
                return outputStream;
            }
            catch
            {
                await outputStream.DisposeAsync();
                throw;
            }
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            var uri = new Uri(fileUrl);
            var key = uri.AbsolutePath.TrimStart('/');
            key = key.Substring(_bucketName.Length + 1);

            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            });
        }
    }
}

