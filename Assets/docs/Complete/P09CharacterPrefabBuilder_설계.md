# P09CharacterPrefabBuilder 설계 문서

> **보관 문서 주의:** 이 문서의 플레이어 공격 데이터 예시는 Ability 전환 이전 구조다. 현재 프리팹은 `CharacterModelData.abilitySet`을 연결하며 최신 기준은 `../TODO/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`를 따른다.

Unity 6 (6000.0.60f1) 기반 TPS 액션 게임용 캐릭터 프리팹 자동 생성 에디터 툴 설계서

---

## 1. 개요 및 목적

### 1.1 배경

P09_Modular_Humanoid 에셋은 159개의 ScriptableObject로 구성된 모듈러 커스터마이징 시스템을 제공하지만, 데모 씬용 런타임 시스템에 머물러 있어 게임 내 NPC/Player/Enemy 프리팹을 손수 조립해야 한다. 갑옷 5부위 × 13개 옵션, 헤어 14종, 얼굴 11종, 무기 13+종을 매번 인스펙터로 끌어다 맞추면 캐릭터 1체당 30분 이상이 소요되며, 캐릭터마다 `MonsterActor`/`EnemyMovementController`/`KinematicCharacterMotor` 등 10여 개의 필수 컴포넌트와 3종 이상의 ScriptableObject(`EnemyStatsSO`, `EnemyBehaviorSO`, `EnemyAttackDataSO`)를 누락 없이 셋업해야 하므로 휴먼 에러가 잦다.

### 1.2 목적

- **단일 EditorWindow에서 캐릭터 프리팹 1체를 5분 이내 완성**
- 기존 P09 SO 시스템을 **읽기 전용으로 재사용**하여 데이터 중복 없이 통합
- `CharacterActorType` / `ActorType` 등 프로젝트 enum과 정합성 확보
- Actor 타입별로 **필수 컴포넌트와 SO를 누락 없이 자동 부여**
- 이름 규칙·저장 경로·아이콘 메타데이터를 코드 한 곳에서 관리
- Phase별 점진 구현이 가능하도록 모듈화

### 1.3 비목표 (Out of Scope)

- 런타임 캐릭터 커스터마이징 UI (별도 시스템)
- 애니메이션 클립/MotionSet 자동 매핑 (Phase 4 이후 검토)
- 맵 스폰 포인트 등록

---

## 2. 파일 구조

모든 파일은 `Assets/02.Scripts/Editor/P09Builder/` 하위에 배치한다. 에디터 어셈블리는 별도 `.asmdef`로 격리한다.

```
Assets/02.Scripts/Editor/P09Builder/
├── P09Builder.Editor.asmdef                  # 에디터 어셈블리 정의
│
├── Window/
│   ├── P09CharacterPrefabBuilderWindow.cs    # 메인 EditorWindow
│   ├── Tabs/
│   │   ├── IBuilderTab.cs                    # 탭 인터페이스
│   │   ├── BasicInfoTab.cs                   # 기본정보 탭
│   │   ├── AppearanceTab.cs                  # 외형 탭
│   │   ├── WeaponTab.cs                      # 무기 탭
│   │   ├── StatsTab.cs                       # 스탯 탭
│   │   └── PreviewTab.cs                     # 미리보기 탭
│   └── Drawers/
│       ├── IconGridDrawer.cs                 # 아이콘 그리드 공통 드로어
│       ├── ColorSwatchDrawer.cs              # 컬러 SO 스와치
│       └── SectionFoldout.cs                 # 접기/펼치기 헤더
│
├── Model/
│   ├── CharacterBuildConfig.cs               # 빌드 설정 데이터 (직렬화 가능)
│   ├── CharacterBuildConfig.Validation.cs    # partial: 유효성 검증
│   ├── BuilderActorKind.cs                   # enum: NPC/Player/Enemy
│   └── BuilderArmorSlot.cs                   # enum: Head/Chest/Arm/Waist/Leg
│
├── Catalog/
│   ├── P09AssetCatalog.cs                    # P09 SO 인덱싱·캐시 (스캔)
│   ├── P09AssetCatalog.Loader.cs             # AssetDatabase 검색 로직
│   ├── IconResolver.cs                       # 아이콘 매핑 (Icons_Equipment)
│   └── P09SoTypeReference.cs                 # 외부 SO 타입 우회 참조
│
├── Build/
│   ├── PrefabBuildPipeline.cs                # 빌드 파이프라인 진입점
│   ├── Steps/
│   │   ├── IBuildStep.cs                     # 빌드 스텝 인터페이스
│   │   ├── InstantiateBaseStep.cs            # 베이스 프리팹 복제
│   │   ├── ApplyAppearanceStep.cs            # 외형 SO 적용
│   │   ├── ApplyWeaponStep.cs                # 무기 부착
│   │   ├── ToggleMagicaClothStep.cs          # MagicaCloth On/Off
│   │   ├── AttachActorComponentsStep.cs      # MonsterActor 등 부착
│   │   ├── GenerateActorDescStep.cs          # SO 자동 발급
│   │   ├── AssignStatsStep.cs                # 스탯 SO 연결
│   │   ├── NameAndSaveStep.cs                # 명명·저장
│   │   └── BuildContext.cs                   # 스텝 간 공유 컨텍스트
│   └── Naming/
│       ├── CharacterNameGenerator.cs         # 이름 생성 규칙
│       └── NameSequenceRegistry.cs           # 시퀀스 영속화
│
├── Customization/
│   ├── ICustomizationApplier.cs              # 외형 적용 추상화
│   ├── ArmorApplier.cs                       # 갑옷 SO 적용
│   ├── HairApplier.cs                        # 헤어 SO 적용
│   ├── FaceApplier.cs                        # 얼굴 SO 적용
│   ├── BodyApplier.cs                        # 신체/Bust 적용
│   └── WeaponApplier.cs                      # 무기 SO 적용
│
├── Preview/
│   ├── PreviewSceneController.cs             # PreviewScene 호스트
│   └── PreviewRenderer.cs                    # RenderTexture 렌더링
│
├── ActorTemplates/
│   ├── IActorTemplate.cs                     # 타입별 템플릿 인터페이스
│   ├── EnemyActorTemplate.cs                 # Enemy 컴포넌트·SO 묶음
│   ├── PlayerActorTemplate.cs
│   └── NpcActorTemplate.cs
│
└── Utils/
    ├── EditorPrefsKeys.cs                    # EditorPrefs 키 상수
    ├── PathConfig.cs                         # 저장 경로 상수
    ├── ProgressBarScope.cs                   # using 기반 진행률
    └── UndoGroup.cs                          # Undo 그룹 헬퍼
```

