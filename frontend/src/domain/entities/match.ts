export type MatchStatus = "Scheduled" | "Finished";

export interface Match {
  id: string;
  homeTeamId: string;
  homeTeamName: string;
  awayTeamId: string;
  awayTeamName: string;
  scheduledAt: string;
  status: MatchStatus;
  homeGoals: number | null;
  awayGoals: number | null;
}
