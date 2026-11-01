# 맵·월드 오브젝트와 아트 선택 가이드

> 조사 기준: 2026-10-31. 맵·월드 제작자가 건축, 생활 소품, 지형과 식생을 찾기 위한 목록이다. 경로는 Unity 프로젝트 루트 기준이다. 파일 존재와 이름을 확인했으며, 별도 표기가 없으면 개별 프리팹의 외형·충돌·동작·성능은 검증하지 않았다.

## 사용 순서

1. 지역의 역할과 플레이어 동선, 랜드마크를 정하고 아래 지역별 조합에서 후보를 고른다.
2. 기본 건축 세트 하나를 정한 뒤 지형·식생·생활 소품을 보완한다. 모델 원본보다 준비된 프리팹을 우선 살펴본다.
3. 플레이어 높이에서 대표 건물·바닥·식생·소품을 함께 배치해 형태, 색, 재질, 크기와 조명을 확인한다. 낮과 밤, 근거리와 원거리에서도 확인한다.
4. 보행·카메라 시야·상호작용·저장 진행을 확인하고 필요한 충돌과 NavMesh를 설정한다. 장식 프리팹이 실제 상호작용이나 함정 기능을 갖췄다고 가정하지 않는다.
5. 생성형 월드는 [월드 저작 README](../../../Tools/OpenWorld/README.md)의 최신 생성·증분 저작 및 보존 경계를 먼저 확인한다. 일반 씬 배치는 [Map Placement Tool 가이드](MAP_PLACEMENT_TOOL_GUIDE.md)를 참고한다.

## 프로젝트 환경 오브젝트

수량은 해당 경로에서 확인한 프리팹 파일 수다. 조립 프리팹도 포함하며 모델 파일과 일대일로 대응한다고 가정하지 않는다.

| 세트와 경로 | 선택할 수 있는 오브젝트 | 추천 용도와 아트 방향 |
|---|---|---|
| [StonegateEast](../../03.Prefabs/Environment/WorldCities/StonegateEast/) · 38개 | 본성, 성문, 성벽, 망루, 해자 다리, 주택·여관·상점·대장간, 시장, 석등, 포장 도로와 광장 | 동양풍 성채 도시. 실제 돌등성 캡처에서는 어두운 기와, 붉은 목재, 밝은 벽과 석재 기단이 주된 형태다. 위계가 드러나는 성문→광장→본성 동선에 사용한다. |
| [LakeVillage](../../03.Prefabs/Environment/LakeVillage/) · 20개 | 어부 집·수상가옥·상점·선고, 사당 본전·도리이·석등·수반·울타리 등 | 호수·항구 주거와 사당. 수변 주택 8개와 사당 부속 12개로 구성된다. 항구 캡처에는 목재·석재와 동양풍 건물이 함께 보인다. 수변 단차와 지붕 형태를 비교해 선정한다. |
| [PoorVillage](../../03.Prefabs/Environment/PoorVillage/) · 33개 | 초가·움집·주막·곳간·헛간, 장승·장독대·지게·평상·우물·울타리, 논둑·바위·돌계단·고목 | 소박한 농촌·외곽 취락. 건물과 생활 소품을 함께 배치해 거주 흔적을 만든다. 이름 기반 용도 분류이며 실제 재질과 완성도는 배치 전 확인한다. `PoorVillage_Layout`은 개별 소품과 별도의 조합 프리팹이다. |
| [Dungeon](../../03.Prefabs/Environment/Dungeon/) · 47개 | 성채·감옥·납골당·제단, 바닥·문·벽·계단, 상자·책장·화로·석관, 가시·화살·칼날 등 함정 외형 | 지하 시설·폐허·성채 내부 후보. 모듈 연결부, 뒷면, 스케일과 충돌을 확인한다. 함정 이름은 외형 분류이며 피해·작동 구현의 근거가 아니다. |
| [HarborRevision](../../03.Prefabs/Environment/HarborRevision/) · 4개 | `Harbor_TimberDeck`, `Harbor_StoneQuay`, `Harbor_Mooring`, `Harbor_FishingSkiff` | 목재 잔교·석축·계선주·어선. 수변 마을에 항구 기능을 부여하는 보조 키트다. 수면 높이와 탑승·낙하 경계를 확인한다. |
| [ShadeCastle](../../08.Models/WorldCities/ShadeCastle/Prefabs/) · 57개 | 성채·성벽·망루·수로, 주택·시장·축사, 절벽·바위·고목·돌계단 | 성채·마을·자연 지형을 한 세트에서 고를 수 있는 후보. 이 목록은 이름과 경로 기준이며 실제 화면·기존 돌등성과의 화풍 일치는 별도 확인한다. |
| [OpenWorld/Dungeons](../../03.Prefabs/Environment/OpenWorld/Dungeons/) · 3개 | `BriarRing`, `ChainTemple`, `SunkenHall` | 기존 던전 저작에서 파생한 조립 공간. 개별 모듈로 추정하지 말고 프리팹 구조·진입면·충돌을 확인한다. |

