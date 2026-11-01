namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A game waiting for people to take its open seats, and who may fill them with bots instead (<c>fill</c>), and when:
/// the first of the people seated to arrive (<see cref="FirstArrival"/>; none, if no one is seated) in
/// <see cref="SecondsUntilFirst"/>, and anyone seated in <see cref="SecondsUntilAnyone"/>; 0, once they may.
/// </summary>
public sealed record VacancyDto(int? FirstArrival, double SecondsUntilFirst, double SecondsUntilAnyone);
