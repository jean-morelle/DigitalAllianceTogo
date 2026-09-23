using System.Text.RegularExpressions;

namespace DigitalAllianceTogo.Application.Fichiers
{
    /// <summary>
    /// Règles des fichiers envoyés : catégories autorisées, taille, types réellement acceptés
    /// (détectés sur le contenu, jamais sur l'extension ou le Content-Type déclarés),
    /// et forme des liens internes « /api/fichiers/{categorie}/{nom} ».
    /// </summary>
    public static partial class ReglesFichiers
    {
        public const long TailleMaximale = 5 * 1024 * 1024; // 5 Mo : une photo de téléphone compressée

        public const string PrefixeLien = "/api/fichiers/";

        /// <summary>Catégories = sous-dossiers de stockage.</summary>
        public static readonly IReadOnlyCollection<string> Categories = new[] { "paiements", "livraisons", CategorieProduits };

        /// <summary>Photos du catalogue : publiques, envoyées par Admin et Catalogue uniquement, jamais en PDF.</summary>
        public const string CategorieProduits = "produits";

        public sealed record TypeFichier(string Extension, string ContentType);

        private static readonly TypeFichier Jpeg = new("jpg", "image/jpeg");
        private static readonly TypeFichier Png = new("png", "image/png");
        private static readonly TypeFichier Webp = new("webp", "image/webp");
        public static readonly TypeFichier Pdf = new("pdf", "application/pdf");

        /// <summary>Type réel d'après les premiers octets (« nombres magiques ») ; null si non accepté.</summary>
        public static TypeFichier? DetecterType(ReadOnlySpan<byte> entete)
        {
            if (entete.Length >= 3 && entete[0] == 0xFF && entete[1] == 0xD8 && entete[2] == 0xFF)
                return Jpeg;
            if (entete.Length >= 8 && entete[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                return Png;
            if (entete.Length >= 12 && entete[..4].SequenceEqual("RIFF"u8) && entete[8..12].SequenceEqual("WEBP"u8))
                return Webp;
            if (entete.Length >= 5 && entete[..5].SequenceEqual("%PDF-"u8))
                return Pdf;
            return null;
        }

        public static TypeFichier? TypeDepuisNom(string nom) => Path.GetExtension(nom).ToLowerInvariant() switch
        {
            ".jpg" => Jpeg,
            ".png" => Png,
            ".webp" => Webp,
            ".pdf" => Pdf,
            _ => null
        };

        /// <summary>Nom généré par l'API : 32 hexadécimaux + extension connue (pas de « ../ » possible).</summary>
        [GeneratedRegex("^[0-9a-f]{32}\\.(jpg|png|webp|pdf)$")]
        private static partial Regex NomValide();

        public static bool EstNomValide(string categorie, string nom) =>
            Categories.Contains(categorie) && NomValide().IsMatch(nom);

        /// <summary>
        /// Lien accepté comme preuve : un fichier envoyé à l'API (/api/fichiers/...) ou une
        /// adresse web http(s) (photo déjà hébergée ailleurs).
        /// </summary>
        public static bool EstLienPreuveValide(string? lien)
        {
            if (string.IsNullOrWhiteSpace(lien))
                return true;
            if (lien.StartsWith(PrefixeLien, StringComparison.Ordinal))
            {
                var parties = lien[PrefixeLien.Length..].Split('/');
                return parties.Length == 2 && EstNomValide(parties[0], parties[1]);
            }
            return Uri.TryCreate(lien, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        }

        /// <summary>Image produit : une photo envoyée dans « produits » ou une adresse web http(s).</summary>
        public static bool EstLienImageProduitValide(string? lien)
        {
            if (string.IsNullOrWhiteSpace(lien))
                return false;
            if (lien.StartsWith(PrefixeLien, StringComparison.Ordinal))
            {
                var parties = lien[PrefixeLien.Length..].Split('/');
                return parties.Length == 2 && parties[0] == CategorieProduits && EstNomValide(parties[0], parties[1])
                    && TypeDepuisNom(parties[1]) != Pdf;
            }
            return EstLienPreuveValide(lien);
        }
    }
}
