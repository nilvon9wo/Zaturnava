using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Chat;

public sealed class InvalidChatException()
    : GameRequestException($"A message may say at most {ChatService.MaximumLength} characters.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Invalid;
}
