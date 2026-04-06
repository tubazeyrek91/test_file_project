using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DistributedStorage.Persistence.Context;

public class MetadataDbContextFactory : IDesignTimeDbContextFactory<MetadataDbContext>
{
    public MetadataDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MetadataDbContext>();
        optionsBuilder.UseSqlite("Data Source=metadata.db");
        return new MetadataDbContext(optionsBuilder.Options);
    }
}
