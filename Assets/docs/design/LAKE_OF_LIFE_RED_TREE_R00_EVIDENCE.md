# 붉은 나무 R00 — 파일 근거 부록

> 2026-10-17 작업 트리의 YAML·JSON·GUID 정적 조사. Unity 로드·런타임 검사가 아니다. [조사 결론](LAKE_OF_LIFE_RED_TREE_R00_AUDIT.md)을 함께 읽는다.

## 1. 플레이어블 대응

정의는 PlayerCharacters 폴더, 모델은 Addressables GUID, 초상화는 PartyMemberData.fullBodySprite를 역조회했다. H09는 제외 비교 행이다.

| enum / 값 | 실제 표시 이름 | 정의 / 모델 | 기본 무기 enum 값 | AbilitySet | 파티 초상화 |
|---|---|---|---|---|---|
| Arin / 13 | "아린" | [PlayerCharacterDefinition_Arin.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Arin.asset) / [PlayerModel_Arin.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_Arin.prefab) | 10 | [AbilitySet_PlayerKatanaAttackData.asset](../../10.Datas/Ability/Migrated/PlayerKatanaAttackData/AbilitySet_PlayerKatanaAttackData.asset) | [Player_Arin_Portrait.png](../../04.Images/UI/Portrait/Player_Arin_Portrait.png) |
| H09 / 12 | 표 없음 | [PlayerCharacterDefinition_H09.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_H09.asset) / [PlayerModel_H09.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_H09.prefab) | 4 | [AbilitySet_PlayerStaff.asset](../../10.Datas/Ability/Player/Staff/AbilitySet_PlayerStaff.asset) | 미해석 |
| Hwarin / 2 | "화린" | [PlayerCharacterDefinition_Hwarin.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Hwarin.asset) / [PlayerModel_Hwarin.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_Hwarin.prefab) | 11 | [AbilitySet_PlayerDoubleAxeAttackData.asset](../../10.Datas/Ability/Migrated/PlayerDoubleAxeAttackData/AbilitySet_PlayerDoubleAxeAttackData.asset) | [Player_Hwarin_Full.png](../../04.Images/UI/Portrait/Player_Hwarin_Full.png) |
| Lian / 4 | "리안" | [PlayerCharacterDefinition_Lian.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Lian.asset) / [PlayerModel_Lian.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_Lian.prefab) | 12 | [AbilitySet_PlayerWhipAttackData.asset](../../10.Datas/Ability/Migrated/PlayerWhipAttackData/AbilitySet_PlayerWhipAttackData.asset) | [Player_Lian_Full.png](../../04.Images/UI/Portrait/Player_Lian_Full.png) |
| Lili / 11 | "릴리" | [PlayerCharacterDefinition_Lili.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Lili.asset) / [PlayerModel_Lili.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_Lili.prefab) | 3 | [AbilitySet_PlayerGreatSwordAttackData.asset](../../10.Datas/Ability/Migrated/PlayerGreatSwordAttackData/AbilitySet_PlayerGreatSwordAttackData.asset) | [Player_Lili_Full.png](../../04.Images/UI/Portrait/Player_Lili_Full.png) |
| Myomyo / 10 | "묘묘" | [PlayerCharacterDefinition_Myomyo.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Myomyo.asset) / [PlayerModel_Myomyo.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_Myomyo.prefab) | 4 | [AbilitySet_PlayerStaff.asset](../../10.Datas/Ability/Player/Staff/AbilitySet_PlayerStaff.asset) | [Player_Myomyo_Full.png](../../04.Images/UI/Portrait/Player_Myomyo_Full.png) |
| MyoRyeong / 9 | "묘령" | [PlayerCharacterDefinition_MyoRyeong.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_MyoRyeong.asset) / [PlayerModel_MyoRyeong.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_MyoRyeong.prefab) | 2 | [AbilitySet_PlayerSwordShieldAttackData.asset](../../10.Datas/Ability/Migrated/PlayerSwordShieldAttackData/AbilitySet_PlayerSwordShieldAttackData.asset) | [Player_MyoRyeong_Full.png](../../04.Images/UI/Portrait/Player_MyoRyeong_Full.png) |
| Raon / 1 | "라온" | [PlayerCharacterDefinition_Raon.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Raon.asset) / [PlayerModel_Raon.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab) | 10 | [AbilitySet_PlayerKatanaAttackData.asset](../../10.Datas/Ability/Migrated/PlayerKatanaAttackData/AbilitySet_PlayerKatanaAttackData.asset) | [Player_Raon_Full.png](../../04.Images/UI/Portrait/Player_Raon_Full.png) |
| Reine / 3 | "렌느" | [PlayerCharacterDefinition_Reine.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Reine.asset) / [PlayerModel_Reine.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_Reine.prefab) | 13 | [AbilitySet_PlayerDefaultAttackData.asset](../../10.Datas/Ability/Migrated/PlayerDefaultAttackData/AbilitySet_PlayerDefaultAttackData.asset) | [Player_Reien_Full.png](../../04.Images/UI/Portrait/Player_Reien_Full.png) |
| SeolA / 5 | "설아" | [PlayerCharacterDefinition_SeolA.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_SeolA.asset) / [PlayerModel_SeolA.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_SeolA.prefab) | 5 | [AbilitySet_PlayerBowAttackData.asset](../../10.Datas/Ability/Migrated/PlayerBowAttackData/AbilitySet_PlayerBowAttackData.asset) | [Player_SeolA_Full.png](../../04.Images/UI/Portrait/Player_SeolA_Full.png) |
| Sera / 6 | "세라" | [PlayerCharacterDefinition_Sera.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Sera.asset) / [PlayerModel_Sera.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_Sera.prefab) | 3 | [AbilitySet_PlayerGreatSwordAttackData.asset](../../10.Datas/Ability/Migrated/PlayerGreatSwordAttackData/AbilitySet_PlayerGreatSwordAttackData.asset) | [Player_Sera_Full.png](../../04.Images/UI/Portrait/Player_Sera_Full.png) |
| YeonHoa / 7 | "연화" | [PlayerCharacterDefinition_YeonHoa.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_YeonHoa.asset) / [PlayerModel_YeonHoa.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_YeonHoa.prefab) | 4 | [AbilitySet_PlayerStaff.asset](../../10.Datas/Ability/Player/Staff/AbilitySet_PlayerStaff.asset) | [Player_YeonHoa_Full.png](../../04.Images/UI/Portrait/Player_YeonHoa_Full.png) |
| Yura / 8 | "유라" | [PlayerCharacterDefinition_Yura.asset](../../10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Yura.asset) / [PlayerModel_Yura.prefab](../../03.Prefabs/Actor/Player/Models/PlayerModel_Yura.prefab) | 14 | [AbilitySet_PlayerDualBladeAttackData.asset](../../10.Datas/Ability/Migrated/PlayerDualBladeAttackData/AbilitySet_PlayerDualBladeAttackData.asset) | [Player_Yura_Full.png](../../04.Images/UI/Portrait/Player_Yura_Full.png) |

