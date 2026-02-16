using FluentValidation;

namespace Features.Chat.GetStreamMessages;

public class GetStreamMessagesQueryValidator : AbstractValidator<GetStreamMessagesQuery>
{
    public GetStreamMessagesQueryValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");
    }
}
