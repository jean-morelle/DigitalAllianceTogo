namespace DigitalAllianceTogo.Application.Common.Interfaces
{
    /// <summary>
    /// Abstraction du hachage de mot de passe. Implémentation concrète (BCrypt)
    /// dans GkasGroup.Infrastructure — l'Application ne sait pas quel algorithme
    /// est utilisé, seulement qu'elle peut hasher et vérifier.
    /// </summary>
    public interface IPasswordHasher
    {
        string Hash(string motDePasse);
        bool Verify(string motDePasse, string hash);
    }
}
