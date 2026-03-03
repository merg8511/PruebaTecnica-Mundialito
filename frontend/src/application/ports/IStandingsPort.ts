import type { Standing } from "@/domain/entities";
import type { PagedResponse } from "@/domain/types/pagination";

export interface IStandingsPort {
  list(): Promise<PagedResponse<Standing>>;
}
