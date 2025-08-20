using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Infrastructure.Data.Configurations;

public class PaymentConfiguration : BaseEntityConfiguration<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);

        builder.ToTable("payments");

        builder.Property(p => p.OrderId)
            .IsRequired()
            .HasColumnName("order_id");

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("status");

        builder.Property(p => p.TransactionId)
            .HasMaxLength(100)
            .HasColumnName("transaction_id");

        builder.Property(p => p.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("payment_method");

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500)
            .HasColumnName("failure_reason");

        builder.Property(p => p.ProcessedAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("processed_at");

        builder.Property(p => p.RefundedAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("refunded_at");

        // Configure Money value object for Amount
        builder.OwnsOne(p => p.Amount, amountBuilder =>
        {
            amountBuilder.Property(m => m.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasColumnName("amount");

            amountBuilder.Property(m => m.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasColumnName("currency")
                .HasDefaultValue("USD");
        });

        // Configure Money value object for RefundAmount
        builder.OwnsOne(p => p.RefundAmount, refundBuilder =>
        {
            refundBuilder.Property(m => m.Amount)
                .HasColumnType("decimal(18,2)")
                .HasColumnName("refund_amount");

            refundBuilder.Property(m => m.Currency)
                .HasMaxLength(3)
                .HasColumnName("refund_currency");
        });

        // Indexes for performance
        builder.HasIndex(p => p.OrderId)
            .IsUnique()
            .HasDatabaseName("IX_payments_order_id");

        builder.HasIndex(p => p.TransactionId)
            .HasDatabaseName("IX_payments_transaction_id");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_payments_status");
    }
}