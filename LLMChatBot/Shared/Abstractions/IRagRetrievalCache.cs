using Domain.DomainInterfaces;

namespace Shared.Abstractions;

public interface IRagRetrievalCache
{
    Task<IReadOnlyList<RagDocumentSearchResult>?> GetAsync(
        double[] embedding,
        int topK,
        double minScore,
        CancellationToken cancellationToken = default
    );

    Task SetAsync(
        double[] embedding,
        int topK,
        double minScore,
        IReadOnlyList<RagDocumentSearchResult> results,
        CancellationToken cancellationToken = default
    );
}
