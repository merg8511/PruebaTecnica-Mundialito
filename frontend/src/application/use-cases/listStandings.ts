import type { IStandingsPort } from "@/application/ports";
import type { Standing } from "@/domain/entities";
import type { PagedResponse } from "@/domain/types/pagination";

export async function listStandings(
  port: IStandingsPort,
): Promise<PagedResponse<Standing>> {
  return port.list();
}
