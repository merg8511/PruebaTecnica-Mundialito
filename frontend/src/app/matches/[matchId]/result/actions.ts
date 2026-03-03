"use server";

import { recordMatchResult } from "@/application/use-cases";
import { matchesAdapter } from "@/infrastructure/adapters";
import type { RecordResultPayload } from "@/application/ports";

export interface ActionResult {
  ok: boolean;
  message?: string;
  detail?: string;
}

export async function recordResultAction(
  matchId: string,
  payload: RecordResultPayload,
): Promise<ActionResult> {
  try {
    await recordMatchResult(matchesAdapter, matchId, payload);
    return { ok: true };
  } catch (err) {
    if (err instanceof Error) {
      const apiErr = err as { status?: number; detail?: string };
      return { ok: false, message: err.message, detail: apiErr.detail };
    }
    return { ok: false, message: "Unexpected error." };
  }
}
