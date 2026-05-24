# 플레이어-몬스터 Hit 리액션 고도화 설계 문서

> 작성일: 2026-05-24
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 분류: 설계서(미구현 계획). 구현 PR 시 본 문서에서 가이드 문서를 별도 추출한다.
> 레퍼런스: God of War / Sekiro / Elden Ring / Devil May Cry 5 / Monster Hunter / Nioh 2 등 AAA 액션게임 히트 리액션 기법 웹 조사 결과

---

## 1. 개요 / 현재 시스템 요약

플레이어와 몬스터 간 Hit 리액션을 "게임스럽게"(타격의 무게감·만족감) 고도화하기 위한 설계 문서. AAA 액션게임의 히트 피드백 기법을 조사하고 현재 구현과의 갭을 도출해 단계별 계획으로 정리한다.

현재 프로젝트의 Hit 리액션은 이미 다음 기반을 갖추고 있다.

| 영역 | 현재 구현 | 핵심 파일 |
|------|----------|----------|
| 리액션 분류 | `AttackReactionType` 10종 (None/Light/Hit/Heavy/KnockBack/Stun/Pull/Airborne/Knockdown/Grab) | `Data/Enum/AttackInfo.cs:56` |
| 공격 데이터 | `HitPhaseData` — damage, poiseDamage, breakDamage, reactionType, reactionDuration, forceReaction, 물리력(pull/airborne/knockBack/drag), hitParticleName | `Data/Combat/CombatData.cs:28` |
| 경직 판정 | `PoiseStat` — 강인도 소진 시 Break, 하이퍼아머, 회복 (몬스터만 사용) | `Component/Common/PoiseStat.cs` |
| 피격자 처리 | `OnDamaged()` — 물리 임펄스 + 상태 전환 + 카메라 셰이크 + FX + 컬러 플래시 | `Object/Player/PlayerActor.cs:888`, `Object/Monster/MonsterActor.cs:249` |
| 공격자 피드백 | `ApplyHitFeedback()` — HitStop + 카메라 Punch/Shake + VitalOrb | `Component/Player/PlayerCombat.cs:760` |
| 히트스톱 | `GameHitStopHandler` — 전역 timeScale 요청 큐 + Actor-only animator speed + SlowMo 포스트프로세스 Volume | `Manager/Handler/Combat/GameHitStopHandler.cs` |
| 피격 플래시 | `ActorColorChanger` — MaterialPropertyBlock 빨강 플래시(0.15s) | `Component/Common/ActorColorChanger.cs:103` |
| 방향성 애니 | `GetHitAnimKey()` — 4방향 Hit_F/B/L/R + Knockback/Knockdown | `State/Player/PlayerHitState.cs:198`, `State/Enemy/EnemyHitState.cs:74` |
| 사운드 메커니즘 | `PlaySoundEvent` — MotionSet 타임라인 기반 사운드 이벤트 | `Data/Event/Animation/MotionEvent_PlaySound.cs` |

본 문서는 완전 신규 체계가 아니라 위 기존 구조를 확장한다.

---

## 2. 기존 Break / Parry 시스템과의 관계

본 설계는 다음 기존 시스템 위에 얹는다. 재발명하지 않는다.

### 이미 구현된(또는 부분 구현) 시스템 — `MONSTER_BREAK_SPECIAL_ATTACK_SYSTEM_DESIGN.md` 참조
- `HitPhaseData`/`AttackData`에 `breakDamage`, `reactionDuration`, `forceReaction`, `forceBreakExpose` 존재.
- `MonsterBreakGauge` 런타임 컴포넌트 — 누적/노출/만료/소비 처리. `EnemyBreakExposedState`로 노출 시 AI 정지.
- `PlayerStunState`, `PlayerKnockdownState`, `EnemyStunState`, `EnemyKnockdownState` 행동 불능 상태 라우팅 완료.
- `PlayerSpecialBreakAttackState`, `EnemySpecialBreakVictimState` 특수공격 1차 실행.

### 본 문서가 더하는 것
- **Poise/Break는 "리액션 진입 판정 축"으로 유지**하고, 본 문서는 그 판정 이후의 **연출 품질(히트스톱·VFX·SFX·플린치·물리)**을 고도화한다.
- 즉 "언제 경직되는가"(Break 문서)와 "경직될 때 얼마나 게임스럽게 보이고 들리는가"(본 문서)를 분리한다.

