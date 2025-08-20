using OnlineShoppingSystem.Application.Common.Attributes;
using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.Tests.Unit.API.Security;

[TestClass]
public class InputValidationTests
{
    [TestClass]
    public class SecureEmailAttributeTests
    {
        private SecureEmailAttribute _attribute = null!;

        [TestInitialize]
        public void Setup()
        {
            _attribute = new SecureEmailAttribute();
        }

        [TestMethod]
        public void IsValid_ValidEmail_ReturnsTrue()
        {
            // Arrange
            var validEmails = new[]
            {
                "test@example.com",
                "user.name@domain.co.uk",
                "user+tag@example.org",
                "123@example.com"
            };

            // Act & Assert
            foreach (var email in validEmails)
            {
                Assert.IsTrue(_attribute.IsValid(email), $"Email {email} should be valid");
            }
        }

        [TestMethod]
        public void IsValid_InvalidEmail_ReturnsFalse()
        {
            // Arrange
            var invalidEmails = new[]
            {
                "invalid-email",
                "@example.com",
                "test@",
                "test..test@example.com",
                "test@example",
                "",
                null,
                "a".PadRight(255, 'a') + "@example.com" // Too long
            };

            // Act & Assert
            foreach (var email in invalidEmails)
            {
                Assert.IsFalse(_attribute.IsValid(email), $"Email {email} should be invalid");
            }
        }
    }

    [TestClass]
    public class StrongPasswordAttributeTests
    {
        private StrongPasswordAttribute _attribute = null!;

        [TestInitialize]
        public void Setup()
        {
            _attribute = new StrongPasswordAttribute();
        }

        [TestMethod]
        public void IsValid_StrongPassword_ReturnsTrue()
        {
            // Arrange
            var strongPasswords = new[]
            {
                "Password123!",
                "MyStr0ng@Pass",
                "C0mpl3x#P@ssw0rd",
                "Secure123$"
            };

            // Act & Assert
            foreach (var password in strongPasswords)
            {
                Assert.IsTrue(_attribute.IsValid(password), $"Password {password} should be valid");
            }
        }

        [TestMethod]
        public void IsValid_WeakPassword_ReturnsFalse()
        {
            // Arrange
            var weakPasswords = new[]
            {
                "password", // No uppercase, digit, special char
                "PASSWORD", // No lowercase, digit, special char
                "Password", // No digit, special char
                "Pass123", // Too short
                "password123", // No uppercase, special char
                "PASSWORD123", // No lowercase, special char
                "Password!", // No digit
                "",
                null
            };

            // Act & Assert
            foreach (var password in weakPasswords)
            {
                Assert.IsFalse(_attribute.IsValid(password), $"Password {password} should be invalid");
            }
        }

        [TestMethod]
        public void IsValid_CustomRequirements_WorksCorrectly()
        {
            // Arrange
            var customAttribute = new StrongPasswordAttribute
            {
                MinLength = 6,
                RequireUppercase = false,
                RequireSpecialChar = false
            };

            // Act & Assert
            Assert.IsTrue(customAttribute.IsValid("password123"));
            Assert.IsFalse(customAttribute.IsValid("pass")); // Too short
            Assert.IsFalse(customAttribute.IsValid("password")); // No digit
        }
    }

    [TestClass]
    public class NoInjectionAttributeTests
    {
        private NoInjectionAttribute _attribute = null!;

        [TestInitialize]
        public void Setup()
        {
            _attribute = new NoInjectionAttribute();
        }

        [TestMethod]
        public void IsValid_SafeInput_ReturnsTrue()
        {
            // Arrange
            var safeInputs = new[]
            {
                "Normal text",
                "Product name with numbers 123",
                "Email@example.com",
                "Some description with punctuation.",
                "",
                null
            };

            // Act & Assert
            foreach (var input in safeInputs)
            {
                Assert.IsTrue(_attribute.IsValid(input), $"Input '{input}' should be valid");
            }
        }

        [TestMethod]
        public void IsValid_DangerousInput_ReturnsFalse()
        {
            // Arrange
            var dangerousInputs = new[]
            {
                "<script>alert('xss')</script>",
                "javascript:alert('xss')",
                "'; DROP TABLE users; --",
                "' OR '1'='1",
                "UNION SELECT * FROM users",
                "onload=alert('xss')",
                "document.cookie",
                "eval(malicious_code)"
            };

            // Act & Assert
            foreach (var input in dangerousInputs)
            {
                Assert.IsFalse(_attribute.IsValid(input), $"Input '{input}' should be invalid");
            }
        }

        [TestMethod]
        public void IsValid_CaseInsensitive_ReturnsFalse()
        {
            // Arrange
            var dangerousInputs = new[]
            {
                "<SCRIPT>alert('xss')</SCRIPT>",
                "JAVASCRIPT:alert('xss')",
                "'; drop table users; --",
                "' or '1'='1",
                "union select * from users"
            };

            // Act & Assert
            foreach (var input in dangerousInputs)
            {
                Assert.IsFalse(_attribute.IsValid(input), $"Input '{input}' should be invalid (case insensitive)");
            }
        }
    }
}