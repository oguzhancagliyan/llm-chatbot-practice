using FluentValidation;

namespace Features.Rag.IngestDocument;

public class IngestDocumentCommandValidator : AbstractValidator<IngestDocumentCommand>
{
    public IngestDocumentCommandValidator()
    {
        RuleFor(x => x.SourceId)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(200_000);
    }
}