**asmdef 의존성:**

```json
{
  "name": "P09Builder.Editor",
  "rootNamespace": "Game.Editor.P09Builder",
  "references": [
    "GUID:<Game.Runtime>",
    "GUID:<Animancer>",
    "GUID:<KinematicCharacterController>",
    "GUID:<MagicaCloth2>"
  ],
  "includePlatforms": ["Editor"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_EDITOR"]
}
```

---

## 3. 클래스 다이어그램

```
┌─────────────────────────────────────────────────────────┐
│         P09CharacterPrefabBuilderWindow                  │
│  - _config: CharacterBuildConfig                         │
│  - _tabs: List<IBuilderTab>                              │
│  - _catalog: P09AssetCatalog                             │
│  - _previewController: PreviewSceneController            │
└────────┬─────────────────────────┬──────────────────────┘
         │                          │
         │ uses                     │ owns
         ▼                          ▼
┌─────────────────────┐    ┌─────────────────────────────┐
│  IBuilderTab        │◄───┤  CharacterBuildConfig       │
│  + DrawGUI()        │    │  - actorKind                │
│  + Validate()       │    │  - sex / bustSize           │
└─────────────────────┘    │  - armorSelections[5]       │
   ▲   ▲   ▲   ▲   ▲       │  - hairStyle / hairColor    │
   │   │   │   │   │       │  - faceType / emotion       │
  Basic App Wpn Stats Prev  │  - weaponGroup              │
                            │  - statsAssignment          │
                            └──────┬──────────────────────┘
                                   │ consumed by
                                   ▼
                        ┌────────────────────────────┐
                        │   PrefabBuildPipeline       │
                        │  + Build(config): Result    │
                        └─────┬───────────────────────┘
                              │ runs in order
                              ▼
                  ┌─────────────────────────────┐
                  │  IBuildStep                  │
                  │  + Execute(BuildContext)     │
                  └─────────────────────────────┘
                     ▲   ▲   ▲   ▲   ▲   ▲
                Instant Appea Wpn Magi Attach Gen Save

                              │ delegates appearance to
                              ▼
                  ┌─────────────────────────────┐
                  │  ICustomizationApplier       │
                  │  + Apply(target, so)         │
                  └─────────────────────────────┘
                     ▲   ▲   ▲   ▲   ▲
                  Armor Hair Face Body Weapon

                              │ delegates components/SOs to
                              ▼
                  ┌─────────────────────────────┐
                  │  IActorTemplate              │
                  │  + AttachComponents(go)      │
                  │  + CreateActorDescAssets()   │
                  │  + GetSaveSubFolder()        │
                  └─────────────────────────────┘
                     ▲       ▲       ▲
                  Enemy   Player    Npc
```

**참조 흐름:**

- `Window` → `Config` (양방향, 탭이 Config를 편집)
- `Window` → `Catalog` (조회만, 캐시된 SO 메타)
- `Window.OnBuildClicked` → `PrefabBuildPipeline.Build(config)` → 각 `IBuildStep` 순차 실행
- `BuildContext`가 스텝 간 산출물(GameObject, 생성된 SO 경로 등) 전달

---

## 4. EditorWindow UI 레이아웃

### 4.1 윈도우 전체 레이아웃

```
┌───────────────────────────────────────────────────────────────────────┐
│ P09 Character Prefab Builder              [Refresh Catalog] [Build ▶] │  ← 툴바
├──────────────────────────────────┬────────────────────────────────────┤
│ [기본정보][외형][무기][스탯][미리보기]│                                    │
│ ─────────────────────────────────│                                    │
│                                  │                                    │
│         탭별 컨텐츠               │       Live Preview                 │
│         (좌측 패널)               │       (우측 고정 패널, 옵션)        │
│                                  │                                    │
│                                  │       [Rotate ◄ ►] [Reset]         │
│                                  │                                    │
├──────────────────────────────────┴────────────────────────────────────┤
│ Status: 준비 완료      |  Generated Name: ENM_M_03_001                │  ← 상태바
└───────────────────────────────────────────────────────────────────────┘
```

- 좌측 70%, 우측 30% (스플리터로 조절 가능)
- 우측 미리보기 패널은 토글 가능 (기본 ON)
- 메뉴 진입: `Tools/P09 Builder/Character Prefab Builder` (단축키 `Ctrl+Shift+P`)

### 4.2 [기본정보] 탭

```
┌──────────────────────────────────────────────────┐
│ ▼ Actor 타입                                      │
│   ◉ NPC      ○ Player      ○ Enemy               │
│                                                   │
│ ▼ 캐릭터 슬롯 (Player 선택 시 활성)                │
│   CharacterActorType: [Honoka ▼]                 │
│      Bokusei / Honoka / Reine / LianLian /       │
│      Nenmir / Sera / Inori / H09                 │
│                                                   │
│ ▼ 성별 / 체형                                     │
│   Sex:       ○ Male  ◉ Female                    │
│   BustSize:  [B ▼]   (Female일 때만 활성)         │
│                                                   │
│ ▼ 자동 명명                                       │
│   Generated Name: ENM_F_03_004                   │
│   [ ] 수동 이름 사용                              │
│   Manual Name:    [____________________]         │
│                                                   │
│ ▼ 저장 경로                                       │
│   Base Folder:    Assets/03.Prefabs/Characters   │
│   Resolved:       .../Enemy/ENM_F_03_004/        │
└──────────────────────────────────────────────────┘
```

