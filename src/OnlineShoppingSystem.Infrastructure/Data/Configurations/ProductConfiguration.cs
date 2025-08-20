using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Data.Configurations;

public class ProductConfiguration : BaseEntityConfiguration<Product>
{
    public override void Configure(EntityTypeBuilder<Product> builder)
    {
        base.Configure(builder);

        builder.ToTable("products");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(2000)
            .HasColumnName("description");

        builder.Property(p => p.Stock)
            .IsRequired()
            .HasColumnName("stock");

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("category");

        // Configure Money value object for Price
        builder.OwnsOne(p => p.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasColumnName("price_amount");

            priceBuilder.Property(m => m.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasColumnName("price_currency")
                .HasDefaultValue("USD");
        });

        // Configure ImageUrls as a JSON column
        builder.Property(p => p.ImageUrls)
            .HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasColumnName("image_urls")
            .HasMaxLength(2000)
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        // Indexes for performance
        builder.HasIndex(p => p.Category)
            .HasDatabaseName("IX_products_category");

        builder.HasIndex(p => p.Name)
            .HasDatabaseName("IX_products_name");

        // Full-text search index for name and description (PostgreSQL specific)
        builder.HasIndex(p => new { p.Name, p.Description })
            .HasDatabaseName("IX_products_search");
    }
}