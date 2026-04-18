using InstagramClone.Common;

namespace InstagramClone.Entities;

public class RefreshToken : BaseEntity
{
    public string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; }

    public string? Device { get; set; }
    public string? IpAddress { get; set; }
}