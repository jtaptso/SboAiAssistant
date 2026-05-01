using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Infrastructure.Persistence.Configurations;

internal sealed class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Mode)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasMany(s => s.Messages)
            .WithOne()
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
