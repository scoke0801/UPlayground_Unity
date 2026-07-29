import "./globals.css";

export const metadata = {
  title: "UPlayGround — Cycle Hunt",
  description: "시드 기반 사이클형 보스 헌팅 HTML 게임 프로토타입",
};

export default function RootLayout({ children }) {
  return (
    <html lang="ko">
      <body>{children}</body>
    </html>
  );
}