### 통합 시 보존해야 할 제약
- **Break 노출 중 피격**: `MonsterActor.OnDamaged()`는 `_breakGauge.IsExposed`일 때 일반 리액션(물리/상태전환)을 건너뛰고 플래시만 유지(`MonsterActor.cs:264`). 본 문서의 VFX/SFX 강화는 이 분기에서도 동작하되 상태 전환은 건드리지 않는다.
- **패리 경직**: `EnemyCombat`는 패리 시 `AttackReactionType.Light` staggerData로 `EnemyHitState` 진입(`MonsterActor.cs:457`). Light 연출 변경 시 패리 경직 체감도 함께 바뀜에 유의.
- **VitalOrb / KillCam**: `ApplyHitFeedback()`는 킬 히트 시 `TryKillCam`으로 조기 분기하고, 일반 히트는 `GameVitalOrb.TrySpawn` 호출(`PlayerCombat.cs:768`). 히트스톱 컨텍스트화 시 이 분기 순서를 깨지 않는다.
- **PlayerGuard 히트스톱**: 가드/패리는 `HitStopIntensity.PlayerGuard`(Actor-only 슬로우)를 쓰며 `IsParryCounterAvailable` 동안 보호된다(`PlayerCombat.cs:764`). 신규 히트스톱 로직은 이 보호를 우회하면 안 된다.
- **MotionWarp**: Hit 상태 진입 시 `controller.MotionWarp?.ClearTarget()`으로 워프를 즉시 취소(Hit 모션 우선). 가산 플린치 도입 시 이 규칙 재검토 필요.

---

## 3. 식별된 갭 (현재 구현 → 고도화 포인트)

| # | AAA 기법 | 현재 구현 상태 | 갭 / 고도화 포인트 | 관련 파일 |
|---|----------|--------------|-------------------|----------|
| G1 | 히트스톱 강도 = 타격 무게·치명 연동 (Sakurai: 데미지↑→히트스톱↑) | `ApplyHitFeedback()`이 공격자 `attackKind`(Light/Heavy/Critical)로만 강도 결정. 피격자 reactionType/poiseBreak/치명 미반영 | 피격 컨텍스트로 강도 결정. 경량 히트는 전역 timeScale 대신 `ExecuteActorOnly`(관련 두 액터만 정지)로 "현실적 히트스톱" | `PlayerCombat.cs:785`, `GameHitStopHandler.cs:165` |
| G2 | 적 공격에도 타격감 피드백 | `EnemyCombat`에는 공격자측 히트스톱이 **전혀 없음**. 적 타격 체감은 플레이어 `OnDamaged`의 셰이크/플래시뿐 | 적 강공격/특수공격 적중 시 경량 히트스톱/카메라 임펄스 추가 | `Component/Enemy/EnemyCombat.cs` |
| G3 | 상체 가산 플린치(로코모션 유지) | Light/Hit도 **전신 클립으로 교체**해 이동 중단. `PlayerHitState` 주석이 "경직 중 아무것도 못 한다"는 답답함을 직접 지적(`PlayerHitState.cs:19`) | AvatarMask 상체 가산 레이어 플린치. 프로젝트는 이미 상/하체 마스크 분리 지원 | `PlayerHitState.cs`, `EnemyHitState.cs` |
| G4 | 타격 강도/재질/방향별 임팩트 VFX + 데칼 | 단일 `hitParticleName`("LiteHit" 등 1개), 방향 무관 표시 | reactionType별 FX 티어 매핑 + hitPoint 노멀/attackDirection으로 FX 회전 + 재질(살/금속/방어구) 변형 | `CombatData.cs:51`, `OnDamaged()` |
| G5 | 레이어드 임팩트 SFX (transient+body+tail, 피치 변주, 치명 큐) | 사운드 메커니즘(`PlaySoundEvent`)은 있으나 **공격자 애니 타임라인 기반**이라 헛스윙에도 발화, 단일 클립, 피치 고정, 2D 경로는 `Debug.Log` 스텁, `PlayClipAtPoint`로 매번 임시 AudioSource 생성. **피격 연결 기반 임팩트 사운드는 부재** | 런타임 hit 기반 임팩트 사운드(reaction/재질별 + 랜덤 피치 + 레이어) + AudioSource 풀링 | `MotionEvent_PlaySound.cs`, `OnDamaged()` |
| G6 | 8방향 + 부위/높이별 리액션 | 4방향(F/B/L/R)만, 높이/부위 무관 | 8방향 + hitPoint.y로 상/중/하 구분 | `GetHitAnimKey()` (both) |
| G7 | 히트스톱 중 미세 진동·스쿼시, 게임패드 진동 | 컬러 플래시만 | 히트스톱 중 jitter/scale punch, 게임패드 rumble, 피격 방향 카메라 너지 | `ActorColorChanger.cs`, 신규 |
| G8 | 넉백 환경 상호작용 | `AddImpulse`로 밀려나기만, 벽 충돌 무반응 | 넉백 중 벽 충돌 → 바운스/추가 경직/추가 데미지 (KCC 충돌 판정 활용) | `OnDamaged()`, MovementController |
| G9 | 파워드/부분 라그돌 | 전부 클립 기반, 사망은 디졸브 | 사망 및 Heavy/Knockdown 시 물리 전환(라그돌) 후 디졸브 연계 | `DissolveController`, Death 상태 |
| G10 | 에어 저글 / 중력 스케일 | Airborne 단발 띄우기 | 공중 추가타로 부유 연장, 중력 스케일 동적 조정, 콤보 상한 | Airborne 상태, MovementController |
| G11 | 부위/약점 크리티컬 | 부위 구분 없음 | 히트박스 per-bone 태깅 → 약점 타격 시 데미지·poise·VFX·SFX 가중 + 전용 리액션 | `EnemyCombat`, 히트박스 |

