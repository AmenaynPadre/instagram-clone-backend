using System.ComponentModel.DataAnnotations;
using InstagramClone.Common;

namespace InstagramClone.Entities;

public class User : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Email { get; set; }
    
    [Required]
    public string PasswordHash { get; set; }
    
    public string? Bio { get; set; }
    
    public string? ProfilePictureUrl { get; set; }
    
    public ICollection<RefreshToken>? RefreshTokens { get; set; }
}