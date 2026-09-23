namespace DigitalAllianceTogo.Domain.Models.Notifications
{
    /// <summary>
    /// Message au client sur l'avancement de ses devis, commandes, livraisons et SAV.
    /// Visible dans son espace (cloche) et envoyé par e-mail en différé : la notification
    /// est enregistrée dans la même transaction que l'événement, puis une tâche de fond l'envoie.
    /// </summary>
    public class Notification
    {
        public Guid Id { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        // Type technique (ex : PaiementConfirme) : filtre et statistiques
        public string Type { get; set; } = string.Empty;
        public string Titre { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // Chemin dans l'espace client (ex : /compte/commandes/{id})
        public string? Lien { get; set; }

        public DateTime? DateLecture { get; set; }

        // ----- Envoi par e-mail -----
        public StatutEnvoiNotification StatutEmail { get; set; } = StatutEnvoiNotification.EnAttente;
        public int TentativesEmail { get; set; }
        public DateTime? DateEnvoiEmail { get; set; }
        public string? ErreurEmail { get; set; }

        public Guid ClientId { get; set; }
        public Models.Security.Client Client { get; set; } = null!;
    }

    public enum StatutEnvoiNotification
    {
        EnAttente,
        Envoye,
        Echec,
        // Pas d'adresse e-mail, ou envoi d'e-mails non configuré à ce moment-là
        NonEnvoye
    }
}
