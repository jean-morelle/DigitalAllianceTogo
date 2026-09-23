using DigitalAllianceTogo.Domain.Models.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Notifications
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
            builder.Property(n => n.Titre).HasMaxLength(200).IsRequired();
            builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
            builder.Property(n => n.Lien).HasMaxLength(300);
            builder.Property(n => n.StatutEmail).HasConversion<string>().HasMaxLength(20);
            builder.Property(n => n.ErreurEmail).HasMaxLength(500);

            builder.HasOne(n => n.Client).WithMany().HasForeignKey(n => n.ClientId).OnDelete(DeleteBehavior.Cascade);

            // Cloche du client (non lues d'abord) et file d'envoi des e-mails
            builder.HasIndex(n => new { n.ClientId, n.DateCreation });
            builder.HasIndex(n => n.StatutEmail);
        }
    }
}
