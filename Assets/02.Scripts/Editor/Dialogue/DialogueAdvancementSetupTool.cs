using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.Dialogue;
using UPlayGround.Manager;
using UPlayGround.Tool.Editor;
using UPlayGround.UI;

namespace UPlayGround.Dialogue.EditorTools
{
    /// <summary>
    /// 대화 시스템 고도화(정지·자동·스킵·이력·인라인 색상)에 필요한 에셋 배선을 한 번에 처리하는 셋업 툴.
    ///
    /// 수행 순서:
    ///  1. DialoguePalette SO 생성(없으면) + Addressables 주소 등록
    ///  2. 컨트롤 바 / 이력 패널 프리팹 초안 생성(각 빌더 위임)
    ///  3. UIPrefabDatabase에 DialogueControlBar / DialogueBacklog 키 등록
    ///  4. UIKeyType enum 재생성
    ///  5. 기존 UI_Dialogue / UI_MonologueDialogue 프리팹에 DialogueTypewriter 부착·배선
    ///  6. 설정 메뉴 프리팹 재생성(대화 설정 드롭다운 2종 반영)
    ///
    /// 전 단계가 재실행 가능(idempotent)하며, 이미 처리된 항목은 건너뛴다.
    /// 비주얼 마감은 생성된 프리팹에서 수작업으로 이어서 진행한다.
    /// </summary>
    public static class DialogueAdvancementSetupTool
    {
        private const string PaletteAssetPath = "Assets/10.Datas/Dialogue/DialoguePalette.asset";

        private const string ControlBarPrefabPath = "Assets/03.Prefabs/UI/Dialogue/UI_DialogueControlBar.prefab";
        private const string BacklogPrefabPath = "Assets/03.Prefabs/UI/Dialogue/UI_DialogueBacklog.prefab";

        private const string MainDialoguePrefabPath = "Assets/03.Prefabs/UI/Scene/UI_Dialogue.prefab";
        private const string MonologuePrefabPath = "Assets/03.Prefabs/UI/Common/UI_MonologueDialogue.prefab";

        private const string UiKeyTypeOutputPath = "Assets/02.Scripts/Data/Path/UIKeyType.cs";

