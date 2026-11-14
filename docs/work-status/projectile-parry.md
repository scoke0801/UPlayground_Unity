# 전투 투사체 패리 설계 상태

- 요청: 웹 조사는 메인 Agent, 설계 최종 확정은 Astra.
- 조사일: 2026-11-14.
- 설계 정본: [투사체 패리](../../Assets/docs/design/combat/PROJECTILE_PARRY_SYSTEM_DESIGN.md).
- 현재 단계: **P0~P4 구현·선택 자동 검사·사용자 명세 인계 완료, 사용자 플레이 인수 대기**. 관련 EditMode103/103·Editor PlayMode7/7 통과. 확장 검사109/110(WoodlandWolf 정책 누락1), 전체 Ability 검사 범위 밖 오류7건은 별도 기록. [P0](projectile-parry/P0.md) · [P1](projectile-parry/P1.md) · [P2](projectile-parry/P2.md) · [P3](projectile-parry/P3.md) · [P4 최종 대조·검사 원본](projectile-parry/P4.md) · [PP01~PP10 사용자 명세](projectile-parry/USER_TEST.md).

## Astra 확정안

- 기존 Guard 입력의 투사체 전용 창을 사용하고 공격 쳐내기·어시스트와 반사 규칙을 공유한다.
- 최초 지원은 명시적으로 등록한 직선 화살·단일 직선 마법탄. 곡사·빔·장판·폭발·분열 등은 제외한다.
- 몸 정면 140도, Guard 창 0.18초, 재개방 0.35초, 반환 수명 2초는 실측 전 튜닝 초깃값이다. 근접 가드 창은 별도 유지한다.
- 같은 Guard 창의 여러 탄은 각각 반사하며 보상은 행동당 1회다. 반사 횟수는 탄 생애 1회다.
- 방어·탄 상태를 단일 확정으로 연결한다. 승인 후 안전한 반사 공간을 확보하지 못하면 피해·후속 효과 없이 흡수 소멸한다.
- 발사 시 공격 수치 출처를 고정하고 반사자에게 처치·기여도를 귀속한다. 원거리 발사자 즉시 경직·전역 슬로우·공격 proc 재귀는 금지한다.
- 구현 P0~P4, 자동 경계, 사용자 인수 PP01~PP10을 설계에 명시했다.

## 조사 근거

메인 Agent가 공식 웹 자료를 조회했다. 게임 사례는 방향을 정하는 참고이며 타 게임의 판정 시간·프레임 수를 프로젝트 수치로 전용하지 않는다.

| 자료 | 확인한 사실 | 적용 경계 |
|---|---|---|
| [Housemarque 개발자 설명, 2025-12-11](https://blog.playstation.com/?p=412940) | 정확한 타이밍의 입력으로 특정 투사체를 반사해 적을 파괴하거나 경직시킨다 | 탄종별 가독성과 반격 보상 참고. 세부 판정 수치는 공개 근거 없음 |
| [Wo Long 공식 매뉴얼](https://www.koeitecmoamerica.com/manual/wolong/en/3000.html) | 적중 직전 Deflect로 피해를 피하고 적 Spirit을 줄이며 자신의 Spirit을 높인다 | 방어를 공격 기회로 바꾸는 참고. 투사체 반사 설명으로 인용하지 않음 |
| [Unity 6000 SphereCastNonAlloc](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/physics/spherecastnonalloc) | 비할당 결과 순서 미보장, 포화 버퍼의 최근접 결과 포함도 미보장 | 거리 정렬만으로 포화 안전성을 확보했다고 판단하지 않음 |

실제 저장된 코드에서 `ReflectableProjectileBehavior`, `ProjectileRuntime.TryReflect`, `DefenseResolver`의 가드 우선·공격 쳐내기 경로를 대조했다. `PlayerActor.OnAttackClashSuccess`는 발사자에게 직접 `MonsterActor.OnParried()`를 호출하므로 투사체 반사에 재사용할 때 원거리 즉시 경직을 분리해야 한다. 곡사 시작점·타깃·수명·분열·폭발과 반사 횟수 정책도 설계 검토 대상으로 전달했다.

## 검증과 보존

- 수행: 위키·전투 저작 규칙·방어/투사체 소비 코드·관련 기존 테스트 항목 읽기, 공식 자료 확인.
- 최종 문서 검사: 메인 Agent가 설계·상태의 로컬 링크 16개 존재 확인, 누락 0. Astra 자체 검사도 설계 링크 누락 0. 위키·설계 목차 연결과 관련 문서 `git diff --check` 통과. 기존 사용자 diff를 포함한 파일은 추가한 부분만 이번 작업으로 취급한다.
- 미수행: Unity 컴파일·EditMode·Play Mode·실제 입력/피드백 검증. 문서 설계 작업이며 런타임 변경 없음.
- 사용자 테스트: 구현 후 명세에 따라 진행. 현재 플레이 품질을 합격으로 판단하지 않는다.
- 기존 변경: 작업 시작 시 다수의 코드·데이터·문서 미커밋 변경 확인. 모두 보존하며 커밋하지 않음.
- 부산물 정리: 해당 없음. 광범위 에셋 텍스트 조회는 중단했으며 에셋 전수 감사로 보고하지 않음.

## 다음 단계

[구현 요청 프롬프트](../../Assets/docs/requests/PROJECTILE_PARRY_IMPLEMENTATION_PROMPTS.md)를 작성했다. 전체 구현 요청 1개와 P0~P4 단계별 요청 5개, 공통 보존·검증·인계 규칙을 포함한다. 이번 후속 작업은 요청서 작성이며 구현은 실행하지 않았다.

요청서 검사: 코드 블록 6개, 로컬 링크 3개 누락 0, 관련 목차·위키 diff 공백 검사 통과. 새 요청서와 meta를 보존하며 임시 부산물은 없다.

구현은 설계 P0 연결 조사부터 진행할 후속 작업이다.





