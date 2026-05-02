using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EventEase.Services
{
    public class BlobStorageService
    {
        private readonly string _connectionString;
        private const string ContainerName = "venue-images";
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };

        public BlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzuriteStorage")
                ?? "UseDevelopmentStorage=true";
        }

        /// <summary>
        /// Validates a file before uploading. Returns an error message, or null if valid.
        /// </summary>
        public string? ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null; // No file is fine — image is optional

            if (file.Length > MaxFileSizeBytes)
                return $"File is too large. Maximum allowed size is 5 MB.";

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
                return $"Invalid file type '{extension}'. Accepted types: JPG, PNG, GIF, WEBP.";

            if (!AllowedMimeTypes.Contains(file.ContentType))
                return $"Invalid file content type '{file.ContentType}'.";

            return null; // Valid
        }

        /// <summary>
        /// Uploads an image to the Azurite/Azure blob container.
        /// Returns the blob URL on success, or null if the upload fails.
        /// </summary>
        public async Task<string?> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            try
            {
                var containerClient = new BlobContainerClient(_connectionString, ContainerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var blobClient = containerClient.GetBlobClient(fileName);

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application.
                // The venue/event will be saved without an image.
                Console.Error.WriteLine($"[BlobStorageService] Upload failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a blob by its stored URL. Silently ignores errors.
        /// </summary>
        public async Task DeleteImageAsync(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            try
            {
                var uri = new Uri(imageUrl);
                var blobName = Path.GetFileName(uri.LocalPath);
                var containerClient = new BlobContainerClient(_connectionString, ContainerName);
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[BlobStorageService] Delete failed: {ex.Message}");
            }
        }
    }
}
