namespace DigitalAllianceTogo.Domain.Models.SAV
{
    public class Diagnostic
    {
        public Guid Id { get; set; }
        public DateTime DateDiagnostic { get; set; } = DateTime.UtcNow;
        public string Conclusion { get; set; } = string.Empty;
        public bool Reparable { get; set; }
        public string? Recommandation { get; set; }

        public Guid TicketSAVId { get; set; }
        public TicketSAV TicketSAV { get; set; } = null!;

        // Diagnostic (0..*) --> (1) Utilisateur : technicien
        public Guid TechnicienId { get; set; }
        public Models.Security.Utilisateur Technicien { get; set; } = null!;
    }
}
