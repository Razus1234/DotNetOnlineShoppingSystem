using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Infrastructure.Data.Configurations;

public class UserConfiguration : BaseEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("users");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254)
            .HasColumnName("email");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("password_hash");

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("full_name");

        // Configure Address value objects as owned entities
        builder.OwnsMany(u => u.Addresses, addressBuilder =>
        {
            addressBuilder.ToTable("user_addresses");
            
            addressBuilder.WithOwner().HasForeignKey("UserId");
            
            addressBuilder.Property<Guid>("Id")
                .ValueGeneratedOnAdd();
            
            addressBuilder.HasKey("Id");

            addressBuilder.Property(a => a.Street)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("street");

            addressBuilder.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("city");

            addressBuilder.Property(a => a.PostalCode)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("postal_code");

            addressBuilder.Property(a => a.Country)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("country");
        });

        // One-to-one relationship with Cart
        builder.HasOne(u => u.Cart)
            .WithOne()
            .HasForeignKey<Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on email
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_users_email");
    }
}