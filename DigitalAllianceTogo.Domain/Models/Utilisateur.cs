namespace DigitalAllianceTogo.Domain.Models
{
    public class Utilisateur
    {
        Guid Id { get; set; }
        string Nom { get; set; } = string.Empty;
        string Prenom { get; set; } = string.Empty;
        string Email { get; set; } = string.Empty;
        string MotDePasseHash { get; set; } = string.Empty;
        bool EstActif { get; set; } = true;
        DateTime DateCreation { get; set; } = DateTime.UtcNow;
    }
}
