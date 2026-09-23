namespace DigitalAllianceTogo.Application.Common.Security
{
    /// <summary>
    /// Noms des rôles tels qu'ils sont stockés en base (table Roles) et émis dans le JWT.
    /// Constantes pour pouvoir les utiliser dans [Authorize(Roles = ...)].
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Commercial = "Commercial";
        public const string GestionnaireStock = "GestionnaireStock";
        public const string Technicien = "Technicien";
        public const string Livreur = "Livreur";
        public const string Client = "Client";
        public const string Catalogue = "Catalogue";
    }
}
