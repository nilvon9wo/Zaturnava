using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// <see cref="Proposer"/> proposes ending the game, and, if <see cref="Resigns"/>, gives up its turns
/// meanwhile. The game waits on each other seat's answer in turn, the one deciding; if one refuses,
/// <see cref="Resume"/> decides again, for <see cref="ResumeActingFor"/> if it was acting for one.
/// </summary>
public sealed record EndProposal(SeatId Proposer, bool Resigns, SeatId Resume, SeatId? ResumeActingFor);