대응 모델 원본은 [08.Models](../../08.Models/) 아래 `WorldCities`, `LakeVillage`, `PoorVillage`, `Dungeon` 등을 확인한다. [05.Models](../../05.Models/)에도 환경 모델이 있다. `.fbx`는 모델 원본이며, 콜라이더·머티리얼·계층이 준비된 배치용 프리팹과 동일한 것으로 취급하지 않는다.

## 기존 외부 에셋 후보

아래 화풍과 용도는 패키지명·폴더·파일명에 근거한 **탐색 방향**이다. 각 패키지를 이번 조사에서 실제 렌더링해 미술 품질이나 URP 호환성을 확인하지 않았다. 프리팹 수에는 효과나 동물 등도 포함될 수 있다.

| 패키지 | 찾을 수 있는 것 | 조합 전 확인할 점 |
|---|---|---|
| [Toon Golden Valley](../../ExternalAssets/Environment/Toon%20Golden%20Valley/Prefabs/) · 365개 | 건물 키트, 소품, 바위, 식생, 원경 산 | 툰 계열 후보. 동양풍 성채와 지붕 실루엣·텍스처 밀도·색을 비교하고 건축 문화권이 맞는 구역에 사용한다. |
| [Dreamscape East Lands](../../ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/) · 270개 | 나무·풀·꽃·수생식물, 바위, 물, 소품, 사슴 | 동양풍 자연환경 후보. 지역의 기존 나무 및 지형과 색·잎 크기·그림자·LOD를 비교한다. |
| [Idyllic Fantasy Nature](../../ExternalAssets/Environment/Idyllic%20Fantasy%20Nature/Prefabs/) · 146개 | 꽃나무, 색상별 활엽수, 관목, 가지, 갈대 등 | 판타지 식생 후보. 강한 색상의 나무는 지역의 계절·분위기와 플레이어 시선 유도에 맞춰 고른다. |
| [Fantastic Dungeon Pack](../../ExternalAssets/Environment/Fantastic%20Dungeon%20Pack/prefabs/) · 1,087개 | 기둥·벽·바닥 등의 던전 모듈, 소품, 불·안개 등 FX | 프로젝트 던전과 석재 크기·표면 묘사·조명 반응을 비교한다. 데모 효과와 스크립트는 필요한 것만 사용한다. |
| [LowPolyFantasyArena2](../../ExternalAssets/Environment/LowPolyFantasyArena2/Prefabs/) · 84개 | 바위·나무·풀·유적·울타리·통·불과 안개 | 로우폴리 형태 후보. 실루엣과 면 분할이 주변 오브젝트보다 지나치게 단순해 보이는지 확인한다. |
| [Farmlands](../../ExternalAssets/Environment/PolyartStudio/Farmlands/Prefabs/) · 6개 | 원경 언덕·산과 폭포 안개 | 원경과 물 주변 후보. 거리에 따른 크기·안개·효과 비용을 확인한다. |

전체 프리팹·모델 경로는 로컬 [파일 조사 목록](../../../output/ObjectInventory/OBJECT_INVENTORY_2026-10-31.md)에서 검색할 수 있다. 이 파일은 조사 시점 스냅샷이며 `output`의 보존·공유 여부는 프로젝트 환경에 따라 다르다. 캐릭터와 무기도 포함하고 `.asset` 메시·FBX 내부 서브메시·씬 인스턴스는 빠진다.

