using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : BaseEntityConfiguration<OrderItem>
{
    public override void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("order_items");

        builder.Property(oi => oi.OrderId)
            .IsRequired()
            .HasColumnName("order_id");

        builder.Property(oi => oi.ProductId)
            .IsRequired()
            .HasColumnName("product_id");

        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("product_name");

        builder.Property(oi => oi.Quantity)
            .IsRequired()
            .HasColumnName("quantity");

        // Configure Money value object for Price
        builder.OwnsOne(oi => oi.Price, priceBuilder =>
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

        // Relationship with Product (for reference, but allows product deletion)
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(oi => oi.OrderId)
            .HasDatabaseName("IX_order_items_order_id");

        builder.HasIndex(oi => oi.ProductId)
            .HasDatabaseName("IX_order_items_product_id");
    }
}