**핵심 동작:**

- `BuilderActorKind` 변경 시 즉시 저장 폴더 미리보기 갱신
- Player 선택 시 `CharacterActorType` 드롭다운 활성화 (이미 사용 중인 타입은 회색 표시)
- Sex 변경 시 BustSize/FacialHair 활성화 토글
- Manual Name 체크 해제 시 자동 생성 이름 즉시 표기 (live)

### 4.3 [외형] 탭

탭 내부에 **수직 폴드아웃 섹션** 5개:

```
▼ 갑옷 (Armor)
  ┌──────────────────────────────────────┐
  │ Head   [None] [Hd_01] ... [Hd_11]    │  ← IconGrid (8col)
  │ Chest  [None] [Ch_01] ... [Ch_13]    │
  │ Arm    [None] [Ar_01] ... [Ar_13]    │
  │ Waist  [None] [Wa_01] ... [Wa_12]    │
  │ Leg    [None] [Lg_01] ... [Lg_13]    │
  └──────────────────────────────────────┘

▼ 헤어
  Style:  [그리드 14개 아이콘 + None]
  Color:  [▢▢▢▢▢▢▢▢▢]  ← 9개 컬러 스와치

▼ 얼굴
  FaceType:    [Type01 ▼] [Type02 ▼]
  EyeColor:    [▢▢▢▢▢]
  SkinColor:   [▢▢▢] (Sex에 따라 3개씩 필터)
  Emotion:     [Idle ▼]    (10종)
  FacialHair:  [None / Beard01 ~ 09]   (Male일 때만)

▼ 신체
  BustSize: ○ A  ◉ B  ○ C   (Female 전용)

▼ 물리
  [v] MagicaCloth 사용
      └ Off 선택 시 P09_Human_No_Physics 베이스로 빌드
```

**IconGrid 동작:**

- 64×64 썸네일 (Project Icons_Equipment에서 매핑)
- 매핑 실패 시 SO의 첫 메시 프리뷰 또는 기본 회색 박스
- 단일 선택. 우클릭 컨텍스트 메뉴에 "Ping in Project" / "Clear" 제공

### 4.4 [무기] 탭

```
▼ 무기 그룹 (사전 정의된 WeaponGroup SO 활용)
  ◉ Group 사용
    [WG_Sword01 ▼]   ← 기존 3개 그룹 중 선택

  ○ 개별 지정
    Right Hand:  [Sword ▼] [그리드 13]
    Left Hand:   [Shield ▼] [그리드 5]
    Bow Slot:    [Bow ▼]
    Quiver/Arrows: [On/Off]
    Staff Slot:  [Staff ▼]

▼ 어태치먼트 본 (자동, 읽기 전용 표시)
  RightHand_Attach:  Bip001 R Hand
  LeftHand_Attach:   Bip001 L Hand
  BowBack_Attach:    Spine2_Attach
```

### 4.5 [스탯] 탭

Actor 타입에 따라 인스펙터가 동적으로 바뀐다.

**Enemy일 때:**

```
▼ EnemyStatsSO
  ◉ 기존 SO 사용:  [EnemyStats_Goblin ▼]
  ○ 새로 생성:    Template: [Default ▼]
                  HP:        100
                  ATK:        10
                  DEF:         5
                  Stagger:    50

▼ EnemyBehaviorSO
  ◉ 기존 SO 사용
  ○ 새로 생성:    Phases: 1   AggroRange: 8m

▼ EnemyAttackDataSO (다중 추가 가능)
  [+] Add  [Attack01 ▼] [Attack02 ▼]

▼ Combat Style
  ◉ Melee  ○ Ranged  ○ Balanced  ○ Support

▼ Recruitable (선택)
  [v] 처치 시 파티 합류 가능
       └ CharacterActorType: [Honoka ▼]
```

**Player일 때:**

```
▼ PlayerAttackDataSO 셋
  Combo01: [LightCombo01 ▼]
  Combo02: ...
▼ PartyConfigSO 등록
  [v] 시작 파티에 포함
  Order: [3]
```

**NPC일 때:**

```
▼ DialogueSO
  ◉ 기존 SO 사용 / ○ 새 빈 SO 생성
▼ Wander 영역
  Radius: 5m
▼ 상호작용 프롬프트
  Prompt Key: [npc_greet_001]
```

### 4.6 [미리보기] 탭

```
┌────────────────────────────────────────┐
│                                         │
│       (RenderTexture preview)           │
│       512 × 768                         │
│                                         │
│  ◄ Drag to rotate ►                     │
│                                         │
├────────────────────────────────────────┤
│ Camera FOV: [====●====]   30°          │
│ Background: [▢] (color picker)         │
│ Animation:  [Idle ▼]                   │
│ [Rebuild Preview]  [Open in SceneView] │
└────────────────────────────────────────┘
```

PreviewScene API (`UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene`)로 격리 씬에 임시 인스턴스를 생성하고 `PreviewRenderUtility`로 렌더한다. 외형 변경 시 디바운스 200ms로 재빌드.

---

## 5. 핵심 알고리즘

### 5.1 이름 자동 생성

**규칙:**

```
{TypePrefix}_{GenderCode}_{ArmorCode}_{Sequence:000}
```

