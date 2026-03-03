namespace Mundialito.Domain.Matches;

/// <summary>
/// Estados posibles de un partido
/// Solo se serializa el nombre ("Scheduled" / "Played").
/// </summary>
public enum MatchStatus
{
    Scheduled = 0,
    Played = 1
}
