"use client";

import { useState, useTransition } from "react";
import { recordResultAction } from "./actions";

interface Props {
  matchId: string;
}

export function RecordResultForm({ matchId }: Props) {
  const [homeGoals, setHomeGoals] = useState(0);
  const [awayGoals, setAwayGoals] = useState(0);
  const [result, setResult] = useState<{ ok: boolean; message?: string; detail?: string } | null>(
    null,
  );
  const [isPending, startTransition] = useTransition();

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (homeGoals < 0 || awayGoals < 0) {
      setResult({ ok: false, message: "Goals cannot be negative." });
      return;
    }
    startTransition(async () => {
      const res = await recordResultAction(matchId, {
        homeGoals,
        awayGoals,
        goalsByPlayer: [],
      });
      setResult(res);
    });
  }

  return (
    <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: "0.75rem", maxWidth: 320 }}>
      <label>
        Home Goals
        <input
          type="number"
          min={0}
          value={homeGoals}
          onChange={(e) => setHomeGoals(Number(e.target.value))}
          style={{ marginLeft: "0.5rem", width: 60 }}
        />
      </label>
      <label>
        Away Goals
        <input
          type="number"
          min={0}
          value={awayGoals}
          onChange={(e) => setAwayGoals(Number(e.target.value))}
          style={{ marginLeft: "0.5rem", width: 60 }}
        />
      </label>
      <button type="submit" disabled={isPending}>
        {isPending ? "Saving…" : "Record Result"}
      </button>

      {result && (
        <div
          style={{
            padding: "0.5rem",
            border: `1px solid ${result.ok ? "green" : "#c00"}`,
            borderRadius: 4,
          }}
        >
          {result.ok ? (
            <span style={{ color: "green" }}>Result recorded successfully.</span>
          ) : (
            <>
              <strong style={{ color: "#c00" }}>Error: </strong>
              {result.message}
              {result.detail && <p style={{ marginTop: 4, fontSize: "0.875em" }}>{result.detail}</p>}
            </>
          )}
        </div>
      )}
    </form>
  );
}