| 토큰 | 규칙 |
|------|------|
| TypePrefix | NPC / PLR / ENM |
| GenderCode | M / F |
| ArmorCode | Chest 슬롯의 SO 인덱스 2자리 (없으면 `00`) |
| Sequence | 동일 prefix 조합 내 1부터 증가, 3자리 zero-pad |

**시퀀스 영속화:** `Library/P09Builder/sequence.json` (프로젝트 루트 기준 외부 파일, 버전관리 제외) 또는 `EditorPrefs` 폴백.

```csharp
public static class CharacterNameGenerator
{
    public static string Generate(CharacterBuildConfig cfg, NameSequenceRegistry registry)
    {
        string typePrefix = cfg.ActorKind switch
        {
            BuilderActorKind.Npc    => "NPC",
            BuilderActorKind.Player => "PLR",
            BuilderActorKind.Enemy  => "ENM",
            _ => "UNK"
        };

        string gender = cfg.Sex == BuilderSex.Male ? "M" : "F";
        int armorIdx  = cfg.ArmorSelections.TryGetIndex(BuilderArmorSlot.Chest);
        string armor  = armorIdx.ToString("00");

        string key = $"{typePrefix}_{gender}_{armor}";
        int seq    = registry.NextSequence(key);

        return $"{key}_{seq:000}";
    }
}
```

**중복 방지 보강:** 생성 직전 `AssetDatabase.LoadAssetAtPath`로 동일 경로 존재 여부 확인 후 충돌 시 시퀀스 +1 재시도 (최대 100회).

### 5.2 Actor_desc 자동 발급 플로우

```
GenerateActorDescStep.Execute(ctx)
│
├── template = ActorTemplateFactory.Create(cfg.ActorKind)
│
├── targetFolder = PathConfig.ActorDescFolder(ctx.PrefabFolder)
│   AssetDatabase.CreateFolder(...)
│
└── foreach (descDef in template.DescDefs)
      so = ScriptableObject.CreateInstance
      ApplyDefaults(so, cfg)
      path = $"{targetFolder}/{descDef.Name}.asset"
      AssetDatabase.CreateAsset(so, path)
      ctx.GeneratedDescs.Add(so)
```

**EnemyActorTemplate의 DescDefs:**

| 이름 | SO 타입 | 기본값 소스 |
|------|---------|------------|
| `{name}_Stats` | `EnemyStatsSO` | `Default_EnemyStats` 템플릿 SO 복제 |
| `{name}_Behavior` | `EnemyBehaviorSO` | 1페이즈 기본 행동 |
| `{name}_Attack01` | `EnemyAttackDataSO` | 1단 히트박스 기본값 |

생성된 SO는 `AttachActorComponentsStep` 종료 후 `AssignStatsStep`에서 `MonsterActor`/`EnemyCombat`/`EnemyAIController`의 SerializedField에 SerializedObject API로 주입.

### 5.3 프리팹 빌드 파이프라인

```
PrefabBuildPipeline.Build(config)
│
├─ 1. Validate(config) ─────────► 실패 시 즉시 중단, 다이얼로그 표시
│
├─ 2. AssetDatabase.StartAssetEditing()
│   try {
│     using (var undo = new UndoGroup("P09 Build"))
│     {
│       var ctx = new BuildContext(config);
│
│       new InstantiateBaseStep().Execute(ctx);          // P09_Human (or No_Physics) 복제
│       new ToggleMagicaClothStep().Execute(ctx);        // 토글
│       new ApplyAppearanceStep().Execute(ctx);          // 갑옷/헤어/얼굴/신체
│       new ApplyWeaponStep().Execute(ctx);              // 무기 부착
│       new AttachActorComponentsStep().Execute(ctx);    // MonsterActor 등
│       new GenerateActorDescStep().Execute(ctx);        // SO 발급
│       new AssignStatsStep().Execute(ctx);              // SO를 컴포넌트에 연결
│       new NameAndSaveStep().Execute(ctx);              // 명명 + SaveAsPrefabAsset
│     }
│   }
├─ 3. AssetDatabase.StopAssetEditing()
├─ 4. AssetDatabase.SaveAssets()
└─ 5. ProjectWindowUtil.ShowCreatedAsset(prefab)
```

**Undo 처리:** 빌드 도중 발생한 `Object.Instantiate` / `AddComponent` / `CreateAsset`을 한 그룹으로 묶어, 빌드 실패 시 `Undo.RevertAllInCurrentGroup()` 호출.

**InstantiateBaseStep 세부:**

```csharp
public void Execute(BuildContext ctx)
{
    string basePrefabPath = ctx.Config.UseMagicaCloth
        ? P09Paths.HumanPrefab
        : P09Paths.HumanNoPhysicsPrefab;

    if (ctx.Config.Sex == BuilderSex.Male)
        basePrefabPath = ctx.Config.UseMagicaCloth
            ? P09Paths.HumanMaleVariant
            : P09Paths.HumanNoPhysicsMaleVariant;
    // Female 분기 동일

    var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
    if (basePrefab == null) throw new BuildException($"Base prefab missing: {basePrefabPath}");

    ctx.RootInstance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
    PrefabUtility.UnpackPrefabInstance(
        ctx.RootInstance,
        PrefabUnpackMode.OutermostRoot,
        InteractionMode.AutomatedAction);
}
```

### 5.4 외형 적용 알고리즘 (ApplyAppearanceStep)

P09의 외형 SO는 데모 시스템에서 `MeshRenderer`에 메시·머티리얼을 주입하는 구조이므로, 동일한 Apply 시그니처로 추상화한다.

