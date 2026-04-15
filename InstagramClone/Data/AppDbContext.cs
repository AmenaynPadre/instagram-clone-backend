using Microsoft.EntityFrameworkCore;

namespace InstagramClone.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}