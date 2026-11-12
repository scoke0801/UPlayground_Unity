# NPC 월드·대사 작업 하네스

운영 진입점: [공통 계약](../../docs/dialogue-bake/CONTRACT.md), [실행 절차](../../docs/dialogue-bake/PIPELINE.md), [검수](../../docs/dialogue-bake/REVIEW.md), [프로젝트 조사](../../docs/dialogue-bake/SOURCE_AUDIT.md).

Node.js 22 환경에서 외부 설치 없이 실행한다.

```powershell
node --test Tools/NpcWorldHarness/harness.test.mjs
node Tools/NpcWorldHarness/harness.mjs discover Tools/NpcWorldHarness/sources.json work/dialogue-bake/reports/source-audit-RUN.json
node Tools/NpcWorldHarness/harness.mjs validate SNAPSHOT.json PACK.json PREVIOUS_PACK.json
node Tools/NpcWorldHarness/harness.mjs compile SNAPSHOT.json PACK.json REVIEW.json work/dialogue-bake/staging/catalog-RUN.json PREVIOUS_PACK.json
```

RUN은 실행별 신규 이름으로 바꾼다. 첫 팩일 때만 PREVIOUS_PACK을 생략한다. 입력은 프로젝트 상대 경로다. 출력은 staging/reports 신규 파일만 허용한다. 검사 실패는 종료 코드 1, discover의 차단 상태는 보고서의 status로 구별한다.

지원: 허용 파일 조사·SHA256, 후보 구조·슬롯 불변성·NPC 지식·공개 조건·조건 레지스트리 검사, 기존 보호 항목 변경 차단, 사용자 승인 해시 검사와 정렬된 staging 카탈로그 생성.

미연결: Unity SO 자동 정규화, 자동 증분 슬롯 계획, 의미 기반 대사 평가, Unity 에셋 import·재생. 월드 런타임 자체는 이 도구가 구현하지 않는다. 해당 작업은 공통 절차의 Phase별로 기존 시스템에 통합한다.
