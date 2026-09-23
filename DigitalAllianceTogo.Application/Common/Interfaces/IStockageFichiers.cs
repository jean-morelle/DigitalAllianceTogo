namespace DigitalAllianceTogo.Application.Common.Interfaces
{
    /// <summary>
    /// Stockage des fichiers envoyés (preuves de paiement, photos et signatures de livraison).
    /// L'implémentation (disque local aujourd'hui, stockage objet demain) vit dans l'Infrastructure.
    /// </summary>
    public interface IStockageFichiers
    {
        /// <summary>Enregistre le contenu sous categorie/nom (nom déjà sûr et unique).</summary>
        Task EnregistrerAsync(string categorie, string nom, Stream contenu, CancellationToken cancellationToken);

        /// <summary>Flux en lecture, ou null si le fichier n'existe pas.</summary>
        Stream? Ouvrir(string categorie, string nom);
    }
}
