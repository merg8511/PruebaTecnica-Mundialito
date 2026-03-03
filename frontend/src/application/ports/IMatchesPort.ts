import type { Match } from "@/domain/entities";
import type { PagedResponse, PageParams } from "@/domain/types/pagination";

export interface MatchesListParams extends PageParams {
  dateFrom?: string;
  dateTo?: string;
  teamId?: string;
  status?: string;
}

export interface RecordResultPayload {
  homeGoals: number;
  awayGoals: number;
  goalsByPlayer: Array<{ playerId: string; goals: number }>;
}

export interface IMatchesPort {
  list(params: MatchesListParams): Promise<PagedResponse<Match>>;
  getById(id: string): Promise<Match>;
  recordResult(matchId: string, payload: RecordResultPayload): Promise<Match>;
}
