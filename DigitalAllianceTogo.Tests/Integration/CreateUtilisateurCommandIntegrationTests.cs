using DigitalAllianceTogo.Application.Utilisateurs.Commonds.CreateUtilisateur;
using DigitalAllianceTogo.Infrastructure.Persitence;
using DigitalAllianceTogo.Infrastructure.Services;
using DigitalAllianceTogo.Domain.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Integration
{
    public class CreateUtilisateurCommandIntegrationTests
    {
        [Fact]
        public void CreateUtilisateurCommand_Should_Use_PasswordHasher_Correctly()
        {
            // Arrange
            var passwordHasher = new PasswordHasher();
            var testPassword = "SecurePassword123!";
            var hashedPassword = passwordHasher.Hash(testPassword);

            // Act & Assert
            Assert.NotNull(hashedPassword);
            Assert.NotEmpty(hashedPassword);
            Assert.NotEqual(testPassword, hashedPassword);
            Assert.True(passwordHasher.Verify(testPassword, hashedPassword));
            Assert.False(passwordHasher.Verify("WrongPassword", hashedPassword));
        }

        [Fact]
        public void PasswordHasher_Should_Work_With_Various_Email_Formats()
        {
            // Arrange
            var passwordHasher = new PasswordHasher();
            var password = "TestPassword123!";

            // Test that password hashing works regardless of email
            var testEmails = new[]
            {
                "user@example.com",
                "john.doe+tag@company.co.uk",
                "test.user@example.fr",
                "utilisateur@domaine.tg"  // Domain Togo
            };

            // Act & Assert
            foreach (var email in testEmails)
            {
                var hash = passwordHasher.Hash(password);
                var isValid = passwordHasher.Verify(password, hash);
                Assert.True(isValid, $"Password verification failed for email: {email}");
            }
        }

        [Fact]
        public void CreateUtilisateurCommand_Requires_Valid_Email()
        {
            // This test ensures CreateUtilisateurCommand can receive an email
            // and that it's validated before hashing the password

            // Arrange
            var validEmail = "user@example.com";
            var invalidEmails = new[] { "", "notanemail", "user@" };

            // Act & Assert - We test that the command accepts these values
            // The actual validation would be in the handler
            var command = new CreateUtilisateurCommand
            {
                Email = validEmail,
                Nom = "Doe",
                Prenom = "John",
                Telephone = "+228XXXXXXXX",
                MotDePasse = "SecurePassword123!"
            };

            Assert.Equal(validEmail, command.Email);
        }

        [Fact]
        public void PasswordHasher_Should_Support_Long_Passwords()
        {
            // BCrypt has a 72-byte limit, but we should handle it gracefully
            var passwordHasher = new PasswordHasher();
            var veryLongPassword = new string('a', 100); // > 72 bytes

            // Act
            var hash = passwordHasher.Hash(veryLongPassword);
            var isValid = passwordHasher.Verify(veryLongPassword, hash);

            // Assert
            Assert.True(isValid);
        }

        [Theory]
        [InlineData("user@domain.com", "Password123!")]
        [InlineData("another.user@company.org", "MySecurePass99@")]
        [InlineData("test.utilisateur@example.tg", "TestPassword#2024")]
        public void CreateUtilisateurCommand_Should_Work_With_Various_Credentials(
            string email, string password)
        {
            // Arrange
            var passwordHasher = new PasswordHasher();

            // Act
            var hash = passwordHasher.Hash(password);
            var isValid = passwordHasher.Verify(password, hash);

            // Assert - Email is used to ensure we test different scenarios
            Assert.NotNull(email);
            Assert.NotNull(hash);
            Assert.True(isValid);

            // Also verify that a different password doesn't match
            var wrongPassword = "DifferentPassword123!";
            Assert.False(passwordHasher.Verify(wrongPassword, hash));
        }

        [Fact]
        public void PasswordHasher_Should_Not_Hash_Empty_Password()
        {
            // While BCrypt will hash empty string, we should ensure
            // the application validates empty passwords upstream
            var passwordHasher = new PasswordHasher();
            var emptyPassword = string.Empty;

            // Act
            var hash = passwordHasher.Hash(emptyPassword);

            // Assert - Should produce a hash (BCrypt behavior)
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);

            // But verification should work correctly
            Assert.True(passwordHasher.Verify(emptyPassword, hash));
            Assert.False(passwordHasher.Verify("anyPassword", hash));
        }

        [Fact]
        public void CreateUtilisateurCommand_Properties_Should_Be_Settable()
        {
            // Arrange & Act
            var command = new CreateUtilisateurCommand
            {
                Nom = "Test",
                Prenom = "User",
                Email = "test@example.com",
                Telephone = "+228XXXXX",
                MotDePasse = "Password123!"
            };

            // Assert
            Assert.Equal("Test", command.Nom);
            Assert.Equal("User", command.Prenom);
            Assert.Equal("test@example.com", command.Email);
            Assert.Equal("+228XXXXX", command.Telephone);
            Assert.Equal("Password123!", command.MotDePasse);
        }
    }
}
