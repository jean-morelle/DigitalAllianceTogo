namespace DigitalAllianceTogo.Domain.Models.SAV
{
    public class Intervention
    {
        public Guid Id { get; set; }
        public DateTime DateDebut { get; set; } = DateTime.UtcNow;
        public DateTime? DateFin { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Resultat { get; set; }

        public Guid TicketSAVId { get; set; }
        public TicketSAV TicketSAV { get; set; } = null!;

        // Intervention (0..*) --> (1) Utilisateur : technicien
        public Guid TechnicienId { get; set; }
        public Models.Security.Utilisateur Technicien { get; set; } = null!;
    }
}