## 2. 화자 테이블과 NPC 참조

현재 고정 화자 바인딩이다. Protagonist/Player 동적 해석은 별도 코드 계약이다.

| 화자 ID | Actor ID | 테이블 초상화 |
|---|---|---|
| 안내인 | NPC_Story_Guide | [Npc_Vardan_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Vardan_Dialogue_Portrait.png) |
| 길잡이 | NPC_Story_Guide | 미해석 |
| 당신 | Player | 미해석 |
| 루시아 | NPC_Lucia | [Npc_Lucia_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Lucia_Dialogue_Portrait.png) |
| 리안 | MonsterLian | [Lian_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Lian_Dialogue_Portrait.png) |
| 모건 | NPC_Morgan | [Npc_Morgan_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Morgan_Dialogue_Portrait.png) |
| 미아 | NPC_Mia | [NPC_Mia_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/NPC_Mia_Dialogue_Portrait.png) |
| 상인 | NPC_Shop | [Npc_Shop_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Shop_Dialogue_Portrait.png) |
| 조안 | NPC_Joan | [Npc_Joan_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Joan_Dialogue_Portrait.png) |
| 조이 | NPC_Joy | [Npc_Joy_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Joy_Dialogue_Portrait.png) |
| 라온 | MonsterBokusei | [MonsterRaon_Dialogue_Portrait.png](../../04.Images/UI/Portrait/MonsterRaon_Dialogue_Portrait.png) |
| 페니 | NPC_Penny | [Npc_Penny_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Penny_Dialogue_Portrait.png) |
| 헤이즐 | NPC_Hazel | [Npc_Hazel_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Hazel_Dialogue_Portrait.png) |
| 화린 | MonsterHonoka | [Hwarin_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Hwarin_Dialogue_Portrait.png) |
| 준 | NPC_Jun | [Npc_Jun_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Jun_Dialogue_Portrait.png) |
| 묘령 | MonsterSiuha | 미해석 |
| 다온 | Npc_Daon | [Npc_Daon_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Daon_Dialogue_Portrait.png) |
| 연목 | Npc_Yeonmok | [Npc_Yeonmok_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Yeonmok_Dialogue_Portrait.png) |
| 소담 | Npc_Sodam | [Npc_Sodam_Dialogue_Portrait.png](../../04.Images/UI/Portrait/Npc/Npc_Sodam_Dialogue_Portrait.png) |

### 씬에 직접 직렬화된 NPC

프리팹 참조 없는 씬 직접 배치도 있다. 모델 누락으로 오판하지 않는다. 재사용 시 GameObject fileID·SceneEntityId·자식 Renderer를 함께 보존한다.

| 씬 이름 / GameObject fileID | Actor ID | NPC 데이터 |
|---|---|---|
| Npc_Lucia / 21945452 | NPC_Lucia | [NPC_Lucia.asset](../../10.Datas/Actor/Npc/NPC_Lucia.asset) |
| NPC_Morgan / 346418507 | NPC_Morgan | [NPC_Morgan_Scenario.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/NPC_Morgan_Scenario.asset) |
| NPC_Guide / 658161054 | NPC_Story_Guide | [NPC_Guide_Scenario.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/NPC_Guide_Scenario.asset) |
| Npc_Mia / 1049215359 | NPC_Mia | [NPC_Mia_Scenario.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/NPC_Mia_Scenario.asset) |
| Npc_Hazel / 1515089789 | NPC_Hazel | [NPC_Hazel_Scenario.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/NPC_Hazel_Scenario.asset) |
| NPC_Joy / 1526290794 | NPC_Joy | [NPC_Joy.asset](../../10.Datas/Actor/Npc/NPC_Joy.asset) |
| Npc_Penny / 1801362251 | NPC_Penny | [NPC_Penny_Scenario.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/NPC_Penny_Scenario.asset) |
| NPC_Joan / 2129792766 | NPC_Joan | [NPC_Joan.asset](../../10.Datas/Actor/Npc/NPC_Joan.asset) |

## 3. 씬 Flow 실행 등록

| 러너 GameObject | graphId / 에셋 | Start 자동 실행 |
|---|---|---|
| ScenarioFirstQuest (1209617157) | flow_lake_search_quest_line / [FLOW_ScenarioFirstQuest.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioFirstQuest.asset) | 1 / MapReady |
| '[STORY] RecruitmentEncounter_MyoRyeongDuel' (7397712116352646935) | flow_test_siuha_duel / [FLOW_ScenarioMyoRyeongDuel.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioMyoRyeongDuel.asset) | 0 / Resume |
| '[STORY] RecruitmentEncounter_LianRescue' (8599999998360949212) | flow_test_lian_rescue / [FLOW_ScenarioLianRescue.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioLianRescue.asset) | 0 / Resume |
| '[STORY] RecruitmentEncounter_HwarinRescue' (8599999999866319110) | flow_test_honoka_rescue / [FLOW_ScenarioHwarinRescue.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioHwarinRescue.asset) | 0 / Resume |
| ScenarioOptionalEvents (8900999999880572703) | flow_scenario_optional_facts / [FLOW_ScenarioOptionalFacts.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioOptionalFacts.asset) | 1 / Resume |
| ScenarioOptionalEvents (8900999999880572703) | flow_lake_jun_rescue / [FLOW_ScenarioJunRescue.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioJunRescue.asset) | 0 / Resume |
| ScenarioHerbRoute (8901000000400264894) | flow_lake_side_quest_route / [FLOW_ScenarioHerbRoute.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioHerbRoute.asset) | 0 / Resume |

### 각 Flow의 대화·퀘스트·사실

파일명이 달라도 graphId가 원본과 같은 경우가 있다. firedKeys를 새 사건에 재사용하지 않는다.

#### FLOW_ScenarioFirstQuest.asset

