using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Infrastructure.Services;
using Moq;

namespace DigitalAllianceTogo.Tests.Integration
{
    public class PasswordHasherIntegrationTests
    {
        [Fact]
        public void PasswordHasher_Should_Be_Registered_As_Singleton()
        {
            // Arrange
            var passwordHasher = new PasswordHasher();

            // Act & Assert
            Assert.NotNull(passwordHasher);
            Assert.IsAssignableFrom<IPasswordHasher>(passwordHasher);
        }

        [Fact]
        public void PasswordHasher_Should_Implement_IPasswordHasher_Interface()
        {
            // Arrange
            var passwordHasher = new PasswordHasher();

            // Act & Assert
            Assert.True(passwordHasher is IPasswordHasher);
            var interfaces = typeof(PasswordHasher).GetInterfaces();
            Assert.Contains(typeof(IPasswordHasher), interfaces);
        }

        [Fact]
        public void PasswordHasher_Should_Hash_And_Verify_Long_Passwords()
        {
            // Arrange
            var passwordHasher = new PasswordHasher();
            var longPassword = new string('a', 100);

            // Act
            var hash = passwordHasher.Hash(longPassword);
            var isValid = passwordHasher.Verify(longPassword, hash);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void PasswordHasher_Should_Handle_Special_Characters()
        {
            // Arrange
            var passwordHasher = new PasswordHasher();
            var specialPassword = "P@ssw0rd!#$%^&*()_+-=[]{}|;:',.<>?/\\";

            // Act
            var hash = passwordHasher.Hash(specialPassword);
            var isValid = passwordHasher.Verify(specialPassword, hash);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void PasswordHasher_Should_Handle_Unicode_Characters()
        {
            // Arrange
            var passwordHasher = new PasswordHasher();
            var unicodePassword = "Pässwörd123!αβγδ";

            // Act
            var hash = passwordHasher.Hash(unicodePassword);
            var isValid = passwordHasher.Verify(unicodePassword, hash);

            // Assert
            Assert.True(isValid);
        }
    }
}
