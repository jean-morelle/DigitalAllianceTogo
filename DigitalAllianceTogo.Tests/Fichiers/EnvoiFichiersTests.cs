using System.Text;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Fichiers;
using DigitalAllianceTogo.Application.Fichiers.Commands;
using DigitalAllianceTogo.Infrastructure.Services;
using ValidationException = DigitalAllianceTogo.Application.Common.Exceptions.ValidationException;

namespace DigitalAllianceTogo.Tests.Fichiers
{
    /// <summary>Envoi de fichiers de preuve : type réel, taille, noms et chemins sûrs.</summary>
    public class EnvoiFichiersTests : IDisposable
    {
        private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
        private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };

        private readonly string _dossier = Path.Combine(Path.GetTempPath(), "dat-tests-" + Guid.NewGuid().ToString("N"));
        private readonly StockageFichiersLocal _stockage;

        public EnvoiFichiersTests()
        {
            _stockage = new StockageFichiersLocal(_dossier);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dossier)) Directory.Delete(_dossier, recursive: true);
        }

        [Fact]
        public async Task Une_photo_est_renommee_stockee_et_relisible()
        {
            var resultat = await EnvoyerAsync("livraisons", Jpeg);

            Assert.Matches("^/api/fichiers/livraisons/[0-9a-f]{32}\\.jpg$", resultat.Url);
            Assert.Equal("image/jpeg", resultat.ContentType);

            var nom = resultat.Url.Split('/').Last();
            await using var flux = _stockage.Ouvrir("livraisons", nom);
            Assert.NotNull(flux);
            using var lu = new MemoryStream();
            await flux!.CopyToAsync(lu);
            Assert.Equal(Jpeg, lu.ToArray());
        }

        [Fact]
        public async Task Le_type_est_detecte_sur_le_contenu_pas_sur_le_nom()
        {
            Assert.EndsWith(".png", (await EnvoyerAsync("paiements", Png)).Url);

            // Page HTML ou exécutable, quel que soit le nom annoncé par le navigateur : refusés
            await Assert.ThrowsAsync<ValidationException>(() => EnvoyerAsync("paiements", Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>")));
            await Assert.ThrowsAsync<ValidationException>(() => EnvoyerAsync("paiements", new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 }));
        }

        [Fact]
        public void Taille_et_categorie_sont_controlees()
        {
            var validateur = new EnvoyerFichierCommandValidator();

            Assert.False(validateur.Validate(new EnvoyerFichierCommand { Categorie = "paiements", Taille = 0 }).IsValid);
            Assert.False(validateur.Validate(new EnvoyerFichierCommand { Categorie = "paiements", Taille = ReglesFichiers.TailleMaximale + 1 }).IsValid);
            Assert.False(validateur.Validate(new EnvoyerFichierCommand { Categorie = "../secrets", Taille = 10 }).IsValid);
            Assert.True(validateur.Validate(new EnvoyerFichierCommand { Categorie = "livraisons", Taille = 10 }).IsValid);
        }

        [Theory]
        [InlineData("paiements", "../../appsettings.json")]
        [InlineData("paiements", "..\\..\\appsettings.json")]
        [InlineData("../../", "0123456789abcdef0123456789abcdef.jpg")]
        [InlineData("paiements", "0123456789abcdef0123456789abcdef.exe")]
        public void Impossible_de_sortir_du_dossier_de_stockage(string categorie, string nom)
        {
            Assert.Throws<ArgumentException>(() => _stockage.Ouvrir(categorie, nom));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("/api/fichiers/livraisons/0123456789abcdef0123456789abcdef.jpg", true)]
        [InlineData("https://exemple.tg/recu.png", true)]
        [InlineData("/api/fichiers/livraisons/../../appsettings.json", false)]
        [InlineData("/api/fichiers/inconnu/0123456789abcdef0123456789abcdef.jpg", false)]
        [InlineData("javascript:alert(1)", false)]
        [InlineData("file:///C:/Windows/win.ini", false)]
        public void Liens_acceptes_comme_preuve(string? lien, bool attendu)
        {
            Assert.Equal(attendu, ReglesFichiers.EstLienPreuveValide(lien));
        }

        [Fact]
        public async Task Photo_produit_reservee_au_catalogue_et_jamais_en_pdf()
        {
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => EnvoyerAsync("produits", Jpeg, Roles.Client));

            var resultat = await EnvoyerAsync("produits", Png, Roles.Catalogue);
            Assert.Matches("^/api/fichiers/produits/[0-9a-f]{32}\\.png$", resultat.Url);
            Assert.True(ReglesFichiers.EstLienImageProduitValide(resultat.Url));

            var pdf = Encoding.ASCII.GetBytes("%PDF-1.7 contenu");
            await Assert.ThrowsAsync<ValidationException>(() => EnvoyerAsync("produits", pdf, Roles.Admin));
        }

        [Theory]
        [InlineData("/api/fichiers/produits/0123456789abcdef0123456789abcdef.webp", true)]
        [InlineData("https://exemple.tg/photo.jpg", true)]
        [InlineData("/api/fichiers/paiements/0123456789abcdef0123456789abcdef.jpg", false)]
        [InlineData("/api/fichiers/produits/0123456789abcdef0123456789abcdef.pdf", false)]
        [InlineData("", false)]
        public void Liens_acceptes_comme_image_produit(string lien, bool attendu)
        {
            Assert.Equal(attendu, ReglesFichiers.EstLienImageProduitValide(lien));
        }

        private Task<FichierEnvoyeDto> EnvoyerAsync(string categorie, byte[] contenu, string role = Roles.Livreur) =>
            new EnvoyerFichierCommandHandler(_stockage, new UtilisateurFictif(role)).Handle(new EnvoyerFichierCommand
            {
                Categorie = categorie, Taille = contenu.Length, Contenu = new MemoryStream(contenu)
            }, default);

        private sealed class UtilisateurFictif(string role) : ICurrentUserService
        {
            public Guid? UtilisateurId { get; } = Guid.NewGuid();
            public bool EstAuthentifie => true;
            public string? AdresseIP => "127.0.0.1";
            public bool EstDansRole(string r) => r == role;
        }
    }
}
