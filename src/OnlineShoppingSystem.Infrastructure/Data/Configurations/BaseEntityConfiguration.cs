using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShoppingSystem.Domain.Common;

namespace OnlineShoppingSystem.Infrastructure.Data.Configurations;

public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        // Add index on CreatedAt for performance
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName($"IX_{typeof(T).Name}_CreatedAt");
    }
}