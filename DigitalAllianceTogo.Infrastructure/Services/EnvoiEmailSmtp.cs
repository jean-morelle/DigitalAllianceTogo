using System.Net;
using System.Net.Mail;
using DigitalAllianceTogo.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace DigitalAllianceTogo.Infrastructure.Services
{
    /// <summary>Section « Email » de la configuration (mot de passe : user-secrets ou variable d'environnement).</summary>
    public class EmailSettings
    {
        public const string SectionName = "Email";

        public string? Hote { get; set; }
        public int Port { get; set; } = 587;
        public bool Ssl { get; set; } = true;
        public string? Utilisateur { get; set; }
        public string? MotDePasse { get; set; }
        public string? Expediteur { get; set; }
        public string NomExpediteur { get; set; } = "Togo Informatique";

        // Adresse publique du site, pour les liens des e-mails
        public string UrlSite { get; set; } = "http://localhost:57547";
    }

    public class EnvoiEmailSmtp : IEnvoiEmail
    {
        private readonly EmailSettings _settings;

        public EnvoiEmailSmtp(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public bool EstConfigure => !string.IsNullOrWhiteSpace(_settings.Hote) && !string.IsNullOrWhiteSpace(_settings.Expediteur);

        public string LienSite(string chemin) => _settings.UrlSite.TrimEnd('/') + chemin;

        public async Task EnvoyerAsync(string destinataire, string sujet, string corpsHtml, CancellationToken cancellationToken)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.Expediteur!, _settings.NomExpediteur),
                Subject = sujet,
                Body = corpsHtml,
                IsBodyHtml = true
            };
            message.To.Add(destinataire);

            using var client = new SmtpClient(_settings.Hote, _settings.Port) { EnableSsl = _settings.Ssl };
            if (!string.IsNullOrWhiteSpace(_settings.Utilisateur))
                client.Credentials = new NetworkCredential(_settings.Utilisateur, _settings.MotDePasse);

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}
