import Link from "next/link";
import { getTeam, listPlayersByTeam } from "@/application/use-cases";
import { teamsAdapter, playersAdapter } from "@/infrastructure/adapters";
import { ErrorMessage } from "@/ui/components/ErrorMessage";

interface PageProps {
  params: Promise<{ teamId: string }>;
  searchParams: Promise<Record<string, string | undefined>>;
}

export default async function TeamDetailPage({ params, searchParams }: PageProps) {
  const { teamId } = await params;
  const sp = await searchParams;
  const pageNumber = Number(sp.pageNumber ?? 1);
  const pageSize = Number(sp.pageSize ?? 10);

  let team;
  let players;
  let fetchError: unknown = null;

  try {
    [team, players] = await Promise.all([
      getTeam(teamsAdapter, teamId),
      listPlayersByTeam(playersAdapter, teamId, { pageNumber, pageSize }),
    ]);
  } catch (err) {
    fetchError = err;
  }

  if (fetchError) {
    return <ErrorMessage error={fetchError} />;
  }

  if (!team) return null;

  return (
    <div>
      <Link href="/teams">← Back to Teams</Link>
      <h2>{team.name}</h2>
      <p>Created: {new Date(team.createdAt).toLocaleDateString()}</p>

      <h3>Players</h3>
      {!players || players.data.length === 0 ? (
        <p>No players registered.</p>
      ) : (
        <>
          <table style={{ borderCollapse: "collapse", width: "100%" }}>
            <thead>
              <tr>
                <th style={th}>#</th>
                <th style={th}>Name</th>
              </tr>
            </thead>
            <tbody>
              {players.data.map((p) => (
                <tr key={p.id}>
                  <td style={td}>{p.number ?? "—"}</td>
                  <td style={td}>{p.fullName}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div style={{ marginTop: "0.75rem", display: "flex", gap: "0.5rem", alignItems: "center" }}>
            <span>Page {players.pageNumber} / {players.totalPages}</span>
            {players.pageNumber > 1 && (
              <Link href={`/teams/${teamId}?pageNumber=${players.pageNumber - 1}&pageSize=${pageSize}`}>
                ← Prev
              </Link>
            )}
            {players.pageNumber < players.totalPages && (
              <Link href={`/teams/${teamId}?pageNumber=${players.pageNumber + 1}&pageSize=${pageSize}`}>
                Next →
              </Link>
            )}
          </div>
        </>
      )}
    </div>
  );
}

const th: React.CSSProperties = { textAlign: "left", borderBottom: "1px solid #ccc", padding: "4px 8px" };
const td: React.CSSProperties = { padding: "4px 8px", borderBottom: "1px solid #eee" };
