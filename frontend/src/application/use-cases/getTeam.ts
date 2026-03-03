import type { ITeamsPort } from "@/application/ports";
import type { Team } from "@/domain/entities";

export async function getTeam(port: ITeamsPort, id: string): Promise<Team> {
  return port.getById(id);
}