- 근거: [FLOW_ScenarioFirstQuest.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioFirstQuest.asset)
- `questId`: `quest_sub_lake_missing_villagers`, `quest_sub_lake_rescue_honoka`, `quest_sub_lake_rescue_lian`, `quest_sub_lake_follow_tracks`, `quest_main_lake_shrine`
- `flagKey`: `lake.story.red_cloth_examined`, `lake.story.honoka_joined`, `lake.story.lian_marker_examined`, `lake.story.lian_joined`, `lake.story.drag_tracks_examined`, `lake.story.siuha_joined`, `lake.story.guide_briefed`
- `eventId`: `lake.story.red_cloth_examined`, `lake.story.honoka_joined`, `lake.story.lian_marker_examined`, `lake.story.lian_joined`, `lake.story.drag_tracks_examined`, `lake.story.siuha_joined`, `lake.story.guide_briefed`
- `entryId`: `MapReady`
- 대화: [DLG_Lake_NewGameOpening.asset](../../10.Datas/Dialogue/Story/Dialogue/DLG_Lake_NewGameOpening.asset)

#### FLOW_ScenarioHerbRoute.asset

- 근거: [FLOW_ScenarioHerbRoute.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioHerbRoute.asset)
- `questId`: `quest_sub_penny_lantern_order`, `quest_sub_morgan_west_road`, `quest_sub_hazel_moved_herb`
- `flagKey`: `lake.side.west_road_cleared`, `lake.side.lantern_1_examined`, `lake.side.lantern_2_examined`, `lake.side.lantern_3_examined`, `lake.side.driver_tracks_found`, `lake.side.herb_bed_examined`
- `eventId`: `lake.side.lantern_1_examined`, `lake.side.lantern_2_examined`, `lake.side.lantern_3_examined`, `lake.side.west_road_cleared`, `lake.side.driver_tracks_found`, `lake.side.herb_bed_examined`
- `entryId`: `Lantern1`, `Lantern2`, `Lantern3`, `WestRoad`, `Loaded`, `HerbBed`
- 대화: [DLG_Lake_LanternObservation01.asset](../../10.Datas/Dialogue/Story/Dialogue/DLG_Lake_LanternObservation01.asset), [DLG_Lake_LanternObservation02.asset](../../10.Datas/Dialogue/Story/Dialogue/DLG_Lake_LanternObservation02.asset), [DLG_Lake_LanternObservation03.asset](../../10.Datas/Dialogue/Story/Dialogue/DLG_Lake_LanternObservation03.asset), [DLG_Lake_WestRoadInspection.asset](../../10.Datas/Dialogue/Story/Dialogue/DLG_Lake_WestRoadInspection.asset), [DLG_Lake_HerbBedObservation.asset](../../10.Datas/Dialogue/Story/Dialogue/DLG_Lake_HerbBedObservation.asset)

#### FLOW_ScenarioHwarinRescue.asset

- 근거: [FLOW_ScenarioHwarinRescue.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioHwarinRescue.asset)
- `questId`: `quest_sub_lake_rescue_lian`, `quest_sub_lake_rescue_honoka`
- `flagKey`: `lake.story.red_cloth_examined`, `lake.story.honoka_joined`
- `eventId`: `lake.story.red_cloth_examined`, `lake.story.honoka_joined`
- `entryId`: `Enter`, `Resume`
- `encounterId`: `test.combat.honoka_rescue`
- 대화: [DLG_ScenarioHwarinRescue.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/DLG_ScenarioHwarinRescue.asset), [DLG_Lake_RedCloth.asset](../../10.Datas/Dialogue/Story/Dialogue/DLG_Lake_RedCloth.asset)

#### FLOW_ScenarioJunRescue.asset

- 근거: [FLOW_ScenarioJunRescue.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioJunRescue.asset)
- `questId`: `quest_sub_hunter_skeleton_patrol`
- `flagKey`: `lake.side.jun_rescued`, `lake.side.jun_rescue_group_defeated`, `lake.story.jun_trail_found`
- `eventId`: `lake.story.jun_trail_found`, `lake.story.jun_party_found`, `lake.side.jun_returned`
- `entryId`: `Loaded`, `Trail`, `Rescue`, `GroupDefeated`
- 대화: [DLG_ScenarioJunRescue.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/DLG_ScenarioJunRescue.asset)

#### FLOW_ScenarioLianRescue.asset

- 근거: [FLOW_ScenarioLianRescue.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioLianRescue.asset)
- `questId`: `quest_sub_lake_follow_tracks`, `quest_sub_lake_rescue_lian`
- `flagKey`: `lake.story.honoka_joined`, `lake.story.lian_marker_examined`, `lake.story.drag_tracks_examined`, `lake.story.lian_joined`
- `eventId`: `lake.story.lian_marker_examined`, `lake.story.drag_tracks_examined`, `lake.story.lian_joined`
- `entryId`: `Enter`, `Resume`
- `encounterId`: `test.combat.lian_rescue`
- 대화: [DLG_ScenarioLianRescue.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/DLG_ScenarioLianRescue.asset), [DLG_Test_HwarinJoined.asset](../../10.Datas/Dialogue/Test/DLG_Test_HwarinJoined.asset)

#### FLOW_ScenarioMyoRyeongDuel.asset

- 근거: [FLOW_ScenarioMyoRyeongDuel.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioMyoRyeongDuel.asset)
- `questId`: `quest_sub_lake_follow_tracks`, `quest_main_lake_shrine`
- `flagKey`: `lake.story.lian_joined`, `lake.story.drag_tracks_examined`, `lake.story.siuha_joined`
- `eventId`: `lake.story.drag_tracks_examined`, `lake.story.siuha_joined`
- `entryId`: `Enter`, `Resume`
- `encounterId`: `test.combat.siuha_duel`
- 대화: [DLG_ScenarioMyoRyeongConfrontation.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/DLG_ScenarioMyoRyeongConfrontation.asset), [DLG_ScenarioMyoRyeongJoined.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/DLG_ScenarioMyoRyeongJoined.asset), [DLG_Lake_DragTracks.asset](../../10.Datas/Dialogue/Story/Dialogue/DLG_Lake_DragTracks.asset)

#### FLOW_ScenarioOptionalFacts.asset

