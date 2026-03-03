import { httpClient } from "@/infrastructure/http/client";
import { buildQueryParams } from "@/infrastructure/http/queryParams";
import type { ITeamsPort, TeamsListParams } from "@/application/ports";
import type { Team } from "@/domain/entities";
import type { PagedResponse } from "@/domain/types/pagination";

export const teamsAdapter: ITeamsPort = {
  async list(params: TeamsListParams): Promise<PagedResponse<Team>> {
    const query = buildQueryParams({
      pageNumber: params.pageNumber,
      pageSize: params.pageSize,
      sortBy: params.sortBy,
      sortDirection: params.sortDirection,
      search: params.search,
    });
    return httpClient.get<PagedResponse<Team>>("teams", query);
  },

  async getById(id: string): Promise<Team> {
    return httpClient.get<Team>(`teams/${id}`);
  },
};