```csharp
public void Execute(BuildContext ctx)
{
    var rig = ctx.RootInstance.GetComponent<CharacterModelData>()
              ?? ctx.RootInstance.AddComponent<CharacterModelData>();

    foreach (var slot in BuilderArmorSlot.All)
    {
        var so = ctx.Config.ArmorSelections.Get(slot);
        if (so == null) { _armorApplier.Clear(rig, slot); continue; }
        _armorApplier.Apply(rig, slot, so);
    }

    _hairApplier.Apply(rig, ctx.Config.HairStyleSo, ctx.Config.HairColorSo);
    _faceApplier.Apply(rig, ctx.Config.FaceTypeSo,
                            ctx.Config.EmotionSo,
                            ctx.Config.EyeColorSo,
                            ctx.Config.FacialHairSo);
    _bodyApplier.Apply(rig, ctx.Config.SkinColorSo, ctx.Config.BustSizeSo);
}
```

각 Applier는 P09 SO에 정의된 mesh/material 필드를 reflection 또는 명시적 어댑터로 추출하여 SkinnedMeshRenderer에 주입한다 (7장에서 상세).

---

## 6. 주요 클래스 시그니처

### 6.1 P09CharacterPrefabBuilderWindow

```csharp
public sealed class P09CharacterPrefabBuilderWindow : EditorWindow
{
    private const string MENU_PATH = "Tools/P09 Builder/Character Prefab Builder";
    private const string PREF_KEY  = "P09Builder.WindowState";

    [SerializeField] private CharacterBuildConfig _config;
    [SerializeField] private int _activeTabIndex;
    [SerializeField] private bool _showPreview = true;

    private readonly List<IBuilderTab> _tabs = new();
    private P09AssetCatalog _catalog;
    private PreviewSceneController _preview;
    private Vector2 _scrollLeft;
    private float _splitterRatio = 0.7f;

    [MenuItem(MENU_PATH, priority = 1100)]
    public static void Open();

    private void OnEnable();
    private void OnDisable();
    private void OnGUI();
    private void DrawToolbar();
    private void DrawTabHeader();
    private void DrawActiveTabBody();
    private void DrawPreviewPanel();
    private void DrawStatusBar();

    private void RefreshCatalog();
    private void OnConfigChanged();   // 미리보기 디바운스 트리거
    private void OnBuildClicked();
    private void OnValidateClicked();
}
```

### 6.2 CharacterBuildConfig

```csharp
[Serializable]
public sealed class CharacterBuildConfig
{
    public BuilderActorKind ActorKind = BuilderActorKind.Enemy;
    public CharacterActorType PlayerCharacterType = CharacterActorType.Bokusei;

    public BuilderSex Sex = BuilderSex.Female;
    public ScriptableObject BustSizeSo;

    public ArmorSelectionMap ArmorSelections = new();   // 5슬롯
    public ScriptableObject HairStyleSo;
    public ScriptableObject HairColorSo;
    public ScriptableObject FaceTypeSo;
    public ScriptableObject EmotionSo;
    public ScriptableObject FacialHairSo;
    public ScriptableObject EyeColorSo;
    public ScriptableObject SkinColorSo;

    public bool UseWeaponGroup = true;
    public ScriptableObject WeaponGroupSo;
    public ScriptableObject SwordSo;
    public ScriptableObject ShieldSo;
    public ScriptableObject BowSo;
    public ScriptableObject StaffSo;
    public bool ShowArrows = false;

    public bool UseMagicaCloth = true;

    public StatsAssignment Stats = new();
    public bool UseManualName;
    public string ManualName;

    public string SaveBaseFolder = "Assets/03.Prefabs/Characters";

    public bool RecruitableOnDefeat;
    public CharacterActorType RecruitableAs;
}

[Serializable]
public sealed class ArmorSelectionMap
{
    [SerializeField] private ScriptableObject[] _slots = new ScriptableObject[5];
    public ScriptableObject Get(BuilderArmorSlot slot);
    public void Set(BuilderArmorSlot slot, ScriptableObject so);
    public int TryGetIndex(BuilderArmorSlot slot);   // 이름에서 인덱스 추출
}

[Serializable]
public sealed class StatsAssignment
{
    // Enemy
    public bool CreateNewEnemyStats = true;
    public ScriptableObject ExistingEnemyStatsSo;
    public int DefaultHp = 100;
    public int DefaultAttack = 10;
    public int DefaultDefense = 5;
    public int DefaultStaggerThreshold = 50;

    public bool CreateNewBehavior = true;
    public ScriptableObject ExistingBehaviorSo;

    public List<ScriptableObject> EnemyAttackSos = new();
    public EnemyCombatStyle CombatStyle = EnemyCombatStyle.Melee;

    // Player
    public List<ScriptableObject> PlayerAttackSos = new();
    public bool AddToStartingParty;
    public int PartyOrder;

    // NPC
    public ScriptableObject DialogueSo;
    public float WanderRadius = 5f;
    public string InteractionPromptKey;
}
```

### 6.3 P09AssetCatalog

```csharp
public sealed class P09AssetCatalog
{
    private const string ROOT =
        "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Scenes/DemoScene_Data/ScriptableObject";

    public IReadOnlyList<ScriptableObject> Heads       { get; private set; }
    public IReadOnlyList<ScriptableObject> Chests      { get; private set; }
    public IReadOnlyList<ScriptableObject> Arms        { get; private set; }
    public IReadOnlyList<ScriptableObject> Waists      { get; private set; }
    public IReadOnlyList<ScriptableObject> Legs        { get; private set; }
    public IReadOnlyList<ScriptableObject> HairStyles  { get; private set; }
    public IReadOnlyList<ScriptableObject> HairColors  { get; private set; }
    public IReadOnlyList<ScriptableObject> FaceTypes   { get; private set; }
    public IReadOnlyList<ScriptableObject> Emotions    { get; private set; }
    public IReadOnlyList<ScriptableObject> FacialHairs { get; private set; }
    public IReadOnlyList<ScriptableObject> EyeColors   { get; private set; }
    public IReadOnlyList<ScriptableObject> SkinColorsMale   { get; private set; }
    public IReadOnlyList<ScriptableObject> SkinColorsFemale { get; private set; }
    public IReadOnlyList<ScriptableObject> BustSizes   { get; private set; }
    public IReadOnlyList<ScriptableObject> Swords      { get; private set; }
    public IReadOnlyList<ScriptableObject> Shields     { get; private set; }
    public IReadOnlyList<ScriptableObject> WeaponGroups{ get; private set; }

    public void Refresh();                          // AssetDatabase.FindAssets 풀 스캔
    public Texture2D GetIcon(ScriptableObject so);
    public string GetDisplayName(ScriptableObject so);
}
```

