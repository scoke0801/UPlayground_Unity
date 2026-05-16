#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UPlayGround.Editor
{
    public class UPlaygroundToolsLauncher : EditorWindow
    {
        private readonly struct ToolEntry
        {
            public readonly string Name;
            public readonly string MenuPath;
            public readonly string Summary;
            public readonly string Detail;

            public ToolEntry(string name, string menuPath, string summary, string detail)
            {
                Name     = name;
                MenuPath = menuPath;
                Summary  = summary;
                Detail   = detail;
            }
        }

        private static readonly (string Category, ToolEntry[] Tools)[] s_categories =
        {
            ("Generator Tool", new[]
            {
                Tool("ID Enum Generator",               "UPlayGround/Generator Tool/ID Enum Generator", "프로젝트 ID enum을 데이터 에셋 기준으로 생성/갱신합니다.", "FX, UI, Actor, Quest 등 문자열 ID를 코드에서 안전하게 참조할 수 있도록 enum 파일을 다시 만듭니다. 데이터 추가 후 enum 누락이 의심될 때 사용합니다."),
                Tool("Item Data Generator",             "UPlayGround/Generator Tool/Item Data Generator", "아이템 데이터 에셋과 ID 구성을 생성합니다.", "ItemSO, EquipmentSO, ItemDatabase 계열 데이터를 일괄 생성하거나 누락 항목을 채우는 생성기입니다. 아이템 테이블 확장 후 데이터 정합성을 맞출 때 사용합니다."),
                Tool("Recipe Data Generator",           "UPlayGround/Generator Tool/Recipe Data Generator", "제작 레시피 데이터를 생성합니다.", "제작 시스템에서 사용할 Recipe 데이터와 관련 DB를 생성/갱신합니다. 신규 제작 항목을 대량으로 넣거나 테이블 기반 데이터를 반영할 때 사용합니다."),
                Tool("Stat Data Generator",             "UPlayGround/Generator Tool/Stat Data Generator", "액터 스탯 SO를 생성/갱신합니다.", "CharacterActorType, Monster ActorDefinition 등 액터 정의에 맞춰 ActorStatSO 누락분을 만들고 기본 스탯 프리셋을 적용하는 용도입니다."),
                Tool("Validate Stat Coverage",          "UPlayGround/Generator Tool/Validate Stat Data Coverage", "액터별 스탯 데이터 연결 누락을 검사합니다.", "ActorDefinitionSO와 ActorStatSO 연결 상태를 확인해 런타임 스탯 초기화가 빠지는 문제를 사전에 찾습니다."),
                Tool("Party Growth Editor",             "UPlayGround/Generator Tool/Party Growth Editor", "파티 캐릭터 성장 데이터를 편집합니다.", "CharacterActorType별 성장 곡선, 기본 스탯, 전투력 미리보기를 스프레드시트 형태로 확인하고 조정합니다."),
                Tool("NPC Data Generator",              "UPlayGround/Generator Tool/NPC Data Generator", "NPC용 ActorDefinition과 대화 데이터를 생성합니다.", "NPC Actor 정의, Talkable 플래그, NpcActorSO 연결을 빠르게 구성하는 생성기입니다."),
                Tool("Main Story Generator",            "UPlayGround/Generator Tool/Main Story Generator", "메인 스토리 데이터 묶음을 생성합니다.", "메인 스토리용 Quest, Dialogue, StoryEntry 데이터를 한 흐름으로 만들 때 사용합니다."),
                Tool("Sub Story Generator",             "UPlayGround/Generator Tool/Sub Story Generator", "서브 스토리 데이터 묶음을 생성합니다.", "서브 퀘스트와 관련 Dialogue/StoryEntry를 생성하고 기본 연결 구조를 잡습니다."),
                Tool("Locomotion Motion Setup",         "UPlayGround/Generator Tool/Locomotion Motion Setup", "이동 애니메이션 MotionSet을 일괄 구성합니다.", "FBX 클립을 기반으로 8방향/속도별 로코모션 MotionSetAsset을 생성하거나 등록합니다."),
                Tool("Camera Shake Presets",            "UPlayGround/Generator Tool/Camera Shake Presets", "기본 카메라 쉐이크 프리셋을 생성합니다.", "CameraShakeData 또는 카메라 효과 프리셋이 누락됐을 때 표준 전투 피드백 프리셋을 채웁니다."),
            }),
            ("Character / Actor", new[]
            {
                Tool("Actor Database Editor",           "UPlayGround/Character/Actor/Actor Database Editor", "ActorDefinitionSO 데이터베이스를 관리합니다.", "Actor ID, 표시 이름, 타입, 프리팹, 스탯/드랍/NPC 데이터 연결을 검색하고 편집합니다. 런타임 스폰 기준 데이터의 중심 편집기입니다."),
                Tool("Actor Runtime Monitor",           "UPlayGround/Character/Actor/Actor Runtime Monitor", "현재 씬의 액터 등록 상태를 확인합니다.", "GameObjectManager/ActorSpawnManager에 등록된 액터, ActorType 필터, 런타임 상태를 점검하는 모니터입니다."),
                Tool("Lossy Scale Inspector",           "UPlayGround/Character/Actor/Lossy Scale Inspector", "선택 오브젝트 계층의 스케일 문제를 검사합니다.", "캐릭터/무기/이펙트 하위 Transform의 lossyScale을 확인해 비정상 스케일 전파를 찾습니다."),
                Tool("애니메이션 에디터",                 "UPlayGround/Character/Actor/애니메이션 에디터", "MotionSet 타임라인을 편집하고 테스트합니다.", "Animancer 기반 MotionSet, MotionEvent, 캐릭터 프리뷰를 다루는 핵심 애니메이션 편집기입니다."),
                Tool("Export Monster Data",             "UPlayGround/Character/Actor/Data/Export Monster Data", "몬스터 데이터를 외부 파일로 내보냅니다.", "몬스터 ActorDefinition/스탯/행동 데이터 점검 또는 백업용으로 데이터를 export합니다."),
                Tool("Import Monster Data",             "UPlayGround/Character/Actor/Data/Import Monster Data", "외부 몬스터 데이터를 프로젝트 에셋으로 반영합니다.", "테이블이나 JSON 등으로 정리한 몬스터 데이터를 ActorDefinition/관련 SO에 다시 적용할 때 사용합니다."),
            }),
            ("Character / AI", new[]
            {
                Tool("Behavior Tree Editor",            "UPlayGround/Character/AI/Behavior Tree Editor", "몬스터 AI Behavior Tree를 편집합니다.", "BehaviorTreeAsset 노드 그래프를 만들고 조건/액션/데코레이터/서비스 노드를 구성하는 AI 편집기입니다."),
                Tool("Generate BT Ground Test",         "UPlayGround/Character/AI/Behavior Tree/Generate Enemy Ground Basic Test", "지상 몬스터 기본 BT 테스트 에셋을 생성합니다.", "Enemy Ground Basic 테스트용 BehaviorTree/EnemyBehavior 데이터를 빠르게 재생성합니다."),
                Tool("BT Json Export",                  "UPlayGround/Character/AI/Behavior Tree Json/Export Selected", "선택한 BT 에셋을 JSON으로 내보냅니다.", "BT 구조를 외부 검토, 백업, 비교용 JSON으로 저장합니다."),
                Tool("BT Json Import",                  "UPlayGround/Character/AI/Behavior Tree Json/Import Json", "JSON에서 BT 에셋을 가져옵니다.", "외부에서 편집한 Behavior Tree JSON을 프로젝트 에셋으로 복원합니다."),
            }),
            ("Gameplay / Combat", new[]
            {
                Tool("공격 데이터 에디터",                "UPlayGround/Gameplay/Combat/공격 데이터 에디터", "플레이어 공격 데이터를 편집합니다.", "PlayerAttackDataSO의 콤보, 강공격, 점프/대시/스킬/차지 공격 데이터를 조정합니다."),
                Tool("MotionSet 기반 공격 데이터 생성기", "UPlayGround/Gameplay/Combat/MotionSet 기반 공격 데이터 생성기", "MotionSet 이벤트에서 공격 데이터 초안을 생성합니다.", "BeginCollisionEvent의 hitPhaseIndex와 타이밍을 분석해 AttackDataSO/HitPhase 구성을 만드는 보조 도구입니다."),
            }),
            ("Gameplay / Item", new[]
            {
                Tool("Item Editor",                     "UPlayGround/Gameplay/Item/Item Editor", "아이템 데이터를 검색/편집합니다.", "ItemSO와 장비 데이터의 기본 정보, 아이콘, 분류, 수치 연결을 관리합니다."),
                Tool("Drop Table Editor",               "UPlayGround/Gameplay/Item/Drop Table Editor", "드랍 테이블을 편집합니다.", "몬스터/상호작용 오브젝트가 사망 또는 채집 시 떨어뜨릴 아이템과 확률을 구성합니다."),
                Tool("Weapon Definition (Add Missing)", "UPlayGround/Gameplay/Item/WeaponDefinition/Create Missing Definitions", "누락된 무기 정의만 생성합니다.", "EquipmentSO 등에 존재하지만 WeaponDefinitionSO가 없는 항목만 추가합니다."),
                Tool("Weapon Definition (Regen All)",   "UPlayGround/Gameplay/Item/WeaponDefinition/Regenerate All Definitions", "전체 무기 정의를 재생성합니다.", "현재 무기 데이터 기준으로 WeaponDefinitionSO 세트를 다시 만듭니다. 기존 수정값 덮어쓰기 가능성이 있어 사용 전 확인이 필요합니다."),
            }),
            ("Gameplay / Crafting", new[]
            {
                Tool("Recipe Editor",                   "UPlayGround/Gameplay/Crafting/Recipe Editor", "제작 레시피를 편집합니다.", "재료, 결과 아이템, 언락 조건 등 제작 시스템의 실제 레시피 데이터를 관리합니다."),
                Tool("Import Recipe Data",              "UPlayGround/Gameplay/Crafting/Import Recipe Data", "외부 레시피 데이터를 가져옵니다.", "테이블 기반 레시피 입력을 프로젝트 Recipe 데이터로 반영합니다."),
            }),
            ("Gameplay / Stat", new[]
            {
                Tool("Stat Database Editor",            "UPlayGround/Gameplay/Stat/Stat Database Editor", "스탯 데이터베이스를 편집합니다.", "ActorStatSO, 스탯 타입, 성장/기본 수치 연결을 검색하고 관리합니다."),
                Tool("Stat Runtime Monitor",            "UPlayGround/Gameplay/Stat/Stat Runtime Monitor", "런타임 스탯 값을 모니터링합니다.", "현재 액터의 ActorStatContainer 값과 modifier 적용 상태를 확인합니다."),
            }),
            ("Gameplay / Quest", new[]
            {
                Tool("Quest Editor",                    "UPlayGround/Gameplay/Quest/Quest Editor", "퀘스트 데이터를 편집합니다.", "QuestSO, 목표, 보상, 진행 조건, ID enum 생성을 관리하는 퀘스트 편집기입니다."),
            }),
            ("Gameplay / GameplayTag", new[]
            {
                Tool("Tag Registry Editor",             "UPlayGround/Gameplay/GameplayTag/Tag Registry Editor", "GameplayTag 레지스트리를 관리합니다.", "상태/전투/AI에서 공유하는 계층형 태그를 등록하고 enum 생성을 위한 기준 데이터를 편집합니다."),
            }),
            ("World / Map", new[]
            {
                Tool("Map Placement Tool",              "UPlayGround/World/Map/Map Placement Tool", "씬에 액터/포탈/오브젝트를 배치합니다.", "ActorDefinition 기반 프리팹을 씬 클릭으로 배치하고 맵 제작용 배치 워크플로를 제공합니다."),
            }),
            ("World / Minimap", new[]
            {
                Tool("Minimap Capture Editor",          "UPlayGround/World/Minimap/Minimap Capture Editor", "미니맵 배경 이미지를 캡처합니다.", "씬을 탑다운 카메라로 촬영하고 PNG 저장 및 Minimap 설정 연결을 보조합니다."),
            }),
            ("World / Camera", new[]
            {
                Tool("Create Dialogue Camera Settings", "UPlayGround/World/Camera/Create Dialogue Camera Settings", "대화용 카메라 설정 에셋을 생성합니다.", "Dialogue 카메라 모드에서 사용할 기본 설정 데이터가 없을 때 생성합니다."),
            }),
            ("Narrative / Dialogue", new[]
            {
                Tool("Speaker Actor Binding",           "UPlayGround/Narrative/Dialogue/Speaker Actor Binding Generator", "대화 화자와 액터 바인딩 테이블을 생성합니다.", "DialogueGraph의 speaker 정보를 씬/Actor 데이터와 연결하기 위한 바인딩 데이터를 만듭니다."),
            }),
            ("Narrative / Story", new[]
            {
                Tool("Dialogue Graph Editor",           "UPlayGround/Narrative/Story/Dialogue Graph Editor", "대화 그래프를 편집합니다.", "DialogueGraphSO와 노드 기반 대화 흐름을 편집하는 스토리/대화 도구입니다."),
            }),
            ("Util", new[]
            {
                Tool("치트 콘솔",                        "UPlayGround/Util/치트 콘솔", "개발용 치트 명령을 실행합니다.", "테스트 중 상태 변경, 아이템 지급, 플래그 조작 등 개발 편의 명령을 실행하는 콘솔입니다."),
                Tool("Avatar Armature Bake",            "UPlayGround/Util/Avatar Armature Bake Tool", "아바타/본 구조 베이크를 보조합니다.", "캐릭터 모델의 Armature 구조를 프로젝트 표준 구조에 맞추거나 검사할 때 사용합니다."),
                Tool("Weapon Motion Setup",             "UPlayGround/Util/Weapon Motion Setup", "무기별 MotionSet 구성을 보조합니다.", "무기 타입별 공격 모션, 애니메이션 클립, MotionSet 연결을 일괄 설정합니다."),
                Tool("Animation Binding Remap",         "UPlayGround/Util/Animation Binding Remap Test", "애니메이션 바인딩 리맵을 테스트합니다.", "FBX/프리팹 구조 변경으로 경로가 달라진 애니메이션 바인딩을 점검합니다."),
                Tool("Actor Screenshot Tool",           "UPlayGround/Util/Actor Screenshot Tool", "액터 스크린샷을 촬영합니다.", "캐릭터/몬스터 프리뷰, 문서, UI 아이콘용 이미지를 캡처하는 보조 도구입니다."),
                Tool("Background Color Remover",        "UPlayGround/Util/Background Color Remover", "이미지 배경색 제거를 보조합니다.", "캡처 이미지나 아이콘 이미지에서 특정 배경색을 제거하는 유틸리티입니다."),
                Tool("URP 머티리얼 변환기",               "UPlayGround/Util/Converter/URP 머티리얼 변환기", "머티리얼을 URP 호환으로 변환합니다.", "레거시/외부 에셋 머티리얼을 URP 프로젝트에서 사용할 수 있도록 변환합니다."),
                Tool("JSON Table Viewer",               "UPlayGround/Util/Viewer/JSON Table Viewer", "JSON 테이블 파일을 확인합니다.", "외부 데이터 테이블 내용을 Unity 에디터 안에서 빠르게 열람합니다."),
                Tool("Missing Script 제거",             "UPlayGround/Util/Missing Script 제거/선택 오브젝트 하위 전체", "선택 오브젝트 하위 Missing Script를 제거합니다.", "프리팹/씬 오브젝트에 남은 깨진 MonoBehaviour 참조를 정리합니다. 선택 대상 전체에 적용되므로 실행 전 범위를 확인해야 합니다."),
            }),
        };

        private SearchField _searchField;
        private string _searchQuery = "";
        private Vector2 _scroll;
        private Vector2 _detailScroll;
        private readonly Dictionary<string, bool> _foldouts = new();
        private ToolEntry? _selectedTool;
        private string _selectedCategory;
        private GUIStyle _selectedToolStyle;
        private GUIStyle _toolRowStyle;
        private GUIStyle _summaryStyle;
        private GUIStyle _mutedWrapStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _detailBoxStyle;

        private static ToolEntry Tool(string name, string menuPath, string summary, string detail) =>
            new ToolEntry(name, menuPath, summary, detail);

        [MenuItem("UPlayGround/Tools Launcher", priority = 1)]
        public static void Open()
        {
            var win = GetWindow<UPlaygroundToolsLauncher>("Tools Launcher");
            win.minSize = new Vector2(420f, 400f);
            win.Show();
        }

        private void OnEnable()
        {
            _searchField = new SearchField();
            foreach (var (cat, _) in s_categories)
                if (!_foldouts.ContainsKey(cat)) _foldouts[cat] = true;

            SelectDefaultToolIfNeeded();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();
            DrawSearchBar();

            if (position.width >= 720f)
            {
                EditorGUILayout.BeginHorizontal();
                float listWidth = Mathf.Clamp(position.width * 0.44f, 340f, 520f);
                DrawToolList(GUILayout.Width(listWidth));
                DrawDetailPanel(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawToolList();
                DrawDetailPanel();
            }
        }

        private void EnsureStyles()
        {
            if (_selectedToolStyle != null) return;

            _selectedToolStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold
            };

            _toolRowStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(12, 4, 2, 2)
            };

            _summaryStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            _mutedWrapStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                wordWrap = true
            };

            _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                margin = new RectOffset(0, 0, 8, 2)
            };

            _detailBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 12),
                margin = new RectOffset(8, 8, 4, 8)
            };
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("UPlayGround Tools", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("모두 열기",  EditorStyles.toolbarButton, GUILayout.MinWidth(60)))
                SetAllFoldouts(true);
            if (GUILayout.Button("모두 닫기", EditorStyles.toolbarButton, GUILayout.MinWidth(60)))
                SetAllFoldouts(false);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.Space(3);
            var rect = EditorGUILayout.GetControlRect(false, 20f);
            rect.x     += 4;
            rect.width -= 8;
            _searchQuery = _searchField.OnGUI(rect, _searchQuery);
            EditorGUILayout.Space(3);
        }

        private void DrawToolList(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(options);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawCategories();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCategories()
        {
            bool filtering = !string.IsNullOrWhiteSpace(_searchQuery);
            string q = _searchQuery.ToLower();

            foreach (var (category, tools) in s_categories)
            {
                ToolEntry[] matches = filtering
                    ? System.Array.FindAll(tools, t => t.Name.ToLower().Contains(q))
                    : tools;

                if (matches.Length == 0) continue;

                if (filtering)
                {
                    EditorGUILayout.LabelField(category, EditorStyles.boldLabel);
                }
                else
                {
                    if (!_foldouts.TryGetValue(category, out bool open)) open = true;
                    _foldouts[category] = EditorGUILayout.Foldout(open, category, true, EditorStyles.foldoutHeader);
                    if (!_foldouts[category]) continue;
                }

                EditorGUI.indentLevel++;
                foreach (var tool in matches)
                {
                    DrawToolRow(category, tool);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }
        }

        private void DrawToolRow(string category, ToolEntry tool)
        {
            bool selected = _selectedTool.HasValue && _selectedTool.Value.MenuPath == tool.MenuPath;

            EditorGUILayout.BeginVertical(_toolRowStyle);
            Rect rowRect = GUILayoutUtility.GetRect(0f, selected ? 42f : 26f, GUILayout.ExpandWidth(true));
            rowRect.x += EditorGUI.indentLevel * 8f;
            rowRect.width -= EditorGUI.indentLevel * 8f;

            if (selected)
                EditorGUI.DrawRect(new Rect(rowRect.x - 4f, rowRect.y - 2f, rowRect.width + 8f, rowRect.height + 4f), new Color(0.25f, 0.36f, 0.52f, 0.35f));

            if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
            {
                SelectTool(category, tool);

                if (Event.current.clickCount == 2)
                    OpenSelectedTool();
            }

            Rect titleRect = new Rect(rowRect.x, rowRect.y, rowRect.width, 18f);
            GUI.Label(titleRect, tool.Name, selected ? EditorStyles.boldLabel : EditorStyles.label);

            if (selected)
            {
                Rect summaryRect = new Rect(rowRect.x, rowRect.y + 19f, rowRect.width, 18f);
                GUI.Label(summaryRect, tool.Summary, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private void SelectTool(string category, ToolEntry tool)
        {
            _selectedCategory = category;
            _selectedTool = tool;
            _detailScroll = Vector2.zero;
        }

        private void DrawDetailPanel(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(_detailBoxStyle, options);

            if (!_selectedTool.HasValue)
            {
                GUILayout.Label("툴을 선택하세요", EditorStyles.boldLabel);
                GUILayout.Label("왼쪽 목록에서 툴을 클릭하면 기능 요약과 사용 상황을 확인할 수 있습니다.", _mutedWrapStyle);
                EditorGUILayout.EndVertical();
                return;
            }

            ToolEntry tool = _selectedTool.Value;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Label(tool.Name, EditorStyles.boldLabel);
            GUILayout.Label(_selectedCategory, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("열기", GUILayout.Width(88f), GUILayout.Height(24f)))
                OpenSelectedTool();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("요약", _sectionTitleStyle);
            GUILayout.Label(tool.Summary, _summaryStyle);
            GUILayout.Space(4);
            GUILayout.Label("상세", _sectionTitleStyle);

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.MinHeight(96f));
            GUILayout.Label(tool.Detail, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.Label("메뉴 경로", _sectionTitleStyle);
            EditorGUILayout.TextField(tool.MenuPath);
            GUILayout.Label("목록 더블클릭으로도 바로 열 수 있습니다.", _mutedWrapStyle);

            EditorGUILayout.EndVertical();
        }

        private void OpenSelectedTool()
        {
            if (!_selectedTool.HasValue) return;
            EditorApplication.ExecuteMenuItem(_selectedTool.Value.MenuPath);
        }

        private bool TryFindTool(string menuPath, out string category, out ToolEntry tool)
        {
            foreach (var (cat, tools) in s_categories)
            {
                foreach (var candidate in tools)
                {
                    if (candidate.MenuPath != menuPath) continue;
                    category = cat;
                    tool = candidate;
                    return true;
                }
            }

            category = null;
            tool = default;
            return false;
        }

        private void SelectDefaultToolIfNeeded()
        {
            if (_selectedTool.HasValue
                && TryFindTool(_selectedTool.Value.MenuPath, out _, out _))
            {
                return;
            }

            foreach (var (category, tools) in s_categories)
            {
                if (tools.Length == 0) continue;
                SelectTool(category, tools[0]);
                return;
            }
        }

        private void OnFocus()
        {
            SelectDefaultToolIfNeeded();
        }

        private void SetAllFoldouts(bool value)
        {
            foreach (var (cat, _) in s_categories)
                _foldouts[cat] = value;
        }
    }
}
#endif
