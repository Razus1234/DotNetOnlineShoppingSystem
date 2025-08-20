using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Infrastructure.Data.Configurations;

public class OrderConfiguration : BaseEntityConfiguration<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);

        builder.ToTable("orders");

        builder.Property(o => o.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("status");

        builder.Property(o => o.ShippedAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("shipped_at");

        builder.Property(o => o.DeliveredAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("delivered_at");

        builder.Property(o => o.CancelledAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("cancelled_at");

        // Configure Money value object for Total
        builder.OwnsOne(o => o.Total, totalBuilder =>
        {
            totalBuilder.Property(m => m.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasColumnName("total_amount");

            totalBuilder.Property(m => m.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasColumnName("total_currency")
                .HasDefaultValue("USD");
        });

        // Configure Address value object for ShippingAddress
        builder.OwnsOne(o => o.ShippingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("shipping_street");

            addressBuilder.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("shipping_city");

            addressBuilder.Property(a => a.PostalCode)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("shipping_postal_code");

            addressBuilder.Property(a => a.Country)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("shipping_country");
        });

        // One-to-many relationship with OrderItems
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-one relationship with Payment
        builder.HasOne(o => o.Payment)
            .WithOne()
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with User
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(o => o.UserId)
            .HasDatabaseName("IX_orders_user_id");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("IX_orders_status");
    }
}