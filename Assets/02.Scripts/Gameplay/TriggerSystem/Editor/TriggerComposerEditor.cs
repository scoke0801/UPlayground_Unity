#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.TriggerSystem.Editor
{
    [CustomEditor(typeof(TriggerComposer))]
    public sealed class TriggerComposerEditor : UnityEditor.Editor
    {
        private const string ManualAssetRoot = "Assets/10.Datas/TriggerSystem/Manual";
        private const string TriggerLayerName = "Trigger";

        private static readonly OptionInfo[] SourceOptions =
        {
            new(typeof(ColliderEnterTriggerSourceSO), "플레이어가 영역에 들어오면", "Trigger Collider 안으로 플레이어가 들어오는 순간 발동합니다."),
            new(typeof(ColliderExitTriggerSourceSO), "플레이어가 영역에서 나가면", "Trigger Collider 밖으로 플레이어가 나가는 순간 발동합니다."),
            new(typeof(GroupDefeatedTriggerSourceSO), "몬스터 그룹이 전멸하면", "연결된 MonsterGroupController의 모든 몬스터가 처치되면 발동합니다."),
        };

        private static readonly OptionInfo[] ConditionOptions =
        {
            new(typeof(AndTriggerConditionSO), "모든 조건 만족", "등록한 조건을 모두 통과해야 실행합니다."),
            new(typeof(OrTriggerConditionSO), "조건 중 하나 만족", "등록한 조건 중 하나라도 통과하면 실행합니다."),
            new(typeof(NotTriggerConditionSO), "조건 반전", "하나의 조건 결과를 반대로 뒤집습니다."),
            new(typeof(GlobalFlagTriggerConditionSO), "글로벌 플래그 확인", "GlobalFlagManager에 저장된 플래그 값을 확인합니다."),
            new(typeof(StoryProgressTriggerConditionSO), "스토리 진행도 확인", "현재 스토리 진행도가 지정한 범위인지 확인합니다."),
            new(typeof(QuestStatusTriggerConditionSO), "퀘스트 상태 확인", "특정 퀘스트가 수락/완료 등 원하는 상태인지 확인합니다."),
            new(typeof(ActorAliveTriggerConditionSO), "대상 생존 여부 확인", "씬 참조나 발동 대상 액터가 살아 있는지 확인합니다."),
            new(typeof(RandomChanceTriggerConditionSO), "확률로 통과", "지정한 확률에 성공했을 때만 실행합니다."),
        };

        private static readonly OptionInfo[] ActionOptions =
        {
            new(typeof(SequenceTriggerActionSO), "여러 동작을 순서대로 실행", "여러 Action을 위에서 아래 순서로 실행합니다."),
            new(typeof(AcceptQuestTriggerActionSO), "퀘스트 수락", "QuestManager에 특정 퀘스트 수락을 요청합니다."),
            new(typeof(NotifyLocationTriggerActionSO), "위치 목표 도달 알림", "퀘스트의 위치 도달 목표를 완료 처리하도록 알립니다."),
            new(typeof(TriggerStoryTriggerActionSO), "스토리/대화 실행", "StoryEntry를 실행합니다."),
            new(typeof(SetStoryProgressTriggerActionSO), "스토리 진행도 변경", "StoryManager의 진행도 값을 변경합니다."),
            new(typeof(SetFlagTriggerActionSO), "글로벌 플래그 변경", "GlobalFlagManager의 플래그 값을 켜거나 끕니다."),
            new(typeof(ActivateGroupTriggerActionSO), "몬스터 그룹 활성화", "연결된 MonsterGroupController를 활성화합니다."),
            new(typeof(PlayCameraSnapshotTriggerActionSO), "카메라 시퀀스 재생", "CameraSnapshotProfile 시퀀스를 재생합니다."),
            new(typeof(UnityEventTriggerActionSO), "UnityEvent 호출", "TriggerUnityEventRelay를 통해 씬 이벤트를 호출합니다."),
            new(typeof(DelayTriggerActionSO), "대기", "다음 Action 실행 전 지정한 시간만큼 기다립니다."),
        };

        private SerializedProperty _triggerId;
        private SerializedProperty _source;
        private SerializedProperty _condition;
        private SerializedProperty _action;
        private SerializedProperty _repeat;
        private SerializedProperty _cooldownSeconds;
        private SerializedProperty _disableColliderAfterTrigger;
        private SerializedProperty _logVerbose;

        private void OnEnable()
        {
            _triggerId = serializedObject.FindProperty("_triggerId");
            _source = serializedObject.FindProperty("_source");
            _condition = serializedObject.FindProperty("_condition");
            _action = serializedObject.FindProperty("_action");
            _repeat = serializedObject.FindProperty("_repeat");
            _cooldownSeconds = serializedObject.FindProperty("_cooldownSeconds");
            _disableColliderAfterTrigger = serializedObject.FindProperty("_disableColliderAfterTrigger");
            _logVerbose = serializedObject.FindProperty("_logVerbose");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSummary();
            DrawValidation();
            DrawPresetButtons();

            DrawSection("기본 식별자", "세션 중 한 번만 발동 같은 중복 방지에 쓰는 이름입니다. 비워두면 GameObject 이름 기준으로 자동 채워집니다.");
            EditorGUILayout.PropertyField(_triggerId, new GUIContent("Trigger ID", "Once Per Session 정책에서 이미 발동한 트리거인지 구분하는 값입니다."));

            DrawSection("1. 언제 발동할까? (Source)", "플레이어가 영역에 들어왔는지, 몬스터 그룹이 전멸했는지처럼 트리거를 시작하는 사건입니다.");
            EditorGUILayout.PropertyField(_source, new GUIContent("발동 사건", "이 트리거가 언제 시작될지 선택합니다."));
            DrawCreateButtons(_source, "발동 사건 만들기", SourceOptions);

            DrawSection("2. 어떤 조건에서 통과할까? (Condition)", "스토리 진행도, 퀘스트 상태, 글로벌 플래그 같은 추가 조건입니다. 비워두면 항상 통과합니다.");
            EditorGUILayout.PropertyField(_condition, new GUIContent("추가 조건", "발동 사건이 들어온 뒤 실행 가능 여부를 검사합니다. 비워두면 조건 없이 실행합니다."));
            DrawCreateButtons(_condition, "조건 만들기", ConditionOptions, allowNone: true);

            DrawSection("3. 무엇을 실행할까? (Action)", "퀘스트 수락, 스토리 실행, 몬스터 그룹 활성화, 카메라 연출 같은 실제 결과입니다.");
            EditorGUILayout.PropertyField(_action, new GUIContent("실행 동작", "조건을 통과했을 때 실행할 동작입니다."));
            DrawCreateButtons(_action, "실행 동작 만들기", ActionOptions);

            DrawSection("4. 반복/잠금 규칙", "한 번만 쓸지, 쿨다운 뒤 다시 쓸지, 발동 후 Collider를 꺼둘지 정합니다.");
            EditorGUILayout.PropertyField(_repeat, new GUIContent("반복 방식", "트리거가 다시 발동할 수 있는 규칙입니다."));
            DrawRepeatHelp();

            using (new EditorGUI.DisabledScope((TriggerRepeatPolicy)_repeat.enumValueIndex != TriggerRepeatPolicy.Cooldown))
                EditorGUILayout.PropertyField(_cooldownSeconds, new GUIContent("쿨다운 초", "Cooldown 반복 방식일 때 다음 발동까지 기다릴 시간입니다."));

            EditorGUILayout.PropertyField(_disableColliderAfterTrigger, new GUIContent("발동 후 Collider 끄기", "성공적으로 실행된 뒤 같은 GameObject의 Collider를 비활성화합니다."));

            DrawSection("5. 디버그", "트리거가 왜 실행되지 않았는지 콘솔에서 확인해야 할 때만 켭니다.");
            EditorGUILayout.PropertyField(_logVerbose, new GUIContent("상세 로그 출력", "반복 정책 차단, 조건 실패, 액션 실행 시작/종료를 콘솔에 출력합니다."));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSummary()
        {
            EditorGUILayout.LabelField("현재 구성 요약", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSummaryLine("언제", Describe(_source.objectReferenceValue));
                DrawSummaryLine("조건", _condition.objectReferenceValue == null ? "조건 없음: 발동하면 바로 실행" : Describe(_condition.objectReferenceValue));
                DrawSummaryLine("무엇", Describe(_action.objectReferenceValue));
                DrawSummaryLine("반복", DescribeRepeat((TriggerRepeatPolicy)_repeat.enumValueIndex));

                var composer = (TriggerComposer)target;
                var collider = composer.GetComponent<Collider>();
                string colliderText = collider == null ? "없음" : $"{collider.GetType().Name}, Is Trigger = {collider.isTrigger}";
                DrawSummaryLine("Collider", colliderText);

                var rigidbody = composer.GetComponent<Rigidbody>();
                string rigidbodyText = rigidbody == null ? "없음" : $"있음, Is Kinematic = {rigidbody.isKinematic}";
                DrawSummaryLine("Rigidbody", rigidbodyText);
                DrawSummaryLine("Layer", LayerMask.LayerToName(composer.gameObject.layer));
            }
        }

        private static void DrawSummaryLine(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(54f));
                EditorGUILayout.LabelField(value, EditorStyles.wordWrappedLabel);
            }
        }

        private void DrawValidation()
        {
            if (_source.objectReferenceValue == null)
                EditorGUILayout.HelpBox("발동 사건(Source)이 비어 있습니다. 언제 시작할지 몰라 발동하지 않습니다.", MessageType.Warning);

            if (_action.objectReferenceValue == null)
                EditorGUILayout.HelpBox("실행 동작(Action)이 비어 있습니다. 조건을 통과해도 실제로 일어나는 일이 없습니다.", MessageType.Warning);

            var composer = (TriggerComposer)target;
            if (composer.GetComponent<Collider>() == null && IsColliderSource(_source.objectReferenceValue))
                EditorGUILayout.HelpBox("영역 진입/이탈 Source를 쓰려면 같은 GameObject에 Collider가 필요합니다. 아래 '필요 컴포넌트/Collider 자동 배치'를 누르면 BoxCollider를 추가하고 배치합니다.", MessageType.Error);

            if (IsColliderSource(_source.objectReferenceValue) && composer.GetComponent<Rigidbody>() == null)
                EditorGUILayout.HelpBox("플레이어가 Rigidbody를 쓰지 않으므로 Trigger 이벤트를 안정적으로 받으려면 트리거 영역 오브젝트에 Kinematic Rigidbody가 필요합니다. 자동 배치 버튼이 추가합니다.", MessageType.Warning);

            if (IsColliderSource(_source.objectReferenceValue) && LayerMask.NameToLayer(TriggerLayerName) < 0)
                EditorGUILayout.HelpBox("'Trigger' Layer가 ProjectSettings에 없습니다. Layer를 만든 뒤 자동 배치 버튼을 다시 누르세요.", MessageType.Warning);

            if (_repeat.enumValueIndex == (int)TriggerRepeatPolicy.OncePerSession && string.IsNullOrWhiteSpace(_triggerId.stringValue))
                EditorGUILayout.HelpBox("게임 실행 중 한 번만 발동하려면 Trigger ID가 필요합니다.", MessageType.Warning);

            if (NeedsSceneReferences(_source.objectReferenceValue, _action.objectReferenceValue) && composer.GetComponent<TriggerSceneReferences>() == null)
                EditorGUILayout.HelpBox("몬스터 그룹을 씬에서 연결해야 하는 구성입니다. TriggerSceneReferences 컴포넌트를 추가한 뒤 Group을 지정하세요.", MessageType.Info);

            if (_action.objectReferenceValue is PlayCameraSnapshotTriggerActionSO && composer.GetComponent<TriggerUnityEventRelay>() == null)
                EditorGUILayout.HelpBox("카메라 시퀀스 시작/완료 UnityEvent가 필요하면 TriggerUnityEventRelay 컴포넌트를 추가하세요.", MessageType.Info);
        }

        private void DrawPresetButtons()
        {
            DrawSection("빠른 프리셋", "자주 쓰는 트리거 조합을 한 번에 구성합니다. 생성된 에셋의 세부값은 Project 창이나 아래 Object Field에서 열어 지정하세요.");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("퀘스트 영역", "플레이어가 들어오면 퀘스트 수락 후 위치 도달 알림을 실행합니다.")))
                    ApplyQuestAreaPreset();

                if (GUILayout.Button(new GUIContent("스토리 영역", "플레이어가 들어오면 StoryEntry를 실행합니다.")))
                    ApplyStoryAreaPreset();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("그룹 전멸 후 스토리", "몬스터 그룹 전멸 시 스토리 진행도 변경 후 StoryEntry를 실행합니다.")))
                    ApplyGroupStoryPreset();

                if (GUILayout.Button(new GUIContent("카메라 영역", "플레이어가 들어오면 CameraSnapshotProfile 시퀀스를 실행합니다.")))
                    ApplyCameraAreaPreset();
            }

            if (GUILayout.Button(new GUIContent("필요 컴포넌트/Collider 자동 배치", "현재 Source/Action에 맞춰 Collider, Kinematic Rigidbody, Trigger Layer, TriggerSceneReferences, TriggerUnityEventRelay를 추가하거나 보정합니다.")))
                AutoFixComponents();
        }

        private void DrawSection(string title, string help)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(help, MessageType.None);
        }

        private void DrawRepeatHelp()
        {
            var repeat = (TriggerRepeatPolicy)_repeat.enumValueIndex;
            string message = repeat switch
            {
                TriggerRepeatPolicy.Once => "한 번 성공하면 다시 발동하지 않습니다. 일반적인 일회성 이벤트에 적합합니다.",
                TriggerRepeatPolicy.OncePerSession => "같은 Trigger ID는 게임 실행 중 한 번만 발동합니다. 씬 재진입 중복 방지에 씁니다.",
                TriggerRepeatPolicy.Cooldown => "성공 후 지정한 초가 지나면 다시 발동할 수 있습니다.",
                TriggerRepeatPolicy.Always => "들어오는 발동 사건마다 실행합니다. 반복 연출이나 테스트 용도에 가깝습니다.",
                _ => "반복 방식을 선택하세요.",
            };

            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        private void DrawCreateButtons(SerializedProperty property, string buttonLabel, OptionInfo[] options, bool allowNone = false)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(buttonLabel, EditorStyles.miniButtonLeft))
                {
                    var menu = new GenericMenu();
                    foreach (var option in options)
                    {
                        OptionInfo captured = option;
                        menu.AddItem(new GUIContent(captured.Label, captured.Description), false, () =>
                        {
                            property.objectReferenceValue = CreateAssetForComposer(captured.Type);
                            serializedObject.ApplyModifiedProperties();
                        });
                    }

                    menu.ShowAsContext();
                }

                using (new EditorGUI.DisabledScope(property.objectReferenceValue == null))
                {
                    if (GUILayout.Button(new GUIContent("선택", "Project 창에서 연결된 에셋을 찾습니다."), EditorStyles.miniButtonMid))
                        EditorGUIUtility.PingObject(property.objectReferenceValue);
                }

                using (new EditorGUI.DisabledScope(!allowNone && property.objectReferenceValue == null))
                {
                    if (GUILayout.Button(new GUIContent("비우기", "현재 연결을 제거합니다."), EditorStyles.miniButtonRight))
                    {
                        property.objectReferenceValue = null;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }

        private void ApplyQuestAreaPreset()
        {
            var source = CreateAssetForComposer(typeof(ColliderEnterTriggerSourceSO), "Source_PlayerEnter");
            var sequence = (SequenceTriggerActionSO)CreateAssetForComposer(typeof(SequenceTriggerActionSO), "Action_QuestArea");
            var acceptQuest = AddSubAsset<AcceptQuestTriggerActionSO>(sequence, "Step_AcceptQuest");
            var notifyLocation = AddSubAsset<NotifyLocationTriggerActionSO>(sequence, "Step_NotifyLocation");

            SetArray(sequence, "_steps", acceptQuest, notifyLocation);
            ConfigureComposer(source, sequence, TriggerRepeatPolicy.Once, disableColliderAfterTrigger: true);
            AutoFixComponents();
        }

        private void ApplyStoryAreaPreset()
        {
            var source = CreateAssetForComposer(typeof(ColliderEnterTriggerSourceSO), "Source_PlayerEnter");
            var action = CreateAssetForComposer(typeof(TriggerStoryTriggerActionSO), "Action_TriggerStory");
            ConfigureComposer(source, action, TriggerRepeatPolicy.Once, disableColliderAfterTrigger: true);
            AutoFixComponents();
        }

        private void ApplyGroupStoryPreset()
        {
            var source = CreateAssetForComposer(typeof(GroupDefeatedTriggerSourceSO), "Source_GroupDefeated");
            var sequence = (SequenceTriggerActionSO)CreateAssetForComposer(typeof(SequenceTriggerActionSO), "Action_GroupStory");
            var setProgress = AddSubAsset<SetStoryProgressTriggerActionSO>(sequence, "Step_SetStoryProgress");
            var triggerStory = AddSubAsset<TriggerStoryTriggerActionSO>(sequence, "Step_TriggerStory");

            SetArray(sequence, "_steps", setProgress, triggerStory);
            ConfigureComposer(source, sequence, TriggerRepeatPolicy.Once, disableColliderAfterTrigger: false);
            AutoFixComponents();
        }

        private void ApplyCameraAreaPreset()
        {
            var source = CreateAssetForComposer(typeof(ColliderEnterTriggerSourceSO), "Source_PlayerEnter");
            var action = CreateAssetForComposer(typeof(PlayCameraSnapshotTriggerActionSO), "Action_CameraSnapshot");
            ConfigureComposer(source, action, TriggerRepeatPolicy.Once, disableColliderAfterTrigger: true);
            AutoFixComponents();
        }

        private void ConfigureComposer(UnityEngine.Object source, UnityEngine.Object action, TriggerRepeatPolicy repeat, bool disableColliderAfterTrigger)
        {
            Undo.RecordObject(target, "Configure Trigger Preset");
            serializedObject.Update();
            _source.objectReferenceValue = source;
            _condition.objectReferenceValue = null;
            _action.objectReferenceValue = action;
            _repeat.enumValueIndex = (int)repeat;
            _disableColliderAfterTrigger.boolValue = disableColliderAfterTrigger;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void AutoFixComponents()
        {
            var composer = (TriggerComposer)target;
            Undo.RecordObject(composer.gameObject, "Auto Fix Trigger Components");

            if (IsColliderSource(_source.objectReferenceValue))
                EnsureTriggerCollider(composer);

            if (NeedsSceneReferences(_source.objectReferenceValue, _action.objectReferenceValue) && composer.GetComponent<TriggerSceneReferences>() == null)
                Undo.AddComponent<TriggerSceneReferences>(composer.gameObject);

            if (_action.objectReferenceValue is PlayCameraSnapshotTriggerActionSO && composer.GetComponent<TriggerUnityEventRelay>() == null)
                Undo.AddComponent<TriggerUnityEventRelay>(composer.gameObject);

            EditorUtility.SetDirty(composer.gameObject);
        }

        private static void EnsureTriggerCollider(TriggerComposer composer)
        {
            var collider = composer.GetComponent<Collider>();
            if (collider == null)
            {
                var box = Undo.AddComponent<BoxCollider>(composer.gameObject);
                ApplyDefaultTriggerBounds(composer.gameObject, box, force: true);
                EditorUtility.SetDirty(box);
            }
            else
            {
                collider.isTrigger = true;

                if (collider is BoxCollider boxCollider)
                    ApplyDefaultTriggerBounds(composer.gameObject, boxCollider, force: IsDefaultUnityBox(boxCollider));

                EditorUtility.SetDirty(collider);
            }

            EnsureTriggerRigidbody(composer.gameObject);
            SetTriggerLayer(composer.gameObject);
        }

        private static void EnsureTriggerRigidbody(GameObject gameObject)
        {
            var rigidbody = gameObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = Undo.AddComponent<Rigidbody>(gameObject);

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            EditorUtility.SetDirty(rigidbody);
        }

        private static void SetTriggerLayer(GameObject gameObject)
        {
            int triggerLayer = LayerMask.NameToLayer(TriggerLayerName);
            if (triggerLayer < 0)
                return;

            gameObject.layer = triggerLayer;
            EditorUtility.SetDirty(gameObject);
        }

        private static void ApplyDefaultTriggerBounds(GameObject gameObject, BoxCollider box, bool force)
        {
            box.isTrigger = true;

            if (!force)
                return;

            Bounds visualBounds;
            if (TryGetVisualBounds(gameObject, out visualBounds))
            {
                Vector3 localCenter = gameObject.transform.InverseTransformPoint(visualBounds.center);
                Vector3 localSize = DivideSafe(visualBounds.size, gameObject.transform.lossyScale);
                box.center = localCenter;
                box.size = new Vector3(
                    Mathf.Max(2f, localSize.x + 1f),
                    Mathf.Max(2.5f, localSize.y + 1f),
                    Mathf.Max(2f, localSize.z + 1f));
            }
            else
            {
                box.center = new Vector3(0f, 1.25f, 0f);
                box.size = new Vector3(4f, 2.5f, 4f);
            }
        }

        private static bool TryGetVisualBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>();
            bool hasBounds = false;

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static Vector3 DivideSafe(Vector3 value, Vector3 divisor)
        {
            return new Vector3(
                Mathf.Approximately(divisor.x, 0f) ? value.x : value.x / Mathf.Abs(divisor.x),
                Mathf.Approximately(divisor.y, 0f) ? value.y : value.y / Mathf.Abs(divisor.y),
                Mathf.Approximately(divisor.z, 0f) ? value.z : value.z / Mathf.Abs(divisor.z));
        }

        private static bool IsDefaultUnityBox(BoxCollider box)
        {
            return box.center == Vector3.zero && box.size == Vector3.one;
        }

        private ScriptableObject CreateAssetForComposer(Type type, string suffix = null)
        {
            EnsureFolder(ManualAssetRoot);

            var composer = (TriggerComposer)target;
            string sceneName = composer.gameObject.scene.IsValid() ? composer.gameObject.scene.name : "Prefab";
            string assetSuffix = string.IsNullOrEmpty(suffix) ? type.Name.Replace("Trigger", string.Empty).Replace("SO", string.Empty) : suffix;
            string fileName = $"{Sanitize(sceneName)}_{Sanitize(composer.gameObject.name)}_{Sanitize(assetSuffix)}.asset";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{ManualAssetRoot}/{fileName}");

            var asset = ScriptableObject.CreateInstance(type);
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        private static T AddSubAsset<T>(UnityEngine.Object parent, string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            AssetDatabase.AddObjectToAsset(asset, parent);
            EditorUtility.SetDirty(parent);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void SetArray(UnityEngine.Object owner, string propertyName, params UnityEngine.Object[] values)
        {
            var serializedOwner = new SerializedObject(owner);
            var property = serializedOwner.FindProperty(propertyName);
            property.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serializedOwner.ApplyModifiedProperties();
            EditorUtility.SetDirty(owner);
            AssetDatabase.SaveAssets();
        }

        private static bool IsColliderSource(UnityEngine.Object source)
        {
            return source is ColliderEnterTriggerSourceSO || source is ColliderExitTriggerSourceSO;
        }

        private static bool NeedsSceneReferences(UnityEngine.Object source, UnityEngine.Object action)
        {
            return source is GroupDefeatedTriggerSourceSO || action is ActivateGroupTriggerActionSO;
        }

        private static string Describe(UnityEngine.Object value)
        {
            if (value == null)
                return "비어 있음";

            foreach (var option in SourceOptions)
            {
                if (option.Type == value.GetType())
                    return option.Label;
            }

            foreach (var option in ConditionOptions)
            {
                if (option.Type == value.GetType())
                    return option.Label;
            }

            foreach (var option in ActionOptions)
            {
                if (option.Type == value.GetType())
                    return option.Label;
            }

            return ObjectNames.NicifyVariableName(value.GetType().Name.Replace("SO", string.Empty));
        }

        private static string DescribeRepeat(TriggerRepeatPolicy repeat)
        {
            return repeat switch
            {
                TriggerRepeatPolicy.Once => "한 번만 발동",
                TriggerRepeatPolicy.OncePerSession => "게임 실행 중 같은 ID는 한 번만 발동",
                TriggerRepeatPolicy.Cooldown => "쿨다운 뒤 다시 발동",
                TriggerRepeatPolicy.Always => "발동 사건마다 매번 실행",
                _ => repeat.ToString(),
            };
        }

        [MenuItem("GameObject/UPlayGround/Trigger/Collider Enter Trigger", false, 10)]
        private static void CreateColliderEnterTrigger(MenuCommand command)
        {
            var gameObject = new GameObject("Trigger_ColliderEnter");
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Collider Enter Trigger");

            var collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 1.25f, 0f);
            collider.size = new Vector3(4f, 2.5f, 4f);

            var rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            SetTriggerLayer(gameObject);

            var composer = gameObject.AddComponent<TriggerComposer>();
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(composer);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');

            return value.Replace(' ', '_').Replace('/', '_').Replace('\\', '_');
        }

        private readonly struct OptionInfo
        {
            public OptionInfo(Type type, string label, string description)
            {
                Type = type;
                Label = label;
                Description = description;
            }

            public Type Type { get; }
            public string Label { get; }
            public string Description { get; }
        }
    }

    public abstract class TriggerSystemAssetEditorBase : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawAssetGuide();
            EditorGUILayout.Space(4f);
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAssetGuide()
        {
            string title = target.GetType().Name.Replace("Trigger", string.Empty).Replace("SO", string.Empty);
            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(title), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(GetDescription(target), MessageType.Info);
            DrawFieldGuide(target);
        }

        private static string GetDescription(UnityEngine.Object value)
        {
            return value switch
            {
                ColliderEnterTriggerSourceSO _ => "플레이어가 Trigger Collider 안으로 들어오면 발동합니다.",
                ColliderExitTriggerSourceSO _ => "플레이어가 Trigger Collider 밖으로 나가면 발동합니다.",
                GroupDefeatedTriggerSourceSO _ => "연결한 몬스터 그룹이 전멸하면 발동합니다. 씬 오브젝트 참조는 TriggerSceneReferences의 Group으로 연결할 수 있습니다.",
                AndTriggerConditionSO _ => "하위 조건을 모두 만족해야 통과합니다. 조건 배열이 비어 있으면 통과합니다.",
                OrTriggerConditionSO _ => "하위 조건 중 하나라도 만족하면 통과합니다. 조건 배열이 비어 있으면 통과합니다.",
                NotTriggerConditionSO _ => "하위 조건 결과를 반대로 바꿉니다. 하위 조건이 비어 있으면 통과합니다.",
                GlobalFlagTriggerConditionSO _ => "GlobalFlagManager의 플래그 값을 확인합니다.",
                StoryProgressTriggerConditionSO _ => "StoryManager의 현재 진행도 값을 확인합니다.",
                QuestStatusTriggerConditionSO _ => "QuestManager에 등록된 퀘스트 상태를 확인합니다.",
                ActorAliveTriggerConditionSO _ => "씬 참조나 발동 대상 액터가 살아 있는지 확인합니다.",
                RandomChanceTriggerConditionSO _ => "확률 판정에 성공했을 때만 통과합니다.",
                SequenceTriggerActionSO _ => "여러 Action을 순서대로 실행합니다. 퀘스트 수락 후 위치 알림처럼 복합 동작에 사용합니다.",
                AcceptQuestTriggerActionSO _ => "지정한 퀘스트를 수락 상태로 만듭니다.",
                NotifyLocationTriggerActionSO _ => "지정한 위치 ID에 도달했다고 QuestManager에 알립니다.",
                TriggerStoryTriggerActionSO _ => "지정한 StoryEntry를 실행합니다.",
                SetStoryProgressTriggerActionSO _ => "StoryManager의 진행도 값을 지정한 값으로 바꿉니다.",
                SetFlagTriggerActionSO _ => "GlobalFlagManager의 플래그 값을 켜거나 끕니다.",
                ActivateGroupTriggerActionSO _ => "MonsterGroupController를 활성화합니다. 직접 지정하지 않으면 TriggerSceneReferences의 Group을 사용합니다.",
                PlayCameraSnapshotTriggerActionSO _ => "CameraSnapshotProfile 시퀀스를 재생합니다. 시작/완료 이벤트가 필요하면 TriggerUnityEventRelay를 함께 사용합니다.",
                UnityEventTriggerActionSO _ => "TriggerUnityEventRelay의 UnityEvent를 호출합니다.",
                DelayTriggerActionSO _ => "Sequence 안에서 다음 Action 실행 전 대기 시간을 줍니다.",
                _ => "Trigger Composer에서 사용하는 설정 에셋입니다.",
            };
        }

        private static void DrawFieldGuide(UnityEngine.Object value)
        {
            string message = value switch
            {
                ColliderEnterTriggerSourceSO _ => "Actor Filter는 통과시킬 액터 타입입니다. Fallback Tag는 GameActor가 없는 Collider를 태그로 판정할 때 사용합니다.",
                ColliderExitTriggerSourceSO _ => "Actor Filter는 통과시킬 액터 타입입니다. Fallback Tag는 GameActor가 없는 Collider를 태그로 판정할 때 사용합니다.",
                GroupDefeatedTriggerSourceSO _ => "Target Group을 비워두면 같은 GameObject의 TriggerSceneReferences.Group 또는 MonsterGroupController를 찾습니다.",
                SequenceTriggerActionSO _ => "Steps 배열에 실행할 Action 에셋을 순서대로 넣으세요. Delay를 섞으면 연출 간격을 만들 수 있습니다.",
                AcceptQuestTriggerActionSO _ => "Quest Id가 None이면 실행되지 않습니다.",
                NotifyLocationTriggerActionSO _ => "Location Id는 퀘스트 목표 데이터와 같은 문자열이어야 합니다.",
                TriggerStoryTriggerActionSO _ => "Story Entry가 비어 있으면 실행되지 않습니다.",
                SetStoryProgressTriggerActionSO _ => "Progress는 StoryManager에 저장할 새 진행도 값입니다.",
                SetFlagTriggerActionSO _ => "Key가 비어 있으면 실행되지 않습니다. Value가 true면 켜고 false면 끕니다.",
                ActivateGroupTriggerActionSO _ => "Target Group을 직접 지정하거나, Trigger Composer 옆 TriggerSceneReferences의 Group에 연결하세요.",
                PlayCameraSnapshotTriggerActionSO _ => "Profile이 필수입니다. Wait For Completed를 켜면 카메라 시퀀스 종료까지 다음 Sequence Step을 기다립니다.",
                UnityEventTriggerActionSO _ => "TriggerUnityEventRelay 컴포넌트가 같은 GameObject에 있어야 씬 이벤트를 호출할 수 있습니다.",
                DelayTriggerActionSO _ => "Seconds가 0이면 대기 없이 바로 다음 Step으로 넘어갑니다.",
                _ => null,
            };

            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, MessageType.None);
        }
    }

    [CustomEditor(typeof(TriggerSourceSO), true)]
    public sealed class TriggerSourceAssetEditor : TriggerSystemAssetEditorBase
    {
    }

    [CustomEditor(typeof(TriggerConditionSO), true)]
    public sealed class TriggerConditionAssetEditor : TriggerSystemAssetEditorBase
    {
    }

    [CustomEditor(typeof(TriggerActionSO), true)]
    public sealed class TriggerActionAssetEditor : TriggerSystemAssetEditorBase
    {
    }
}
#endif
