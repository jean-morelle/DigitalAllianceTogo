namespace DigitalAllianceTogo.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiresAtUtc) GenerateToken(Guid utilisateurId, string email, IEnumerable<string> roles);
    }
}
