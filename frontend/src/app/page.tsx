import Link from "next/link";

export default function HomePage() {
  return (
    <div>
      <h1>Mundialito</h1>
      <p>Tournament management system.</p>
      <ul style={{ lineHeight: 2 }}>
        <li><Link href="/teams">Teams</Link></li>
        <li><Link href="/matches">Matches</Link></li>
        <li><Link href="/standings">Standings</Link></li>
        <li><Link href="/scorers">Top Scorers</Link></li>
      </ul>
    </div>
  );
}
