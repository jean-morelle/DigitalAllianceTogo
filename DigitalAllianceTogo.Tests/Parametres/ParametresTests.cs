using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Parametres;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Parametres
{
    /// <summary>Paramètres de l'entreprise : numéros Mobile Money affichés au client.</summary>
    public class ParametresTests
    {
        private readonly ApplicationDbContext _context;
        private readonly ParametresHandler _handler;

        public ParametresTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _handler = new ParametresHandler(_context, new AuditService(_context, new FakeCurrentUser { UtilisateurId = Guid.NewGuid() }));
        }

        private static ModifierParametresCommand Commande(string? tmoney, string? flooz) => new()
        {
            SeuilRemiseCommercialPourcent = 10m,
            SeuilAugmentationModificationPourcent = 10m,
            DelaiExpirationPaiementHeures = 48,
            DureeValiditeDevisJours = 15,
            NumeroTMoney = tmoney,
            NumeroFlooz = flooz,
            NomBeneficiairePaiement = "  TOGO INFORMATIQUE  ",
        };

        [Fact]
        public async Task Les_numeros_sont_vides_par_defaut()
        {
            var infos = await _handler.Handle(new GetInfosPaiementQuery(), default);

            Assert.Null(infos.NumeroTMoney);
            Assert.Null(infos.NumeroFlooz);
            Assert.Equal(48, infos.DelaiPaiementHeures);
        }

        [Fact]
        public async Task Modifier_enregistre_les_numeros_et_trace_l_audit()
        {
            await _handler.Handle(Commande("+228 90 00 00 00", " "), default);

            var infos = await _handler.Handle(new GetInfosPaiementQuery(), default);
            Assert.Equal("+228 90 00 00 00", infos.NumeroTMoney);
            Assert.Null(infos.NumeroFlooz);
            Assert.Equal("TOGO INFORMATIQUE", infos.NomBeneficiaire);
            Assert.Single(await _context.JournauxAudit.Where(j => j.Action == "ModificationParametres").ToListAsync());
        }

        [Theory]
        [InlineData("90 00 00 00", true)]
        [InlineData("+22890000000", true)]
        [InlineData("", true)]
        [InlineData("abc", false)]
        [InlineData("1234", false)]
        public void Format_du_numero(string numero, bool valide)
        {
            var resultat = new ModifierParametresCommandValidator().Validate(Commande(numero, null));

            Assert.Equal(valide, resultat.IsValid);
        }

        private sealed class FakeCurrentUser : ICurrentUserService
        {
            public Guid? UtilisateurId { get; set; }
            public string[] Roles { get; set; } = Array.Empty<string>();
            public bool EstAuthentifie => UtilisateurId.HasValue;
            public string? AdresseIP => "127.0.0.1";
            public bool EstDansRole(string role) => Roles.Contains(role);
        }
    }
}
