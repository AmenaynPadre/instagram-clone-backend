using InstagramClone.Common;
using InstagramClone.Responses;

namespace InstagramClone.Services.Media;

public interface IMediaService
{
    Task<Result<MediaResponse>> UploadAsync(IFormFile file);
    Task<Result<List<MediaResponse>>> UploadManyAsync(List<IFormFile> files);
    Task<Result<bool>> DeleteAsync(Guid mediaId);
}