import Link from "next/link";
import { listScorers } from "@/application/use-cases";
import { scorersAdapter } from "@/infrastructure/adapters";
import { ErrorMessage } from "@/ui/components/ErrorMessage";

interface PageProps {
  searchParams: Promise<Record<string, string | undefined>>;
}

export default async function ScorersPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const pageNumber = Number(params.pageNumber ?? 1);
  const pageSize = Number(params.pageSize ?? 10);
  const sortBy = params.sortBy;
  const sortDirection = (params.sortDirection as "asc" | "desc") ?? "desc";
  const search = params.search;

  let result;
  let fetchError: unknown = null;

  try {
    result = await listScorers(scorersAdapter, {
      pageNumber,
      pageSize,
      sortBy: sortBy ?? "goals",
      sortDirection,
      search,
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
      <h2>Top Scorers</h2>

      {/* Filter */}
      <form method="GET" style={{ marginBottom: "1rem", display: "flex", gap: "0.5rem" }}>
        <input name="search" defaultValue={search ?? ""} placeholder="Search player…" />
        <select name="sortDirection" defaultValue={sortDirection}>
          <option value="desc">Most goals first</option>
          <option value="asc">Least goals first</option>
        </select>
        <button type="submit">Apply</button>
      </form>

      {result.data.length === 0 ? (
        <p>No scorers yet.</p>
      ) : (
        <table style={{ borderCollapse: "collapse", width: "100%" }}>
          <thead>
            <tr>
              <th style={th}>#</th>
              <th style={th}>Player</th>
              <th style={th}>Team</th>
              <th style={th}>Goals</th>
            </tr>
          </thead>
          <tbody>
            {result.data.map((s, idx) => (
              <tr key={s.playerId}>
                <td style={td}>{(pageNumber - 1) * pageSize + idx + 1}</td>
                <td style={td}>{s.playerName}</td>
                <td style={td}>{s.teamName}</td>
                <td style={td}>{s.goals}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {/* Pagination */}
      <div style={{ marginTop: "1rem", display: "flex", gap: "0.5rem", alignItems: "center" }}>
        <span>Page {result.pageNumber} / {result.totalPages} ({result.totalRecords} total)</span>
        {result.pageNumber > 1 && (
          <Link href={`/scorers?pageNumber=${result.pageNumber - 1}&pageSize=${pageSize}&search=${search ?? ""}&sortDirection=${sortDirection}`}>
            ← Prev
          </Link>
        )}
        {result.pageNumber < result.totalPages && (
          <Link href={`/scorers?pageNumber=${result.pageNumber + 1}&pageSize=${pageSize}&search=${search ?? ""}&sortDirection=${sortDirection}`}>
            Next →
          </Link>
        )}
      </div>
    </div>
  );
}

const th: React.CSSProperties = { textAlign: "left", borderBottom: "1px solid #ccc", padding: "4px 8px" };
const td: React.CSSProperties = { padding: "4px 8px", borderBottom: "1px solid #eee" };
