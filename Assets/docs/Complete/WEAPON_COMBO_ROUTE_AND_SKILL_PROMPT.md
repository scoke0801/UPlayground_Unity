# 무기 타입별 콤보 라우트 매핑 & 스킬 키 제시(HUD) 설계

> **보관 문서 주의:** 이 문서는 Ability 전환 전 콤보/스킬 구조의 작업 기록이다. 현재 단일 소스는 `AbilitySetSO`이며 최신 기준은 `../TODO/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`를 따른다.

> 상태: **설계 + HUD 위젯 코드 1차 작성**(2026-06-05). Unity 컴파일/플레이 검증 대기(이 환경 CLI 빌드 없음).
> 레퍼런스: **명조(Wuthering Waves)** 입력 트리거 구조.
> 선행: 연계 라우트 엔진 = [PLAYER_COMBAT_SKILL_LINK_SYSTEM_DESIGN.md](../Complete/PLAYER_COMBAT_SKILL_LINK_SYSTEM_DESIGN.md)(Phase 1/1.1 구현됨).

---

## 0. 이 문서가 다루는 것

1. **무기 타입별 콤보 → 연계스킬 매핑표**(§2~3) — 이미 구현된 입력 토큰(약/강/차지/점프/대시/스킬)만으로 11개 무기 타입의 스타일을 분기.
2. **스킬 키 제시 HUD(방식1, 명조식 상태 글로우)**(§4) — 현재 입력열이 어떤 라우트의 도중인지 판정해, **다음에 누를 키**를 글리프로 띄워준다.

> ⚠ 무기축 게이팅(A안: 무기 GameplayTag)은 **미구현**. 현재 `comboRoutes`는 캐릭터(`PlayerAttackDataSO`)에 귀속된다. §3.3 참조.

---

## 1. 명조(Wuthering Waves) 입력 트리거 — 웹 조사 요약

핵심 철학은 **입력 오버로딩(context-sensitive input)**: 버튼 수를 늘리지 않고, **같은 버튼이 맥락(타이밍/홀드/공중/회피 직후)에 따라 다른 액션으로 분기**한다.

| 트리거 | 입력/맥락 | 결과 |
|---|---|---|
| 기본공격 N1→N5 | 기본공격 **연타** | 콤보 단계 진행 |
| 강공(Heavy) | 기본공격 **홀드** | 스태미나 소모 강타 |
| 공중공격/강하 | **공중에서** 기본공격 | 캐릭터별 공중 변형 |
| 닷지 카운터 | **회피 직후** 기본공격 | 반격 |
| 강화 후속 | **특정 단계의 타이밍 윈도우** 내 재입력(예: Emotion Particle) | 강화 추가타 |
| 공명스킬 | 쿨다운제, 콤보 사이 위빙 | 버스트 |
| 공명해방(궁) | 게이지 풀 | 궁극기 |
| 패링 | 적 공격의 **빛 링이 가장 밝을 때** 공격 | 마비 |

**키 "제시" 방식**: 명조는 격투게임식 커맨드 리스트를 띄우지 **않는다.** 대신
- **상태 글로우** — 포르테/콘체르토가 차면 스킬·초상화 아이콘이 빛나 "지금 이 입력이 특별하다"를 알림.
- **월드 텔레그래프** — 파티클/링 VFX로 타이밍 윈도우를 알림.

→ 본작의 HUD도 "**무엇을 눌러라**"가 아니라 "**지금 이 콤보의 다음은 이 키**"를 글로우로 보여주는 방식(§4)을 채택한다.

