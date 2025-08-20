using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Data.Configurations;

public class CartItemConfiguration : BaseEntityConfiguration<CartItem>
{
    public override void Configure(EntityTypeBuilder<CartItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("cart_items");

        builder.Property(ci => ci.CartId)
            .IsRequired()
            .HasColumnName("cart_id");

        builder.Property(ci => ci.ProductId)
            .IsRequired()
            .HasColumnName("product_id");

        builder.Property(ci => ci.ProductName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("product_name");

        builder.Property(ci => ci.Quantity)
            .IsRequired()
            .HasColumnName("quantity");

        // Configure Money value object for Price
        builder.OwnsOne(ci => ci.Price, priceBuilder =>
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

        // Relationship with Product
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(ci => ci.CartId)
            .HasDatabaseName("IX_cart_items_cart_id");

        builder.HasIndex(ci => ci.ProductId)
            .HasDatabaseName("IX_cart_items_product_id");

        // Unique constraint to prevent duplicate products in same cart
        builder.HasIndex(ci => new { ci.CartId, ci.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_cart_items_cart_product");
    }
}