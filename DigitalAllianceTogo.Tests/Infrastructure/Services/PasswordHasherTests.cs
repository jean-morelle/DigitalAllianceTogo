using DigitalAllianceTogo.Infrastructure.Services;

namespace DigitalAllianceTogo.Tests.Infrastructure.Services
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _passwordHasher;

        public PasswordHasherTests()
        {
            _passwordHasher = new PasswordHasher();
        }

        [Fact]
        public void Hash_ShouldReturnValidHash()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = _passwordHasher.Hash(password);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.NotEqual(password, hash);
        }

        [Fact]
        public void Hash_ShouldReturnDifferentHashForSamePassword()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash1 = _passwordHasher.Hash(password);
            var hash2 = _passwordHasher.Hash(password);

            // Assert
            Assert.NotNull(hash1);
            Assert.NotNull(hash2);
            Assert.NotEqual(hash1, hash2); // Différents car salts différents
        }

        [Fact]
        public void Verify_ShouldReturnTrueForCorrectPassword()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = _passwordHasher.Hash(password);

            // Act
            var result = _passwordHasher.Verify(password, hash);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Verify_ShouldReturnFalseForIncorrectPassword()
        {
            // Arrange
            var password = "TestPassword123!";
            var incorrectPassword = "WrongPassword456!";
            var hash = _passwordHasher.Hash(password);

            // Act
            var result = _passwordHasher.Verify(incorrectPassword, hash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Verify_ShouldReturnFalseForNullHash()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var result = _passwordHasher.Verify(password, null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Verify_ShouldReturnFalseForEmptyHash()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var result = _passwordHasher.Verify(password, string.Empty);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("password")]
        [InlineData("MyPassword123!@#")]
        [InlineData("12345")]
        [InlineData("AzErTy")]
        public void PasswordHasherShouldWorkWithVariousPasswords(string password)
        {
            // Arrange & Act
            var hash = _passwordHasher.Hash(password);
            var isValid = _passwordHasher.Verify(password, hash);

            // Assert
            Assert.True(isValid);
        }
    }
}
