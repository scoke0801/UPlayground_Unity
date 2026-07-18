# UPlayground 프로젝트 안내서 — 비개발 팀원용

> **보관 문서 주의:** 이 문서의 플레이어 공격 데이터 설명은 Ability 전환 이전 구조다. 현재 단일 소스는 `AbilitySetSO`이며 최신 기준은 `../TODO/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`를 따른다.

> 이 문서는 기획자, 아티스트, 사운드 디자이너, 시나리오 작가 등  
> 프로그래밍 경험이 없는 팀원을 위해 작성되었습니다.

---

## 이 게임은 무엇인가요?

**UPlayground**는 1인칭 뒤에서 바라보는 시점(TPS)의 싱글플레이 액션 게임입니다.  
Unity 6 엔진으로 제작 중이며, 1인 개발 프로젝트입니다.

- 플레이어는 **보쿠세이(Bokusei)** 를 기본 캐릭터로 시작합니다.
- 적을 처치하면 **새로운 동료 캐릭터를 파티에 영입**할 수 있습니다.
- 영입된 캐릭터는 언제든지 전환하며 플레이할 수 있습니다.

---

## 등장 캐릭터

| 캐릭터 이름 | 설명 |
|-------------|------|
| Bokusei | 기본 플레이어블 캐릭터 (항상 사용 가능) |
| Honoka | 적 처치를 통해 영입 가능 |
| Reine | 적 처치를 통해 영입 가능 |
| LianLian | 적 처치를 통해 영입 가능 |
| Nenmir | 적 처치를 통해 영입 가능 |
| Sera | 적 처치를 통해 영입 가능 |
| Inori | 적 처치를 통해 영입 가능 |
| H09 | 적 처치를 통해 영입 가능 |

캐릭터를 전환할 때 게임 내부적으로는 **같은 플레이어 객체가 겉모습(모델)만 바꿉니다.**  
마치 의상을 갈아입는 것과 비슷한 구조입니다.

---

## 게임의 주요 시스템 — 쉬운 말로 설명

### 1. 매니저 시스템 — "담당 부서들"

게임이 시작되면 **21개의 담당 부서(매니저)** 가 순서대로 준비 작업을 합니다.  
각 부서는 정해진 역할만 담당하며 서로 협력합니다.  
일부 부서는 그 안에 더 세분화된 **핸들러(소속 팀)** 를 두어 관련 기능을 묶어서 처리합니다.

| 부서 이름 | 담당 역할 |
|-----------|-----------|
| SaveManager | 게임 저장·불러오기 |
| InputManager | 키보드/게임패드 입력 처리 |
| SettingsManager | 그래픽/오디오/키 설정 적용 |
| AssetManager | 이미지·사운드·UI 파일 불러오기 |
| UIManager | 화면에 표시되는 모든 UI 관리 |
| CameraManager | 카메라 움직임·흔들림 효과 |
| ItemManager | 아이템 목록 관리 |
| InventoryManager | 플레이어 인벤토리 |
| EventManager | 게임 내 이벤트 전달 (예: "적이 사망했다") |
| **GameCombatManager** | **전투 관련 효과 총괄 (산하 핸들러로 처리)** |
| GlobalFlagManager | 퀘스트 조건 기록 (예: "특정 이벤트를 봤는가") |
| DialogueManager | 대화 연출 실행 |
| StoryManager | 스토리 진행 관리 |
| GameTimeManager | 게임 내 시간 흐름 |
| ActorSpawnManager | 적·NPC·오브젝트 생성 |
| PartyManager | 파티 구성, 캐릭터 해금, 전환 |
| SceneManager | 씬(맵) 전환 및 로딩 화면 |
| CheatManager | 개발용 테스트 치트 기능 |
| RecipeManager | 제작(크래프팅) 레시피 |
| QuestManager | 퀘스트 목표 추적·보상 지급 |

**GameCombatManager 소속 핸들러:**

| 핸들러 이름 | 담당 역할 |
|-------------|-----------|
| HitStopHandler | 타격 시 잠깐 느려지는 효과 (히트 스탑) |
| VitalOrbHandler | 회복 구슬(오브) 스폰 규칙 |

---

### 2. 캐릭터(Actor) 종류 — "게임에 존재하는 모든 존재"

게임 안의 모든 움직이는 존재는 **GameActor**라는 공통 기반을 가집니다.

