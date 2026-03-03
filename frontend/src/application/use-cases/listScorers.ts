import type { IScorersPort, ScorersListParams } from "@/application/ports";
import type { Scorer } from "@/domain/entities";
import type { PagedResponse } from "@/domain/types/pagination";

export async function listScorers(
  port: IScorersPort,
  params: ScorersListParams = {},
): Promise<PagedResponse<Scorer>> {
  return port.list(params);
}
