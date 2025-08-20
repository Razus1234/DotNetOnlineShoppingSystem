using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Data.Configurations;

public class CartConfiguration : BaseEntityConfiguration<Cart>
{
    public override void Configure(EntityTypeBuilder<Cart> builder)
    {
        base.Configure(builder);

        builder.ToTable("carts");

        builder.Property(c => c.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        // One-to-many relationship with CartItems
        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on UserId for performance
        builder.HasIndex(c => c.UserId)
            .IsUnique()
            .HasDatabaseName("IX_carts_user_id");
    }
}