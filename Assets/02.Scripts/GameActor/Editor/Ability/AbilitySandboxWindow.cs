#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Editor.Ability
{
    public sealed class AbilitySandboxWindow : EditorWindow
    {
        [SerializeField] private GameObject _ownerPrefab;
        [SerializeField] private GameObject _targetPrefab;
        [SerializeField] private GameplayAbilitySO _ability;
        [SerializeField] private float _distance = 2f;
        private Button _runButton;
        private HelpBox _reportBox;

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/Ability Runtime Sandbox")]
        public static void Open()
        {
            var window = GetWindow<AbilitySandboxWindow>();
            window.titleContent = new GUIContent("Ability Sandbox");
            window.minSize = new Vector2(560f, 360f);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 12f;
            rootVisualElement.style.paddingRight = 12f;
            rootVisualElement.style.paddingTop = 10f;
            rootVisualElement.style.paddingBottom = 10f;

            var title = new Label("Ability Runtime Sandbox");
            title.style.fontSize = 16f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 8f;
            rootVisualElement.Add(title);
            rootVisualElement.Add(new HelpBox(
                "선택한 실제 Actor 프리팹을 Play Mode에서 임시 생성하고 "
                + "ActorAbilitySystem의 Prepare → Commit → End 경로를 실행합니다. "
                + "Motion 재생·상태 머신·히트 판정은 프리팹과 게임 부트스트랩에 "
                + "의존하므로 이 검사는 전체 게임 스모크를 대체하지 않습니다.",
                HelpBoxMessageType.Info));

            VisualElement inputs = CreateSection("실행 입력");
            var ownerField = new ObjectField("Owner Actor Prefab")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
                value = _ownerPrefab,
            };
            ownerField.tooltip = "ActorAbilitySystem을 실행할 GameActor 포함 프리팹입니다.";
            ownerField.RegisterValueChangedCallback(evt =>
            {
                _ownerPrefab = evt.newValue as GameObject;
                RefreshRunState();
            });
            inputs.Add(ownerField);

            var targetField = new ObjectField("Target Actor Prefab (선택)")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
                value = _targetPrefab,
            };
            targetField.RegisterValueChangedCallback(
                evt => _targetPrefab = evt.newValue as GameObject);
            inputs.Add(targetField);

            var abilityField = new ObjectField("Ability")
            {
                objectType = typeof(GameplayAbilitySO),
                allowSceneObjects = false,
                value = _ability,
            };
            abilityField.RegisterValueChangedCallback(evt =>
            {
                _ability = evt.newValue as GameplayAbilitySO;
                RefreshRunState();
            });
            inputs.Add(abilityField);

            var distanceField = new FloatField("대상 거리")
            {
                value = Mathf.Max(0f, _distance),
            };
            distanceField.RegisterValueChangedCallback(evt =>
            {
                _distance = Mathf.Max(0f, evt.newValue);
                if (!Mathf.Approximately(distanceField.value, _distance))
                    distanceField.SetValueWithoutNotify(_distance);
            });
            inputs.Add(distanceField);

            _runButton = new Button(() => AbilitySandboxRunner.Request(
                _ownerPrefab,
                _targetPrefab,
                _ability,
                _distance))
            {
                text = "Play Mode에서 ASC 수직 슬라이스 실행",
            };
            _runButton.style.height = 34f;
            _runButton.style.marginTop = 8f;
            inputs.Add(_runButton);
            rootVisualElement.Add(inputs);

            VisualElement result = CreateSection("마지막 결과");
            _reportBox = new HelpBox(string.Empty, HelpBoxMessageType.None);
            _reportBox.style.whiteSpace = WhiteSpace.Normal;
            result.Add(_reportBox);
            result.Add(new Button(RefreshReport) { text = "결과 새로고침" });
            rootVisualElement.Add(result);
            RefreshReport();
            RefreshRunState();
        }

        private static VisualElement CreateSection(string title)
        {
            var section = new VisualElement();
            section.style.marginTop = 12f;
            section.style.paddingLeft = 10f;
            section.style.paddingRight = 10f;
            section.style.paddingTop = 8f;
            section.style.paddingBottom = 10f;
            section.style.borderLeftWidth = 1f;
            section.style.borderRightWidth = 1f;
            section.style.borderTopWidth = 1f;
            section.style.borderBottomWidth = 1f;
            var heading = new Label(title);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 6f;
            section.Add(heading);
            return section;
        }

        private void RefreshRunState()
        {
            bool valid = _ownerPrefab != null
                && _ownerPrefab.GetComponentInChildren<GameActor>(true) != null
                && _ability != null;
            _runButton?.SetEnabled(
                valid && !EditorApplication.isPlayingOrWillChangePlaymode);
            if (_runButton != null)
            {
                _runButton.tooltip = valid
                    ? "임시 Play Mode에 진입해 Prepare → Commit → End를 실행합니다."
                    : "GameActor가 포함된 Owner 프리팹과 Ability를 선택하세요.";
            }
        }

        private void RefreshReport()
        {
            if (_reportBox == null)
                return;
            _reportBox.text = SessionState.GetString(
                AbilitySandboxRunner.ReportKey,
                "아직 실행하지 않았습니다.");
        }
    }

    [InitializeOnLoad]
    internal static class AbilitySandboxRunner
    {
        internal const string ReportKey =
            "UPlayGround.AbilitySandbox.Report";
        private const string PendingKey =
            "UPlayGround.AbilitySandbox.Pending";
        private const string OwnerKey =
            "UPlayGround.AbilitySandbox.Owner";
        private const string TargetKey =
            "UPlayGround.AbilitySandbox.Target";
        private const string AbilityKey =
            "UPlayGround.AbilitySandbox.Ability";
        private const string DistanceKey =
            "UPlayGround.AbilitySandbox.Distance";

        static AbilitySandboxRunner()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        internal static void Request(
            GameObject owner,
            GameObject target,
            GameplayAbilitySO ability,
            float distance)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(
                OwnerKey,
                AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(owner)));
            SessionState.SetString(
                TargetKey,
                target != null
                    ? AssetDatabase.AssetPathToGUID(
                        AssetDatabase.GetAssetPath(target))
                    : string.Empty);
            SessionState.SetString(
                AbilityKey,
                AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(ability)));
            SessionState.SetFloat(DistanceKey, distance);
            SessionState.SetString(ReportKey, "Play Mode 진입 대기 중...");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode
                || !SessionState.GetBool(PendingKey, false))
                return;
            EditorApplication.delayCall += Execute;
        }

        private static void Execute()
        {
            GameObject ownerObject = null;
            GameObject targetObject = null;
            AbilitySetSO transientSet = null;
            try
            {
                GameObject ownerPrefab = Load<GameObject>(OwnerKey);
                GameObject targetPrefab = Load<GameObject>(TargetKey);
                GameplayAbilitySO ability =
                    Load<GameplayAbilitySO>(AbilityKey);
                if (ownerPrefab == null || ability == null)
                    throw new InvalidOperationException(
                        "Owner 프리팹 또는 Ability를 다시 불러오지 못했습니다.");

                ownerObject = UnityEngine.Object.Instantiate(ownerPrefab);
                ownerObject.name = "__AbilitySandboxOwner";
                GameActor owner =
                    ownerObject.GetComponentInChildren<GameActor>(true);
                if (owner == null)
                    throw new InvalidOperationException(
                        "Owner 프리팹에 GameActor가 없습니다.");
                owner.gameObject.SetActive(true);

                GameActor target = null;
                if (targetPrefab != null)
                {
                    targetObject = UnityEngine.Object.Instantiate(targetPrefab);
                    targetObject.name = "__AbilitySandboxTarget";
                    target = targetObject.GetComponentInChildren<GameActor>(true);
                    targetObject.transform.position =
                        Vector3.forward
                        * SessionState.GetFloat(DistanceKey, 2f);
                    targetObject.SetActive(true);
                }

                transientSet = ScriptableObject.CreateInstance<AbilitySetSO>();
                transientSet.additionalAbilities.Add(ability);
                owner.Abilities.SetAbilitySet(transientSet);

                AbilityActivationResult prepare =
                    owner.Abilities.TryPrepareAbility(
                        ability,
                        true,
                        target,
                        out AbilityExecutionHandle handle,
                        out AbilityVariantDefinition variant);
                AbilityActivationResult commit =
                    prepare == AbilityActivationResult.Success
                        ? owner.Abilities.Commit(handle)
                        : prepare;
                int tasksDuring =
                    owner.AbilitySystem.Runtime.Tasks.Count;
                string variantId = variant?.variantId ?? "(없음)";
                owner.Abilities.EndActiveAbility(completed: true);
                int tasksAfter =
                    owner.AbilitySystem.Runtime.Tasks.Count;
                AbilitySystemSaveData snapshot =
                    owner.Abilities.CaptureAbilitySystemStateForCharacter(
                        false);
                int effectsAfter = snapshot.activeEffects.Count;
                string verdict =
                    commit == AbilityActivationResult.Success
                    && tasksAfter == 0
                        ? "PASS"
                        : "CHECK";
                string report =
                    $"Ability: {ability.abilityId}\n"
                    + $"Prepare: {prepare}\n"
                    + $"Variant: {variantId}\n"
                    + $"Commit: {commit}\n"
                    + $"실행 중 Task: {tasksDuring}\n"
                    + $"종료 후 Task: {tasksAfter}\n"
                    + $"종료 후 저장 대상 Effect: {effectsAfter}\n"
                    + $"판정: {verdict}";
                SessionState.SetString(ReportKey, report);
            }
            catch (Exception exception)
            {
                SessionState.SetString(
                    ReportKey,
                    $"Sandbox 실행 실패\n{exception.GetType().Name}: "
                    + exception.Message);
                Debug.LogException(exception);
            }
            finally
            {
                if (ownerObject != null)
                    UnityEngine.Object.Destroy(ownerObject);
                if (targetObject != null)
                    UnityEngine.Object.Destroy(targetObject);
                if (transientSet != null)
                    UnityEngine.Object.Destroy(transientSet);
                SessionState.SetBool(PendingKey, false);
                EditorApplication.ExitPlaymode();
            }
        }

        private static T Load<T>(string key) where T : UnityEngine.Object
        {
            string guid = SessionState.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(guid))
                return null;
            return AssetDatabase.LoadAssetAtPath<T>(
                AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
#endif
