using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Infrastructure.Persistence.Configurations;

internal sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.SessionId).IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.IsGroundedBySap).IsRequired();

        builder.HasIndex(m => m.SessionId);
    }
}