## 지역별 조합 출발점

| 제작할 지역 | 먼저 볼 건축·지형 | 보완할 소품·식생 | 검토할 플레이 경험 |
|---|---|---|---|
| 성채 도시 | StonegateEast 성문·성벽·본성·도로 | 시장·석등·우물, Dreamscape 식생 후보 | 멀리서 본성의 방향이 읽히고 성문·계단·골목이 끊기지 않는가. |
| 수변 마을·항구 | LakeVillage 수변 주택, HarborRevision 석축·잔교 | 어선·계선주·생활 소품, 갈대·수생식물 후보 | 부두 끝과 물가가 명확하고 주민·상호작용 위치를 가리지 않는가. |
| 농촌·변두리 | PoorVillage 주택·논둑·돌계단 | 우물·장독대·지게·고목과 풀 후보 | 주거·농사·작업 공간이 구분되고 길과 채집 위치를 읽을 수 있는가. |
| 던전·폐허 | Dungeon의 벽·바닥·계단 또는 조립형 던전 | 상자·제단·화로와 Fantastic Dungeon Pack 후보 | 입구와 출구가 읽히고 전투 공간·회피·카메라 시야가 확보되는가. |
| 숲·고원 | 기존 Terrain, Dreamscape·Idyllic 나무와 바위 후보 | 풀·꽃·고목·원경 산 후보 | 길의 실루엣과 목적지를 식생이 가리지 않고, 반복 군락이 지루하지 않은가. |

한 지역에서 서로 다른 팩을 섞을 때는 색상만 맞추지 말고 **실제 캐릭터 높이 대비 스케일, 윤곽, 표면 디테일 밀도, 광택, 그림자와 조명 반응**을 함께 본다. 이 목록의 추천 조합은 출발점이며 미술 승인이나 실제 플레이 검증을 대신하지 않는다.

## 저작과 검증 경계

- `LakeVillageWorld`는 생성 씬과 직접 저작이 함께 존재한다. 전체 생성이 최신 배치를 덮어쓸 수 있으므로 실행 전 [월드 저작 README](../../../Tools/OpenWorld/README.md)의 최신 기록과 백업·재적용 경계를 확인한다.
- 장식과 게임플레이 오브젝트를 구분한다. 채집·보상·포털·NPC 등 저장 ID가 있는 콘텐츠는 기존 콘텐츠 계약과 [컨텐츠 시스템 지침](CONTENT_SYSTEM_AUTHORING_GUIDE.md)을 따른다.
- 돌등성 진입부는 `Assets/10.Datas/World/OpenWorld/CitadelArtRevision/KeepCityEntry.asset`과 `CityEntryStair.asset` 파생 메시가 필요하다. 생성 씬과 일부 데이터는 Git ignore 대상이므로 에셋 전달 시 실제 파일과 `.meta`를 함께 확인한다.
- 프리팹별 머티리얼 누락, 셰이더, 스케일·피벗, 지면 접지, 충돌, NavMesh, 가림과 성능을 실제 씬에서 점검한다. 미술용 정지 캡처와 Editor 경로 검사는 Play Mode 입력 보행·저장/재진입을 대신하지 않는다. 검증 범위는 [테스트 작업 지침](TEST_VALIDATION_GUIDE.md)을 따른다.

## 조사 기준과 한계

대상은 `Assets/ExternalAssets`, `Assets/03.Prefabs`, `Assets/05.Models`, `Assets/08.Models`의 프리팹 및 모델 파일이다. 2026-10-31에 수정된 파일 396개는 신규 후보이며 수정·생성 시각만으로 최초 제작일을 확정할 수 없다. 일부 대상이 Git ignore이므로 Git 이력의 신규 파일 부재도 신규 에셋 부재를 뜻하지 않는다. 돌등성·항구의 아트 설명은 해당 날짜의 Editor 캡처를 참고했으며 조명·LOD를 조정한 정지 화면이다.
