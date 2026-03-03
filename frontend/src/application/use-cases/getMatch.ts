import type { IMatchesPort } from "@/application/ports";
import type { Match } from "@/domain/entities";

export async function getMatch(port: IMatchesPort, id: string): Promise<Match> {
  return port.getById(id);
}
