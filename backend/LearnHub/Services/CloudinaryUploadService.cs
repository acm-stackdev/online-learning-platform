using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LearnHub.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace LearnHub.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadAsync(IFormFile file, ContentType contentType);
        Task<string> UploadRawAsync(byte[] fileBytes, string fileName);
    }

    public class CloudinaryUploadService : IFileUploadService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryUploadService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
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
    }
}