```
GameActor (모든 존재의 공통 기반)
├── PlayerActor        — 플레이어가 조종하는 캐릭터
├── MonsterActor       — 적 (일반, 엘리트, 보스)
├── NpcActor           — 대화 가능한 NPC
├── GatheringActor     — 채집·낚시 오브젝트
├── ItemActor          — 바닥에 드랍된 아이템
├── VitalOrbActor      — 회복 오브(구슬)
└── Projectile         — 날아가는 투사체 (직선형 / 범위형)
```

---

### 3. 상태 시스템 — "캐릭터가 지금 무엇을 하고 있는가"

모든 캐릭터는 **한 번에 하나의 상태**에 있습니다.  
예를 들어 플레이어는 "대기 중", "달리는 중", "공격 중" 같은 상태 중 하나에 있으며,  
조건이 맞으면 다른 상태로 전환됩니다.

**플레이어의 상태 목록 (19가지):**

| 상태 | 설명 |
|------|------|
| Idle | 가만히 서 있음 |
| GroundMove | 이동 중 |
| Airborn | 공중에 떠 있음 |
| Attack | 공격 중 |
| DashAttack | 대시 공격 중 |
| JumpAttack | 점프 공격 중 |
| FinishAttack | 피니셔 공격 중 |
| Charge | 차징(모으기) 중 |
| Dash | 대시 중 |
| Dodge | 회피 중 |
| Guard | 가드 중 |
| GuardBreak | 가드 브레이크 당함 |
| Crouching | 앉아 있음 |
| Hit | 피격 당함 |
| Death | 사망 |
| Interaction | 상호작용 중 |
| Grabbed | 잡힘 |
| Stop | 멈춤 |
| TurnInPlace | 제자리 방향 전환 |

**적의 상태 목록 (15가지 + 비행 전용 9가지):**

지상 적은 순찰 → 발견 → 추격 → 공격 → 퇴각 등 AI에 따라 상태를 전환합니다.  
비행하는 적은 별도의 비행 상태 세트(이륙, 공중 선회, 다이브 공격 등)를 가집니다.

---

### 4. 애니메이션 시스템 — "동작이 어떻게 재생되는가"

게임의 모든 동작(공격, 이동, 피격 등)은 **MotionSet**이라는 단위로 관리됩니다.

- **MotionSet** = 하나의 행동을 구성하는 애니메이션 클립들의 순서
- 예: "3단 콤보 공격" = [1타 클립 → 2타 클립 → 3타 클립]
- 각 클립에는 **타이밍 이벤트**가 붙어 있어, 정확한 프레임에 효과가 발동됩니다.

**타이밍 이벤트로 할 수 있는 것들:**

| 이벤트 종류 | 실제 효과 |
|-------------|-----------|
| Collision | 히트박스 활성화 (타격 판정) |
| Particle | 파티클 VFX 재생 |
| PlaySound | 효과음 재생 |
| FootStep | 발소리 재생 |
| CameraEffect | 카메라 흔들림 등 연출 |
| TimeScale | 타격 시 슬로우 모션 |
| SpawnProjectile | 투사체 발사 |
| ComboWindow | 다음 콤보 입력 허용 구간 |
| Invincibility | 무적 구간 활성화 |

---

### 5. 입력 시스템 — "어떤 화면이 열려 있느냐에 따라 입력이 달라진다"

게임은 **5단계 우선순위**로 입력을 처리합니다.  
팝업이 열려 있으면 그 아래 인게임 조작은 막힙니다.

| 우선순위 (낮을수록 먼저 차단됨) | 화면 |
|--------------------------------|------|
| HUD (가장 낮음) | 인게임 플레이 화면 |
| Scene | 씬 내 상호작용 |
| Popup | 인벤토리·퀘스트 등 팝업 |
| System | 설정 화면 |
| Top (가장 높음) | 치트 콘솔 등 최상위 |

또한 **선입력(InputBuffer)** 기능이 있어, 동작이 끝나기 직전에 버튼을 눌러도  
다음 동작이 자연스럽게 이어집니다.

---

### 6. 데이터 구조 — "수치와 설정은 어디에 있나"

모든 게임 수치(적의 체력, 공격력, 아이템 정보 등)는 **ScriptableObject(SO)** 라는  
별도 데이터 파일로 분리되어 있습니다. 코드를 건드리지 않고 수치만 조정할 수 있습니다.

