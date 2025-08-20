using FluentAssertions;
using OnlineShoppingSystem.Domain.Common;

namespace OnlineShoppingSystem.Tests.Unit.Domain.Common;

// Test entity for testing BaseEntity functionality
public class TestEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public TestEntity() : base() { }

    public TestEntity(Guid id) : base(id) { }

    public TestEntity(string name) : base()
    {
        Name = name;
    }
}

[TestClass]
public class BaseEntityTests
{
    [TestMethod]
    public void Constructor_Default_ShouldGenerateNewId()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBe(Guid.Empty);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.CreatedAt.Should().BeCloseTo(entity.UpdatedAt, TimeSpan.FromMilliseconds(1));
    }

    [TestMethod]
    public void Constructor_WithId_ShouldUseProvidedId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();

        // Act
        var entity = new TestEntity(expectedId);

        // Assert
        entity.Id.Should().Be(expectedId);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Constructor_MultipleEntities_ShouldHaveUniqueIds()
    {
        // Act
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Assert
        entity1.Id.Should().NotBe(entity2.Id);
    }

    [TestMethod]
    public void UpdateTimestamp_ShouldUpdateOnlyUpdatedAt()
    {
        // Arrange
        var entity = new TestEntity();
        var originalCreatedAt = entity.CreatedAt;
        var originalUpdatedAt = entity.UpdatedAt;

        // Wait a small amount to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        entity.UpdateTimestamp();

        // Assert
        entity.CreatedAt.Should().Be(originalCreatedAt);
        entity.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void CreatedAt_ShouldBeUtc()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [TestMethod]
    public void UpdatedAt_ShouldBeUtc()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [TestMethod]
    public void UpdateTimestamp_UpdatedAt_ShouldBeUtc()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.UpdateTimestamp();

        // Assert
        entity.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }
}