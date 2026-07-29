# UPlayGround Cycle Hunt HTML Prototype

시드 기반 사이클형 보스 헌팅의 핵심 루프를 빠르게 체험하기 위한 브라우저 게임입니다.

## 실행

```powershell
npm install
npm run dev
```

브라우저에서 `http://localhost:3000`을 엽니다.

## 조작

- `WASD` / 방향키: 이동
- `Space` / `J`: 공격
- `Shift`: 회피 대시
- `Q`: 영입한 BossAssist 호출
- `E`: 유해 회수 / 포털 정산
- `H`: 도움말

## 반영한 사이클 규칙

- 입력 시드에 따른 외곽 보스 배치 순서
- 외곽 보스 3체 격파 후 중앙 봉인 해제
- 외곽 보스 영입을 파티 합류가 아닌 BossAssist로 처리
- 전멸 시 미정산 재료 일부를 유해에 보관
- 중앙 보스 처치와 포털 정산을 별도 단계로 분리
- 포털 정산 후에만 미정산 재료를 영구 보상으로 확정
