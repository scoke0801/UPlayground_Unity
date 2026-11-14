# Staff 공격 Ability·모션 확장

[설계](../../../Assets/docs/design/combat/monster/STAFF_RANGED_MAGE_BT_DESIGN.md#공격-구성-확장) · [전투 위키](../../wiki/combat.md).

## 조사·계획

2026-11-14. P09 Staff 기존 3종과 플레이어 Staff 20개 Ability/매핑을 실제 로드했다. 플레이어 Light 여러 항목은 같은 클립을 공유하므로 이름 개수만큼 공격을 늘리지 않는다. 기존 세 클립에 Sub_Spell1·Magic_Attack_2·HumanF@Attack2H04를 추가해 서로 다른 6개 클립으로 투사체 2·Beam 2·근접 2종을 구성한다.

기존 미커밋 투사체 런타임·이동·BT 변경을 보존한다. 공유 플레이어 원본은 수정하지 않는다. 전체 Ability 검사 기준선은 1297건 중 Error 7건이며 [원본](staff-expansion/ability-before.txt)에 보존한다. 이번 확장으로 추가된 오류를 따로 비교한다.

## 저작 반영

기존 3종과 신규 Fan/ChargedBeam/Repulse를 포함해 총 6종을 공용 AbilitySet·P09StaffAnimationSet에 연결했다. 전용 투사체 정의 4개를 만들어 플레이어 공용 정의를 보존했다. Fan의 3발/26도 패턴, 몬스터용 조준·HitPhase, 시전 시작 VFX·발사 시점 SFX·위험 예고, 플레이어 입력 전용 이벤트 제거를 반영했다. 14종 프리팹은 이미 공용 Set/매핑을 사용하므로 다시 저장하지 않는다.

원본 모션의 접촉/발사 타이밍을 재사용했으며 새 시각적 접촉 검수는 진행 중이다. BT를 역할별 공격 요청으로 변경해 근접 후보가 원거리 시전에 섞이지 않도록 했다. 신규 공격을 제작표 목록에도 반영했다. 기존 P09 빌더는 전투 세트가 있으면 이를 재사용하며 이번에는 생성기 C#이나 삭제/재생성 흐름을 바꾸지 않았다.

전체 Ability 검사: 기존 Error 7건 그대로, 신규 Error 0, P09Staff 관련 오류/경고 0. 이름 로컬라이즈 키 부재 Info 6건은 남는다. [변경 후 원본](staff-expansion/ability-after.txt). 첫 EditMode 실행은 이전 로드 어셈블리(공격 3종 기대값)로 실행되어 실패했다. 소스에는 6종·신규 케이스가 있으므로 정상 컴파일 시작/종료와 새 어셈블리 로드 후 재실행한다. 이 실패를 제품 데이터 통과로 처리하지 않는다. MCP 연결이 리로드 뒤 간헐적으로 끊겨 동일 Editor를 다시 지정했고, 추가 Editor를 열거나 캐시를 지우지 않았다.

## 자동 검사 결과와 실행 차단

- 정상 컴파일 뒤 확장된 관련 EditMode **15/15 통과**. [원본 결과](staff-expansion/editmode-results.json). 몬스터 연결 14종×6공격, 신규 주문 피해 스냅샷 4종, 발사 정의의 사거리/생존 거리/차단 레이어, 기존 실제 Hitscan 벽 차단·단일 피해·풀 초기화를 포함한다. 새 주문의 사거리/레이어 검사는 데이터 계약 검사이며 실제 새 주문별 명중 시연과 구분한다.
- [BT 검사](staff-expansion/bt-validation.txt): 81노드, importer 오류/경고 0, 그래프 오류/경고 0.
- [전체 Ability 전후 비교](staff-expansion/ability-before.txt) / [변경 후](staff-expansion/ability-after.txt): 기존 7 Error 그대로, 신규 Error 0. 기존 오류는 PlayerDefaultAttackData의 미완성 공격·CharacterPassiveDatabase 항목이며 이번에 임의 수정하지 않았다.
- 이후 별도 펫 테스트에서 `PetSkillCatalogSO.CooldownGroup` 미정의 컴파일 오류가 발생했다(`PetSupportAbilityTests.cs`, `PetV2AssetTests.cs`). MCP play 도구는 성공 문구를 반환했지만 실제 Editor state와 후속 조회는 `playing=false`, 플레이어 없음이었다. 따라서 Play 진입/신규 6종 실행·미술 품질을 완료로 판정하지 않는다. 이 오류는 Staff 데이터와 다른 범위라 수정하지 않았다.
- 최종 조회에서는 진행 중인 다른 변경의 `ProjectileRuntime.cs` 오류로 바뀌었다: 261행 `GameActor.IsAlive` 미정의, 619행 string→int 인수 불일치. [최종 Console 원본](staff-expansion/play-blocker-console.json). 이 파일은 이번 저작에서 수정하지 않았으며, 같은 Editor의 검증 기준선이 바뀌었으므로 앞선 15/15를 최신 전체 코드의 컴파일 통과로 승계하지 않는다.

**판정: 6종 저작·연결 적용 및 관련 자동 검사 통과. 인게임 실행·최종 품질은 컴파일 차단 해소 후 사용자 테스트 대기.** 이전 3종의 Play 관찰을 확장 6종의 검증으로 승계하지 않는다.

## 사용자 테스트 명세

준비: 위 펫 테스트 컴파일 오류를 해소하고, LakeVillageWorld 또는 전투 시험장에서 해당 P09 Staff와 교전한다. 키보드/마우스 또는 게임패드의 현재 이동·공격·가드·회피 입력을 사용한다. 실제 피해를 받는 상태로 확인하며 저장 데이터는 덮어쓰지 않는다.

| ID | 시작·조작 | 기대 결과 / 실패 기준 |
|---|---|---|
| A01 | Lv10 Staff와 8~14m에서 좌우 이동·가드 | Bolt와 Fan 3발이 구분되고 지팡이 끝에서 발사된다. Fan 전탄 즉사급 피해·미리 보이지 않는 피해·벽 관통은 실패 |
| A02 | Lv15 이상 Staff와 8~18m에서 교전, 시전 중 옆으로 회피·엄폐 | 노란 예고 뒤 Beam 1회, 벽에 차단되고 지속 VFX 동안 중복 피해가 없어야 한다. 손 방향·Beam 시작점 불일치도 실패 |
| A03 | Lv25 이상 Staff와 8~18m에서 교전 | 응축 섬광은 붉은 예고·긴 준비가 읽히고 회피 기회가 있다. 가드로 막히거나 예고 전 피해가 나면 실패 |
| A04 | Lv15 이상 Staff의 후퇴를 따라잡아 2m 이내에서 공격·가드·패링 | Sweep/Repulse가 서로 다른 동작으로 보이고 실제 지팡이 접촉과 피해가 맞는다. 원거리에서 근접 공격을 위해 돌진하거나 강제 무한 넉백이면 실패 |
| A05 | 6종 각각 시전 중 피격/처치, 이후 새 Staff와 재교전 | 발사·타격·예고/VFX가 정상 정리된다. 사망 뒤 유령 발사·다음 시전에 이전 Beam/대상 기록 잔류는 실패 |

회신 형식: `A01~A05 / 통과·실패·미수행 / 실제 결과 / 레벨·입력 장치·필요한 영상/로그`.

## 보존·정리

원본 플레이어 Staff 데이터와 프리팹은 수정하지 않았다. 기존 몬스터 Ability 3개·Payload 3개·Motion 3개·Set/매핑의 GUID를 보존했다. 기존 에셋 저장으로 추가된 무관한 기본 직렬화 필드는 작업 전 사본과 대조해 정리했다. 새 에셋·meta, 검사 원본·설계·상태·위키를 보존한다. `output/staff-ability-expansion-before`는 커밋 전 상태/기존 미추적 데이터 복구 근거이므로 보존한다. Player/Addressables 빌드·커밋은 수행하지 않았다. Play에 진입하지 못했으므로 테스트 액터·가상 입력·테스트 무적을 생성하지 않았다.

최종 사본 용량은 44,816바이트다. 기존 Staff meta 해시 전부 일치, 6종 Ability/Payload/Motion와 meta 존재, 제작표 중복 없음, 설계·상태 링크 및 변경 C#/JSON diff 공백 검사를 확인했다. 기존 공용 런타임에서 `SpawnProjectileEvent`의 `CommitTargetTracking()` 중복 호출도 발견했으나 이번 변경에 포함하지 않았다.
