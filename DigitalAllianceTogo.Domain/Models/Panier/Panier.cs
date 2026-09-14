namespace DigitalAllianceTogo.Domain.Models.Panier
{
    public class Panier
    {
        public Guid Id { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime DateModification { get; set; } = DateTime.UtcNow;
        public bool Actif { get; set; } = true;

        public Guid UtilisateurId { get; set; }
        public Models.Security.Utilisateur Utilisateur { get; set; } = null!;

        public ICollection<LignePanier> Lignes { get; set; } = new List<LignePanier>();
    }
}
