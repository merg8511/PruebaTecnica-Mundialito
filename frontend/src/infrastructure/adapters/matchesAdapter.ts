import { httpClient } from "@/infrastructure/http/client";
import { buildQueryParams } from "@/infrastructure/http/queryParams";
import type { IMatchesPort, MatchesListParams, RecordResultPayload } from "@/application/ports";
import type { Match } from "@/domain/entities";
import type { PagedResponse } from "@/domain/types/pagination";

export const matchesAdapter: IMatchesPort = {
  async list(params: MatchesListParams): Promise<PagedResponse<Match>> {
    const query = buildQueryParams({
      pageNumber: params.pageNumber,
      pageSize: params.pageSize,
      sortBy: params.sortBy,
      sortDirection: params.sortDirection,
      dateFrom: params.dateFrom,
      dateTo: params.dateTo,
      teamId: params.teamId,
      status: params.status,
    });
    return httpClient.get<PagedResponse<Match>>("matches", query);
  },

  async getById(id: string): Promise<Match> {
    return httpClient.get<Match>(`matches/${id}`);
  },

  async recordResult(matchId: string, payload: RecordResultPayload): Promise<Match> {
    return httpClient.post<Match>(`matches/${matchId}/results`, payload);
  },
};
