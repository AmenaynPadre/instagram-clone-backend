using InstagramClone.Common;
using InstagramClone.Data;
using InstagramClone.Exceptions;
using InstagramClone.Responses;
using Microsoft.EntityFrameworkCore;

namespace InstagramClone.Services.Media;

public class LocalMediaService : IMediaService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;

    public LocalMediaService(
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        AppDbContext context)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public async Task<Result<MediaResponse>> UploadAsync(IFormFile file)
    {
        ValidateFile(file);

        var media = await SaveFileAsync(file);

        await _context.Medias.AddAsync(media);

        await _context.SaveChangesAsync();

        return Result<MediaResponse>.Ok(MapToResponse(media));
    }

    public async Task<Result<List<MediaResponse>>> UploadManyAsync(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return Result<List<MediaResponse>>
                .Fail("No files uploaded");

        var uploadedMedia = new List<Entities.Media>();

        foreach (var file in files)
        {
            ValidateFile(file);

            var media = await SaveFileAsync(file);

            uploadedMedia.Add(media);
        }

        await _context.Medias.AddRangeAsync(uploadedMedia);

        await _context.SaveChangesAsync();

        var response = uploadedMedia
            .Select(MapToResponse)
            .ToList();

        return Result<List<MediaResponse>>.Ok(response);
    }

    public async Task<Result<bool>> DeleteAsync(Guid mediaId)
    {
        var media = await _context.Medias
            .FirstOrDefaultAsync(x => x.Id == mediaId);

        if (media == null)
            return Result<bool>.Fail("Media not found");

        var filePath = Path.Combine(
            _environment.WebRootPath,
            "uploads",
            media.FileName);

        if (File.Exists(filePath))
            File.Delete(filePath);

        _context.Medias.Remove(media);

        await _context.SaveChangesAsync();

        return Result<bool>.Ok(true, "Media deleted");
    }

    // =========================
    // PRIVATE METHODS
    // =========================

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new BadRequestException("File is empty");

        var allowedTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "video/mp4"
        };

        if (!allowedTypes.Contains(file.ContentType))
            throw new BadRequestException("Invalid file type");

        var maxSize = file.ContentType.StartsWith("video/")
            ? 100 * 1024 * 1024
            : 10 * 1024 * 1024;

        if (file.Length > maxSize)
            throw new BadRequestException("File too large");
    }

    private async Task<Entities.Media> SaveFileAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(
            _environment.WebRootPath,
            "uploads");

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName =
            $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream =
            new FileStream(filePath, FileMode.Create);

        await file.CopyToAsync(stream);

        var request = _httpContextAccessor.HttpContext.Request;

        var url =
            $"{request.Scheme}://{request.Host}/uploads/{fileName}";

        return new Entities.Media
        {
            Url = url,
            FileName = fileName,
            ContentType = file.ContentType,
            Size = file.Length,
            Type = file.ContentType.StartsWith("video/")
                ? MediaType.Video
                : MediaType.Image
        };
    }

    private MediaResponse MapToResponse(Entities.Media media)
    {
        return new MediaResponse
        {
            Id = media.Id,
            Url = media.Url,
            Type = media.Type
        };
    }
}