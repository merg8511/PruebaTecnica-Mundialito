import { listStandings } from "@/application/use-cases";
import { standingsAdapter } from "@/infrastructure/adapters";
import { ErrorMessage } from "@/ui/components/ErrorMessage";

export default async function StandingsPage() {
  let result;
  let fetchError: unknown = null;

  try {
    result = await listStandings(standingsAdapter);
  } catch (err) {
    fetchError = err;
  }

  if (fetchError) {
    return <ErrorMessage error={fetchError} />;
  }

  if (!result) return null;

  return (
    <div>
      <h2>Standings</h2>

      {result.data.length === 0 ? (
        <p>No standings available yet.</p>
      ) : (
        <table style={{ borderCollapse: "collapse", width: "100%" }}>
          <thead>
            <tr>
              <th style={th}>#</th>
              <th style={th}>Team</th>
              <th style={th}>P</th>
              <th style={th}>W</th>
              <th style={th}>D</th>
              <th style={th}>L</th>
              <th style={th}>GF</th>
              <th style={th}>GA</th>
              <th style={th}>GD</th>
              <th style={th}>Pts</th>
            </tr>
          </thead>
          <tbody>
            {result.data.map((row, idx) => (
              <tr key={row.teamId}>
                <td style={td}>{idx + 1}</td>
                <td style={td}>{row.teamName}</td>
                <td style={td}>{row.played}</td>
                <td style={td}>{row.wins}</td>
                <td style={td}>{row.draws}</td>
                <td style={td}>{row.losses}</td>
                <td style={td}>{row.goalsFor}</td>
                <td style={td}>{row.goalsAgainst}</td>
                <td style={td}>{row.goalDifference}</td>
                <td style={{ ...td, fontWeight: "bold" }}>{row.points}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

const th: React.CSSProperties = { textAlign: "left", borderBottom: "1px solid #ccc", padding: "4px 8px" };
const td: React.CSSProperties = { padding: "4px 8px", borderBottom: "1px solid #eee" };
