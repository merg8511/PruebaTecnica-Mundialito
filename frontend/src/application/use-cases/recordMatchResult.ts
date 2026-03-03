import type { IMatchesPort, RecordResultPayload } from "@/application/ports";
import type { Match } from "@/domain/entities";

export async function recordMatchResult(
  port: IMatchesPort,
  matchId: string,
  payload: RecordResultPayload,
): Promise<Match> {
  return port.recordResult(matchId, payload);
}
