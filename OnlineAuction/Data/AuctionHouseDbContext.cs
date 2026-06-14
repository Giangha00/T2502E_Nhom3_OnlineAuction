using Microsoft.EntityFrameworkCore;
using OnlineAuction.Entities;

namespace OnlineAuction.Data;

public class AuctionHouseDbContext : DbContext
{
    public AuctionHouseDbContext(DbContextOptions<AuctionHouseDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(160).IsRequired();
            entity.Property(user => user.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(user => user.AvatarUrl).HasMaxLength(260);
            entity.Property(user => user.InitialPassword).HasMaxLength(120).IsRequired();
        });
    }
}