### 6.4 IBuilderTab

```csharp
public interface IBuilderTab
{
    string Title { get; }
    void Initialize(P09CharacterPrefabBuilderWindow window);
    void DrawGUI(CharacterBuildConfig config, P09AssetCatalog catalog);
    IEnumerable<string> Validate(CharacterBuildConfig config);
}
```

### 6.5 PrefabBuildPipeline

```csharp
public sealed class PrefabBuildPipeline
{
    private readonly List<IBuildStep> _steps;

    public PrefabBuildPipeline()
    {
        _steps = new List<IBuildStep>
        {
            new InstantiateBaseStep(),
            new ToggleMagicaClothStep(),
            new ApplyAppearanceStep(),
            new ApplyWeaponStep(),
            new AttachActorComponentsStep(),
            new GenerateActorDescStep(),
            new AssignStatsStep(),
            new NameAndSaveStep()
        };
    }

    public BuildResult Build(CharacterBuildConfig config);
}

public readonly struct BuildResult
{
    public bool Success { get; }
    public GameObject Prefab { get; }
    public string PrefabPath { get; }
    public IReadOnlyList<string> GeneratedAssetPaths { get; }
    public string ErrorMessage { get; }
}

public interface IBuildStep
{
    void Execute(BuildContext ctx);
}

public sealed class BuildContext
{
    public CharacterBuildConfig Config { get; }
    public GameObject RootInstance { get; set; }
    public string PrefabFolder { get; set; }
    public string PrefabName { get; set; }
    public List<ScriptableObject> GeneratedDescs { get; } = new();
    public Dictionary<string, object> Bag { get; } = new();
}
```

### 6.6 IActorTemplate

```csharp
public interface IActorTemplate
{
    BuilderActorKind Kind { get; }
    string SubFolderName { get; }            // "Enemy" / "Player" / "NPC"
    void AttachComponents(GameObject root, CharacterBuildConfig config);
    IEnumerable<DescAssetDef> GetDescDefs(CharacterBuildConfig config);
    void WireDescAssets(GameObject root, IReadOnlyList<ScriptableObject> generated,
                        CharacterBuildConfig config);
}

public readonly struct DescAssetDef
{
    public string AssetNameSuffix { get; }
    public Type ScriptableObjectType { get; }
    public Action<ScriptableObject, CharacterBuildConfig> ApplyDefaults { get; }
}
```

**EnemyActorTemplate 구현 골자:**

```csharp
public sealed class EnemyActorTemplate : IActorTemplate
{
    public BuilderActorKind Kind => BuilderActorKind.Enemy;
    public string SubFolderName => "Enemy";

    public void AttachComponents(GameObject root, CharacterBuildConfig cfg)
    {
        var motor    = Undo.AddComponent<KinematicCharacterMotor>(root);
        motor.Capsule.radius = 0.4f;
        motor.Capsule.height = 1.8f;

        Undo.AddComponent<EnemyMovementController>(root);
        var actor    = Undo.AddComponent<MonsterActor>(root);
        Undo.AddComponent<EnemyAIController>(root);
        Undo.AddComponent<EnemyDetection>(root);
        Undo.AddComponent<EnemyCombat>(root);
        Undo.AddComponent<PoiseStat>(root);
        Undo.AddComponent<ActorColorChanger>(root);
        Undo.AddComponent<DissolveController>(root);

        if (root.GetComponent<Animator>() == null)
            Undo.AddComponent<Animator>(root);
        if (root.GetComponent<CapsuleCollider>() == null)
        {
            var col = Undo.AddComponent<CapsuleCollider>(root);
            col.radius = 0.4f; col.height = 1.8f;
            col.center = new Vector3(0, 0.9f, 0);
        }

        if (cfg.RecruitableOnDefeat)
            ReflectionUtil.SetSerializedField(actor, "_recruitableAs", cfg.RecruitableAs);
    }

    public IEnumerable<DescAssetDef> GetDescDefs(CharacterBuildConfig cfg)
    {
        yield return new DescAssetDef("_Stats", typeof(EnemyStatsSO), (so, c) => {
            ReflectionUtil.SetSerializedField(so, "_maxHp",              c.Stats.DefaultHp);
            ReflectionUtil.SetSerializedField(so, "_attack",             c.Stats.DefaultAttack);
            ReflectionUtil.SetSerializedField(so, "_defense",            c.Stats.DefaultDefense);
            ReflectionUtil.SetSerializedField(so, "_staggerThreshold",   c.Stats.DefaultStaggerThreshold);
        });
        if (cfg.Stats.CreateNewBehavior)
            yield return new DescAssetDef("_Behavior", typeof(EnemyBehaviorSO), (so, c) => { /* 1페이즈 */ });
    }

    public void WireDescAssets(GameObject root, IReadOnlyList<ScriptableObject> gen,
                               CharacterBuildConfig cfg)
    {
        var actor  = root.GetComponent<MonsterActor>();
        var brain  = root.GetComponent<EnemyAIController>();
        var combat = root.GetComponent<EnemyCombat>();

        var stats    = gen.OfType<EnemyStatsSO>().FirstOrDefault()
                    ?? cfg.Stats.ExistingEnemyStatsSo as EnemyStatsSO;
        var behavior = gen.OfType<EnemyBehaviorSO>().FirstOrDefault()
                    ?? cfg.Stats.ExistingBehaviorSo  as EnemyBehaviorSO;

        ReflectionUtil.SetSerializedField(actor,  "_stats",    stats);
        ReflectionUtil.SetSerializedField(brain,  "_behavior", behavior);
        ReflectionUtil.SetSerializedField(combat, "_attacks",  cfg.Stats.EnemyAttackSos);
    }
}
```