- 근거: [FLOW_ScenarioOptionalFacts.asset](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioOptionalFacts.asset)
- `flagKey`: `lake.story.jun_trail_found`, `lake.side.jun_rescued`, `lake.side.jun_finished`, `lake.side.herb_bed_examined`, `lake.side.lantern_1_examined`, `lake.side.lantern_2_examined`, `lake.side.lantern_3_examined`, `lake.side.west_road_cleared`, `lake.side.driver_tracks_found`, `lake.story.mia_spoken`, `lake.side.herb_accepted`, `lake.side.lantern_accepted`, `lake.side.salt_accepted`, `lake.side.lantern_cleared`, `lake.side.west_road_ready`
- `eventId`: `lake.story.jun_trail_found`, `lake.story.jun_party_found`, `lake.side.jun_returned`, `lake.side.herb_bed_examined`, `lake.side.lantern_1_examined`, `lake.side.lantern_2_examined`, `lake.side.lantern_3_examined`, `lake.side.west_road_cleared`, `lake.side.driver_tracks_found`
- `entryId`: `Resume`, `Loaded`, `changed_0`, `changed_1`, `changed_2`, `changed_3`, `changed_4`, `changed_5`, `changed_6`, `changed_7`, `changed_8`, `changed_9`, `changed_10`, `changed_11`, `changed_12`

## 4. 몬스터 정적 연결

프리팹의 _abilitySet과 _motionSet을 역조회했다. ActorDefinition/Profile 주입, Avatar 호환성, 클립 subasset 로드, 이벤트 실행과 명중은 Unity 확인 대상이다. aiSelectable=1 Payload를 포함한 Ability만 열거하며 공통 비공격 Ability는 생략한다.

| 프리팹 | AbilitySet | Actor MotionSet |
|---|---|---|
| [MonsterActor_Ent_V1.prefab](../../03.Prefabs/Actor/Monster/MonsterActor_Ent_V1.prefab) | [AbilitySet_Ent.asset](../../10.Datas/Ability/Actor/Ent/AbilitySet_Ent.asset) | [Ent_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_MotionAsset.asset) |
| [MonsterActor_Ent_V2.prefab](../../03.Prefabs/Actor/Monster/MonsterActor_Ent_V2.prefab) | [AbilitySet_Ent.asset](../../10.Datas/Ability/Actor/Ent/AbilitySet_Ent.asset) | [Ent_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_MotionAsset.asset) |
| [MonsterActor_Golem.prefab](../../03.Prefabs/Actor/Monster/MonsterActor_Golem.prefab) | [AbilitySet_Golem.asset](../../10.Datas/Ability/Actor/Golem/AbilitySet_Golem.asset) | [Ent_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_MotionAsset.asset) |
| [MonsterActor_SpiderMinion_V1.prefab](../../03.Prefabs/Actor/Monster/MonsterActor_SpiderMinion_V1.prefab) | [AbilitySet_Spider_Minion.asset](../../10.Datas/Ability/Actor/Spider_Minion/AbilitySet_Spider_Minion.asset) | [SpiderMinion_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderMinion/SpiderMinion_MotionAsset.asset) |
| [MonsterActor_SpiderMinion_V2.prefab](../../03.Prefabs/Actor/Monster/MonsterActor_SpiderMinion_V2.prefab) | [AbilitySet_Spider_Minion.asset](../../10.Datas/Ability/Actor/Spider_Minion/AbilitySet_Spider_Minion.asset) | [SpiderMinion_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderMinion/SpiderMinion_MotionAsset.asset) |
| [MonsterActor_SpiderMinion_V3.prefab](../../03.Prefabs/Actor/Monster/MonsterActor_SpiderMinion_V3.prefab) | [AbilitySet_Spider_Minion.asset](../../10.Datas/Ability/Actor/Spider_Minion/AbilitySet_Spider_Minion.asset) | [SpiderMinion_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderMinion/SpiderMinion_MotionAsset.asset) |
| [MonsterActor_SpiderQueen_V1.prefab](../../03.Prefabs/Actor/Monster/MonsterActor_SpiderQueen_V1.prefab) | [AbilitySet_Spider_Queen.asset](../../10.Datas/Ability/Actor/Spider_Queen/AbilitySet_Spider_Queen.asset) | [SpiderQueen_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen/SpiderQueen_MotionAsset.asset) |
| [MonsterActor_SpiderQueen_V2.prefab](../../03.Prefabs/Actor/Monster/MonsterActor_SpiderQueen_V2.prefab) | [AbilitySet_Spider_Queen.asset](../../10.Datas/Ability/Actor/Spider_Queen/AbilitySet_Spider_Queen.asset) | [SpiderQueen_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen/SpiderQueen_MotionAsset.asset) |
| [MonsterActor_SpiderQueen_V3.prefab](../../03.Prefabs/Actor/Monster/MonsterActor_SpiderQueen_V3.prefab) | [AbilitySet_Spider_Queen.asset](../../10.Datas/Ability/Actor/Spider_Queen/AbilitySet_Spider_Queen.asset) | [SpiderQueen_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen/SpiderQueen_MotionAsset.asset) |
| [Monster_ChildPlant_V1.prefab](../../03.Prefabs/Actor/Monster/Monster_ChildPlant_V1.prefab) | [AbilitySet_ChildPlant.asset](../../10.Datas/Ability/Actor/ChildPlant/AbilitySet_ChildPlant.asset) | [ChildPlant_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Child/ChildPlant_MotionSet.asset) |
| [Monster_ChildPlant_V2.prefab](../../03.Prefabs/Actor/Monster/Monster_ChildPlant_V2.prefab) | [AbilitySet_ChildPlant.asset](../../10.Datas/Ability/Actor/ChildPlant/AbilitySet_ChildPlant.asset) | [ChildPlant_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Child/ChildPlant_MotionSet.asset) |
| [Monster_ChildPlant_V3.prefab](../../03.Prefabs/Actor/Monster/Monster_ChildPlant_V3.prefab) | [AbilitySet_ChildPlant.asset](../../10.Datas/Ability/Actor/ChildPlant/AbilitySet_ChildPlant.asset) | [ChildPlant_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Child/ChildPlant_MotionSet.asset) |
| [Monster_Dryad.prefab](../../03.Prefabs/Actor/Monster/Monster_Dryad.prefab) | [AbilitySet_dryad.asset](../../10.Datas/Ability/Actor/dryad/AbilitySet_dryad.asset) | [Dryad_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Dryad/Dryad_MotionSet.asset) |
| [Monster_Golem_Black.prefab](../../03.Prefabs/Actor/Monster/Monster_Golem_Black.prefab) | [AbilitySet_Golem.asset](../../10.Datas/Ability/Actor/Golem/AbilitySet_Golem.asset) | [Ent_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_MotionAsset.asset) |
| [Monster_Golem_Inferno.prefab](../../03.Prefabs/Actor/Monster/Monster_Golem_Inferno.prefab) | [AbilitySet_Golem.asset](../../10.Datas/Ability/Actor/Golem/AbilitySet_Golem.asset) | [Ent_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_MotionAsset.asset) |
| [Monster_Golem_Normal.prefab](../../03.Prefabs/Actor/Monster/Monster_Golem_Normal.prefab) | [AbilitySet_Golem.asset](../../10.Datas/Ability/Actor/Golem/AbilitySet_Golem.asset) | [Ent_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_MotionAsset.asset) |
| [Monster_Plant_V1.prefab](../../03.Prefabs/Actor/Monster/Monster_Plant_V1.prefab) | [AbilitySet_MainPlant.asset](../../10.Datas/Ability/Actor/MainPlant/AbilitySet_MainPlant.asset) | [MainPlant_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Main/MainPlant_MotionSet.asset) |
| [Monster_Plant_V2.prefab](../../03.Prefabs/Actor/Monster/Monster_Plant_V2.prefab) | [AbilitySet_MainPlant.asset](../../10.Datas/Ability/Actor/MainPlant/AbilitySet_MainPlant.asset) | [MainPlant_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Main/MainPlant_MotionSet.asset) |
| [Monster_Plant_V3.prefab](../../03.Prefabs/Actor/Monster/Monster_Plant_V3.prefab) | [AbilitySet_MainPlant.asset](../../10.Datas/Ability/Actor/MainPlant/AbilitySet_MainPlant.asset) | [MainPlant_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Main/MainPlant_MotionSet.asset) |
| [Monster_RootPlant_V1.prefab](../../03.Prefabs/Actor/Monster/Monster_RootPlant_V1.prefab) | [AbilitySet_PlantRoot.asset](../../10.Datas/Ability/Actor/PlantRoot/AbilitySet_PlantRoot.asset) | [PlantRoot_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Root/PlantRoot_MotionSet.asset) |
| [Monster_RootPlant_V2.prefab](../../03.Prefabs/Actor/Monster/Monster_RootPlant_V2.prefab) | [AbilitySet_PlantRoot.asset](../../10.Datas/Ability/Actor/PlantRoot/AbilitySet_PlantRoot.asset) | [PlantRoot_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Root/PlantRoot_MotionSet.asset) |
| [Monster_RootPlant_V3.prefab](../../03.Prefabs/Actor/Monster/Monster_RootPlant_V3.prefab) | [AbilitySet_PlantRoot.asset](../../10.Datas/Ability/Actor/PlantRoot/AbilitySet_PlantRoot.asset) | [PlantRoot_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Root/PlantRoot_MotionSet.asset) |

