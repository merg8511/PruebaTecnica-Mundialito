import { httpClient } from "@/infrastructure/http/client";
import { buildQueryParams } from "@/infrastructure/http/queryParams";
import type { IScorersPort, ScorersListParams } from "@/application/ports";
import type { Scorer } from "@/domain/entities";
import type { PagedResponse } from "@/domain/types/pagination";

export const scorersAdapter: IScorersPort = {
  async list(params: ScorersListParams): Promise<PagedResponse<Scorer>> {
    const query = buildQueryParams({
      pageNumber: params.pageNumber,
      pageSize: params.pageSize,
      sortBy: params.sortBy,
      sortDirection: params.sortDirection,
      teamId: params.teamId,
      search: params.search,
    });
    return httpClient.get<PagedResponse<Scorer>>("scorers", query);
  },
};
