import type { Metadata } from "next";
import "./globals.css";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Mundialito",
  description: "Tournament management frontend",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>
        <nav
          style={{
            display: "flex",
            gap: "1.5rem",
            padding: "0.75rem 1.5rem",
            borderBottom: "1px solid #ddd",
            fontFamily: "monospace",
          }}
        >
          <Link href="/">Home</Link>
          <Link href="/teams">Teams</Link>
          <Link href="/matches">Matches</Link>
          <Link href="/standings">Standings</Link>
          <Link href="/scorers">Scorers</Link>
        </nav>
        <main style={{ padding: "1.5rem", fontFamily: "monospace" }}>
          {children}
        </main>
      </body>
    </html>
  );
}