---

## 4. Phase 1 — 데이터 주도 즉효 (저위험·고효과)

기존 데이터 구조(`HitPhaseData`)와 매니저(`GameHitStopHandler`)를 활용해 코드 분기·데이터만 추가하는 단계. 신규 에셋·시스템 최소화.

### 4-1. 히트스톱 컨텍스트화 (G1, G2)
- `ApplyHitFeedback()`의 강도 결정을 공격자 `attackKind` 단독 → **피격자 `reactionType` + poiseBroken + 치명 여부** 조합으로 변경.
  - 예: Light/Hit 반응 → Actor-only 히트스톱(두 액터만 정지, 월드는 유지). Heavy/KnockBack/Airborne → 전역 Heavy. 치명/처형 → Critical.
- `EnemyCombat`에 공격자측 경량 히트스톱/카메라 임펄스 훅 추가(적 강공격·특수공격 한정).
- 제약: `PlayerGuard` 보호(`IsParryCounterAvailable`)와 KillCam 조기 분기 순서를 유지한다.

### 4-2. 임팩트 VFX 티어링 + 방향 정렬 (G4)
- `HitPhaseData.hitParticleName` 단일 지정을 **reactionType별 기본 FX 매핑**으로 보강(데이터 미지정 시 reaction 티어의 기본 FX 사용).
- `OnDamaged()`의 `ShowFX` 호출 시 FX를 `attackDirection`/hit 노멀로 **회전**시켜 타격 방향성을 시각화.
- 재질 태그(살/금속/방어구)별 FX 변형 키 추가(데이터 주도).

### 4-3. 컨텍스트 임팩트 SFX (G5)
- `OnDamaged()`(피격 연결 시점)에서 **reaction/재질별 임팩트 사운드**를 재생하는 런타임 경로 추가.
- 레이어드(transient + body) + **랜덤 피치 변주** + 치명 시 전용 큐.
- 기존 `PlaySoundEvent`의 한계(타임라인 기반·헛스윙 발화·피치 고정·2D 스텁·`PlayClipAtPoint` 할당)를 보완하는 **AudioSource 풀링** 기반 재생 유틸 도입.

