import type { Team } from "@/domain/entities";
import type { PagedResponse, PageParams } from "@/domain/types/pagination";

export interface TeamsListParams extends PageParams {
  search?: string;
}

export interface ITeamsPort {
  list(params: TeamsListParams): Promise<PagedResponse<Team>>;
  getById(id: string): Promise<Team>;
}