        // 저작 기본 팔레트. 이미 에셋이 있으면 덮어쓰지 않는다.
        private static readonly (string key, Color color)[] DefaultPaletteEntries =
        {
            ("emphasis", new Color(1f, 0.62f, 0.20f)),   // 강조 — 주황
            ("item",     new Color(0.36f, 0.84f, 0.82f)), // 아이템 — 청록
            ("danger",   new Color(0.94f, 0.30f, 0.30f)), // 경고 — 적
            ("quest",    new Color(0.98f, 0.86f, 0.40f)), // 퀘스트 — 노랑
            ("ally",     new Color(0.56f, 0.82f, 0.45f)), // 아군 — 녹
        };

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/내러티브/대화/대화 고도화 셋업",
            priority = UPlaygroundMenuPriority.NarrativeDialogue + 1)]
        public static void RunAll()
        {
            // AssetDatabase 배치 편집(StartAssetEditing)으로 감싸지 않는다.
            // 갓 만든 에셋의 GUID 조회와 프리팹 저장이 임포트 완료를 전제로 하기 때문이다.
            var report = new List<string>
            {
                EnsurePaletteAsset(),
                BuildDialogueUIPrefabs(),
                RegisterUIPrefabDatabaseEntries(),
                WireTypewriterIntoExistingPrefabs(),
                RebuildSettingMenuPrefab(),

                // enum 재생성은 .cs를 써서 재컴파일을 유발하므로 프리팹 작업이 모두 끝난 뒤 마지막에 수행한다.
                RegenerateUIKeyTypeEnum()
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[대화 고도화 셋업] 완료\n - " + string.Join("\n - ", report));
        }

        // ── 1. 팔레트 에셋 ────────────────────────────────────────────

        private static string EnsurePaletteAsset()
        {
            var palette = AssetDatabase.LoadAssetAtPath<DialoguePaletteSO>(PaletteAssetPath);
            bool created = false;

            if (palette == null)
            {
                EnsureFolder(Path.GetDirectoryName(PaletteAssetPath)?.Replace('\\', '/'));

                palette = ScriptableObject.CreateInstance<DialoguePaletteSO>();
                ApplyDefaultPaletteEntries(palette);
                AssetDatabase.CreateAsset(palette, PaletteAssetPath);
                created = true;
            }

            bool addressed = EnsureAddressable(palette, DialoguePaletteSO.AddressableKey);

            return created
                ? $"DialoguePalette 생성: {PaletteAssetPath} (기본 키 {DefaultPaletteEntries.Length}종)" +
                  (addressed ? $" / Addressables 주소 '{DialoguePaletteSO.AddressableKey}' 등록" : " / Addressables 등록 실패")
                : $"DialoguePalette 이미 존재 — 내용 유지" +
                  (addressed ? " / Addressables 주소 확인" : " / Addressables 등록 실패");
        }

        // entries는 private 직렬화 필드이므로 SerializedObject로 채운다.
        private static void ApplyDefaultPaletteEntries(DialoguePaletteSO palette)
        {
            var so = new SerializedObject(palette);
            SerializedProperty entries = so.FindProperty("entries");
            if (entries == null)
            {
                Debug.LogWarning("[대화 고도화 셋업] DialoguePaletteSO의 'entries' 필드를 찾지 못했습니다.");
                return;
            }

            entries.ClearArray();
            for (int i = 0; i < DefaultPaletteEntries.Length; i++)
            {
                entries.InsertArrayElementAtIndex(i);
                SerializedProperty element = entries.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("key").stringValue = DefaultPaletteEntries[i].key;
                element.FindPropertyRelative("color").colorValue = DefaultPaletteEntries[i].color;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // UPlayGround.Object 네임스페이스와 충돌하므로 UnityEngine.Object를 명시한다.
        private static bool EnsureAddressable(UnityEngine.Object asset, string address)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null || string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("[대화 고도화 셋업] Addressables Settings를 찾지 못해 주소 등록을 건너뜁니다.");
                return false;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            return true;
        }

        // ── 2. 프리팹 초안 생성 ───────────────────────────────────────

        private static string BuildDialogueUIPrefabs()
        {
            UPlayGround.UI.EditorTools.UIDialogueControlBarPrefabBuilder.Build();
            UPlayGround.UI.EditorTools.UIDialogueBacklogPrefabBuilder.Build();
            return "컨트롤 바 / 이력 패널 프리팹 초안 생성";
        }

        // ── 3. UIPrefabDatabase 등록 ──────────────────────────────────

        private static string RegisterUIPrefabDatabaseEntries()
        {
            UIPrefabDatabase database = FindAsset<UIPrefabDatabase>();
            if (database == null)
                return "UIPrefabDatabase를 찾지 못해 키 등록을 건너뜀";

            var added = new List<string>();

            added.AddRange(RegisterEntry(database, DialogueUIKeys.DialogueControlBar, ControlBarPrefabPath,
                CanvasLayer.Scene, "대화 재생 컨트롤 바(정지·자동·스킵·이력)"));
            added.AddRange(RegisterEntry(database, DialogueUIKeys.DialogueBacklog, BacklogPrefabPath,
                CanvasLayer.Popup, "이전 대화내역 패널"));

            if (added.Count == 0)
                return "UIPrefabDatabase 키 이미 등록됨 — 변경 없음";

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            return $"UIPrefabDatabase 등록: {string.Join(", ", added)}";
        }

        private static IEnumerable<string> RegisterEntry(
            UIPrefabDatabase database, string key, string prefabPath, CanvasLayer layer, string description)
        {
            if (database.HasKey(key))
                yield break;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[대화 고도화 셋업] '{key}' 프리팹을 찾지 못했습니다: {prefabPath}");
                yield break;
            }

            database.AddPrefab(key, prefab, layer, description);
            yield return key;
        }

        // ── 4. UIKeyType 재생성 ───────────────────────────────────────

        private static string RegenerateUIKeyTypeEnum()
        {
            UIPrefabDatabase database = FindAsset<UIPrefabDatabase>();
            if (database == null)
                return "UIPrefabDatabase를 찾지 못해 UIKeyType 재생성을 건너뜀";

            var raw = new List<(string rawName, string key)>();
            var so = new SerializedObject(database);
            SerializedProperty prefabs = so.FindProperty("prefabs");

            if (prefabs == null)
                return "UIPrefabDatabase의 'prefabs' 필드를 찾지 못해 UIKeyType 재생성을 건너뜀";

            for (int i = 0; i < prefabs.arraySize; i++)
            {
                string key = prefabs.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue;
                if (!string.IsNullOrEmpty(key))
                    raw.Add((key, key));
            }

            bool generated = IdEnumGeneratorUtility.GenerateStringKeyEnum(
                "UIKeyType", "ToKey", "UI Prefab",
                UiKeyTypeOutputPath, "UPlayGround.Data.Path",
                IdEnumGeneratorUtility.DeduplicateEntries(raw));

            return generated
                ? $"UIKeyType 재생성 완료 ({raw.Count}개 키)"
                : "UIKeyType 재생성 실패 — 중복 키를 확인하세요";
        }

        // ── 5. 기존 대화 프리팹에 타이프라이터 배선 ────────────────────

        private static string WireTypewriterIntoExistingPrefabs()
        {
            var results = new List<string>
            {
                // Main 본문만 스크롤로 감싼다. 대사가 길면 상자를 넘치기 때문이다.
                // 독백은 화면 중앙 단문 레이아웃이라 감싸면 정렬이 깨진다.
                WireTypewriter<UI_Dialogue>(MainDialoguePrefabPath, "dialogueBodyText", wrapInScrollView: true),
                WireTypewriter<UI_MonologueDialogue>(MonologuePrefabPath, "monologueText", wrapInScrollView: false)
            };

            return string.Join(" / ", results);
        }

        private static string WireTypewriter<T>(string prefabPath, string bodyTextFieldName, bool wrapInScrollView)
            where T : UI_Base
        {
            if (!File.Exists(prefabPath))
                return $"{Path.GetFileNameWithoutExtension(prefabPath)} 프리팹 없음 — 건너뜀";

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var view = root.GetComponent<T>();
                if (view == null)
                    return $"{Path.GetFileNameWithoutExtension(prefabPath)}에 {typeof(T).Name} 없음 — 건너뜀";

                var so = new SerializedObject(view);

                SerializedProperty typewriterProperty = so.FindProperty("typewriter");
                if (typewriterProperty == null)
                    return $"{typeof(T).Name}에 'typewriter' 필드 없음 — 건너뜀";

                SerializedProperty bodyProperty = so.FindProperty(bodyTextFieldName);
                var bodyText = bodyProperty?.objectReferenceValue as TextMeshProUGUI;
                if (bodyText == null)
                    return $"{typeof(T).Name}의 '{bodyTextFieldName}' 참조가 비어 있어 배선 불가";

                // 본문 텍스트와 같은 GameObject에 타이프라이터를 둔다(런타임 자동 부착과 동일한 위치).
                var typewriter = bodyText.GetComponent<DialogueTypewriter>();
                if (typewriter == null)
                    typewriter = bodyText.gameObject.AddComponent<DialogueTypewriter>();

                ScrollRect bodyScrollRect = wrapInScrollView ? EnsureBodyScrollView(bodyText) : null;

                var typewriterSo = new SerializedObject(typewriter);
                SetRef(typewriterSo, "targetText", bodyText);
                if (bodyScrollRect != null)
                    SetRef(typewriterSo, "bodyScrollRect", bodyScrollRect);
                typewriterSo.ApplyModifiedPropertiesWithoutUndo();

                typewriterProperty.objectReferenceValue = typewriter;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                return bodyScrollRect != null
                    ? $"{typeof(T).Name} 타이프라이터 + 본문 스크롤 배선 완료"
                    : $"{typeof(T).Name} 타이프라이터 배선 완료";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 본문 텍스트를 스크롤 뷰로 감싸, 대사가 길어도 대화 상자를 넘치지 않게 한다.
        /// 스크롤바는 노출하지 않고 휠로만 이동한다. 이미 감싸져 있으면 설정만 다시 맞춘다.
        /// </summary>
        private static ScrollRect EnsureBodyScrollView(TextMeshProUGUI bodyText)
        {
            RectTransform textRt = bodyText.rectTransform;
            ScrollRect existing = textRt.GetComponentInParent<ScrollRect>(true);

            if (existing == null)
            {
                Transform originalParent = textRt.parent;
                if (originalParent == null)
                {
                    Debug.LogWarning("[대화 고도화 셋업] 본문 텍스트에 부모가 없어 스크롤 뷰를 만들 수 없습니다.");
                    return null;
                }

                int siblingIndex = textRt.GetSiblingIndex();

                // 스크롤 루트는 본문이 차지하던 사각형을 그대로 물려받는다(기존 여백 유지).
                var scrollGo = new GameObject("ChatScrollView", typeof(RectTransform));
                var scrollRt = scrollGo.GetComponent<RectTransform>();
                scrollRt.SetParent(originalParent, false);
                scrollRt.SetSiblingIndex(siblingIndex);
                scrollRt.anchorMin = textRt.anchorMin;
                scrollRt.anchorMax = textRt.anchorMax;
                scrollRt.pivot = textRt.pivot;
                scrollRt.anchoredPosition = textRt.anchoredPosition;
                scrollRt.sizeDelta = textRt.sizeDelta;

                var viewportGo = new GameObject("Viewport", typeof(RectTransform));
                viewportGo.transform.SetParent(scrollGo.transform, false);
                Stretch(viewportGo);
                // 레이캐스트를 가로채면 '아무 곳이나 클릭해 진행'하는 전체 영역 버튼을 막으므로
                // Image 없이 RectMask2D만 둔다. 휠 입력은 아래의 전달자가 넘겨준다.
                viewportGo.AddComponent<RectMask2D>();

                textRt.SetParent(viewportGo.transform, false);

                existing = scrollGo.AddComponent<ScrollRect>();
                existing.viewport = viewportGo.GetComponent<RectTransform>();
                existing.content = textRt;
            }

            // 본문 자체를 content로 쓴다. 높이는 TMP preferredHeight를 ContentSizeFitter가 반영한다.
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.offsetMin = new Vector2(0f, textRt.offsetMin.y);
            textRt.offsetMax = new Vector2(0f, textRt.offsetMax.y);
            textRt.anchoredPosition = new Vector2(0f, 0f);

            var fitter = bodyText.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = bodyText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            bodyText.enableWordWrapping = true;
            bodyText.overflowMode = TextOverflowModes.Overflow;

            existing.horizontal = false;
            existing.vertical = true;
            existing.movementType = ScrollRect.MovementType.Clamped;
            existing.scrollSensitivity = 40f;
            existing.horizontalScrollbar = null;
            existing.verticalScrollbar = null;

            // 전체 영역 진행 버튼이 위에 얹혀 있어 휠 이벤트가 ScrollRect까지 오지 않는다.
            // 공통 조상에 전달자를 두면 버블링 경로에서 되돌릴 수 있다.
            Transform forwarderHost = existing.transform.parent;
            if (forwarderHost != null)
            {
                var forwarder = forwarderHost.GetComponent<UIScrollRectForwarder>();
                if (forwarder == null) forwarder = forwarderHost.gameObject.AddComponent<UIScrollRectForwarder>();
                forwarder.Target = existing;
            }

            return existing;
        }

        // ── 6. 설정 메뉴 프리팹 ───────────────────────────────────────

        private static string RebuildSettingMenuPrefab()
        {
            UPlayGround.UI.SettingMenu.EditorTools.UISettingMenuPrefabBuilder.Build();
            return "설정 메뉴 프리팹 재생성(대화 설정 드롭다운 반영)";
        }

        // ── 공용 헬퍼 ─────────────────────────────────────────────────

        private static TAsset FindAsset<TAsset>() where TAsset : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}");
            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<TAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void SetRef(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
            else Debug.LogWarning($"[대화 고도화 셋업] 직렬화 필드 '{propertyName}'을 찾지 못했습니다.");
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
                return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