### AbilitySet_Ent.asset

매핑: [Ent_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_MotionAsset.asset).

| Ability / Payload fileID | MotionKey → MotionSet | 클립 파일 / fileID | HitPhase / 타이밍 이벤트 |
|---|---|---|---|
| [GA_Ent_01_Attack_1.asset](../../10.Datas/Ability/Actor/Ent/GA_Ent_01_Attack_1.asset) / -501028763237202998 | Ent.Attack.1.01 → [Ent_Attack_L.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_Attack_L.asset) | [Anim_Hand_Attack_L.fbx](../../ExternalAssets/Character/Ent/AnimationClips/Humanoid/Anim_Hand_Attack_L.fbx) / `-3568207325639417703` | 1개 / BeginCollisionEvent 1.1874999–1.6375 |
| [GA_Ent_02_Attack_2.asset](../../10.Datas/Ability/Actor/Ent/GA_Ent_02_Attack_2.asset) / 6373264796227661630 | Ent.Attack.2.02 → [Ent_Attack_R.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_Attack_R.asset) | [Anim_Hand_Attack_R.fbx](../../ExternalAssets/Character/Ent/AnimationClips/Humanoid/Anim_Hand_Attack_R.fbx) / `8970874683280315550` | 1개 / BeginCollisionEvent 1.3125001–1.8125001 |
| [GA_Ent_03_Attack_3.asset](../../10.Datas/Ability/Actor/Ent/GA_Ent_03_Attack_3.asset) / 2890540647611296003 | Ent.Attack.3.03 → [Ent_Attack_Leg_L.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_Attack_Leg_L.asset) | [Anim_Leg_Attack_L.fbx](../../ExternalAssets/Character/Ent/AnimationClips/Humanoid/Anim_Leg_Attack_L.fbx) / `5325916286891137228` | 1개 / BeginCollisionEvent 1.0625–1.5375 |
| [GA_Ent_04_Attack_4.asset](../../10.Datas/Ability/Actor/Ent/GA_Ent_04_Attack_4.asset) / 2520797849329375105 | Ent.Attack.4.04 → [Ent_Attack_Leg_R.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_Attack_Leg_R.asset) | [Anim_Leg_Attack_R.fbx](../../ExternalAssets/Character/Ent/AnimationClips/Humanoid/Anim_Leg_Attack_R.fbx) / `2154262455844783879` | 1개 / BeginCollisionEvent 1.0250001–1.3625 |
| [GA_Ent_05_Skill_1.asset](../../10.Datas/Ability/Actor/Ent/GA_Ent_05_Skill_1.asset) / 8503454093969689781 | Ent.Skill.1.05 → [Ent_Heal.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_Heal.asset) | [Anim_Healing.fbx](../../ExternalAssets/Character/Ent/AnimationClips/Humanoid/Anim_Healing.fbx) / `2918604158282704777` | 1개 / HealSkillEvent 0.79999995–3.35 |

### AbilitySet_Golem.asset

매핑: [Ent_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_MotionAsset.asset).

