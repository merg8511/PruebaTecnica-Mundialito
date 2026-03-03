import type { IPlayersPort } from "@/application/ports";
import type { Player } from "@/domain/entities";
import type { PageParams } from "@/domain/types/pagination";
import type { PagedResponse } from "@/domain/types/pagination";

export async function listPlayersByTeam(
  port: IPlayersPort,
  teamId: string,
  params: PageParams = {},
): Promise<PagedResponse<Player>> {
  return port.listByTeam(teamId, params);
}
