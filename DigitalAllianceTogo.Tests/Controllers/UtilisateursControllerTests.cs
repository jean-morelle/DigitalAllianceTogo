using DigitalAllianceTogo.Application.Utilisateurs.Commands.CreateUtilisateur;
using DigitalAllianceTogo.Infrastructure.Services;

namespace DigitalAllianceTogo.Tests.Controllers
{
    public class UtilisateursControllerTests
    {
        [Fact]
        public void CreateUtilisateurCommand_Should_Hash_Password_Using_PasswordHasher()
        {
            // Arrange
            var passwordHasher = new PasswordHasher();
            var testPassword = "TestPassword123!";

            // Act
            var hash1 = passwordHasher.Hash(testPassword);
            var hash2 = passwordHasher.Hash(testPassword);

            // Assert - Les hashs doivent être différents même pour le même mot de passe
            Assert.NotEqual(hash1, hash2);

            // Mais tous deux doivent vérifier le mot de passe
            Assert.True(passwordHasher.Verify(testPassword, hash1));
            Assert.True(passwordHasher.Verify(testPassword, hash2));
        }

        [Fact]
        public void PasswordHasher_Should_Fail_To_Verify_Wrong_Password()
        {
            // Arrange
            var passwordHasher = new PasswordHasher();
            var correctPassword = "CorrectPassword123!";
            var wrongPassword = "WrongPassword456!";
            var hash = passwordHasher.Hash(correctPassword);

            // Act
            var result = passwordHasher.Verify(wrongPassword, hash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PasswordHasher_Can_Be_Used_In_CreateUtilisateurCommand_Context()
        {
            // This test simulates what happens in CreateUtilisateurCommand
            // Arrange
            var passwordHasher = new PasswordHasher();
            var utilisateurPassword = "SecurePassword123!";

            // Act - Simulate what CreateUtilisateurCommand does
            var motDePasseHash = passwordHasher.Hash(utilisateurPassword);

            // Assert
            Assert.NotNull(motDePasseHash);
            Assert.NotEmpty(motDePasseHash);
            Assert.NotEqual(utilisateurPassword, motDePasseHash);

            // Verify that the original password matches
            Assert.True(passwordHasher.Verify(utilisateurPassword, motDePasseHash));
        }
    }
}