| 데이터 파일 | 담고 있는 내용 |
|-------------|---------------|
| ItemDatabase | 전체 아이템 목록 |
| EnemyStatsSO | 몬스터 스탯 (체력, 공격력 등) |
| EnemyBehaviorSO | 몬스터 AI 행동 패턴 (페이즈별) |
| AbilitySetSO | 몬스터 공격 데이터 (다단 히트 포함) |
| PlayerAttackDataSO | 플레이어 공격 데이터 |
| PartyConfigSO | 시작 파티 구성 |
| MotionSetAsset | 애니메이션 타임라인 |
| RecipeDatabase | 제작 레시피 목록 |
| SettingsData | 그래픽·오디오·키 설정 |

---

### 7. UI 시스템 — "화면에 나오는 것들"

모든 UI는 **캔버스 레이어**에 따라 앞뒤가 결정됩니다.

| 레이어 | 표시되는 것 |
|--------|------------|
| HUD | 체력바, 스태미나, 미니맵 등 인게임 HUD |
| Scene | 씬 오버레이 (씬 전환 연출 등) |
| Popup | 인벤토리, 제작창, 대화창 등 팝업 |
| System | 설정, 일시정지 메뉴 |
| WorldSpace | 적 머리 위의 HP바 등 3D 공간 UI |

---

## 에디터 도구 — "Unity 안에서 쓸 수 있는 편의 기능들"

Unity 상단 메뉴의 **UPlayGround** 항목에서 다양한 에디터 도구를 사용할 수 있습니다.  
주로 데이터를 생성하거나 편집할 때 씁니다.

| 메뉴 위치 | 도구 이름 | 주요 용도 |
|-----------|-----------|-----------|
| UPlayGround/Item/Item Editor | 아이템 에디터 | 아이템 생성·편집 |
| UPlayGround/Crafting/Recipe Editor | 레시피 에디터 | 제작 레시피 편집 |
| UPlayGround/Drop Table Editor | 드랍 테이블 에디터 | 몬스터가 드랍하는 아이템 설정 |
| UPlayGround/Quest/Quest Editor | 퀘스트 에디터 | 퀘스트 생성·편집 |
| UPlayGround/Map/Map Placement Tool | 맵 배치 툴 | 씬에 적·NPC·포탈 배치 |
| UPlayGround/Minimap/Minimap Capture | 미니맵 캡처 | 씬을 위에서 촬영해 미니맵 이미지 생성 |
| UPlayGround/Stat/Stat Database Editor | 스탯 에디터 | 캐릭터/몬스터 수치 검색·편집·비교 |
| Window/MotionSet Editor | 모션셋 에디터 | 애니메이션 타임라인 편집 |
| UPlayGround/Cheat Console | 치트 콘솔 | 개발 테스트용 치트 명령 |

---

## 프로젝트 파일 구조 — "어디에 무엇이 있나"

```
Assets/
├── 01.Scenes/          게임 씬 파일 (맵)
├── 02.Scripts/         모든 프로그래밍 코드
├── 10.Datas/           수치 데이터 파일 (ScriptableObject)
│   ├── Actor/          캐릭터·몬스터 데이터
│   └── Item/           아이템 데이터
└── docs/               시스템 설명 문서 모음
```

---

## 상세 시스템 문서 목록

각 시스템에 대해 더 자세히 알고 싶다면 아래 문서들을 참고하세요.  
(이 문서들은 기술적인 내용을 포함하고 있습니다.)

| 문서 | 내용 |
|------|------|
| QUEST_SYSTEM_GUIDE.md | 퀘스트 시스템 |
| DIALOGUE_SYSTEM_GUIDE.md | 대화 시스템 |
| STORY_SYSTEM_GUIDE.md | 스토리 진행 시스템 |
| ITEM_DATA_SYSTEM_GUIDE.md | 아이템 데이터 구조 |
| CRAFTING_SYSTEM_GUIDE.md | 제작(크래프팅) 시스템 |
| SAVE_SYSTEM_GUIDE.md | 세이브·로드 시스템 |
| CAMERA_SYSTEM_GUIDE.md | 카메라 시스템 |
| MINIMAP_SYSTEM_GUIDE.md | 미니맵 시스템 |
| STAT_SYSTEM_GUIDE.md | 스탯 시스템 |
| INPUT_SYSTEM_GUIDE.md | 입력 시스템 |

---

*이 문서는 `Assets/docs/ProjectReadme.md` 를 기반으로 작성되었습니다.*