| Ability / Payload fileID | MotionKey → MotionSet | 클립 파일 / fileID | HitPhase / 타이밍 이벤트 |
|---|---|---|---|
| [GA_Golem_01_Attack_1.asset](../../10.Datas/Ability/Actor/Golem/GA_Golem_01_Attack_1.asset) / -4344629354627557580 | Golem.Attack.1.01 → [Ent_Attack_L.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_Attack_L.asset) | [Anim_Hand_Attack_L.fbx](../../ExternalAssets/Character/Ent/AnimationClips/Humanoid/Anim_Hand_Attack_L.fbx) / `-3568207325639417703` | 1개 / BeginCollisionEvent 1.1874999–1.6375 |
| [GA_Golem_02_Attack_2.asset](../../10.Datas/Ability/Actor/Golem/GA_Golem_02_Attack_2.asset) / -233378873087062010 | Golem.Attack.2.02 → [Ent_Attack_R.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_Attack_R.asset) | [Anim_Hand_Attack_R.fbx](../../ExternalAssets/Character/Ent/AnimationClips/Humanoid/Anim_Hand_Attack_R.fbx) / `8970874683280315550` | 1개 / BeginCollisionEvent 1.3125001–1.8125001 |
| [GA_Golem_03_Attack_3.asset](../../10.Datas/Ability/Actor/Golem/GA_Golem_03_Attack_3.asset) / -3250435652246376648 | Golem.Attack.3.03 → [Ent_Attack_Leg_L.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_Attack_Leg_L.asset) | [Anim_Leg_Attack_L.fbx](../../ExternalAssets/Character/Ent/AnimationClips/Humanoid/Anim_Leg_Attack_L.fbx) / `5325916286891137228` | 1개 / BeginCollisionEvent 1.0625–1.5375 |
| [GA_Golem_04_Attack_4.asset](../../10.Datas/Ability/Actor/Golem/GA_Golem_04_Attack_4.asset) / -1198163884290721349 | Golem.Attack.4.04 → [Ent_Attack_Leg_R.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_Attack_Leg_R.asset) | [Anim_Leg_Attack_R.fbx](../../ExternalAssets/Character/Ent/AnimationClips/Humanoid/Anim_Leg_Attack_R.fbx) / `2154262455844783879` | 1개 / BeginCollisionEvent 1.0250001–1.3625 |

### AbilitySet_Spider_Minion.asset

매핑: [SpiderMinion_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderMinion/SpiderMinion_MotionAsset.asset).

| Ability / Payload fileID | MotionKey → MotionSet | 클립 파일 / fileID | HitPhase / 타이밍 이벤트 |
|---|---|---|---|
| [GA_Spider_Minion_01_Attack_1.asset](../../10.Datas/Ability/Actor/Spider_Minion/GA_Spider_Minion_01_Attack_1.asset) / -8425682910851609066 | Spider.Minion.Attack.1.01 → [SpirderMinion_Attack.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderMinion/SpirderMinion_Attack.asset) | [Anim_Attack.fbx](../../ExternalAssets/Character/SpiderMnion/AnimationClips/Spider/Anim_Attack.fbx) / `6720303635017113406` | 1개 / BeginCollisionEvent 0.20629796–0.38381016 |

### AbilitySet_Spider_Queen.asset

매핑: [SpiderQueen_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen/SpiderQueen_MotionAsset.asset).

| Ability / Payload fileID | MotionKey → MotionSet | 클립 파일 / fileID | HitPhase / 타이밍 이벤트 |
|---|---|---|---|
| [GA_Spider_Queen_01_Attack_1.asset](../../10.Datas/Ability/Actor/Spider_Queen/GA_Spider_Queen_01_Attack_1.asset) / 989073661883866974 | Spider.Queen.Attack.1.01 → [SpiderQueen_Attack_1.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen/SpiderQueen_Attack_1.asset) | [Anim_Attack_Left.fbx](../../ExternalAssets/Character/SpiderQueen/AnimationClips/Anim_Attack_Left.fbx) / `-4855906848265491450` | 1개 / BeginCollisionEvent 0.3840546–0.76388884 |
| [GA_Spider_Queen_02_Attack_2.asset](../../10.Datas/Ability/Actor/Spider_Queen/GA_Spider_Queen_02_Attack_2.asset) / 7046150292475866418 | Spider.Queen.Attack.2.02 → [SpiderQueen_Attack_2.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen/SpiderQueen_Attack_2.asset) | [Anim_Attack_Right.fbx](../../ExternalAssets/Character/SpiderQueen/AnimationClips/Anim_Attack_Right.fbx) / `8457827382905983551` | 1개 / BeginCollisionEvent 0.5671296–0.9201389 |
| [GA_Spider_Queen_03_HeavyAttack_1.asset](../../10.Datas/Ability/Actor/Spider_Queen/GA_Spider_Queen_03_HeavyAttack_1.asset) / 3357154412535222100 | Spider.Queen.HeavyAttack.1.03 → [SpiderQueen_Heavy_Attack_1.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen/SpiderQueen_Heavy_Attack_1.asset) | [Anim_Attack_1.fbx](../../ExternalAssets/Character/SpiderQueen/AnimationClips/Anim_Attack_1.fbx) / `8992522252433359312` | 1개 / BeginCollisionEvent 1.0875001–1.5 |
| [GA_Spider_Queen_04_HeavyAttack_2.asset](../../10.Datas/Ability/Actor/Spider_Queen/GA_Spider_Queen_04_HeavyAttack_2.asset) / 1911244451376133068 | Spider.Queen.HeavyAttack.2.04 → [SpiderQueen_Heavy_Attack_2.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen/SpiderQueen_Heavy_Attack_2.asset) | [Anim_Attack_3.fbx](../../ExternalAssets/Character/SpiderQueen/AnimationClips/Anim_Attack_3.fbx) / `-7159147207538092535` | 1개 / BeginCollisionEvent 0.42221898–0.744858 |
| [GA_Spider_Queen_05_Skill_1.asset](../../10.Datas/Ability/Actor/Spider_Queen/GA_Spider_Queen_05_Skill_1.asset) / 1739755250251777545 | Spider.Queen.Skill.1.05 → [SpiderQueen_Skill_Call.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen/SpiderQueen_Skill_Call.asset) | [Anim_Call_Cast.fbx](../../ExternalAssets/Character/SpiderQueen/AnimationClips/Anim_Call_Cast.fbx) / `4304346519646757399` | 1개 / BeginParticleEvent 0.0625–1.4<br>SpawnSkillEvent 0.8–1.3 |
| [GA_Spider_Queen_06_Skill_2.asset](../../10.Datas/Ability/Actor/Spider_Queen/GA_Spider_Queen_06_Skill_2.asset) / -2438281605382120954 | Spider.Queen.Skill.2.06 → [SpiderQueen_Skill_Throw_Web.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen/SpiderQueen_Skill_Throw_Web.asset) | [Anim_Throw_a_web.fbx](../../ExternalAssets/Character/SpiderQueen/AnimationClips/Anim_Throw_a_web.fbx) / `5385136767152346346` | 1개 / SingleShotPattern 2.2875001–5.975 |