### 4-4. 상체 가산 플린치 (G3) — 트레이드오프 명시
- Light/Hit 반응을 **전신 상태 전환 대신 AvatarMask 상체 가산 레이어 플린치**로 처리해 로코모션을 유지.
- **트레이드오프(중요)**: `PlayerHitState`의 cancel-window(0 / 0.2s / 0.5s) 설계는 "전신 경직 중 캔슬 허용"으로 답답함을 풀고 있다(`PlayerHitState.cs:30`). Light/Hit을 가산 플린치로 바꾸면 — 이들은 **Hit 상태에 진입하지 않으므로** cancel-window 개념은 Heavy 이상 전신 경직에만 유효해진다. 두 설계가 동시에 살아있지 않도록, "Light/Hit = 가산 플린치(이동/공격 흐름 유지), Heavy+ = 전신 경직 + cancel-window"로 역할을 명확히 분리한다.
- Poise/Break 판정과 충돌하지 않도록: 가산 플린치는 연출 레이어일 뿐 행동 불능 여부는 기존 판정을 따른다.

---

## 5. Phase 2 — 방향 / 진동 / 환경

### 5-1. 8방향 + 높이별 리액션 (G6)
- `GetHitAnimKey()`를 4방향 → 8방향으로 확장하고, hitPoint.y(피격 높이)로 상/중/하 변형 선택.

### 5-2. 피격 임팩트 강화 (G7)
- 히트스톱 동안 피격자 미세 jitter / scale punch(스쿼시) 추가 — `ActorColorChanger` 확장 또는 신규 컴포넌트.
- 게임패드 진동(rumble) — reaction 강도별 패턴.
- 피격 방향 기반 카메라 너지(공격자측 Punch와 별개로 피격자/플레이어 카메라에 약한 방향 임펄스).

### 5-3. 환경 상호작용 넉백 (G8)
- KnockBack/Airborne 이동 중 벽/장애물 충돌 판정 → 바운스 또는 추가 경직/데미지. KCC 충돌 정보 활용.

---

## 6. Phase 3 — 라그돌 / 저글 / 부위 크리티컬

### 6-1. 파워드/부분 라그돌 (G9)
- 사망 및 Heavy/Knockdown 강반응 시 짧은 클립 후 물리(라그돌) 전환 → 기존 `DissolveController` 사망 디졸브와 연계.
- 부분 라그돌(상체만 물리, 하체 클립) 옵션 검토.

### 6-2. 에어 저글 / 중력 스케일 (G10)
- Airborne 상태 중 공중 추가타로 부유 시간 연장, 중력 스케일 동적 조정, 누적 공중 시간 상한으로 무한 콤보 방지.

### 6-3. 부위 / 약점 크리티컬 (G11)
- 히트박스 per-bone 태깅 → 약점 타격 시 데미지·poiseDamage·breakDamage·VFX·SFX 가중 + 전용 리액션 모션.

---

## 7. 영향받는 파일 목록

| 파일 | 관련 Phase | 변경 성격 |
|------|-----------|----------|
| `Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs` (`ApplyHitFeedback` 760) | P1 (G1) | 히트스톱 강도 결정 로직 변경 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs` | P1 (G2) | 공격자측 히트스톱/임펄스 훅 추가 |
| `Assets/02.Scripts/Manager/Handler/Combat/GameHitStopHandler.cs` (`ExecuteActorOnly` 165) | P1 (G1) | Actor-only 경로 활용 확대 |
| `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.cs` (`OnDamaged` 888) | P1 (G4,G5) | VFX 회전/티어, 임팩트 SFX 호출 |
| `Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs` (`OnDamaged` 249) | P1 (G4,G5) | VFX 회전/티어, 임팩트 SFX 호출 (Break 노출 분기 보존) |
| `Assets/02.Scripts/Data/Combat/CombatData.cs` (`HitPhaseData` 28) | P1 (G4) | reaction별 FX/재질 키 데이터 추가 |
| `Assets/02.Scripts/GameActor/State/Player/PlayerHitState.cs` | P1 (G3), P2 (G6) | 가산 플린치 분리, 8방향 확장 |
| `Assets/02.Scripts/GameActor/State/Enemy/EnemyHitState.cs` | P1 (G3), P2 (G6) | 가산 플린치 분리, 8방향 확장 |
| `Assets/02.Scripts/GameActor/Component/Common/ActorColorChanger.cs` (`OnHit` 103) | P2 (G7) | jitter/scale punch 확장 |
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_PlaySound.cs` | P1 (G5) | 풀링/피치 변주 보완(또는 신규 임팩트 사운드 유틸) |
| `DissolveController` / Death 상태 | P3 (G9) | 라그돌 전환 연계 |

