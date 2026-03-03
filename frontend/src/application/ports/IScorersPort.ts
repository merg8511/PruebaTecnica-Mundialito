import type { Scorer } from "@/domain/entities";
import type { PagedResponse, PageParams } from "@/domain/types/pagination";

export interface ScorersListParams extends PageParams {
  teamId?: string;
  search?: string;
}

export interface IScorersPort {
  list(params: ScorersListParams): Promise<PagedResponse<Scorer>>;
}