### AbilitySet_ChildPlant.asset

매핑: [ChildPlant_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Child/ChildPlant_MotionSet.asset).

| Ability / Payload fileID | MotionKey → MotionSet | 클립 파일 / fileID | HitPhase / 타이밍 이벤트 |
|---|---|---|---|
| [GA_ChildPlant_01_Attack_1.asset](../../10.Datas/Ability/Actor/ChildPlant/GA_ChildPlant_01_Attack_1.asset) / 5134131849532890941 | ChildPlant.Attack.1.01 → [ChildPlant_Attack.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Child/ChildPlant_Attack.asset) | [Anim_Enemy_Plant.fbx](<../../ExternalAssets/Character/Carnivorous plant character/AnimationClips/Anim_Enemy_Plant.fbx>) / `8992522252433359312` | 1개 / BeginCollisionEvent 0.51488644–0.89894104 |
| [GA_ChildPlant_02_Attack_2.asset](../../10.Datas/Ability/Actor/ChildPlant/GA_ChildPlant_02_Attack_2.asset) / 949124248931109625 | ChildPlant.Attack.2.02 → [ChildPlant_Attack_2.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Child/ChildPlant_Attack_2.asset) | [Anim_Enemy_Plant.fbx](<../../ExternalAssets/Character/Carnivorous plant character/AnimationClips/Anim_Enemy_Plant.fbx>) / `8457827382905983551` | 1개 / BeginCollisionEvent 0.15332538–0.36140978 |

### AbilitySet_dryad.asset

매핑: [Dryad_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Dryad/Dryad_MotionSet.asset).

| Ability / Payload fileID | MotionKey → MotionSet | 클립 파일 / fileID | HitPhase / 타이밍 이벤트 |
|---|---|---|---|
| [GA_dryad_01_Attack_1.asset](../../10.Datas/Ability/Actor/dryad/GA_dryad_01_Attack_1.asset) / -2874744449545469621 | dryad.Attack.1.01 → 미해석 | 직접 매핑 없음 | 1개 / 이벤트 미추출 |
| [GA_dryad_02_Attack_2.asset](../../10.Datas/Ability/Actor/dryad/GA_dryad_02_Attack_2.asset) / -7205146192179179120 | dryad.Attack.2.02 → 미해석 | 직접 매핑 없음 | 1개 / 이벤트 미추출 |
| [GA_dryad_03_Attack_3.asset](../../10.Datas/Ability/Actor/dryad/GA_dryad_03_Attack_3.asset) / -8328342747691615450 | dryad.Attack.3.03 → 미해석 | 직접 매핑 없음 | 1개 / 이벤트 미추출 |
| [GA_dryad_04_Fly_Move.asset](../../10.Datas/Ability/Actor/dryad/GA_dryad_04_Fly_Move.asset) / -4522347424176176725 | dryad.Fly.Move.04 → [Griffin_FlyMove.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Griffin/Griffin_FlyMove.asset) | [Griffin Animation.fbx](<../../ExternalAssets/Character/Griffin Character/AnimationClips/Griffin Animation.fbx>) / `5685546115976074017` | 1개 / 이벤트 미추출 |

### AbilitySet_MainPlant.asset

매핑: [MainPlant_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Main/MainPlant_MotionSet.asset).

| Ability / Payload fileID | MotionKey → MotionSet | 클립 파일 / fileID | HitPhase / 타이밍 이벤트 |
|---|---|---|---|
| [GA_MainPlant_01_Attack_1.asset](../../10.Datas/Ability/Actor/MainPlant/GA_MainPlant_01_Attack_1.asset) / -6394701907668934052 | MainPlant.Attack.1.01 → [MainPlant_Attack.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Main/MainPlant_Attack.asset) | [Anim_Main_Plant.fbx](<../../ExternalAssets/Character/Carnivorous plant character/AnimationClips/Anim_Main_Plant.fbx>) / `6720303635017113406` | 1개 / BeginCollisionEvent 0.19466728–0.4866682 |
| [GA_MainPlant_02_Skill_1.asset](../../10.Datas/Ability/Actor/MainPlant/GA_MainPlant_02_Skill_1.asset) / -4777765825056933881 | MainPlant.Skill.1.02 → [MainPlant_Skill_Spawn.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Main/MainPlant_Skill_Spawn.asset) | [Anim_Main_Plant.fbx](<../../ExternalAssets/Character/Carnivorous plant character/AnimationClips/Anim_Main_Plant.fbx>) / `-5990177634178439634` | 1개 / BeginParticleEvent 0–3.3<br>SpawnSkillEvent 0.6375–1.1375004 |
| [GA_MainPlant_03_Skill_2.asset](../../10.Datas/Ability/Actor/MainPlant/GA_MainPlant_03_Skill_2.asset) / -3976211223002069013 | MainPlant.Skill.2.03 → [MainPlant_Skill_Projectile.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Main/MainPlant_Skill_Projectile.asset) | [Anim_Main_Plant.fbx](<../../ExternalAssets/Character/Carnivorous plant character/AnimationClips/Anim_Main_Plant.fbx>) / `7435700985363743425` | 1개 / SingleShotPattern 0.5771448–1.0771447 |

### AbilitySet_PlantRoot.asset

매핑: [PlantRoot_MotionSet.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Root/PlantRoot_MotionSet.asset).

| Ability / Payload fileID | MotionKey → MotionSet | 클립 파일 / fileID | HitPhase / 타이밍 이벤트 |
|---|---|---|---|
| [GA_PlantRoot_01_Attack_1.asset](../../10.Datas/Ability/Actor/PlantRoot/GA_PlantRoot_01_Attack_1.asset) / -936174316976120525 | PlantRoot.Attack.1.01 → [PlantRoot_Attack_1.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Root/PlantRoot_Attack_1.asset) | [Anim_Enemy_Root.fbx](<../../ExternalAssets/Character/Carnivorous plant character/AnimationClips/Anim_Enemy_Root.fbx>) / `8992522252433359312` | 1개 / BeginCollisionEvent 1.3343195–2.066568 |
| [GA_PlantRoot_02_Attack_2.asset](../../10.Datas/Ability/Actor/PlantRoot/GA_PlantRoot_02_Attack_2.asset) / -1787604486800710002 | PlantRoot.Attack.2.02 → [PlantRoot_Attack_2.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant/Root/PlantRoot_Attack_2.asset) | [Anim_Enemy_Root.fbx](<../../ExternalAssets/Character/Carnivorous plant character/AnimationClips/Anim_Enemy_Root.fbx>) / `8457827382905983551` | 1개 / BeginCollisionEvent 0.37422237–0.73775274 |

