using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Fichiers;

namespace DigitalAllianceTogo.Infrastructure.Services
{
    /// <summary>
    /// Stockage sur le disque du serveur, dans un dossier HORS de wwwroot : les fichiers ne sont
    /// jamais servis directement, seulement via l'API (authentification obligatoire).
    /// Remplaçable par un stockage objet (S3, Azure Blob...) sans toucher à l'Application.
    /// </summary>
    public class StockageFichiersLocal : IStockageFichiers
    {
        private readonly string _racine;

        public StockageFichiersLocal(string racine)
        {
            _racine = Path.GetFullPath(racine);
            Directory.CreateDirectory(_racine);
        }

        public async Task EnregistrerAsync(string categorie, string nom, Stream contenu, CancellationToken cancellationToken)
        {
            var chemin = Chemin(categorie, nom);
            Directory.CreateDirectory(Path.GetDirectoryName(chemin)!);

            // CreateNew : un nom déjà pris n'est jamais écrasé
            await using var fichier = new FileStream(chemin, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await contenu.CopyToAsync(fichier, cancellationToken);
        }

        public Stream? Ouvrir(string categorie, string nom)
        {
            var chemin = Chemin(categorie, nom);
            return File.Exists(chemin) ? new FileStream(chemin, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
        }

        /// <summary>Double sécurité contre la sortie du dossier : nom validé + chemin vérifié.</summary>
        private string Chemin(string categorie, string nom)
        {
            if (!ReglesFichiers.EstNomValide(categorie, nom))
                throw new ArgumentException("Nom de fichier invalide.", nameof(nom));

            var chemin = Path.GetFullPath(Path.Combine(_racine, categorie, nom));
            if (!chemin.StartsWith(_racine + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new ArgumentException("Chemin de fichier invalide.", nameof(nom));
            return chemin;
        }
    }
}
