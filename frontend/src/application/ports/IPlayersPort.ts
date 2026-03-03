import type { Player } from "@/domain/entities";
import type { PagedResponse, PageParams } from "@/domain/types/pagination";

export interface PlayersListParams extends PageParams {
  teamId?: string;
  search?: string;
}

export interface IPlayersPort {
  listByTeam(teamId: string, params: PageParams): Promise<PagedResponse<Player>>;
  list(params: PlayersListParams): Promise<PagedResponse<Player>>;
}
