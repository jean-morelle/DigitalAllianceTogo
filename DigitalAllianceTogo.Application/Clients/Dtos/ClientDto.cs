namespace DigitalAllianceTogo.Application.Clients.Dtos
{
    public class ClientDto
    {
        public Guid Id { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string? Prenom { get; set; }
        public string? RaisonSociale { get; set; }
        public string Telephone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }

        /// <summary>Vrai si le client a un compte pour se connecter au site.</summary>
        public bool ACompte { get; set; }
    }

    /// <summary>Fiche client avec adresses et historique commercial (§4 : consulter l'historique client).</summary>
    public class ClientDetailDto : ClientDto
    {
        public List<AdresseDto> Adresses { get; set; } = new();
        public int NombreDevis { get; set; }
        public int NombreCommandes { get; set; }
        public List<HistoriqueItemDto> DerniersDevis { get; set; } = new();
        public List<HistoriqueItemDto> DernieresCommandes { get; set; } = new();
    }

    public class AdresseDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public string Ligne1 { get; set; } = string.Empty;
        public string? Ligne2 { get; set; }
        public string Ville { get; set; } = string.Empty;
        public string Pays { get; set; } = string.Empty;
        public string CodePostal { get; set; } = string.Empty;
    }

    public class HistoriqueItemDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime Date { get; set; }
    }
}
