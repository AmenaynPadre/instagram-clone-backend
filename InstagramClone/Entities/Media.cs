using InstagramClone.Common;

namespace InstagramClone.Entities;

public class Media : BaseEntity
{
    public string Url { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public long Size { get; set; }

    public MediaType Type { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }
}