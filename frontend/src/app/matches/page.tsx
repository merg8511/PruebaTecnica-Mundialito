import Link from "next/link";
import { listMatches } from "@/application/use-cases";
import { matchesAdapter } from "@/infrastructure/adapters";
import { ErrorMessage } from "@/ui/components/ErrorMessage";

interface PageProps {
  searchParams: Promise<Record<string, string | undefined>>;
}

export default async function MatchesPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const pageNumber = Number(params.pageNumber ?? 1);
  const pageSize = Number(params.pageSize ?? 10);
  const sortBy = params.sortBy;
  const sortDirection = (params.sortDirection as "asc" | "desc") ?? "asc";
  const status = params.status;
  const teamId = params.teamId;
  const dateFrom = params.dateFrom;
  const dateTo = params.dateTo;

  let result;
  let fetchError: unknown = null;

  try {
    result = await listMatches(matchesAdapter, {
      pageNumber,
      pageSize,
      sortBy,
      sortDirection,
      status,
      teamId,
      dateFrom,
      dateTo,
    });
  } catch (err) {
    fetchError = err;
  }

  if (fetchError) {
    return <ErrorMessage error={fetchError} />;
  }

  if (!result) return null;

  return (
    <div>
      <h2>Matches</h2>

      {/* Filter form */}
      <form method="GET" style={{ marginBottom: "1rem", display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
        <select name="status" defaultValue={status ?? ""}>
          <option value="">All statuses</option>
          <option value="Scheduled">Scheduled</option>
          <option value="Finished">Finished</option>
        </select>
        <input type="date" name="dateFrom" defaultValue={dateFrom ?? ""} />
        <input type="date" name="dateTo" defaultValue={dateTo ?? ""} />
        <select name="sortBy" defaultValue={sortBy ?? ""}>
          <option value="">Sort by…</option>
          <option value="scheduledAt">Date</option>
        </select>
        <select name="sortDirection" defaultValue={sortDirection}>
          <option value="asc">ASC</option>
          <option value="desc">DESC</option>
        </select>
        <button type="submit">Apply</button>
      </form>

      {result.data.length === 0 ? (
        <p>No matches found.</p>
      ) : (
        <table style={{ borderCollapse: "collapse", width: "100%" }}>
          <thead>
            <tr>
              <th style={th}>Home</th>
              <th style={th}>Away</th>
              <th style={th}>Date</th>
              <th style={th}>Status</th>
              <th style={th}>Score</th>
              <th style={th}></th>
            </tr>
          </thead>
          <tbody>
            {result.data.map((m) => (
              <tr key={m.id}>
                <td style={td}>{m.homeTeamName}</td>
                <td style={td}>{m.awayTeamName}</td>
                <td style={td}>{new Date(m.scheduledAt).toLocaleDateString()}</td>
                <td style={td}>{m.status}</td>
                <td style={td}>
                  {m.homeGoals !== null && m.awayGoals !== null
                    ? `${m.homeGoals} – ${m.awayGoals}`
                    : "—"}
                </td>
                <td style={td}>
                  {m.status === "Scheduled" && (
                    <Link href={`/matches/${m.id}/result`}>Record Result</Link>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {/* Pagination */}
      <div style={{ marginTop: "1rem", display: "flex", gap: "0.5rem", alignItems: "center" }}>
        <span>Page {result.pageNumber} / {result.totalPages} ({result.totalRecords} total)</span>
        {result.pageNumber > 1 && (
          <Link href={`/matches?pageNumber=${result.pageNumber - 1}&pageSize=${pageSize}&status=${status ?? ""}&sortBy=${sortBy ?? ""}&sortDirection=${sortDirection}`}>
            ← Prev
          </Link>
        )}
        {result.pageNumber < result.totalPages && (
          <Link href={`/matches?pageNumber=${result.pageNumber + 1}&pageSize=${pageSize}&status=${status ?? ""}&sortBy=${sortBy ?? ""}&sortDirection=${sortDirection}`}>
            Next →
          </Link>
        )}
      </div>
    </div>
  );
}

const th: React.CSSProperties = { textAlign: "left", borderBottom: "1px solid #ccc", padding: "4px 8px" };
const td: React.CSSProperties = { padding: "4px 8px", borderBottom: "1px solid #eee" };
