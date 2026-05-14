using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SapAiAssistant.Domain.Entities;
using System.Text.Json;

namespace SapAiAssistant.Infrastructure.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.UploadedAt).IsRequired();
        builder.Property(d => d.ChunkCount).IsRequired();

        builder.HasMany<DocumentChunk>()
            .WithOne()
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DocumentId).IsRequired();

        builder.Property(c => c.DocumentName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.ChunkIndex).IsRequired();

        builder.Property(c => c.Content).IsRequired();

        // Store embedding as JSON text (portable, no sqlite-vec extension needed for v1)
        var comparer = new ValueComparer<float[]>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
            v => v.ToArray());

        var embeddingProp = builder.Property(c => c.Embedding)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<float>())
            .HasColumnType("TEXT")
            .IsRequired();
        embeddingProp.Metadata.SetValueComparer(comparer);

        builder.HasIndex(c => c.DocumentId);
    }
}