> file:line은 2026-05-24 기준. 구현 전 현재 코드와 대조할 것.

---

## 8. 출처 (웹 조사)

### 히트 리액션 / 애니메이션 기술
- Witcher Combat - Hit reaction by body part (UE4): https://www.youtube.com/watch?v=zanLmcNbsrs
- Reactive Melee Combat — IK-based animation solution (PDF): https://assets.ctfassets.net/y4twieuxp19i/2wqlZuwkIgg86cCckQcQYY/25e752b714b77289fa2262400a3c99db/Paper.pdf
- Animancer - Layers: https://kybernetik.com.au/animancer/docs/manual/blending/layers/
- Ragdoll physics - Wikipedia: https://en.wikipedia.org/wiki/Ragdoll_physics
- Posture - Sekiro Wiki: https://sekiroshadowsdietwice.wiki.fextralife.com/Posture
- Stance - Elden Ring Wiki: https://eldenring.wiki.fextralife.com/Stance
- Toughness - Nioh 2 Wiki: https://nioh2.wiki.fextralife.com/Toughness
- DMC3 Launchers: https://intothebluesky.com/2021/03/20/devil-may-cry-files-04-dmc3-launchers/
- God of War Ragnarok Stagger: https://www.newsweek.com/god-war-ragnarok-stagger-guide-1757672

### 히트스톱 / 게임 필
- Ahmad — A More Realistic HitStop: https://www.ahmadmohammadnejad.com/sandbox/a-more-realistic-hitstop
- Sakurai — Thinking About Hitstop: https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/
- Hitstop/Hitfreeze/Hitlag — CritPoints: https://critpoints.net/2017/05/17/hitstophitfreezehitlaghitpausehitshit/
- Jan Willem Nijman (Vlambeer) — The Art of Screenshake: https://www.youtube.com/watch?v=AJdEqssNZ-U
- The art of screenshake (notes): http://notebook.maryrosecook.com/Theartofscreenshake,JanWillemNijman.html

### VFX / 오디오 / 카메라 / 포스트프로세스
- DMC5 Director on combat feel (Kotaku): https://kotaku.com/devil-may-cry-5s-director-tells-us-how-they-made-combat-1833642299
- How 3rd person melee games communicate hit feel — Jason de Heras: https://www.jasondeheras.com/gamedesign/2021/4/23/how-do-3rd-person-melee-games-communicate-game-and-hit-feel
- Cinemachine Impulse: https://docs.unity3d.com/Packages/com.unity.cinemachine@2.3/manual/CinemachineImpulse.html
- GDC Vault — Oh My! That Sound Made the Game Feel Better!: https://gdcvault.com/play/1022808/Oh-My-That-Sound-Made
- Effects in Hades (80.lv): https://80.lv/articles/a-behind-the-scenes-look-at-the-effects-in-hades
- Juicy damage UI feedback — Lennart Nacke: https://acagamic.medium.com/juicy-damage-feedback-in-games-7c1758d69a42
- Custom Post Processing in URP — Febucci: https://blog.febucci.com/2022/05/custom-post-processing-in-urp/
- Designing Game Feel: A Survey (arXiv): https://arxiv.org/pdf/2011.09201

---

## 9. 구현 우선순위 요약

| 우선순위 | 항목 | 근거 |
|---------|------|------|
| 1 | P1 히트스톱 컨텍스트화 (G1, G2) | 기존 매니저 활용, 코드 분기만으로 타격감 즉시 개선 |
| 2 | P1 임팩트 SFX (G5) | 피격 연결 사운드 부재 — 체감 격차가 가장 큼 |
| 3 | P1 VFX 티어링/방향 (G4) | 데이터 주도, 저위험 |
| 4 | P1 상체 가산 플린치 (G3) | 답답함 해소 효과 크나 cancel-window 설계 조정 동반 |
| 5 | P2 8방향/진동/환경 (G6~G8) | 추가 애니/입력 작업 필요 |
| 6 | P3 라그돌/저글/부위 (G9~G11) | 신규 시스템·큰 작업량 |
