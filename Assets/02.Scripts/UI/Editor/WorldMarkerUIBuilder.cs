using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>
    /// 인게임 월드 마커 UI(<see cref="UI_HudWorldMarker"/> + <see cref="UIWorldMarkerIcon"/>) 프리팹 초안을
    /// 자동 생성하는 에디터 툴. 기존 UIMapPanelsBuilder의 헬퍼 스타일을 따른다.
    ///
    /// 생성물:
    ///   1) 마커 아이콘 프리팹  : Assets/03.Prefabs/UI/HUD/WorldMarker/UIWorldMarkerIcon.prefab
    ///   2) HUD 패널 프리팹     : Assets/03.Prefabs/UI/HUD/WorldMarker/UI_Hud_WorldMarker.prefab
    ///   3) Config 에셋(없으면) : Assets/10.Datas/UI/WorldMarkerConfig.asset
    ///
    /// 이 툴은 "초안"만 만든다. 스프라이트/폰트/레이아웃 미세조정은 생성 후 인스펙터에서 한다.
    /// 마지막으로 UIPrefabDatabase에 패널 프리팹을 키 "HudWorldMarker"로 등록해야 인게임에서 노출된다.
    /// </summary>
    public static class WorldMarkerUIBuilder
    {
        private const string PrefabDir  = "Assets/03.Prefabs/UI/HUD/WorldMarker";
        private const string IconPath   = PrefabDir + "/UIWorldMarkerIcon.prefab";
        private const string PanelPath  = PrefabDir + "/UI_Hud_WorldMarker.prefab";
        private const string ConfigDir  = "Assets/10.Datas/UI";
        private const string ConfigPath = ConfigDir + "/WorldMarkerConfig.asset";
        private const string DatabaseKey = "HudWorldMarker";

        private static readonly Color IconTint = Color.white;
        private static readonly Color DistText = new Color(1f, 0.95f, 0.7f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // 진입점: UI 에디터 창(UPlayGround/UI 에디터)의 "HUD ▸ 월드 마커" 항목에서 호출한다.
        // 별도 최상위 메뉴(UPlayGround/UI/...)로 노출하지 않는다.
        public static void Build()
        {
            bool iconExists  = File.Exists(IconPath);
            bool panelExists = File.Exists(PanelPath);
            if (iconExists || panelExists)
            {
                bool ok = EditorUtility.DisplayDialog("World Marker UI 빌더",
                    "이미 생성된 프리팹이 있습니다. 덮어쓰면 인스펙터 수정 내용이 사라집니다.\n\n" +
                    (iconExists ? $"- {IconPath}\n" : "") +
                    (panelExists ? $"- {PanelPath}\n" : "") +
                    "\n덮어쓸까요?", "덮어쓰기", "취소");
                if (!ok) return;
            }

            EnsureFolder(PrefabDir);
            EnsureFolder(ConfigDir);

            WorldMarkerConfigSO config = GetOrCreateConfig();
            GameObject iconPrefab = BuildIconPrefab();
            var iconComponent = iconPrefab != null ? iconPrefab.GetComponent<UIWorldMarkerIcon>() : null;
            GameObject panelPrefab = BuildPanelPrefab(config, iconComponent);

            bool registered = RegisterInDatabase(panelPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("World Marker UI 빌더",
                "월드 마커 UI 초안을 생성했습니다.\n\n" +
                $"아이콘 : {IconPath}\n" +
                $"패널   : {PanelPath}\n" +
                $"Config : {ConfigPath}\n\n" +
                $"DB 등록 : {(registered ? $"'{DatabaseKey}' 키로 자동 등록 완료" : "UIPrefabDatabase를 찾지 못해 수동 등록 필요")}\n\n" +
                "남은 작업:\n" +
                "1) 아이콘 Image의 스프라이트를 실제 마커 그림으로 교체\n" +
                "2) (선택) 퀘스트 마커 아이콘/색상을 QuestWorldMarkerBridge에서 조정" +
                (registered ? "" : "\n3) UIPrefabDatabase.asset에 패널 프리팹을 키 \"HudWorldMarker\"로 수동 등록"),
                "확인");

            if (panelPrefab != null)
                EditorGUIUtility.PingObject(panelPrefab);
        }

        // ── Config ────────────────────────────────────────────────────────
        private static WorldMarkerConfigSO GetOrCreateConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WorldMarkerConfigSO>(ConfigPath);
            if (existing != null) return existing;

            var config = ScriptableObject.CreateInstance<WorldMarkerConfigSO>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        // ── UIPrefabDatabase 자동 등록 ─────────────────────────────────────
        private static bool RegisterInDatabase(GameObject panelPrefab)
        {
            if (panelPrefab == null) return false;

            string[] guids = AssetDatabase.FindAssets("t:UIPrefabDatabase");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[WorldMarkerUIBuilder] UIPrefabDatabase 에셋을 찾지 못했습니다. 수동 등록이 필요합니다.");
                return false;
            }
            if (guids.Length > 1)
                Debug.LogWarning($"[WorldMarkerUIBuilder] UIPrefabDatabase가 {guids.Length}개 발견되어 첫 번째에 등록합니다.");

            string dbPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var db = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(dbPath);
            if (db == null) return false;

            // 이미 있으면 최신 프리팹 참조로 교체(제거 후 재추가). 없으면 신규 추가.
            if (db.HasKey(DatabaseKey))
                db.RemovePrefab(DatabaseKey);
            db.AddPrefab(DatabaseKey, panelPrefab, CanvasLayer.HUD, "인게임 월드 마커 HUD (원신식 웨이포인트)");

            EditorUtility.SetDirty(db);
            Debug.Log($"[WorldMarkerUIBuilder] UIPrefabDatabase에 '{DatabaseKey}' 등록 완료: {dbPath}");
            return true;
        }

        // ── 마커 아이콘 프리팹 ─────────────────────────────────────────────
        private static GameObject BuildIconPrefab()
        {
            // 루트: UIWorldMarkerIcon (pivot 아래-중앙 기준으로 타겟 위에 뜨도록)
            var root = NewUI("UIWorldMarkerIcon", null);
            var rootRt = Rt(root);
            rootRt.sizeDelta = new Vector2(72, 96);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            var iconComp = root.AddComponent<UIWorldMarkerIcon>();

            // 아이콘 이미지 (상단)
            var iconGo = NewUI("Icon", root.transform);
            SetAnchored(Rt(iconGo), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(64, 64), new Vector2(0, 0));
            var image = AddImage(iconGo, IconTint, UISprite, sliced: false);
            image.preserveAspect = true;

            // 거리 라벨 (아이콘 아래)
            var distGo = NewUI("Distance", root.transform);
            SetAnchored(Rt(distGo), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(80, 28), new Vector2(0, 2));
            var dist = AddText(distGo, "00m", 20, DistText, TextAlignmentOptions.Center);
            dist.raycastTarget = false;
            dist.enableWordWrapping = false;

            // 참조 와이어링
            var so = new SerializedObject(iconComp);
            SetRef(so, "_icon", image);
            SetRef(so, "_distanceText", dist);
            so.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, IconPath, out bool success);
            Object.DestroyImmediate(root);
            if (!success)
            {
                Debug.LogError($"[WorldMarkerUIBuilder] 아이콘 프리팹 저장 실패: {IconPath}");
                return null;
            }
            return saved;
        }

        // ── HUD 패널 프리팹 ────────────────────────────────────────────────
        private static GameObject BuildPanelPrefab(WorldMarkerConfigSO config, UIWorldMarkerIcon iconPrefab)
        {
            // 루트: RectTransform + Canvas + UI_HudWorldMarker (UI_Base는 Canvas를 RequireComponent)
            var root = NewUI("UI_Hud_WorldMarker", null);
            Stretch(root);
            root.AddComponent<Canvas>();
            var panel = root.AddComponent<UI_HudWorldMarker>();

            // 마커 컨테이너 (전체 화면 스트레치, pivot 중앙)
            var container = NewUI("MarkerContainer", root.transform);
            Stretch(container);
            var containerRt = Rt(container);
            containerRt.pivot = new Vector2(0.5f, 0.5f);

            // 추적 퀘스트 → WorldMarkerRegistry 자동 등록 브리지
            var bridge = root.AddComponent<QuestWorldMarkerBridge>();
            var bso = new SerializedObject(bridge);
            SetRef(bso, "_questIcon", UISprite); // 초안 아이콘. 실제 퀘스트 마커 그림으로 교체 권장.
            bso.ApplyModifiedPropertiesWithoutUndo();

            // 참조 와이어링
            var so = new SerializedObject(panel);
            SetEnum(so, "_layer", (int)CanvasLayer.HUD);
            SetRef(so, "_config", config);
            SetRef(so, "_markerContainer", containerRt);
            SetRef(so, "_markerPrefab", iconPrefab);
            so.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, PanelPath, out bool success);
            Object.DestroyImmediate(root);
            if (!success)
            {
                Debug.LogError($"[WorldMarkerUIBuilder] 패널 프리팹 저장 실패: {PanelPath}");
                return null;
            }
            return saved;
        }

        // ── 헬퍼 (UIMapPanelsBuilder 스타일) ───────────────────────────────
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static RectTransform Rt(GameObject go) => go.GetComponent<RectTransform>();

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = Rt(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

        private static Image AddImage(GameObject go, Color color, Sprite sprite = null, bool sliced = false)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null) { img.sprite = sprite; img.type = sliced ? Image.Type.Sliced : Image.Type.Simple; }
            return img;
        }

        private static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
            return t;
        }

        private static void SetRef(SerializedObject so, string propName, Object value)
        {
            var p = so.FindProperty(propName);
            if (p == null) { Debug.LogWarning($"[WorldMarkerUIBuilder] 프로퍼티 없음: {propName}"); return; }
            p.objectReferenceValue = value;
        }

        // CanvasLayer는 값이 비연속(HUD=0, Scene=1000 ...)이라 열거자 '값'을 직렬화 int로 그대로 쓴다.
        // (enumValueIndex는 서수라 부적합) CanvasLayer.HUD == 0 이므로 intValue=0.
        private static void SetEnum(SerializedObject so, string propName, int value)
        {
            var p = so.FindProperty(propName);
            if (p == null) { Debug.LogWarning($"[WorldMarkerUIBuilder] 프로퍼티 없음: {propName}"); return; }
            p.intValue = value;
        }
    }
}
