namespace DigitalAllianceTogo.Application.Common.Interfaces
{
    /// <summary>Envoi d'e-mails (SMTP en production). Non configuré : rien n'est envoyé.</summary>
    public interface IEnvoiEmail
    {
        bool EstConfigure { get; }

        /// <summary>Adresse complète d'une page du site (ex : /compte/commandes/…).</summary>
        string LienSite(string chemin);

        Task EnvoyerAsync(string destinataire, string sujet, string corpsHtml, CancellationToken cancellationToken);
    }
}
