import Link from "next/link";
import { getMatch } from "@/application/use-cases";
import { matchesAdapter } from "@/infrastructure/adapters";
import { ErrorMessage } from "@/ui/components/ErrorMessage";
import { RecordResultForm } from "./RecordResultForm";

interface PageProps {
  params: Promise<{ matchId: string }>;
}

export default async function RecordResultPage({ params }: PageProps) {
  const { matchId } = await params;

  let match;
  let fetchError: unknown = null;

  try {
    match = await getMatch(matchesAdapter, matchId);
  } catch (err) {
    fetchError = err;
  }

  if (fetchError) {
    return (
      <div>
        <Link href="/matches">← Back to Matches</Link>
        <ErrorMessage error={fetchError} />
      </div>
    );
  }

  if (!match) return null;

  if (match.status !== "Scheduled") {
    return (
      <div>
        <Link href="/matches">← Back to Matches</Link>
        <p>
          Result already recorded for this match ({match.homeTeamName}{" "}
          {match.homeGoals} – {match.awayGoals} {match.awayTeamName}).
        </p>
      </div>
    );
  }

  return (
    <div>
      <Link href="/matches">← Back to Matches</Link>
      <h2>Record Result</h2>
      <p>
        <strong>{match.homeTeamName}</strong> vs <strong>{match.awayTeamName}</strong>
        {" — "}
        {new Date(match.scheduledAt).toLocaleDateString()}
      </p>
      <RecordResultForm matchId={matchId} />
    </div>
  );
}
