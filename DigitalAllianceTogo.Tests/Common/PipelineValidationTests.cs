using DigitalAllianceTogo.Application.Common.Behaviours;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Livraisons.Commands.ConfirmerLivraison;
using DigitalAllianceTogo.Application.Paiements.Commands.RejeterPaiement;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ValidationException = DigitalAllianceTogo.Application.Common.Exceptions.ValidationException;

namespace DigitalAllianceTogo.Tests.Common
{
    /// <summary>
    /// Les validateurs doivent aussi s'exécuter pour les commandes SANS valeur de retour
    /// (IRequest). Une contrainte « where TRequest : IRequest&lt;TResponse&gt; » sur les
    /// behaviours les excluait silencieusement : aucune validation, aucun log.
    /// </summary>
    public class PipelineValidationTests
    {
        private readonly ISender _sender;

        public PipelineValidationTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssemblyContaining<RejeterPaiementCommand>();
                config.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
                config.AddOpenBehavior(typeof(LoggingBehaviour<,>));
                config.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            });
            services.AddValidatorsFromAssemblyContaining<RejeterPaiementCommand>();
            services.AddScoped(_ => Mock.Of<IApplicationDbContext>());
            services.AddScoped(_ => Mock.Of<ICurrentUserService>());
            services.AddScoped(_ => Mock.Of<IAuditService>());

            _sender = services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<ISender>();
        }

        [Fact]
        public async Task Rejet_de_paiement_sans_motif_est_refuse_avant_le_handler()
        {
            await Assert.ThrowsAsync<ValidationException>(() =>
                _sender.Send(new RejeterPaiementCommand { Id = Guid.NewGuid(), Motif = "" }));
        }

        [Fact]
        public async Task Livraison_sans_preuve_est_refusee_avant_le_handler()
        {
            await Assert.ThrowsAsync<ValidationException>(() =>
                _sender.Send(new ConfirmerLivraisonCommand { Id = Guid.NewGuid() }));
        }
    }
}
