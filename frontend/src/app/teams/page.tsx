import Link from "next/link";
import { listTeams } from "@/application/use-cases";
import { teamsAdapter } from "@/infrastructure/adapters";
import { ErrorMessage } from "@/ui/components/ErrorMessage";

interface PageProps {
  searchParams: Promise<Record<string, string | undefined>>;
}

export default async function TeamsPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const pageNumber = Number(params.pageNumber ?? 1);
  const pageSize = Number(params.pageSize ?? 10);
  const sortBy = params.sortBy;
  const sortDirection = (params.sortDirection as "asc" | "desc") ?? "asc";
  const search = params.search;

  let result;
  let fetchError: unknown = null;

  try {
    result = await listTeams(teamsAdapter, {
      pageNumber,
      pageSize,
      sortBy,
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
      <h2>Teams</h2>

      {/* Filter form */}
      <form method="GET" style={{ marginBottom: "1rem", display: "flex", gap: "0.5rem" }}>
        <input name="search" defaultValue={search ?? ""} placeholder="Search…" />
        <select name="sortBy" defaultValue={sortBy ?? ""}>
          <option value="">Sort by…</option>
          <option value="name">Name</option>
          <option value="createdAt">Created</option>
        </select>
        <select name="sortDirection" defaultValue={sortDirection}>
          <option value="asc">ASC</option>
          <option value="desc">DESC</option>
        </select>
        <button type="submit">Apply</button>
      </form>

      {result.data.length === 0 ? (
        <p>No teams found.</p>
      ) : (
        <table style={{ borderCollapse: "collapse", width: "100%" }}>
          <thead>
            <tr>
              <th style={th}>Name</th>
              <th style={th}>Created</th>
              <th style={th}></th>
            </tr>
          </thead>
          <tbody>
            {result.data.map((team) => (
              <tr key={team.id}>
                <td style={td}>{team.name}</td>
                <td style={td}>{new Date(team.createdAt).toLocaleDateString()}</td>
                <td style={td}>
                  <Link href={`/teams/${team.id}`}>Detail</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {/* Pagination */}
      <div style={{ marginTop: "1rem", display: "flex", gap: "0.5rem", alignItems: "center" }}>
        <span>
          Page {result.pageNumber} / {result.totalPages} ({result.totalRecords} total)
        </span>
        {result.pageNumber > 1 && (
          <Link
            href={`/teams?pageNumber=${result.pageNumber - 1}&pageSize=${pageSize}&search=${search ?? ""}&sortBy=${sortBy ?? ""}&sortDirection=${sortDirection}`}
          >
            ← Prev
          </Link>
        )}
        {result.pageNumber < result.totalPages && (
          <Link
            href={`/teams?pageNumber=${result.pageNumber + 1}&pageSize=${pageSize}&search=${search ?? ""}&sortBy=${sortBy ?? ""}&sortDirection=${sortDirection}`}
          >
            Next →
          </Link>
        )}
      </div>
    </div>
  );
}

const th: React.CSSProperties = { textAlign: "left", borderBottom: "1px solid #ccc", padding: "4px 8px" };
const td: React.CSSProperties = { padding: "4px 8px", borderBottom: "1px solid #eee" };