---

## 7. P09 ScriptableObject 시스템 연동 전략

P09 에셋의 159개 SO는 데모용으로 작성되어 우리 프로젝트 어셈블리가 직접 참조하면 결합도가 높아진다. 다음 3계층 어댑터로 격리한다.

### 7.1 외부 SO 타입 우회 참조 (P09SoTypeReference)

```csharp
internal static class P09SoTypeReference
{
    public static readonly Type ArmorSoType   = ResolveType("Mythril.MH.ArmorSO");
    public static readonly Type HairStyleType = ResolveType("Mythril.MH.HairStyleSO");
    // ...

    private static Type ResolveType(string fullName) =>
        AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(fullName, throwOnError: false))
            .FirstOrDefault(t => t != null);
}
```

타입을 못 찾으면 카탈로그가 빈 리스트를 반환하고 윈도우 상단에 경고 표시.

### 7.2 SerializedObject 기반 필드 추출

P09 SO 필드명을 reflection으로 직접 알아낼 수 없을 때 `SerializedObject` + `SerializedProperty` 순회로 mesh/material 후보 필드를 휴리스틱하게 탐색.

```csharp
internal static class P09SoReader
{
    public static IEnumerable<Mesh> ExtractMeshes(ScriptableObject so)
    {
        var sObj = new SerializedObject(so);
        var it   = sObj.GetIterator();
        while (it.NextVisible(true))
        {
            if (it.propertyType == SerializedPropertyType.ObjectReference &&
                it.objectReferenceValue is Mesh m)
                yield return m;
        }
    }

    public static IEnumerable<Material> ExtractMaterials(ScriptableObject so) { /* 동일 */ }
}
```

### 7.3 Catalog 풀스캔 규칙

`P09AssetCatalog.Refresh()`는 `AssetDatabase.FindAssets("t:ScriptableObject", new[] { ROOT })`로 전체를 스캔하고, **자산 경로의 폴더명**(`Head/`, `Chest/` 등)으로 분류한다. P09 SO 클래스 타입에 의존하지 않고도 폴더 기반 분류가 안정적이다.

### 7.4 WeaponGroup SO 분해

`WeaponGroup` SO 1개는 Sword/Shield/Bow 등 다수 SO 참조를 묶는다. 이 SO도 `SerializedProperty` 순회로 내부 참조를 풀어내고, 빌드 시 개별 무기 Applier로 위임한다.

### 7.5 비파괴 원칙

- P09 SO는 **읽기 전용**으로 사용. 수정·복제·이동하지 않는다.
- 프로젝트 자산은 `Assets/03.Prefabs/Characters/` 및 `Assets/10.Datas/Generated/` 하위에만 생성.

---

## 8. 구현 시 주의사항 및 에지 케이스

### 8.1 Unity 6 / URP 관련

- **PrefabUtility.SaveAsPrefabAsset 호출 시점**: 모든 컴포넌트 부착·SO 연결이 끝난 후 마지막에 단 한 번. 도중에 저장하면 Variant 관계가 꼬일 수 있다.
- **MagicaCloth2 컴포넌트 제거 금지**: `UseMagicaCloth=false`면 사후 제거가 아니라 `P09_Human_No_Physics` 베이스로 빌드.
- **URP 머티리얼**: P09는 lilToon 기반. URP Lit으로 변환하지 말고 그대로 유지.

### 8.2 KCC 관련

- `KinematicCharacterMotor`는 `Animator` 보다 먼저 `Awake`되어야 함. 컴포넌트 추가 순서 강제: Motor → MovementController → Actor → Brain.
- Capsule 기본값(radius 0.4, height 1.8)은 P09 메시 기준.

### 8.3 SerializedField 주입

- `MonsterActor._stats` 같은 private 필드는 `SerializedObject.FindProperty` 후 `objectReferenceValue` 할당하고 `ApplyModifiedPropertiesWithoutUndo()`. reflection으로 직접 set하면 인스펙터 직렬화가 누락된다.

### 8.4 동시성·재진입

- 빌드 중 사용자가 또 빌드 버튼을 눌러도 무시 (`_isBuilding` 락).
- `AssetDatabase.StartAssetEditing` / `StopAssetEditing`은 반드시 try-finally로 페어링.

### 8.5 이름 충돌

- `Manual Name` 사용 시 동일 이름 프리팹이 이미 있으면 다이얼로그 (덮어쓰기 / 취소 / 시퀀스 추가).

### 8.6 빌드 실패 롤백

1. 생성된 SO 자산 경로 추적 → `AssetDatabase.DeleteAsset` 일괄 정리
2. RootInstance 씬 잔존 시 `Object.DestroyImmediate`
3. `Undo.RevertAllInCurrentGroup()`

### 8.7 카탈로그 캐시 만료

- `AssetPostprocessor`로 P09 SO 폴더 변경 감지 → `_catalog.Refresh()` 자동 호출.

### 8.8 미리보기 누수

- `PreviewSceneController`는 `OnDisable`에서 `EditorSceneManager.ClosePreviewScene` 명시 호출.
- `RenderTexture`는 토글 시 `Release` + 재생성.

### 8.9 Player 케이스 특이점

- 이미 존재하는 `CharacterActorType` 슬롯 선택 시 경고만 띄우고 빌드는 허용.
- `PartyConfigSO` 직접 수정은 옵션 체크 시에만 백업 후 변경, 다이얼로그 확인.

