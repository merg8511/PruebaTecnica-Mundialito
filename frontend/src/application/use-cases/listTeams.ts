import type { ITeamsPort, TeamsListParams } from "@/application/ports";
import type { Team } from "@/domain/entities";
import type { PagedResponse } from "@/domain/types/pagination";

export async function listTeams(
  port: ITeamsPort,
  params: TeamsListParams = {},
): Promise<PagedResponse<Team>> {
  return port.list(params);
}
