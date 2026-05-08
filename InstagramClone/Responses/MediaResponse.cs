using InstagramClone.Common;

namespace InstagramClone.Responses;

public class MediaResponse
{
    public Guid Id { get; set; }
    public string Url { get; set; }
    public MediaType Type { get; set; }
}