## 5. 선언 경로 치수

길이는 JSON points의 3D 선분 합, 경사는 atan2(절대 높이 차, 수평거리)의 최댓값이다. Terrain·NavMesh·KCC 통과 결과가 아니다. width 설정의 생성 해석과 장애물 사이 유효 폭은 Unity에서 확인한다.

| route | width 설정 | 선분 합(m) | 최대 선언 경사(도) | 시작 → 끝 (x,y,z) |
|---|---|---|---|---|
| Main_LakePilgrimage | 3.4 | 878.7 | 12.0 | 1118.367, 51.768, 391.171 → 818, 102.98, 1082 |
| Jun_Rescue | 2.3 | 88.9 | 5.1 | 1068, 59.5, 424 → 1007, 62.41, 445 |
| Herbs_Path | 2.3 | 43.4 | 0.7 | 1012, 62.277, 444 → 994, 61.932, 482 |
| Cart_Detour | 2.3 | 49.9 | 11.1 | 974, 64.671, 506 → 962, 67.594, 522 |
| OptionalCombat_OldRoad | 2.3 | 79.9 | 2.2 | 1007, 62.41, 445 → 994, 61.932, 482 |
| Lakeside_FullCircuit | 3.2 | 1359.6 | 1.9 | 1067, 79, 570 → 1067, 79, 570 |
| Village_LakeRoad | 3.5 | 183.6 | 10.4 | 1085.91, 57.91, 420.8 → 1067, 79, 570 |
| Lake_ForestReturn | 3 | 153.3 | 2.8 | 921.942, 81.822, 600.708 → 1067, 79, 570 |
| Lake_RevealReturn | 2.8 | 75.1 | 2.9 | 885.688, 86.206, 651.38 → 935.936, 82.838, 683.429 |
| Lake_GuardianReturn | 3 | 81.4 | 2.8 | 848.373, 90.31, 779.943 → 907, 88, 836 |
| Lake_NorthTempleReturn | 3 | 264.2 | 1.9 | 825.393, 98.988, 1007.599 → 1067, 97, 1102 |
| Village_Square | 3 | 86.4 | 5.0 | 1085.91, 57.91, 420.8 → 1011.21, 62.49, 430.63 |
| Village_BackLane | 2.3 | 102.5 | 5.7 | 1011.21, 62.49, 430.63 → 1085.91, 57.91, 420.8 |
| LakeIslands_SouthArrival | 4.2 | 135.7 | 1.3 | 1067, 79, 570 → 1050, 81, 703 |
| LakeIslands_FlowerToHeart | 3.5 | 118.9 | 1.5 | 1050, 81, 703 → 1063, 83, 817 |
| LakeIslands_WestArrival | 3.6 | 165.6 | 7.9 | 907, 88, 836 → 1063, 83, 817 |
| LakeIslands_HeartToShrine | 3.2 | 147.6 | 1.1 | 1063, 83, 817 → 1080, 85, 960 |
| LakeIslands_EastArrival | 3.6 | 90.7 | 3.6 | 1227, 88, 836 → 1149, 83, 880 |
| LakeIslands_HeartToBamboo | 3.3 | 119.2 | 0.2 | 1063, 83, 817 → 1149, 83, 880 |
| LakeIslands_BambooToShrine | 3.2 | 115.7 | 1.7 | 1149, 83, 880 → 1080, 85, 960 |
| LakeIslands_NorthReturn | 3.6 | 148.1 | 7.0 | 1080, 85, 960 → 1067, 97, 1102 |
| LakeIslands_NorthwestArrival | 3.7 | 97.0 | 8.0 | 944.433, 93.785, 1006.982 → 1006, 84, 934 |
| LakeIslands_NorthwestToBamboo | 3.3 | 162.0 | 0.6 | 1006, 84, 934 → 1149, 83, 880 |
| LakeIslands_FlowerToSoutheast | 3.3 | 95.1 | 0.6 | 1050, 81, 703 → 1142, 81.5, 724 |
| LakeIslands_SoutheastToBamboo | 3.3 | 159.1 | 0.8 | 1142, 81.5, 724 → 1149, 83, 880 |
| LakeIslands_HeartShrineApproach | 2.5 | 12.1 | 0.0 | 1094, 83.1, 814 → 1095, 83.1, 826 |

## 6. 입력 파일 식별

파일이 바뀌면 아래 SHA-256과 비교해 연결을 재조사한다. 과거 테스트 결과의 통과 인증이 아니다.

| 파일 | SHA-256 |
|---|---|
| [LakeOfLifeScenario.unity](../../01.Scenes/Scenario/LakeOfLifeScenario.unity) | 8aa0a7bb5a501eddaf345183e7eb5e6a46d812e01e08d8f741823af1684d27be |
| [LakeOfLife.unity](../../01.Scenes/Ingame/LakeOfLife.unity) | ccb4477392cdf5fc3eded0f2ebba9904b9127335d9d31a0f4bd012b392ddc41f |
| [LakeOfLifeScenario.layout.json](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario.layout.json) | 679d91107aa143127e3b78e4d72984bf102fc3b32cac40b5161630e7fb287893 |
| [LakeOfLifeScenario.world.json](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario.world.json) | d999d5bf7cba72a150a43a34441bf74b89fe697627a877e1244bf85e8c1662f1 |
| [MapBackground.png](../../10.Datas/World/ScenarioMaps/LakeOfLifeScenario/MapBackground.png) | 9962ce36d9f76f770dbcab8ae8907534be709d53bc5b6accf7b94ea2793ce076 |
| [PartyMemberData.asset](../../10.Datas/Party/PartyMemberData.asset) | 0d227155751eaecb09ac49d1215cbf889ca067e654f91e8719f4a47c877dfff1 |
| [SpeakerActorBindingTable.asset](../../10.Datas/Dialogue/SpeakerActorBindingTable.asset) | 9218db20b2ff359d1aab5acaf9019fea250b601e294c44ae7f2e2cdfdad89bdc |
| [Ent_MotionAsset.asset](../../10.Datas/Actor/Animation/ActorMotion/MotionSet/Ent/Ent_MotionAsset.asset) | 7cb4e8ac98558e6af25711a77061285d3e0165c6c1efc307aa28ea2edde953ab |
