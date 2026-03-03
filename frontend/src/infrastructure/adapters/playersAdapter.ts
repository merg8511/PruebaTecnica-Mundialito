import { httpClient } from "@/infrastructure/http/client";
import { buildQueryParams } from "@/infrastructure/http/queryParams";
import type { IPlayersPort, PlayersListParams } from "@/application/ports";
import type { Player } from "@/domain/entities";
import type { PageParams, PagedResponse } from "@/domain/types/pagination";

export const playersAdapter: IPlayersPort = {
  async listByTeam(teamId: string, params: PageParams): Promise<PagedResponse<Player>> {
    const query = buildQueryParams({
      pageNumber: params.pageNumber,
      pageSize: params.pageSize,
      sortBy: params.sortBy,
      sortDirection: params.sortDirection,
    });
    return httpClient.get<PagedResponse<Player>>(`teams/${teamId}/players`, query);
  },

  async list(params: PlayersListParams): Promise<PagedResponse<Player>> {
    const query = buildQueryParams({
      pageNumber: params.pageNumber,
      pageSize: params.pageSize,
      sortBy: params.sortBy,
      sortDirection: params.sortDirection,
      teamId: params.teamId,
      search: params.search,
    });
    return httpClient.get<PagedResponse<Player>>("players", query);
  },
};
