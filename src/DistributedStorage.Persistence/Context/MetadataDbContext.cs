using DistributedStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DistributedStorage.Persistence.Context;

public class MetadataDbContext : DbContext
{
    public DbSet<FileMetadata> Files => Set<FileMetadata>();
    public DbSet<ChunkMetadata> Chunks => Set<ChunkMetadata>();

    public MetadataDbContext(DbContextOptions<MetadataDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileMetadata>(entity =>
        {
            entity.HasKey(f => f.FileId);

            entity.HasMany(f => f.Chunks)
                  .WithOne()
                  .HasForeignKey("FileId")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChunkMetadata>(entity =>
        {
            entity.HasKey(c => c.Id);
        });
    }
}
