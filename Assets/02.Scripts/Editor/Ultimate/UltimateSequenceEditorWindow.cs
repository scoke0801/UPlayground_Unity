#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation.Editor;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Data.Editor
{
    public class UltimateSequenceEditorWindow : EditorWindow
    {
        private UltimateSequenceAsset _asset;
        private SerializedObject _serializedAsset;
        private Vector2 _scroll;
        private CharacterActorType _newOwnerType = CharacterActorType.Bokusei;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/캐릭터/궁극기/궁극기 시퀀스 에디터", priority = 140)]
        public static void Open()
        {
            GetWindow<UltimateSequenceEditorWindow>("궁극기 시퀀스");
        }

        public static void Open(UltimateSequenceAsset asset)
        {
            var window = GetWindow<UltimateSequenceEditorWindow>("궁극기 시퀀스");
            window.SetAsset(asset);
            window.Show();
            window.Focus();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is UltimateSequenceAsset selected)
                SetAsset(selected);
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6f);

            if (_asset == null)
            {
                DrawEmptyState();
                return;
            }

            _serializedAsset ??= new SerializedObject(_asset);
            _serializedAsset.Update();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawValidation();
            DrawQuickLinks();
            DrawAssetProperties();
            DrawTimelinePreview();
            DrawPlayModeTest();
            EditorGUILayout.EndScrollView();

            if (_serializedAsset.ApplyModifiedProperties())
                EditorUtility.SetDirty(_asset);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _asset = (UltimateSequenceAsset)EditorGUILayout.ObjectField(
                    "궁극기 에셋",
                    _asset,
                    typeof(UltimateSequenceAsset),
                    false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _newOwnerType = (CharacterActorType)EditorGUILayout.EnumPopup(
                        "새 에셋 캐릭터",
                        _newOwnerType);
                    if (GUILayout.Button("캐릭터별 에셋 생성", GUILayout.Width(140f)))
                        CreateAsset();
                }
            }
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.HelpBox(
                "UltimateSequenceAsset을 선택하거나 캐릭터별 에셋을 생성하세요.",
                MessageType.Info);
        }

        private void DrawValidation()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("검증", EditorStyles.boldLabel);

                if (_asset.IsValid(out string error))
                    EditorGUILayout.HelpBox("필수 데이터가 연결되어 있습니다.", MessageType.Info);
                else
                    EditorGUILayout.HelpBox(error, MessageType.Error);

                if (_asset.cameraProfile == null)
                    EditorGUILayout.HelpBox("카메라 프로필이 없습니다. 카메라 없는 궁극기로 실행됩니다.", MessageType.Warning);

                float motionDuration = ResolveMotionDuration();
                if (_asset.events != null)
                {
                    foreach (UltimateTimelineEvent timelineEvent in _asset.events)
                    {
                        if (timelineEvent != null && timelineEvent.EndTime > motionDuration + 0.001f)
                        {
                            EditorGUILayout.HelpBox(
                                $"'{timelineEvent.DisplayName}' 이벤트가 모션 길이({motionDuration:0.000}s)를 초과합니다.",
                                MessageType.Warning);
                        }
                    }
                }
            }
        }

        private void DrawQuickLinks()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(_asset.motionSet == null))
                {
                    if (GUILayout.Button("MotionSet 에디터"))
                        MotionSetEditorWindow.Open(_asset.motionSet);
                }

                using (new EditorGUI.DisabledScope(_asset.cameraProfile == null))
                {
                    if (GUILayout.Button("카메라 스냅샷 에디터"))
                        CameraSnapshotEditorWindow.Open(_asset.cameraProfile);
                }

                if (GUILayout.Button("카메라 동기 촬영"))
                {
                    DialogueCameraRecorderWindow.OpenForMotion(_asset.motionSet);
                }
            }
        }

        private void DrawAssetProperties()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("시퀀스 데이터", EditorStyles.boldLabel);

                DrawProperty("ownerType");
                DrawProperty("motionSet");
                DrawProperty("cameraProfile");
                DrawProperty("motionFadeDuration");
                DrawProperty("consumeUltimateGauge");
                DrawProperty("lockSettings", true);
                DrawProperty("targetPolicy", true);
                DrawProperty("placementSettings", true);
                DrawProperty("timelineUseUnscaledTime");

                SerializedProperty events = _serializedAsset.FindProperty("events");
                EditorGUILayout.PropertyField(events, new GUIContent("연출 이벤트"), true);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ 이벤트 추가"))
                        ShowAddEventMenu(events);

                    using (new EditorGUI.DisabledScope(events.arraySize == 0))
                    {
                        if (GUILayout.Button("마지막 이벤트 삭제"))
                            events.DeleteArrayElementAtIndex(events.arraySize - 1);
                    }
                }
            }
        }

        private void DrawTimelinePreview()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("타임라인 미리보기", EditorStyles.boldLabel);

                float duration = Mathf.Max(0.01f, ResolveMotionDuration());
                Rect ruler = GUILayoutUtility.GetRect(10f, 30f, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(ruler, new Color(0.12f, 0.12f, 0.12f));

                for (int i = 0; i <= 5; i++)
                {
                    float t = i / 5f;
                    float x = Mathf.Lerp(ruler.x, ruler.xMax, t);
                    EditorGUI.DrawRect(new Rect(x, ruler.y, 1f, ruler.height), new Color(0.4f, 0.4f, 0.4f));
                    GUI.Label(new Rect(x + 2f, ruler.y, 55f, 16f), $"{duration * t:0.00}s", EditorStyles.miniLabel);
                }

                if (_asset.events == null)
                    return;

                foreach (UltimateTimelineEvent timelineEvent in _asset.events)
                {
                    if (timelineEvent == null)
                        continue;

                    float start = Mathf.Clamp01(timelineEvent.startTime / duration);
                    float end = Mathf.Clamp01(timelineEvent.EndTime / duration);
                    float width = Mathf.Max(4f, (end - start) * ruler.width);
                    Rect bar = new(
                        ruler.x + start * ruler.width,
                        ruler.y + 17f,
                        width,
                        10f);
                    EditorGUI.DrawRect(bar, new Color(0.25f, 0.65f, 0.95f, 0.9f));
                    GUI.Label(
                        new Rect(bar.x, ruler.yMax + 2f, Mathf.Max(90f, bar.width), 16f),
                        $"{timelineEvent.DisplayName} @{timelineEvent.startTime:0.00}",
                        EditorStyles.miniLabel);
                }

                GUILayout.Space(Mathf.Max(16f, _asset.events.Count * 2f));
            }
        }

        private void DrawPlayModeTest()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("PlayMode 테스트", EditorStyles.boldLabel);

                PlayerActor player = Application.isPlaying
                    ? GameObjectManager.Instance?.Player
                      ?? FindFirstObjectByType<PlayerActor>()
                    : null;
                UltimateSequencePlayer sequencePlayer = player != null
                    ? player.GetComponent<UltimateSequencePlayer>()
                    : null;

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!Application.isPlaying || player == null))
                    {
                        if (GUILayout.Button("자원 무시 테스트 실행", GUILayout.Height(26f)))
                            player.GetCombat()?.RequestUltimate(_asset, null, true);
                    }

                    using (new EditorGUI.DisabledScope(sequencePlayer == null || !sequencePlayer.IsPlaying))
                    {
                        if (GUILayout.Button("중단", GUILayout.Width(80f), GUILayout.Height(26f)))
                            sequencePlayer.Interrupt();
                    }
                }

                if (!Application.isPlaying)
                    EditorGUILayout.HelpBox("테스트 실행은 PlayMode에서 사용할 수 있습니다.", MessageType.Info);
                else if (player == null)
                    EditorGUILayout.HelpBox("씬에서 PlayerActor를 찾지 못했습니다.", MessageType.Warning);
            }
        }

        private void DrawProperty(string propertyName, bool includeChildren = false)
        {
            SerializedProperty property = _serializedAsset.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, includeChildren);
        }

        private void ShowAddEventMenu(SerializedProperty events)
        {
            var menu = new GenericMenu();
            AddEventMenuItem<UltimateSpawnVfxEvent>(menu, events, "VFX 생성");
            AddEventMenuItem<UltimateSoundEvent>(menu, events, "SFX / Voice");
            AddEventMenuItem<UltimateTimeScaleEvent>(menu, events, "TimeScale");
            AddEventMenuItem<UltimateCameraEffectEvent>(menu, events, "Camera Effect");
            AddEventMenuItem<UltimateCameraShakeEvent>(menu, events, "Camera Shake");
            AddEventMenuItem<UltimateDamageWindowEvent>(menu, events, "Damage Window");
            AddEventMenuItem<UltimateCustomCallbackEvent>(menu, events, "Custom Callback");
            menu.ShowAsContext();
        }

        private void AddEventMenuItem<T>(
            GenericMenu menu,
            SerializedProperty events,
            string label)
            where T : UltimateTimelineEvent, new()
        {
            menu.AddItem(new GUIContent(label), false, () =>
            {
                _serializedAsset.Update();
                int index = events.arraySize;
                events.InsertArrayElementAtIndex(index);
                events.GetArrayElementAtIndex(index).managedReferenceValue = new T();
                _serializedAsset.ApplyModifiedProperties();
                EditorUtility.SetDirty(_asset);
            });
        }

        private float ResolveMotionDuration()
        {
            return _asset != null
                   && _asset.motionSet != null
                   && _asset.motionSet.motionSet != null
                ? _asset.motionSet.motionSet.TotalDuration
                : 0f;
        }

        private void CreateAsset()
        {
            if (_newOwnerType == CharacterActorType.None)
            {
                EditorUtility.DisplayDialog("생성 불가", "캐릭터 타입을 지정하세요.", "확인");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "궁극기 시퀀스 생성",
                $"UltimateSequence_{_newOwnerType}",
                "asset",
                "저장 위치를 선택하세요.",
                "Assets/10.Datas");
            if (string.IsNullOrEmpty(path))
                return;

            var asset = CreateInstance<UltimateSequenceAsset>();
            asset.ownerType = _newOwnerType;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            SetAsset(asset);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void SetAsset(UltimateSequenceAsset asset)
        {
            _asset = asset;
            _serializedAsset = asset != null ? new SerializedObject(asset) : null;
            Repaint();
        }
    }

    [CustomEditor(typeof(UltimateSequenceAsset))]
    public class UltimateSequenceAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(6f);
            if (GUILayout.Button("궁극기 시퀀스 에디터 열기", GUILayout.Height(26f)))
                UltimateSequenceEditorWindow.Open((UltimateSequenceAsset)target);
        }
    }
}
#endif
