namespace DigitalAllianceTogo.Application.Common
{
    /// <summary>
    /// Références lisibles et uniques (index unique en base) :
    /// DEV-20260923-3F9A1C, CMD-..., CLI-..., PAY-...
    /// </summary>
    public static class References
    {
        public static string Generer(string prefixe) =>
            $"{prefixe}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }
}
