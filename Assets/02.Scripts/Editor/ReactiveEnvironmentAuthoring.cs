using System;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.Components;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.EditorTools
{
    /// <summary>선택 대상의 변경 범위를 확인하고 기존 ID·구매 원본을 보존하는 환경 저작 창.</summary>
    public sealed class ReactiveEnvironmentAuthoring : EditorWindow
    {
        [SerializeField] private ReactiveEnvironmentDefinitionSO _profile;
        [SerializeField] private EnvironmentController _weather;
        private string _preview;
        private Vector2 _scroll;
        private UnityEditor.Editor _inspector;

        [UPlaygroundTool("World/반응 환경 저작")]
        public static void Open() => GetWindow<ReactiveEnvironmentAuthoring>("반응 환경 저작");

        private void OnDisable() { if (_inspector != null) DestroyImmediate(_inspector); }
        private void OnSelectionChange() { _preview = null; Repaint(); }
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _profile = (ReactiveEnvironmentDefinitionSO)EditorGUILayout.ObjectField("반응 프로필", _profile, typeof(ReactiveEnvironmentDefinitionSO), false);
            _weather = (EnvironmentController)EditorGUILayout.ObjectField("날씨", _weather, typeof(EnvironmentController), true);
            var target = Selection.activeGameObject;
            EditorGUILayout.ObjectField("선택 루트", target, typeof(GameObject), true);
            EditorGUILayout.HelpBox("구매 원본은 wrapper로 먼저 감싸세요. 반응 슬롯·제외·피격 Collider·열/비 샘플은 명시 저작합니다. 벌목 피벗·잔해·Collider 크기는 자동 확정하지 않습니다.", MessageType.Info);
            if (GUILayout.Button("변경 범위 / 누락 미리보기")) _preview = Preview(target, _profile);
            if (_preview == null && target != null) _preview = Preview(target, _profile);
            if (_preview != null) EditorGUILayout.HelpBox(_preview, MessageType.None);
            using (new EditorGUI.DisabledScope(target == null || _profile == null || EditorApplication.isPlaying))
                if (GUILayout.Button("미리본 선택 대상 연결 (Undo 지원)"))
                {
                    try { Apply(target, _profile, _weather); _preview = Preview(target, _profile); }
                    catch (Exception error) { _preview = error.Message; }
                }
            using (new EditorGUI.DisabledScope(target == null || !EditorUtility.IsPersistent(target)))
                if (GUILayout.Button("선택 원본의 프로젝트 wrapper 생성"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("환경 wrapper", target.name + "_Reactive", "prefab", "프로젝트 소유 경로를 지정하세요.");
                    if (!string.IsNullOrEmpty(path))
                    {
                        try { Selection.activeObject = CreateWrapper(target, path); }
                        catch (Exception error) { _preview = error.Message; }
                    }
                }
            if (target != null && target.GetComponent<Terrain>() != null && !EditorUtility.IsPersistent(target))
            {
                if (GUILayout.Button("선택 Terrain에 셀 어댑터 추가 (Undo)"))
                {
                    if (target.GetComponent<TerrainDetailReaction>() == null) Undo.AddComponent<TerrainDetailReaction>(target);
                    _preview = "Terrain Inspector에서 프로필·날씨·구획·레이어·대응 툰 프리팹·원본 revision을 명시하세요.";
                }
            }
            var reaction = target != null ? target.GetComponent<ReactiveEnvironmentObject>() : null;
            if (reaction != null)
            {
                UnityEditor.Editor.CreateCachedEditor(reaction, null, ref _inspector);
                _inspector.OnInspectorGUI();
                if (GUILayout.Button("구성 검사"))
                {
                    var bindings = new SerializedObject(reaction);
                    var identity = target.GetComponent<SceneEntityId>();
                    _preview = "배치 ID: " + identity.Guid + "\n" + Preview(target, _profile)
                        + "\n프리팹은 원본 ID를 비워 두며 런타임 배치 후 날씨/지역까지 등록 검사합니다.";
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>저장 없이 추가할 구성과 실제 모델의 후보·누락을 표시한다.</summary>
        public static string Preview(GameObject target, ReactiveEnvironmentDefinitionSO profile)
        {
            if (target == null) return "대상을 선택하세요.";
            var text = new StringBuilder();
            text.AppendLine("대상: " + target.name);
            text.AppendLine("프로필: " + (profile != null ? profile.definitionId : "누락"));
            text.AppendLine("기존 SceneEntityId는 보존. 누락 시만 추가. Collider·재질·잔해 자동 생성 없음.");
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
                text.AppendLine("Renderer 후보: " + AnimationUtility.CalculateTransformPath(renderer.transform, target.transform)
                    + " / 슬롯 " + renderer.sharedMaterials.Length);
            foreach (var collider in target.GetComponentsInChildren<Collider>(true))
                text.AppendLine("피격 후보: " + collider.name + " / " + LayerMask.LayerToName(collider.gameObject.layer));
            if (target.GetComponentInChildren<Collider>(true) == null) text.AppendLine("누락: 피격 Collider");
            if (profile != null && profile.reaction.canBreak) text.AppendLine("필수: 파괴 레시피·원형/잔해·충돌·실제 절단점/낙하 피벗");
            var reaction = target.GetComponent<ReactiveEnvironmentObject>();
            if (reaction == null) text.AppendLine("추가: ReactiveEnvironmentObject·SceneEntityId·열/비 샘플");
            else
            {
                var serialized = new SerializedObject(reaction);
                text.AppendLine("명시 표면 " + serialized.FindProperty("_surfaceBindings").arraySize
                    + " / 제외 Renderer " + serialized.FindProperty("_excludedSurfaces").arraySize
                    + " / 피격 Collider " + serialized.FindProperty("_hitColliders").arraySize);
                var bindings = new EnvironmentSurfaceBinding[serialized.FindProperty("_surfaceBindings").arraySize];
                var array = serialized.FindProperty("_surfaceBindings");
                for (int i = 0; i < bindings.Length; i++)
                {
                    var element = array.GetArrayElementAtIndex(i);
                    bindings[i] = new EnvironmentSurfaceBinding {
                        renderer = element.FindPropertyRelative("renderer").objectReferenceValue as Renderer,
                        slots = ReadSlots(element.FindPropertyRelative("slots")),
                        excludedSlots = ReadSlots(element.FindPropertyRelative("excludedSlots")) };
                }
                var excludedArray = serialized.FindProperty("_excludedSurfaces");
                var excluded = new Renderer[excludedArray.arraySize];
                for (int i = 0; i < excluded.Length; i++) excluded[i] = excludedArray.GetArrayElementAtIndex(i).objectReferenceValue as Renderer;
                text.AppendLine(ReactiveEnvironmentSurface.Validate(target.transform, bindings, excluded) ?? "표면/LOD/슬롯 구성 통과");
            }
            return text.ToString();
        }

        private static int[] ReadSlots(SerializedProperty array)
        {
            var slots = new int[array.arraySize];
            for (int i = 0; i < slots.Length; i++) slots[i] = array.GetArrayElementAtIndex(i).intValue;
            return slots;
        }

        /// <summary>선택 루트만 변경하며 실패 시 해당 Undo 그룹 전체를 복구한다.</summary>
        public static void Apply(GameObject target, ReactiveEnvironmentDefinitionSO profile, EnvironmentController weather)
        {
            if (target == null || profile == null || !profile.IsValid()) throw new ArgumentException("대상/프로필 오류입니다.");
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            string source = stage != null ? stage.assetPath : AssetDatabase.GetAssetPath(target);
            if (EditorUtility.IsPersistent(target) || source.StartsWith("Assets/ExternalAssets/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("프로젝트 wrapper의 Prefab Mode 또는 씬 인스턴스를 선택하세요.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("반응 환경 연결");
            try
            {
                var id = target.GetComponent<SceneEntityId>();
                if (id == null) id = Undo.AddComponent<SceneEntityId>(target);
                var reaction = target.GetComponent<ReactiveEnvironmentObject>();
                if (reaction == null) reaction = Undo.AddComponent<ReactiveEnvironmentObject>(target);
                Undo.RecordObject(reaction, "반응 프로필 연결");
                var serialized = new SerializedObject(reaction);
                serialized.FindProperty("_definition").objectReferenceValue = profile;
                serialized.FindProperty("_weather").objectReferenceValue = weather;
                if (serialized.FindProperty("_rainSamples").arraySize == 0)
                {
                    var samples = new GameObject("ReactionSamples");
                    Undo.RegisterCreatedObjectUndo(samples, "반응 샘플 생성");
                    Undo.SetTransformParent(samples.transform, target.transform, "반응 샘플 연결");
                    samples.transform.localPosition = Vector3.zero;
                    foreach (string name in new[] { "_rainSamples", "_heatSamples" })
                    {
                        var array = serialized.FindProperty(name); array.arraySize = 1;
                        array.GetArrayElementAtIndex(0).objectReferenceValue = samples.transform;
                    }
                }
                if (serialized.FindProperty("_hitColliders").arraySize == 0)
                {
                    var colliders = target.GetComponentsInChildren<Collider>(true);
                    var array = serialized.FindProperty("_hitColliders"); array.arraySize = colliders.Length;
                    for (int i = 0; i < colliders.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = colliders[i];
                }
                serialized.ApplyModifiedProperties();
                if (stage == null && target.GetComponent<WorldPlacementMetadata>() == null)
                {
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        var metadata = Undo.AddComponent<WorldPlacementMetadata>(target);
                        metadata.EditorSetPlacementInfo(WorldPlacementMetadata.PlacementSourceKind.DirectPrefab,
                            AssetDatabase.AssetPathToGUID(prefabPath), WorldPlacementMetadata.PlacementBakeMode.RuntimeData,
                            "", 0, target.activeSelf);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(metadata);
                    }
                }
                if (profile.reaction.canBreak && target.GetComponent<ReactiveEnvironmentDestruction>() == null)
                    Undo.AddComponent<ReactiveEnvironmentDestruction>(target);
                if (stage != null) { Undo.RecordObject(id, "프리팹 배치 ID 비우기"); id.EditorSetGuid(""); }
                PrefabUtility.RecordPrefabInstancePropertyModifications(reaction);
                Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        /// <summary>기존 프리팹을 중첩한 새 wrapper만 생성하고 실패 시 새 파일만 제거한다.</summary>
        public static GameObject CreateWrapper(GameObject source, string path)
        {
            if (source == null || !PrefabUtility.IsPartOfPrefabAsset(source) || source.transform.parent != null
                || !path.StartsWith("Assets/03.Prefabs/Environment/", StringComparison.Ordinal)
                || AssetDatabase.LoadMainAssetAtPath(path) != null || System.IO.File.Exists(path))
                throw new ArgumentException("새 프로젝트 환경 프리팹 경로와 원본 루트가 필요합니다.");
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var wrapper = new GameObject(source.name + "_Reactive");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wrapper, scene);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                model.transform.SetParent(wrapper.transform, false);
                var result = PrefabUtility.SaveAsPrefabAsset(wrapper, path);
                if (result == null) throw new InvalidOperationException("wrapper 저장 실패");
                return result;
            }
            catch { if (System.IO.File.Exists(path)) AssetDatabase.DeleteAsset(path); throw; }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