출처: [Game8 Combat Guide](https://game8.co/games/Wuthering-Waves/archives/452894), [Fandom: Combat](https://wutheringwaves.fandom.com/wiki/Combat), [Fandom: Resonance Skill](https://wutheringwaves.fandom.com/wiki/Resonance_Skill), [Wutheringlab](https://wutheringlab.com/guide/wuthering-waves-combat-system-guide/).

---

## 2. 입력 토큰 매핑 (이미 존재)

사용자 표현 ↔ 엔진 토큰(`ComboInputToken`) ↔ 입력 액션(글리프 표시용):

| 표현 | 토큰 | 약어 | 입력 액션(`PlayerAction`) |
|---|---|---|---|
| 약공 | `LightAttack` | L | `Attack` |
| 강공 | `HeavyAttack` | H | `HeavyAttack` |
| 강공 홀드(차지) | `Charge` | C | `HeavyAttack` (홀드) |
| 점프 | `Jump` | J | `Jump` |
| 대시(회피) | `Dodge` | D | `Dodge` |
| 스킬1 | `Skill1` | S1 | `SkillAbility` |
| 스킬2 | `Skill2` | S2 | `SkillUltimate` |

명조 대응: L=Basic, H/C=Heavy, D+L=닷지 카운터, J+L=공중공격, S1/S2=공명스킬/해방.

---

## 3. 무기 타입별 콤보 → 연계스킬 매핑표

`WeaponType` 11종. 라우트 패턴은 **왼→오 입력 순서**, Suffix 매칭(긴 콤보 끝에서도 짧은 라우트 성립). 모두 기존 토큰만 사용.

| 무기 | 스타일 | 라우트 패턴 | 발동 스킬(예시) | 명조 대응 |
|---|---|---|---|---|
| **Sword** 한손검 | 균형 | `L L L H` | 마무리 강타(피니셔) | Basic→Heavy 캡 |
| | | `D L` | 스텝인 베기 | 닷지 카운터 |
| | | `L L S1` | 약콤보 스킬 캔슬 | 콤보 위빙 |
| **SwordShield** 검방패 | 방어 | `L L H` | 방패 밀치기(경직) | Heavy 셋업 |
| | | `D H` | 방패 돌진 | 닷지 카운터 |
| | | `L L L S1` | 가드 카운터 스킬 | 콤보 위빙 |
| **GreatSword** 대검 | 중량 | `L L C` | 차지 대회전 | hold-Heavy |
| | | `D H` | 돌진 내려베기 | 닷지 카운터 |
| | | `H H` | 연속 강타(느림) | Heavy 체인 |
| **Staff** 지팡이 | 캐스터 | `L L L C` | 차지 폭발 마법 | hold-Heavy |
| | | `D S1` | 백스텝 캐스트 | 카이팅 |
| | | `H`(홀드=C) | 차지 빔 | Heavy 차지 |
| **Bow** 활 | 원거리 | `C` | 차지샷 | hold-Heavy |
| | | `D H` | 백스텝 사격 | 카이팅 |
| | | `J L` | 공중 정밀사격 | 공중공격 |
| **Arrow** | Bow 계열 | (Bow와 동일군) | — | — |
| **Katana** 카타나 | 고속/거합 | `L L L L H` | 거합 베기 | Basic 다단→Heavy |
| | | `D L` | 발도 카운터 | 닷지 카운터 |
| | | `L L S1` | 이아이 스킬 | 콤보 위빙 |
| **DoubleAxe** 쌍도끼 | 광폭/중량 | `H H H` | 광폭 연타 | Heavy 체인 |
| | | `L L C` | 차지 회전베기 | hold-Heavy |
| | | `D H` | 도약 내려찍기 | 닷지 카운터 |
| **Whip** 채찍 | 광역/리치 | `L L L H` | 끌어당기기(Pull) | Heavy 캡 |
| | | `D L` | 회피 후 견제 | 닷지 카운터 |
| | | `L L S1` | 포박 스킬 | 콤보 위빙 |
| **Spear** 창 | 리치/찌르기 | `L L C` | 차지 관통 찌르기 | hold-Heavy |
| | | `J L` | 공중 내려찍기 | 공중공격 |
| | | `D H` | 돌진 찌르기 | 닷지 카운터 |
| **DualBlade** 쌍검 | 연속/속공 | `L L L L L H` | 난무 피니셔 | Basic 다단→Heavy |
| | | `D L L` | 교차 베기 | 닷지 카운터 |
| | | `L L S1` | 난무 스킬 | 콤보 위빙 |

### 3.1 저작 방법(현재)
각 라우트는 캐릭터의 `PlayerAttackDataSO.comboRoutes`에 `ComboRouteEntry`로 등록한다. `PlayerAttackDataSODrawer`의 "연계" 탭(시각 편집 + 진단 + 시뮬레이터)에서 패턴/조건/실행공격을 채운다.

위 표의 스킬명("거합 베기" 등)은 **`ComboRouteEntry.displayName`**(플레이어 노출용, HUD 표시)에 적는다. 비우면 `routeName`(에디터 식별자, 기본 "New Route")으로 폴백하므로 HUD에 의미 있는 텍스트를 보이려면 displayName을 채울 것.

### 3.2 공중 라우트 주의
`J L` 같은 공중 라우트의 `animKey`는 **해당 캐릭터 MotionSet에 실제로 존재**해야 한다(공중 호스트는 모션 사전검사가 없음 — 선행 문서 §"알려진 비대칭" 참조).

### 3.3 무기축 게이팅 — 미구현(A안 제안)
현재 라우트는 캐릭터 단위라 "무기별 다른 콤보"는 **단일 무기 캐릭터에선 그대로 동작**하지만, 한 캐릭터가 무기를 바꾸면 분기되지 않는다. 무기별 분기는 다음 중 하나로 확장:
- **A안(권장)**: 무기 GameplayTag(`State.Weapon.Katana` 등) 신설 → `PlayerEquipment.SetWeaponType`에서 태그 세팅 → `ComboRouteEntry.requiredTagIds`로 라우트 게이트. 기존 태그 인프라 재사용, 신규 시스템 0. 태그는 Tag Registry 생성기로 추가.
- **B안**: `WeaponDefinitionSO`가 자체 `comboRoutes`를 들고 `PlayerCombat.ComboRoutes`가 장착 무기 셋과 머지.

---

## 4. 스킬 키 제시 HUD (방식1 — 명조식 상태 글로우)

### 4.1 동작
현재 입력 토큰 윈도우(`ComboInputTracker`)가 **어떤 라우트의 도중(prefix)** 인지 판정하고, 그 라우트를 **완성/전진시키는 다음 토큰**을 글리프로 띄운다. 라우트가 여러 개면 각 분기를 한 줄씩(예: 다음에 `강공`→대회전 / `스킬1`→이아이).

명조처럼 "지금 이 순간 누를 수 있는 특별 입력"만 노출 — 정적 커맨드 리스트가 아니다.

### 4.2 매칭 로직 — `ComboRouteResolver.CollectHints`(순수 static, 신규)
실행/에디터 공유를 위해 기존 Resolver에 추가. 라우트별로:
1. `IsExecutable` / 태그 / 지상·공중 / 자원(`CanAffordRoute`) 게이트 통과(= 실제로 발동 가능한 라우트만 힌트).
2. **스트림 접미 ↔ 패턴 접두 최대 겹침 길이 k** 계산.
3. `1 ≤ k < patternLen`이면 → 다음 토큰 = `pattern[k]`를 힌트로 수집.
   - `k=0`(아직 시작 안 함)·`k=len`(이미 완성, 트래커가 Clear) 제외 → 노이즈 없음.

예: 라우트 `L L L H`, 현재 윈도우 `[L L]` → k=2 → 다음 `약공`. 윈도우 `[L L L]` → k=3 → 다음 `강공`.

**의도적 결정 — 중립 상태에선 힌트 없음(`k≥1`):** 명조는 포르테가 차면 중립에서도 스킬 아이콘을 글로우하지만, 본 HUD는 **이미 라우트 접두를 1토큰 이상 입력한 뒤에만** 다음 키를 띄운다(콤보 도중 안내에 집중, 중립 노이즈 제거). "중립에서 콤보 시작 키 안내"가 필요하면 `k≥0` 분기를 별도 모드로 추가.

**알려진 한계:** 힌트 갱신 시그니처가 (토큰 윈도우 + 지상/공중)만 키로 쓴다 → 윈도우 불변인데 게이지만 바뀌면 자원 게이팅 힌트가 한 박자 늦게 갱신될 수 있다(실전에선 게이지 변화가 공격과 동반되므로 거의 무해).

### 4.3 위젯 구성(신규 스크립트)
- `ComboTokenInput`(static) — 토큰 → (`PlayerAction` 액션명, 홀드 여부). 글리프 해석용.
- `UIComboRouteHintRow` — 한 분기 = 글리프 1개(`UIInputPromptIcon` 재사용, 디바이스 자동 전환) + 스킬명 라벨.
- `UIComboRouteHint` — 호스트. `PartyManager.ActiveCharacter` 구독(`OnSwapCompleted`), 매 프레임 윈도우 시그니처가 바뀔 때만 `CollectHints` 재계산 후 행 풀 갱신(SetAction 핫호출 방지).

### 4.5 채택 방향 — 고정 스킬바 + 슬롯 글로우 통합 (명조 사진 레퍼런스)

사용자 레퍼런스(명조 인게임 HUD): **우측 파티 패널 + 하단 고정 스킬바(키캡 + 글로우)**.
- **우측 파티 패널은 이미 존재** — `UI/HUD/Party/UIHudPartyEntry`(초상화/HP/스킬게이지 풀 글로우=`IsSkillGaugeFull`+`_glowObject`/스왑쿨다운+숫자). 프리팹 배선만 필요.
- **하단 스킬바는 신규**. 별도 힌트 행 대신 **스킬바 슬롯의 글로우를 `CollectHints`로 구동**(명조 동작과 동일).

**글로우/게이지 표현(최종)**:
- **ReadyGlow + ComboGlow** ← 둘 다 `CollectHints`의 다음 토큰이 이 슬롯일 때 켠다(콤보 도중 다음 키 강조). 게이지 충족과 무관.
- **게이지(자원)** = 글로우/fill 없이 **dim**으로만(부족 시 어둡게). 공유 게이지의 연속 fill은 파티 패널이 담당.
- ※초기 설계는 ReadyGlow=게이지충족(사진 R발광)이었으나, 사용자 요청으로 글로우를 콤보 힌트 전용으로, 게이지 fill 제거 → dim만 유지.

**신규 코드**:
- `UI/InputPrompt/UISkillSlot.cs` — 슬롯 1개. `ComboInputToken`으로 정의(키캡/힌트매칭/게이지슬롯 일원화). 아이콘+키캡(`UIInputPromptIcon` 재사용)+ReadyGlow/ComboGlow(콤보힌트)+dim(게이지).
- `UI/InputPrompt/UI_HUD_Skill.cs` — 호스트(**`UI_Base` 상속**, UIManager 생명주기). `PartyManager` 활성캐릭터 추적, 게이지는 `OnGaugeChanged` 이벤트 구동, 콤보 힌트는 입력 윈도우 시그니처 변화 시에만 재계산.

**UI_Base 등록 절차(에디터)**: ① HudSkill 프리팹(Canvas + `UI_HUD_Skill` + `UISkillSlot` 슬롯들) ② `UIPrefabDatabase`에 key `"HudSkill"` 항목 추가 ③ ID Enum Generator로 `UIKeyType` 재생성(`UIKeyType.cs`는 자동생성 — 손편집 금지) ④ `UI_HUD_GamePlay.OnShow`에 `ShowUI(UIKeyType.HudSkill)`, `OnHide`에 `HideUI(UIKeyType.HudSkill)` 추가(`UI_HUD_Party` 패턴, 재생성 후 컴파일). UI_HUD_GamePlay는 global ns → `using UPlayGround.UI.InputPrompt;` 필요.

**스킬 데이터 사실**(설계 근거):
- 스킬은 **쿨다운 없음**. `PlayerSkillGauge`의 **게이지 비용**(`_skillCost[slot]`, `CanUseSkill`)으로만 게이팅 → Ready = 게이지 충족.
- 토큰→게이지 슬롯: `Skill1→0`, `Skill2→1`(슬롯별 `_gaugeSlotOverride`로 교정 가능). `ExecuteSkillAttack`은 `skillAttackList[index]`(캐릭터별)을 쓰며, 게이지 슬롯과 1:1 가정.
- **스킬 아이콘 파이프라인 부재**(`AbilityAttackInfo`에 icon 필드 없음) → v1은 **슬롯 프리팹에 아이콘 직렬화**. ⚠ 캐릭터 교체 시 아이콘은 안 바뀜(스왑 미추적). 추후 캐릭터별 스킬 아이콘 소스(예: `skillAttackList`에 icon 필드 추가) 도입 시 스왑 대응.

**`UIComboRouteHint`(지난 턴 행 위젯)와의 관계**: 둘 다 `CollectHints`를 두뇌로 공유하는 **대체 출력**이다. 사진처럼 가려면 **스킬바 슬롯 글로우(`UI_HUD_Skill`)**를 쓰고, 행 위젯은 미사용. 한 HUD에 둘을 동시에 라이브로 두지 말 것.

### 4.4 Unity 에디터 잔여 작업(코드만으로 안 됨)
1. HUD 캔버스에 `UIComboRouteHint` 프리팹 배치, 행 컨테이너/템플릿/`InputGlyphDataSO` 연결.
2. `UIComboRouteHintRow` 템플릿 프리팹(아이콘+라벨) 작성.
3. 글로우 연출(머티리얼/애니메이션)은 행 프리팹에서 처리(코드는 표시/숨김만).
4. 런타임 검증: 콤보 중 다음 키가 올바르게 뜨고, 무기/캐릭터 교체·공중 전환 시 갱신되는지.

---

## 5. 향후
- A안(무기 태그) 구현 시 본 매핑표가 그대로 무기별 라우트로 분리됨.
- 방식2(정적 커맨드 리스트)는 스킬/연습 화면 보조용으로 별도.
- `ActorRuntimeMonitorWindow` 토큰 윈도우 컬럼(선행 문서 Phase 3 잔여)과 동일 데이터 소스.
