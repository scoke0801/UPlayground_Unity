# 설계 폴더 문서 분류

`Assets/docs/`의 설계·요청·조사·시안 문서를 **종류**와 **분야**로 나눈 지도다. 2026-11-13 두 차례 정리했다: 1차로 작업 상태·검증 문서를 `docs/work-status/`로, 가이드를 `Assets/docs/guide/`로 옮겼고, 2차로 `Assets/docs/design/`에 섞여 있던 문서를 역할별 폴더(`design/`·`requests/`·`research/`·`reference/`)로 나눴다. 문서 안과 다른 문서의 링크·경로 표기는 새 위치로 고쳤다.

- 설계 정본은 종류가 `설계`인 문서다. 실제 구현·검증 상태는 `작업 상태·인계`·`검증` 문서와 코드·에셋을 대조해 판단한다.
- 가이드(저작·운영 지침) 성격 문서는 이 위키에 연결하지 않는다. 아래 [가이드 성격 문서](#가이드-성격-문서-미연결)에 이동 내역만 적었다.
- 연결 열의 `기존`은 이번 조사 전에 이미 위키·work-status에 링크되어 있던 문서, `추가`는 이번에 연결한 문서다.

분야별 현재 판단은 [전투](combat.md) · [콘텐츠](content.md) · [NPC](npc.md) · [월드·아트](world-art.md) · [UI·플레이어](player-ui.md) · [개발](development.md) 페이지를 먼저 본다.

## 폴더 구조

역할이 같은 문서는 한 폴더에 모았고, 폴더마다 한국어 `README.md`가 문서 목록과 읽는 순서를 안내한다.

| 폴더 | 역할 | 분야 |
|---|---|---|
| [design/](../../Assets/docs/design/README.md) | **설계 정본.** 아래 분야 폴더로 나뉜다 | — |
| ↳ [combat/](../../Assets/docs/design/combat/README.md) | 몬스터·플레이어 전투, 교전 구도 | 전투 |
| ↳ [content/](../../Assets/docs/design/content/README.md) | 스토리·퀘스트·세계관·대화 연출. `scenario-gameplay/`는 00~08 번호순 묶음 | 콘텐츠 |
| ↳ [npc/](../../Assets/docs/design/npc/README.md) | NPC 생활 AI·외형 | NPC |
| ↳ [ui-player/](../../Assets/docs/design/ui-player/README.md) | 입력 UI·파티·펫·HUD | UI·플레이어 |
| ↳ [world-art/](../../Assets/docs/design/world-art/README.md) | 환경·지형 테마·연속 월드·호수 마을 | 월드·아트 |
| ↳ [dev-tools/](../../Assets/docs/design/dev-tools/README.md) | 에디터·프레임워크 구조·디버그 도구 | 개발 |
| [requests/](../../Assets/docs/requests/README.md) | 작업 요청서·프롬프트(현재 지시 아님) | 전 분야 |
| [research/](../../Assets/docs/research/README.md) | 조사·근거 자료 | 전 분야 |
| [reference/](../../Assets/docs/reference/README.md) | 시안·참조 이미지(정본 아님) | 전 분야 |
| [work-status/](../work-status/README.md) | 구현 진행·인계·검증 기록 | 전 분야 |
| `Assets/docs/guide/` | 저작·운영 지침. 위키에 연결하지 않음 | — |

`design/`의 분류 기준은 문서 안의 머리말·제목이며, 설계와 구현 상태가 섞인 문서(예: 전투 구도 설계)는 설계로 분류했다.

## 설계

무엇을 어떻게 만들지 정한 기획·구조·계약. 구현 상태는 근거 문서(work-status)에서 확인한다.

| 문서 | 분야 | 연결 |
|---|---|---|
| [P09 NPC 의상·외형 베리에이션 확장 설계](../../Assets/docs/design/npc/NPC_APPEARANCE_VARIATION_DESIGN.md) · `design/npc/NPC_APPEARANCE_VARIATION_DESIGN.md` | NPC | 추가 |
| [NPC 생활 AI 확장 설계](../../Assets/docs/design/npc/NPC_LIFE_AI_DESIGN.md) · `design/npc/NPC_LIFE_AI_DESIGN.md` | NPC | 추가 |
| [NPC 인물 배치와 P09 외형 제작 명세](../../Assets/docs/design/content/scenario-gameplay/03_NPC_AND_P09.md) · `design/content/scenario-gameplay/03_NPC_AND_P09.md` | NPC | 추가 |
| [UPlayground · 캐릭터별 스킬 게이지 & HUD 기획](../../Assets/docs/design/ui-player/hud-character-gauge/HUD_CHARACTER_GAUGE_DESIGN.html) · `design/ui-player/hud-character-gauge/HUD_CHARACTER_GAUGE_DESIGN.html` | UI·플레이어 | 추가 |
| [입력 키 프롬프트 UI 시스템 설계 문서](../../Assets/docs/design/ui-player/INPUT_PROMPT_UI_SYSTEM_DESIGN.md) · `design/ui-player/INPUT_PROMPT_UI_SYSTEM_DESIGN.md` | UI·플레이어 | 추가 |
| [파티 체력 회복 인터렉션 오브젝트 설계 문서](../../Assets/docs/design/ui-player/PARTY_HEAL_REST_POINT_SYSTEM_DESIGN.md) · `design/ui-player/PARTY_HEAL_REST_POINT_SYSTEM_DESIGN.md` | UI·플레이어 | 추가 |
| [원숭이 동행 펫 시스템 설계](../../Assets/docs/design/ui-player/pet-system/PET_SYSTEM_DESIGN.html) · `design/ui-player/pet-system/PET_SYSTEM_DESIGN.html` | UI·플레이어 | 추가 |
| [펫 시스템 2차 설계 — 추종·탑승·스킬 성장](../../Assets/docs/design/ui-player/pet-system/PET_SYSTEM_V2_DESIGN.html) · `design/ui-player/pet-system/PET_SYSTEM_V2_DESIGN.html` | UI·플레이어 | 기존 |
| [BT 에디터 디자인 개선 제안(UI 설계안)](../../Assets/docs/design/dev-tools/BT%20Editor%20Design%20Guide.html) · `design/dev-tools/BT Editor Design Guide.html` | 개발 | 추가 |
| [카메라 프레임워크 구조](../../Assets/docs/design/dev-tools/CAMERA_FRAMEWORK_STRUCTURE_DESIGN.html) · `design/dev-tools/CAMERA_FRAMEWORK_STRUCTURE_DESIGN.html` | 개발 | 추가 |
| [디버깅 기즈모 시스템 설계](../../Assets/docs/design/dev-tools/DEBUG_GIZMO_SYSTEM_DESIGN.md) · `design/dev-tools/DEBUG_GIZMO_SYSTEM_DESIGN.md` | 개발 | 추가 |
| [CPU 야생 생물과 GPU 환경 군집의 역할 분리](../../Assets/docs/design/world-art/environment/AMBIENT_FAUNA_ROLE_SEPARATION.md) · `design/world-art/environment/AMBIENT_FAUNA_ROLE_SEPARATION.md` | 월드·아트 | 기존 |
| [Anime Character Auto Shading Generator 설계 문서](../../Assets/docs/design/world-art/environment/ANIME_AUTO_SHADING_GENERATOR_DESIGN.md) · `design/world-art/environment/ANIME_AUTO_SHADING_GENERATOR_DESIGN.md` | 월드·아트 | 추가 |
| [생명의 호수 생활 공간·가뭄 마을 확장 기획](../../Assets/docs/design/world-art/lake-of-life-world/LAKE_OF_LIFE_LIVING_WORLD_PLAN.md) · `design/world-art/lake-of-life-world/LAKE_OF_LIFE_LIVING_WORLD_PLAN.md` | 월드·아트 | 추가 |
| [호수 마을 월드 비주얼 품질 개선 설계](../../Assets/docs/design/world-art/lake-of-life-world/LAKE_VILLAGE_VISUAL_QUALITY_DESIGN.md) · `design/world-art/lake-of-life-world/LAKE_VILLAGE_VISUAL_QUALITY_DESIGN.md` | 월드·아트 | 추가 |
| [시나리오 지역의 연속 월드 통합](../../Assets/docs/design/world-art/open-world/OPEN_WORLD_INTEGRATION_PLAN.md) · `design/world-art/open-world/OPEN_WORLD_INTEGRATION_PLAN.md` | 월드·아트 | 추가 |
| [반응형 환경 통합 계약](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md) · `design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md` | 월드·아트 | 기존 |
| [지역 하늘·대기와 구름 2차 폴리싱](../../Assets/docs/design/world-art/environment/REGIONAL_SKY_ATMOSPHERE_DESIGN.html) · `design/world-art/environment/REGIONAL_SKY_ATMOSPHERE_DESIGN.html` | 월드·아트 | 기존 |
| [독립 정규 월드 제작 설계안](../../Assets/docs/design/world-art/open-world/REGULAR_WORLD_PRODUCTION_DESIGN.html) · `design/world-art/open-world/REGULAR_WORLD_PRODUCTION_DESIGN.html` | 월드·아트 | 추가 |
| [생명의 호수 구역별 건물과 오브젝트 배치 명세](../../Assets/docs/design/content/scenario-gameplay/02_WORLD_AND_PLACEMENT.md) · `design/content/scenario-gameplay/02_WORLD_AND_PLACEMENT.md` | 월드·아트 | 추가 |
| [시나리오 오브젝트와 이미지 연출 제작 명세](../../Assets/docs/design/content/scenario-gameplay/05_ART_PRODUCTION.md) · `design/content/scenario-gameplay/05_ART_PRODUCTION.md` | 월드·아트 | 추가 |
| [툰 워터 월드 — 기존 시스템과 렌더링 통합 계약](../../Assets/docs/design/world-art/environment/TOON_WATER_EXISTING_INTEGRATION.md) · `design/world-art/environment/TOON_WATER_EXISTING_INTEGRATION.md` | 월드·아트 | 기존 |
| [실제 맵 기반 세계관과 생명의 호수 지도](../../Assets/docs/design/content/world-setting/WORLD_ATLAS_CONCEPT_DRAFT.md) · `design/content/world-setting/WORLD_ATLAS_CONCEPT_DRAFT.md` | 월드·아트 | 추가 |
| [생명의 호수에서 이어지는 월드 확장 콘셉트](../../Assets/docs/design/content/world-setting/WORLD_EXPANSION_CONCEPT.md) · `design/content/world-setting/WORLD_EXPANSION_CONCEPT.md` | 월드·아트 | 추가 |
| [월드 테마 지형 제작·통합 설계](../../Assets/docs/design/world-art/terrain-theme/WORLD_THEME_TERRAIN_PRODUCTION_DESIGN.html) · `design/world-art/terrain-theme/WORLD_THEME_TERRAIN_PRODUCTION_DESIGN.html` | 월드·아트 | 추가 |
| [테마 지형 재작업 설계 — 미술 목표 미달과 재제작 기준](../../Assets/docs/design/world-art/terrain-theme/WORLD_THEME_TERRAIN_REWORK_DESIGN.html) · `design/world-art/terrain-theme/WORLD_THEME_TERRAIN_REWORK_DESIGN.html` | 월드·아트 | 기존 |
| [V2 환경 미술 확장 설계](../../Assets/docs/design/world-art/terrain-theme/WORLD_THEME_VISUAL_V2_EXPANSION_DESIGN.html) · `design/world-art/terrain-theme/WORLD_THEME_VISUAL_V2_EXPANSION_DESIGN.html` | 월드·아트 | 추가 |
| [보스 다단계 행동 계획(Hierarchical Plan) 설계 문서](../../Assets/docs/design/combat/monster/BOSS_HIERARCHICAL_PLAN_DESIGN.md) · `design/combat/monster/BOSS_HIERARCHICAL_PLAN_DESIGN.md` | 전투 | 추가 |
| [캐릭터 스왑 회피 / 카운터 시스템 설계 문서](../../Assets/docs/design/combat/player/CHARACTER_SWAP_EVADE_SYSTEM_DESIGN.md) · `design/combat/player/CHARACTER_SWAP_EVADE_SYSTEM_DESIGN.md` | 전투 | 추가 |
| [캐릭터 스왑 잔류 공격 시스템 설계 문서](../../Assets/docs/design/combat/player/CHARACTER_SWAP_RESIDUAL_ATTACK_SYSTEM_DESIGN.md) · `design/combat/player/CHARACTER_SWAP_RESIDUAL_ATTACK_SYSTEM_DESIGN.md` | 전투 | 추가 |
| [전투 구도 개선 설계 — 소탕전·결투전·카메라 구도 (이름은 GUIDE이나 설계·상태 혼합)](../../Assets/docs/design/combat/encounter/COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md) · `design/combat/encounter/COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md` | 전투 | 추가 |
| [적 스태미나 모델 설계 문서](../../Assets/docs/design/combat/monster/ENEMY_STAMINA_MODEL_DESIGN.md) · `design/combat/monster/ENEMY_STAMINA_MODEL_DESIGN.md` | 전투 | 추가 |
| [휴머노이드 캐릭터 보스전 설계 — Bokusei / Siuha / Lian / Hichi / Lili](../../Assets/docs/design/combat/monster/HUMANOID_CHARACTER_BOSS_FIGHT_DESIGN.md) · `design/combat/monster/HUMANOID_CHARACTER_BOSS_FIGHT_DESIGN.md` | 전투 | 추가 |
| [근거리 몬스터 AI 설계 — "단련된 검사" (Behavior Tree / Rules JSON 기준)](../../Assets/docs/design/combat/monster/MONSTER_AI_MELEE_SWORDSMAN_DESIGN_KR.md) · `design/combat/monster/MONSTER_AI_MELEE_SWORDSMAN_DESIGN_KR.md` | 전투 | 추가 |
| [몬스터 브레이크 / 행동 불능 / 특수공격 시스템 기획 문서](../../Assets/docs/design/combat/monster/MONSTER_BREAK_SPECIAL_ATTACK_SYSTEM_DESIGN.md) · `design/combat/monster/MONSTER_BREAK_SPECIAL_ATTACK_SYSTEM_DESIGN.md` | 전투 | 추가 |
| [몬스터 Intent 가중치 외부화 설계 문서](../../Assets/docs/design/combat/monster/MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md) · `design/combat/monster/MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md` | 전투 | 추가 |
| [몬스터 진영 전투 및 공동전투 영입 조우 구현 스펙](../../Assets/docs/design/combat/monster/MONSTER_VS_MONSTER_RECRUITMENT_ENCOUNTER_SPEC.md) · `design/combat/monster/MONSTER_VS_MONSTER_RECRUITMENT_ENCOUNTER_SPEC.md` | 전투 | 추가 |
| [플레이어 스킬 시스템 개선 설계 문서](../../Assets/docs/design/combat/player/PLAYER_SKILL_SYSTEM_REDESIGN_PLAN.md) · `design/combat/player/PLAYER_SKILL_SYSTEM_REDESIGN_PLAN.md` | 전투 | 추가 |
| [궁극기 연출 시스템 설계 문서](../../Assets/docs/design/combat/player/ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md) · `design/combat/player/ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md` | 전투 | 추가 |
| [연출 스테이지 시스템 설계 v2](../../Assets/docs/design/content/presentation/CINEMATIC_STAGE_SYSTEM_DESIGN.html) · `design/content/presentation/CINEMATIC_STAGE_SYSTEM_DESIGN.html` | 콘텐츠 | 추가 |
| [연출 스테이지(Cinematic Stage) 시스템 설계 문서](../../Assets/docs/design/content/presentation/CINEMATIC_STAGE_SYSTEM_DESIGN.md) · `design/content/presentation/CINEMATIC_STAGE_SYSTEM_DESIGN.md` | 콘텐츠 | 추가 |
| [생명의 호수 DLG 연출 적용 설계](../../Assets/docs/design/content/presentation/LAKE_DIALOGUE_PRESENTATION_DESIGN.md) · `design/content/presentation/LAKE_DIALOGUE_PRESENTATION_DESIGN.md` | 콘텐츠 | 추가 |
| [생명의 호수 제1장 수렴형 플로우 구현 스펙](../../Assets/docs/design/content/lake-of-life/LAKE_OF_LIFE_CHAPTER1_CONVERGENCE_FLOW_SPEC.md) · `design/content/lake-of-life/LAKE_OF_LIFE_CHAPTER1_CONVERGENCE_FLOW_SPEC.md` | 콘텐츠 | 추가 |
| [생명의 호수 제1장 곁가지 퀘스트 스펙](../../Assets/docs/design/content/lake-of-life/LAKE_OF_LIFE_CHAPTER1_SIDE_QUEST_SPEC.md) · `design/content/lake-of-life/LAKE_OF_LIFE_CHAPTER1_SIDE_QUEST_SPEC.md` | 콘텐츠 | 추가 |
| [생명의 호수 제1장 스토리 플로우 수정 스펙](../../Assets/docs/design/content/lake-of-life/LAKE_OF_LIFE_CHAPTER1_STORY_FLOW_REVISION_SPEC.md) · `design/content/lake-of-life/LAKE_OF_LIFE_CHAPTER1_STORY_FLOW_REVISION_SPEC.md` | 콘텐츠 | 추가 |
| [LakeOfLife 대화·진행 개정 기준](../../Assets/docs/design/content/lake-of-life/LAKE_OF_LIFE_DIALOGUE_REVISION_V3.md) · `design/content/lake-of-life/LAKE_OF_LIFE_DIALOGUE_REVISION_V3.md` | 콘텐츠 | 추가 |
| [생명의 호수 스토리 모드 대화와 진행 전면 개편 기획](../../Assets/docs/design/content/lake-of-life/LAKE_OF_LIFE_NARRATIVE_OVERHAUL_PLAN.md) · `design/content/lake-of-life/LAKE_OF_LIFE_NARRATIVE_OVERHAUL_PLAN.md` | 콘텐츠 | 기존 |
| [생명의 호수 — 붉은 나무의 축복 단계별 제작 명세](../../Assets/docs/design/content/red-tree/LAKE_OF_LIFE_RED_TREE_IMPLEMENTATION_SPEC.md) · `design/content/red-tree/LAKE_OF_LIFE_RED_TREE_IMPLEMENTATION_SPEC.md` | 콘텐츠 | 추가 |
| [붉은 나무의 축복 인물과 생활 사건 및 대사 기준](../../Assets/docs/design/content/red-tree/LAKE_OF_LIFE_RED_TREE_R01_CAST.md) · `design/content/red-tree/LAKE_OF_LIFE_RED_TREE_R01_CAST.md` | 콘텐츠 | 추가 |
| [생명의 호수 붉은 나무의 축복 R01 상세 시나리오와 진행 계약](../../Assets/docs/design/content/red-tree/LAKE_OF_LIFE_RED_TREE_R01_SCENARIO.md) · `design/content/red-tree/LAKE_OF_LIFE_RED_TREE_R01_SCENARIO.md` | 콘텐츠 | 추가 |
| [붉은 나무의 축복 S00부터 S13까지 장면 카드와 대사](../../Assets/docs/design/content/red-tree/LAKE_OF_LIFE_RED_TREE_R01_SCENE_CARDS.md) · `design/content/red-tree/LAKE_OF_LIFE_RED_TREE_R01_SCENE_CARDS.md` | 콘텐츠 | 추가 |
| [생명의 호수 정규 스토리 기획안 붉은 나무의 축복](../../Assets/docs/design/content/red-tree/LAKE_OF_LIFE_RED_TREE_STORY_PLAN.md) · `design/content/red-tree/LAKE_OF_LIFE_RED_TREE_STORY_PLAN.md` | 콘텐츠 | 추가 |
| [플레이어블 캐릭터 대화 표정과 생동감 연출 적용 기획](../../Assets/docs/design/content/presentation/PLAYABLE_CHARACTER_DIALOGUE_PRESENTATION_PLAN.md) · `design/content/presentation/PLAYABLE_CHARACTER_DIALOGUE_PRESENTATION_PLAN.md` | 콘텐츠 | 추가 |
| [그늘성 시나리오 — 다시 오가는 길](../../Assets/docs/design/content/world-setting/SHADE_CASTLE_SCENARIO.md) · `design/content/world-setting/SHADE_CASTLE_SCENARIO.md` | 콘텐츠 | 추가 |
| [생명의 호수 시나리오 맵 플레이 전면 개편 기획](../../Assets/docs/design/content/scenario-gameplay/00_MASTER_PLAN.md) · `design/content/scenario-gameplay/00_MASTER_PLAN.md` | 콘텐츠 | 기존 |
| [생명의 호수 시작과 영입 및 퀘스트 제작 명세](../../Assets/docs/design/content/scenario-gameplay/01_STORY_AND_QUESTS.md) · `design/content/scenario-gameplay/01_STORY_AND_QUESTS.md` | 콘텐츠 | 추가 |
| [몬스터 조우와 선택 탐험 제작 명세](../../Assets/docs/design/content/scenario-gameplay/04_ENCOUNTERS_AND_EXPLORATION.md) · `design/content/scenario-gameplay/04_ENCOUNTERS_AND_EXPLORATION.md` | 콘텐츠 | 추가 |
| [동료·퀘스트·전투 연출 지침](../../Assets/docs/design/content/scenario-gameplay/08_PRESENTATION_DIRECTION.md) · `design/content/scenario-gameplay/08_PRESENTATION_DIRECTION.md` | 콘텐츠 | 추가 |
| [통합 세계관 바이블](../../Assets/docs/design/content/world-setting/WORLD_BIBLE.md) · `design/content/world-setting/WORLD_BIBLE.md` | 콘텐츠 | 추가 |

## 작업 상태·인계

구현이 시작된 뒤의 진행 기록·단계 인계·변경 목록. 원칙적으로 `docs/work-status/` 성격이다.

| 문서 | 분야 | 연결 |
|---|---|---|
| [A 현장 기준](../../docs/work-status/npc-life-ai/A_BASELINE.md) · `docs/work-status/npc-life-ai/A_BASELINE.md` | NPC | 추가 |
| [B 목적 일과](../../docs/work-status/npc-life-ai/B_PURPOSEFUL_ROUTINES.md) · `docs/work-status/npc-life-ai/B_PURPOSEFUL_ROUTINES.md` | NPC | 추가 |
| [변경 및 전달 목록](../../docs/work-status/npc-life-ai/CHANGED_ASSETS.md) · `docs/work-status/npc-life-ai/CHANGED_ASSETS.md` | NPC | 추가 |
| [C 두 주민 교류](../../docs/work-status/npc-life-ai/C_SOCIAL_SESSIONS.md) · `docs/work-status/npc-life-ai/C_SOCIAL_SESSIONS.md` | NPC | 추가 |
| [D 주변 반응](../../docs/work-status/npc-life-ai/D_WORLD_REACTIONS.md) · `docs/work-status/npc-life-ai/D_WORLD_REACTIONS.md` | NPC | 추가 |
| [E 수명·저장·성능](../../docs/work-status/npc-life-ai/E_LIFECYCLE_AND_PERFORMANCE.md) · `docs/work-status/npc-life-ai/E_LIFECYCLE_AND_PERFORMANCE.md` | NPC | 추가 |
| [NPC 생활 AI 진행 상태](../../docs/work-status/npc-life-ai/STATUS.md) · `docs/work-status/npc-life-ai/STATUS.md` | NPC | 기존 |
| [펫 시스템 1차 구현 및 테스트](../../docs/work-status/pet-system/PET_SYSTEM_IMPLEMENTATION.html) · `docs/work-status/pet-system/PET_SYSTEM_IMPLEMENTATION.html` | UI·플레이어 | 추가 |
| [펫 V2 구현 및 사용자 검수](../../docs/work-status/pet-system/PET_SYSTEM_V2_IMPLEMENTATION.html) · `docs/work-status/pet-system/PET_SYSTEM_V2_IMPLEMENTATION.html` | UI·플레이어 | 기존 |
| [생명의 호수 생활 확장 A — 현장 명세와 병렬 작업 계약](../../docs/work-status/living-world/A_BASELINE.md) · `docs/work-status/living-world/A_BASELINE.md` | 월드·아트 | 추가 |
| [B — 맵 이동·저장 인계](../../docs/work-status/living-world/B_MAP_HANDOFF.md) · `docs/work-status/living-world/B_MAP_HANDOFF.md` | 월드·아트 | 추가 |
| [C — NPC 생활 행동 인계](../../docs/work-status/living-world/C_NPC_HANDOFF.md) · `docs/work-status/living-world/C_NPC_HANDOFF.md` | 월드·아트 | 추가 |
| [D 환경 구현 인계](../../docs/work-status/living-world/D_ENVIRONMENT_HANDOFF.md) · `docs/work-status/living-world/D_ENVIRONMENT_HANDOFF.md` | 월드·아트 | 추가 |
| [E — 생활 확장 자산 조사·제작 판정 인계](../../docs/work-status/living-world/E_ASSET_HANDOFF.md) · `docs/work-status/living-world/E_ASSET_HANDOFF.md` | 월드·아트 | 추가 |
| [E — 주민 6명의 생활·피신 모션 인계](../../docs/work-status/living-world/E_MOTION_HANDOFF.md) · `docs/work-status/living-world/E_MOTION_HANDOFF.md` | 월드·아트 | 추가 |
| [I — 두 맵 본편 통합 기록](../../docs/work-status/living-world/I_INTEGRATION.md) · `docs/work-status/living-world/I_INTEGRATION.md` | 월드·아트 | 기존 |
| [붉은 나무 R02 실행·저장 전환](../../docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R02_TRANSITION.md) · `docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R02_TRANSITION.md` | 콘텐츠 | 추가 |
| [붉은 나무 R01 잔여 확인 · R02 · R03 구현 기록](../../docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R03_IMPLEMENTATION.md) · `docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R03_IMPLEMENTATION.md` | 콘텐츠 | 기존 |
| [붉은 나무 R04 — 피해 마을과 첫 진실](../../docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R04_IMPLEMENTATION.md) · `docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R04_IMPLEMENTATION.md` | 콘텐츠 | 추가 |
| [붉은 나무 R05 — 은폐와 명령 거부](../../docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R05_IMPLEMENTATION.md) · `docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R05_IMPLEMENTATION.md` | 콘텐츠 | 추가 |
| [붉은 나무 R06 — 표식 책임과 마지막 동료 합류](../../docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R06_IMPLEMENTATION.md) · `docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R06_IMPLEMENTATION.md` | 콘텐츠 | 추가 |
| [붉은 나무 R07 — 공동 대응 구현 기록](../../docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R07_IMPLEMENTATION.md) · `docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R07_IMPLEMENTATION.md` | 콘텐츠 | 추가 |
| [붉은 나무 R08 — 루시아 최종전 구현 기록](../../docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R08_IMPLEMENTATION.md) · `docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R08_IMPLEMENTATION.md` | 콘텐츠 | 추가 |
| [붉은 나무 R09 — 첫 반환과 남은 일](../../docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R09_IMPLEMENTATION.md) · `docs/work-status/red-tree-blessing/LAKE_OF_LIFE_RED_TREE_R09_IMPLEMENTATION.md` | 콘텐츠 | 기존 |

## 검증·테스트 명세

검수·인수 절차와 검증 결과. 사용자 직접 확인과 자동 검사를 구분해 읽는다.

| 문서 | 분야 | 연결 |
|---|---|---|
| [NPC 일상 말풍선 사용자 테스트](../../docs/work-status/npc-ambient-speech/NPC_AMBIENT_SPEECH_TEST.html) · `docs/work-status/npc-ambient-speech/NPC_AMBIENT_SPEECH_TEST.html` | NPC | 추가 |
| [F 인수 검토](../../docs/work-status/npc-life-ai/F_ACCEPTANCE.md) · `docs/work-status/npc-life-ai/F_ACCEPTANCE.md` | NPC | 추가 |
| [생활 AI 사용자 검수](../../docs/work-status/npc-life-ai/USER_TEST_SPEC.md) · `docs/work-status/npc-life-ai/USER_TEST_SPEC.md` | NPC | 추가 |
| [주민 모션 검증 결과(JSON)](../../docs/work-status/living-world/E_MOTION_VALIDATION.json) · `docs/work-status/living-world/E_MOTION_VALIDATION.json` | 월드·아트 | 추가 |
| [V — 생활 확장 통합 검증](../../docs/work-status/living-world/V_VALIDATION.md) · `docs/work-status/living-world/V_VALIDATION.md` | 월드·아트 | 추가 |
| [연속 월드 재설계 인수 테스트](../../docs/work-status/open-world/OPEN_WORLD_CITY_TEST_SPEC.md) · `docs/work-status/open-world/OPEN_WORLD_CITY_TEST_SPEC.md` | 월드·아트 | 추가 |
| [세계관 지도 기반 확장 월드 검수](../../docs/work-status/open-world/OPEN_WORLD_EXPANSION_TEST_SPEC.md) · `docs/work-status/open-world/OPEN_WORLD_EXPANSION_TEST_SPEC.md` | 월드·아트 | 추가 |
| [다섯 탐험 지역 사용자 테스트](../../docs/work-status/open-world/OPEN_WORLD_EXPLORATION_TEST_SPEC.md) · `docs/work-status/open-world/OPEN_WORLD_EXPLORATION_TEST_SPEC.md` | 월드·아트 | 추가 |
| [연속 월드 통합 테스트 명세](../../docs/work-status/open-world/OPEN_WORLD_USER_TEST_SPEC.md) · `docs/work-status/open-world/OPEN_WORLD_USER_TEST_SPEC.md` | 월드·아트 | 추가 |
| [구름 흐름 진단·전후 비교](../../docs/work-status/regional-sky/REGIONAL_SKY_CLOUD_FLOW_TEST_SPEC.html) · `docs/work-status/regional-sky/REGIONAL_SKY_CLOUD_FLOW_TEST_SPEC.html` | 월드·아트 | 추가 |
| [지역 하늘 본편 연결·2차 폴리싱 검증](../../docs/work-status/regional-sky/REGIONAL_SKY_FOLLOWUP_TEST_SPEC.html) · `docs/work-status/regional-sky/REGIONAL_SKY_FOLLOWUP_TEST_SPEC.html` | 월드·아트 | 기존 |
| [소탕 전투와 Mac Laya 검증 설계](../../Assets/docs/design/combat/encounter/SWEEP_COMBAT_AND_MAC_LAYA_VALIDATION_DESIGN.md) · `design/combat/encounter/SWEEP_COMBAT_AND_MAC_LAYA_VALIDATION_DESIGN.md` | 전투 | 추가 |
| [화린 대화 연출 구현·사용자 검수 명세](../../docs/work-status/dialogue-presentation/HWARIN_DIALOGUE_PRESENTATION_REVIEW.md) · `docs/work-status/dialogue-presentation/HWARIN_DIALOGUE_PRESENTATION_REVIEW.md` | 콘텐츠 | 추가 |
| [개편 검증 기준과 요구 사항 추적](../../docs/work-status/scenario-gameplay/07_ACCEPTANCE_AND_TRACEABILITY.md) · `docs/work-status/scenario-gameplay/07_ACCEPTANCE_AND_TRACEABILITY.md` | 콘텐츠 | 기존 |

## 조사·근거

설계 근거로 모은 조사·감사·출처 자료. 지시나 완료 증거로 쓰지 않는다.

| 문서 | 분야 | 연결 |
|---|---|---|
| [NPC 일상 말풍선 조사와 적용 기준](../../Assets/docs/research/NPC_AMBIENT_SPEECH_RESEARCH.html) · `research/NPC_AMBIENT_SPEECH_RESEARCH.html` | NPC | 추가 |
| [주민 모션 출처 목록(JSON)](../../docs/work-status/living-world/E_MOTION_SOURCES.json) · `docs/work-status/living-world/E_MOTION_SOURCES.json` | 월드·아트 | 추가 |
| [연속 월드 구현의 외부 근거](../../Assets/docs/research/OPEN_WORLD_WEB_RESEARCH.md) · `research/OPEN_WORLD_WEB_RESEARCH.md` | 월드·아트 | 추가 |
| [LakeVillageWorld 동물 관찰 근거와 행동 반영](../../Assets/docs/research/WILDLIFE_OBSERVATION_IMPLEMENTATION.md) · `research/WILDLIFE_OBSERVATION_IMPLEMENTATION.md` | 월드·아트 | 추가 |
| [생명의 호수 — 붉은 나무의 축복 R00 조사](../../Assets/docs/research/LAKE_OF_LIFE_RED_TREE_R00_AUDIT.md) · `research/LAKE_OF_LIFE_RED_TREE_R00_AUDIT.md` | 콘텐츠 | 추가 |
| [붉은 나무 R00 — 파일 근거 부록](../../Assets/docs/research/LAKE_OF_LIFE_RED_TREE_R00_EVIDENCE.md) · `research/LAKE_OF_LIFE_RED_TREE_R00_EVIDENCE.md` | 콘텐츠 | 추가 |
| [시나리오 개편 근거 스냅샷(JSON)](../../Assets/docs/design/content/scenario-gameplay/SOURCE_SNAPSHOT.json) · `design/content/scenario-gameplay/SOURCE_SNAPSHOT.json` | 콘텐츠 | 추가 |

## 작업 요청서·프롬프트

AI에게 작업을 맡길 때 쓴 요청문·단계 분할. 현재 지시로 재사용하기 전에 최신 코드와 대조한다.

| 문서 | 분야 | 연결 |
|---|---|---|
| [NPC 생활 AI 단계별 구현 요청 프롬프트](../../Assets/docs/requests/NPC_LIFE_AI_IMPLEMENTATION_PROMPTS.md) · `requests/NPC_LIFE_AI_IMPLEMENTATION_PROMPTS.md` | NPC | 추가 |
| [펫 시스템 2차 구현 요청 프롬프트](../../Assets/docs/requests/PET_SYSTEM_V2_IMPLEMENTATION_PROMPT.txt) · `requests/PET_SYSTEM_V2_IMPLEMENTATION_PROMPT.txt` | UI·플레이어 | 추가 |
| [생명의 호수 생활 확장 — 단계별 작업 분할과 에이전트 지시서](../../Assets/docs/requests/LAKE_OF_LIFE_LIVING_WORLD_TASKS.md) · `requests/LAKE_OF_LIFE_LIVING_WORLD_TASKS.md` | 월드·아트 | 추가 |
| [지형 디테일 풀 미렌더 원인 규명·수정 작업 요청서](../../Assets/docs/requests/TERRAIN_DETAIL_GRASS_RENDER_FIX_TASK.md) · `requests/TERRAIN_DETAIL_GRASS_RENDER_FIX_TASK.md` | 월드·아트 | 추가 |
| [전투 구도 개선 — 작업 요청 프롬프트](../../Assets/docs/requests/COMBAT_ENCOUNTER_COMPOSITION_TASK_PROMPTS.md) · `requests/COMBAT_ENCOUNTER_COMPOSITION_TASK_PROMPTS.md` | 전투 | 추가 |
| [생명의 호수 제1장 스토리 일러스트 제작 프롬프트](../../Assets/docs/requests/LAKE_OF_LIFE_CHAPTER1_STORY_ILLUSTRATION_PROMPTS.md) · `requests/LAKE_OF_LIFE_CHAPTER1_STORY_ILLUSTRATION_PROMPTS.md` | 콘텐츠 | 추가 |
| [단계별 개발 작업과 AI 직접 수정 프롬프트](../../Assets/docs/design/content/scenario-gameplay/06_IMPLEMENTATION_AND_PROMPTS.md) · `design/content/scenario-gameplay/06_IMPLEMENTATION_AND_PROMPTS.md` | 콘텐츠 | 추가 |

## 시안·참조 자료

UI 시안·컨셉 이미지·보관 시안 등 참조용 자료. 정본이 아니다.

| 문서 | 분야 | 연결 |
|---|---|---|
| [파티 편성](../../Assets/docs/reference/ui-mockups/Party%20UI%20v2.html) · `reference/ui-mockups/Party UI v2.html` | UI·플레이어 | 추가 |
| [길 장식 레퍼런스 이미지 목록](../../Assets/docs/reference/regular-world/PathDressing/manifest.json) · `reference/regular-world/PathDressing/manifest.json` | 월드·아트 | 추가 |
| [지역 컨셉 이미지 목록](../../Assets/docs/reference/regular-world/RegionalConcepts/manifest.json) · `reference/regular-world/RegionalConcepts/manifest.json` | 월드·아트 | 추가 |
| [루트 배치 데이터(SVG 원본)](../../Assets/docs/reference/regular-world/Routes/route-layout.json) · `reference/regular-world/Routes/route-layout.json` | 월드·아트 | 추가 |
| [[보관 시안] Attack Data Editor v2](../../Assets/docs/reference/tool-mockups/Attack%20Data%20Editor%20v2.html) · `reference/tool-mockups/Attack Data Editor v2.html` | 전투 | 추가 |
| [[보관 시안] Attack Data Editor](../../Assets/docs/reference/tool-mockups/AttackDataEditor.html) · `reference/tool-mockups/AttackDataEditor.html` | 전투 | 추가 |

## 가이드 성격 문서 (미연결)

저작·운영 지침은 위키에 연결하지 않는다. 지침의 위치는 `Assets/docs/guide/`다.

- `game_dialogue_natural_korean_guide.md` — 게임 스토리 대사 작성 가이드. 이 폴더에서 `Assets/docs/guide/`로 옮겼고 AGENTS.md·[콘텐츠](content.md)의 참조를 새 경로로 고쳤다. 콘텐츠 위키의 기존 링크는 유지했다
- `COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md`·`BT Editor Design Guide.html`은 이름에 GUIDE가 있으나 내용이 설계·제안이라 `설계`로 분류해 옮기지 않았다. 위 표에 있다

## 이동 내역

| 차수 | 이동 | 새 위치 |
|---|---|---|
| 1차 | 작업 상태·검증 문서 38개 | `docs/work-status/` 하위 폴더 |
| 1차 | 가이드 1개(대사 작성 가이드) | `Assets/docs/guide/` |
| 2차 | 설계 문서 약 60개 | `Assets/docs/design/<분야>/<시스템>/` |
| 2차 | 작업 요청서·프롬프트 6개 | `Assets/docs/requests/` |
| 2차 | 조사·근거 5개 | `Assets/docs/research/` |
| 2차 | 시안·참조(UI·도구 시안, 정규 월드 참조 이미지) | `Assets/docs/reference/` |

`ScenarioGameplay/`는 번호순 묶음이라 요청서(06)와 근거 스냅샷을 포함해 `design/content/scenario-gameplay/`에 통째로 옮겼다. Assets 밖으로 나간 문서의 `.meta`는 삭제했고 새 폴더의 `.meta`는 새로 만들었다. `output/`·`_archive/`·실행 로그의 과거 경로 표기는 당시 기록이라 고치지 않았다.

## 정리 후보 (아직 하지 않음)

- **큰 파일:** `HUD_CHARACTER_GAUGE_DESIGN.html`(약 21MB), `WORLD_THEME_TERRAIN_PRODUCTION_DESIGN.html`(약 2.9MB)는 이미지가 내장되어 있어 편집·검색이 무겁다.
- **중복 형식:** `CINEMATIC_STAGE_SYSTEM_DESIGN`은 `.md`(2026-07-31 v2 개정)와 `.html`(v2 제목)이 함께 있다. 두 형식의 내용 일치와 정본 지정을 확인해야 한다.
- **`Assets/docs` 루트:** `EXTERNAL_MODEL_P09_CONVERSION_GUIDE.md`·`EXTERNAL_MODEL_P09_REQUEST_PROMPT.txt` 등 지침·요청 성격 문서가 루트에 남아 있다. 이번 범위(design)가 아니라 옮기지 않았다.

[위키 시작](README.md)
