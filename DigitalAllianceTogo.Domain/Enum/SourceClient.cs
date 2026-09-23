namespace DigitalAllianceTogo.Domain.Enum
{
    /// <summary>
    /// Canal par lequel le client est arrivé (§37) : permet de mesurer
    /// la conversion des réseaux sociaux vers la plateforme.
    /// </summary>
    public enum SourceClient
    {
        SiteWeb = 1,
        WhatsApp = 2,
        Facebook = 3,
        TikTok = 4,
        Instagram = 5,
        Boutique = 6,
        Telephone = 7,
        Recommandation = 8,
        Autre = 9
    }
}
