import { httpClient } from "@/infrastructure/http/client";
import type { IStandingsPort } from "@/application/ports";
import type { Standing } from "@/domain/entities";
import type { PagedResponse } from "@/domain/types/pagination";

export const standingsAdapter: IStandingsPort = {
  async list(): Promise<PagedResponse<Standing>> {
    return httpClient.get<PagedResponse<Standing>>("standings");
  },
};
