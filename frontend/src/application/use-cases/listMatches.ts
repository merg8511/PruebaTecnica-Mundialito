import type { IMatchesPort, MatchesListParams } from "@/application/ports";
import type { Match } from "@/domain/entities";
import type { PagedResponse } from "@/domain/types/pagination";

export async function listMatches(
  port: IMatchesPort,
  params: MatchesListParams = {},
): Promise<PagedResponse<Match>> {
  return port.list(params);
}