### 8.10 빌드 프리셋

양산을 위해 `CharacterBuildConfig` 자체를 SO로 저장/로드 가능하게 (`P09BuildPreset.asset`). 우상단 `[Save Preset]` / `[Load Preset]` 버튼.

---

## 9. 구현 순서 (Phase 1 → Phase 3)

### Phase 1: 골격과 Enemy 빌드 (최소 가치 검증)

목표: **Enemy 1체를 자동 생성**해 씬에 배치 가능한 수준.

| 작업 | 산출물 |
|------|--------|
| 1.1 asmdef + 폴더 구조 | `P09Builder.Editor.asmdef`, 빈 폴더 |
| 1.2 EditorWindow 셸 + 탭 헤더 | `P09CharacterPrefabBuilderWindow`, 5개 탭 빈 구현 |
| 1.3 `CharacterBuildConfig` 직렬화 + EditorPrefs 저장/복원 | Config 영속성 |
| 1.4 `P09AssetCatalog` 풀스캔 | 폴더별 SO 리스트 |
| 1.5 `BasicInfoTab` 완성 (Actor 종류, 성별, 이름) | 자동 명명 동작 |
| 1.6 `AppearanceTab` 갑옷+헤어만 | IconGrid 1차 구현 |
| 1.7 `WeaponTab` 무기 그룹만 | 그룹 단일 선택 |
| 1.8 `PrefabBuildPipeline` + Enemy 스텝 전부 | Enemy 프리팹 자동 생성 성공 |
| 1.9 `EnemyActorTemplate` 컴포넌트·SO 발급 | MonsterActor 등 자동 부착 |
| 1.10 `NameAndSaveStep` + `CharacterNameGenerator` | 시퀀스 영속화 |

**완료 기준:** `ENM_F_03_001.prefab`이 `Assets/03.Prefabs/Characters/Enemy/ENM_F_03_001/`에 생성되고, 씬 드롭만으로 `MonsterActor`가 동작.

### Phase 2: 외형 완성 + Player/NPC 지원

| 작업 | 산출물 |
|------|--------|
| 2.1 `FaceApplier`, `BodyApplier` 완성 | 얼굴/신체 적용 |
| 2.2 BustSize, FacialHair 조건부 UI | Sex 분기 |
| 2.3 SkinColor Sex 필터 | Male/Female별 |
| 2.4 개별 무기 지정 모드 | Sword/Shield/Bow/Staff 분리 |
| 2.5 MagicaCloth On/Off 베이스 분기 | `ToggleMagicaClothStep` |
| 2.6 `PlayerActorTemplate` | Player 빌드 |
| 2.7 `NpcActorTemplate` | NPC 빌드 |
| 2.8 `StatsTab` 동적 분기 (Enemy/Player/NPC) | 타입별 인스펙터 |
| 2.9 기존 SO 사용 vs 신규 생성 토글 | 양쪽 경로 |
| 2.10 Recruitable 옵션 | 파티 합류 연동 |
| 2.11 `BuildPreset` SO Save/Load | 양산 지원 |

**완료 기준:** 3종 액터 모두 빌드 가능. Phase 1 출력과 비교해 외형이 P09 데모씬과 동등.

### Phase 3: 미리보기·UX·안정화

| 작업 | 산출물 |
|------|--------|
| 3.1 `PreviewSceneController` + RenderTexture | 우측 패널 실시간 |
| 3.2 외형 변경 디바운스(200ms) → 재빌드 | 부드러운 갱신 |
| 3.3 SceneView 임시 인스턴스 모드 | "Open in SceneView" |
| 3.4 `IconResolver` Icons_Equipment 매핑 | 아이콘 풀 표시 |
| 3.5 ColorSwatch 드로어 | 컬러 시각화 |
| 3.6 Validate 통합 + 상태바 메시지 | 빌드 전 사전 차단 |
| 3.7 빌드 실패 롤백 | 원복 보장 |
| 3.8 `AssetPostprocessor` 카탈로그 자동 갱신 | P09 변경 감지 |
| 3.9 빌드 진행률 ProgressBar | 사용자 피드백 |
| 3.10 단축키, MenuItem 정리 | `Ctrl+Shift+P` |
| 3.11 빌드 로그 콘솔 영역 | 디버깅 |
| 3.12 EditMode 회귀 테스트 | 안정화 |

**완료 기준:** 미리보기 패널에서 즉시 외형 확인, 빌드 실패 시 깔끔한 롤백, 5분 이내 캐릭터 1체 양산 가능.

### Phase 4 (옵션): 향후 확장 후보

- MotionSet 자동 매핑 (캐릭터 타입별 기본 모션셋)
- 다중 캐릭터 일괄 빌드 (CSV / SO 리스트 입력)
- 무기 트레일/이펙트 자동 부착
- Addressables 그룹 자동 등록
- LOD 자동 생성

---

## 10. 핵심 경로 요약

| 항목 | 경로 |
|------|------|
| 에디터 코드 루트 | `Assets/02.Scripts/Editor/P09Builder/` |
| 생성 프리팹 저장 루트 | `Assets/03.Prefabs/Characters/` |
| 생성 SO 저장 루트 | `Assets/03.Prefabs/Characters/{Kind}/{Name}/Descs/` |
| P09 SO 카탈로그 ROOT | `Assets/ExternalAssets/Character/P09_Modular_Humanoid/Scenes/DemoScene_Data/ScriptableObject` |
| P09 베이스 프리팹 (MagicaCloth) | `Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Prefab/P09_Human.prefab` |
| P09 베이스 프리팹 (No Physics) | `Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Prefab/No_MagicaCloth/P09_Human_No_Physics.prefab` |
| 시퀀스 영속 파일 | `Library/P09Builder/sequence.json` |
| 빌드 프리셋 SO | `Assets/10.Datas/Generated/BuildPresets/` |
