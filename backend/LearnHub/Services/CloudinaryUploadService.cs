using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LearnHub.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LearnHub.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadAsync(IFormFile file, ContentType contentType);
        Task<string> UploadRawAsync(byte[] fileBytes, string fileName);
        Task DeleteAsync(string url, ContentType contentType);
    }

    public class CloudinaryUploadService : IFileUploadService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryUploadService> _logger;

        public CloudinaryUploadService(Cloudinary cloudinary, ILogger<CloudinaryUploadService> logger)
        {
            _cloudinary = cloudinary;
            _logger = logger;
        }

        public async Task<string> UploadAsync(IFormFile file, ContentType contentType)
        {
            await using var stream = file.OpenReadStream();
            var fileDescription = new FileDescription(file.FileName, stream);

            if (contentType == ContentType.Video)
            {
                var result = await _cloudinary.UploadAsync(new VideoUploadParams { File = fileDescription });
                if (result.Error is not null)
                    throw new ApiException($"File upload failed: {result.Error.Message}", 502);

                return result.SecureUrl.ToString();
            }
            else
            {
                var result = await _cloudinary.UploadAsync(new RawUploadParams { File = fileDescription });
                if (result.Error is not null)
                    throw new ApiException($"File upload failed: {result.Error.Message}", 502);

                return result.SecureUrl.ToString();
            }
        }

        public async Task<string> UploadRawAsync(byte[] fileBytes, string fileName)
        {
            using var stream = new MemoryStream(fileBytes);
            var fileDescription = new FileDescription(fileName, stream);

            var result = await _cloudinary.UploadAsync(new RawUploadParams { File = fileDescription });
            if (result.Error is not null)
                throw new ApiException($"File upload failed: {result.Error.Message}", 502);

            return result.SecureUrl.ToString();
        }

        public async Task DeleteAsync(string url, ContentType contentType)
        {
            var publicId = ExtractPublicId(url, contentType);
            if (publicId is null)
            {
                _logger.LogWarning("Could not extract a Cloudinary public_id from {Url}; skipping delete.", url);
                return;
            }

            try
            {
                var destroyParams = new DeletionParams(publicId)
                {
                    ResourceType = contentType == ContentType.Video ? ResourceType.Video : ResourceType.Raw,
                };

                var result = await _cloudinary.DestroyAsync(destroyParams);
                if (result.Error is not null)
                {
                    _logger.LogWarning("Cloudinary delete failed for {PublicId}: {Error}", publicId, result.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cloudinary delete threw for {PublicId}.", publicId);
            }
        }

        private static string? ExtractPublicId(string url, ContentType contentType)
        {
            const string marker = "/upload/";
            var markerIndex = url.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
                return null;

            var afterUpload = url[(markerIndex + marker.Length)..];

            var slashIndex = afterUpload.IndexOf('/');
            if (slashIndex > 0 && afterUpload[0] == 'v' && afterUpload[1..slashIndex].All(char.IsDigit))
                afterUpload = afterUpload[(slashIndex + 1)..];

            if (contentType == ContentType.Video)
            {
                var dotIndex = afterUpload.LastIndexOf('.');
                if (dotIndex > 0)
                    afterUpload = afterUpload[..dotIndex];
            }

            return string.IsNullOrEmpty(afterUpload) ? null : afterUpload;
        }
    }
}
