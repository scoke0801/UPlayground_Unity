# 실행 절차

먼저 [공통 계약](CONTRACT.md), [검수 규약](REVIEW.md), [기존 시스템 조사](SOURCE_AUDIT.md)를 읽는다.

## 월드 이벤트·NPC 구현 작업

1. **Phase 0 조사:** `node Tools/NpcWorldHarness/harness.mjs discover Tools/NpcWorldHarness/sources.json work/dialogue-bake/reports/source-audit-RUN.json`으로 기존 파일 해시와 차단 사유를 기록한다. RUN은 실행별 이름으로 바꾼다. 코드 존재는 기능 검증 통과가 아니다.
2. 현재 요청의 Phase 하나를 정한다. 신규 Clock/Save/AI 매니저를 만들기 전에 기존 서비스·계약·asmdef와의 연결표, 변경 파일, 데이터 소유자, 기존 시스템으로 표현 불가능한 원시 기능을 기록한다.
3. Phase 순서는 원본 설계의 Core → Event Runtime → NPC Goals → Presentation → LOD/Save → Editor → 성능이다. 구현된 기존 기능은 재사용하고 없는 항목만 추가한다. 사건별 C# 분기 대신 데이터와 Handler 조합을 사용한다.
4. 작업 기록에 입력/출력 계약, 취소·타임아웃·씬 이탈·로드·중복 명령/보상·역할/슬롯 해제 경계를 적는다. 논리 결과와 연출 완료를 구분한다. 오프스크린 자동 성공은 데이터의 명시적 정책 없이는 허용하지 않는다.
5. `TEST_VALIDATION_GUIDE.md`에 따라 관련 검증을 고른다. 설계서 T01~T18 중 적용 ID와 재현 절차·기대값·결과를 기록한다. 성능 예시 수치를 측정값처럼 보고하지 않는다. 빌드는 별도 지시가 있을 때만 한다.

## 오프라인 대사 Bake 작업

1. **discover:** 위 명령으로 허용 목록과 출처 변경을 확인한다. 권위 에셋을 자동 수정하지 않는다.
2. **normalize/resolve:** 실제 에셋의 GUID/ID와 필드를 읽고 snapshot JSON으로 명시적으로 변환한다. source_refs와 SHA256, NPC별 지식·공개 시점·허용 조건 값을 연결한다. YAML 문자열 검색만으로 참조와 지식을 추정하지 않는다. 아직 자동 Unity SO 정규화 어댑터는 없다. 확정 근거 없는 값은 Unknown으로 보고하고 슬롯을 만들지 않는다.
3. **plan:** 문장을 쓰기 전에 슬롯과 Voice Profile·관계·이미지를 검수한다. 승인된 슬롯만 고정하고 침묵을 허용한다.
4. **realize:** 슬롯별 제한된 자료만 읽어 한국어 후보 JSON을 staging에 작성한다. `review_status`는 draft로 시작한다. 모델·프롬프트/정책 버전·입력 해시·실행 로그를 실행 보고서에 남긴다.
5. **lint:** `node Tools/NpcWorldHarness/harness.mjs validate SNAPSHOT PACK [PREVIOUS_PACK]`. 실패면 종료하고 출처·조건·지식·보호 충돌을 수정한다. 이전 팩이 있는 작업에서 PREVIOUS_PACK을 생략하지 않는다.
6. **incremental:** 이전 snapshot의 sources 해시, NPC/조건/정책/slot의 정규화 내용과 현재 것을 비교한다. 변경 source_refs/fact_refs를 쓰는 슬롯과 변경 NPC·조건·정책의 소비 슬롯을 영향 목록으로 기록한다. 영향 없는 기존 항목은 그대로 보존하고, 보호된 항목은 자동 재생성하지 않는다. 삭제 ID는 폐기 제안으로 남긴다. 현재 자동 증분 계획기는 없으므로 에이전트가 비교 보고서를 작성한다.
7. **review:** REVIEW.md에 따라 사용자 검수 후 정확한 팩·스냅샷 해시를 가진 검수 기록 JSON을 남긴다. 자동 승인 커맨드는 제공하지 않는다.
8. **compile:** `node Tools/NpcWorldHarness/harness.mjs compile SNAPSHOT PACK REVIEW work/dialogue-bake/staging/catalog-RUN.json [PREVIOUS_PACK]`. 성공하면 고정 순서 카탈로그가 생성된다. 기존 에셋 적용은 지원 조건·중단·쿨다운·ID 보존을 검토하는 별도 통합 단계다.

## 하네스 자체 검증

`node --test Tools/NpcWorldHarness/harness.test.mjs`

예제는 테스트 코드 안의 합성 자료이며 정식 설정과 분리한다. 이 명령은 Unity 런타임 검증을 대체하지 않는다.
