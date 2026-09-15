using DigitalAllianceTogo.Application.Common.Interfaces;
using BCrypt.Net;


namespace DigitalAllianceTogo.Infrastructure.Services
{
    /// <summary>
    /// Implémentation concrète de IPasswordHasher avec BCrypt.
    /// L'Application ne connaît que l'interface, jamais BCrypt directement 
    /// on pourrait changer d'algorithme ici sans toucher à la moindre Command/Query.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string motDePasse) => BCrypt.Net.BCrypt.HashPassword(motDePasse);

        public bool Verify(string motDePasse, string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(motDePasse, hash);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
