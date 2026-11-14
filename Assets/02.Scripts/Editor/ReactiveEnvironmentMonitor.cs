using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Gameplay.World.ReactiveEnvironment;
using UPlayGround.Manager;

namespace UPlayGround.EditorTools
{
    /// <summary>환경 에셋을 소환하고 실제 반응·파괴·보상 경로를 시험한다.</summary>
    public sealed class ReactiveEnvironmentMonitor : EditorWindow
    {
        [Serializable]
        private sealed class SpawnedAsset
        {
            public ReactiveEnvironmentObject target;
            public GameObject prefab;
        }

        [SerializeField] private ReactiveEnvironmentObject _target;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private List<SpawnedAsset> _spawned = new();
        [SerializeField] private float _distance = 5;
        [SerializeField] private float _heat = 1;
        [SerializeField] private float _water = 1;
        [SerializeField] private float _damage = 25;
        private ulong _nextAttack = (ulong)DateTime.UtcNow.Ticks;

        private string _message;
        private Vector2 _scroll;

        [UPlaygroundTool("World/환경 반응 모니터")]
        public static void Open()
        {
            var window = GetWindow<ReactiveEnvironmentMonitor>("환경 에셋 치트");
            window.minSize = new Vector2(360, 450);
        }

        private void CreateGUI()
        {

            UPlaygroundEditorUX.PrepareRoot(rootVisualElement, "reactive-environment-monitor");
            rootVisualElement.Add(new IMGUIContainer(Draw));
        }

        private void OnInspectorUpdate() => Repaint();

        private void Draw()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;
            EditorGUILayout.HelpBox("Editor Play Mode 전용. 실제 보상과 저장 경로를 사용하므로 테스트 슬롯에서 실행하세요. 비는 기존 날씨 미리보기에서 조절합니다.", MessageType.Info);
            _prefab = (GameObject)EditorGUILayout.ObjectField("테스트 프리팹", _prefab, typeof(GameObject), false);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                _distance = EditorGUILayout.Slider("전방 거리 (m)", _distance, 2, 20);

                if (GUILayout.Button("지정 프리팹 소환")) SpawnInFront();
            }

            var world = WorldStateManager.Instance;
            if (world != null)
            {
                EditorGUILayout.LabelField("반응 대상 / 저장 기록", world.EnvironmentTargetCount + " / " + world.EnvironmentRecordCount);
                EditorGUILayout.LabelField("최근 substep 공급원 / 차폐 Raycast", world.EnvironmentSourceCount + " / " + world.EnvironmentOcclusionQueries);
            }
            EditorGUILayout.Space();
            _target = (ReactiveEnvironmentObject)EditorGUILayout.ObjectField("반응 대상", _target, typeof(ReactiveEnvironmentObject), true);
            if (GUILayout.Button("Hierarchy 선택 대상 사용"))
                _target = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<ReactiveEnvironmentObject>() : null;
            _spawned.RemoveAll(entry => entry.target == null);
            foreach (SpawnedAsset entry in _spawned)
                if (GUILayout.Button("대상 선택: " + entry.target.name)) Select(entry.target);
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.Info);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("이 창에서 소환한 개체 모두 제거")) ClearSpawned();
            }
            if (_target == null) return;
            var state = _target.State;
            EditorGUILayout.LabelField("배치 ID", _target.StableId ?? "등록 대기");
            EditorGUILayout.LabelField("열 / 젖음 / 그을림", $"{state.heat:F2} / {state.wetness:F2} / {state.charAmount:F2}");
            EditorGUILayout.LabelField("연료 / 내구도", $"{state.fuel:F2} / {state.durability:F2}");
            EditorGUILayout.LabelField("연소 / 파괴 / 등록", $"{state.combustion} / {state.isBroken} / {_target.IsAlive()}");
            EditorGUILayout.ObjectField("반응 프로필", _target.Definition, typeof(UPlayGround.Data.ReactiveEnvironment.ReactiveEnvironmentDefinitionSO), false);
            if (GUILayout.Button("날씨·입력·전파 진단 기록"))
            {
                string diagnostic = $"{DateTime.UtcNow:O} id={_target.StableId}\n{_target.DescribeRain()}\n"
                    + (world != null ? world.DescribeEnvironmentInputs(_target) + "\n" + world.DescribeEnvironmentSources(_target) : "중앙 작성자 없음");
                _message = diagnostic;
                Debug.Log(diagnostic, _target);
            }
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || !_target.IsAlive()))
            {
                if (GUILayout.Button("선택 위치에 실제 낙뢰 요청 (저장 반영)"))
                {
                    var weather = FindFirstObjectByType<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>();
                    if (weather != null && weather.TryRequestLightningAt(_target.ReactionPosition, out var strike))
                    {
                        _message = $"실제 낙뢰 시험 map={strike.MapId} id={strike.StrikeId} minute={strike.GameMinute:F3} point={strike.Point} normal={strike.Normal} layer={strike.SurfaceLayerId} radius={strike.Radius} heat={strike.Heat} profile={strike.ProfileId}";
                        Debug.Log(_message, _target);
                    }
                    else _message = "낙뢰 요청 거부: 프로필 후보 구역·첫 표면·지역·날씨 서비스를 확인하세요.";
                }
                _heat = EditorGUILayout.Slider("열 충격", _heat, 0, 10);
                if (GUILayout.Button("열 적용")) Apply(0, _heat, 0);
                _water = EditorGUILayout.Slider("물 충격", _water, 0, 10);
                if (GUILayout.Button("물 적용")) Apply(0, 0, _water);
                _damage = Mathf.Max(0, EditorGUILayout.FloatField("구조 피해", _damage));
                if (GUILayout.Button("구조 타격")) Apply(_damage, 0, 0);
                if (GUILayout.Button("남은 내구도만큼 파괴")) Apply(state.durability, 0, 0);
            }
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || FindOwned(_target) == null))
            {
                if (GUILayout.Button("대상 초기화: 새 개체로 교체")) ReplaceSelected();
                if (GUILayout.Button("대상 제거")) RemoveSelected();
            }
            EditorGUILayout.HelpBox("초기화/제거는 이 창이 소환한 개체만 대상으로 합니다. 이미 나온 보상과 저장 원장은 유지합니다. 반복 입력의 화학 반응은 게임 시간 경과 후 확인하세요.", MessageType.None);
        }

        private void SpawnInFront()
        {
            var player = PartyManager.Instance != null ? PartyManager.Instance.ActiveCharacter : null;
            if (player == null || WorldStateManager.Instance == null)
            {
                Report("소환 실패: 게임 매니저와 활성 플레이어가 필요합니다.");
                return;
            }
            Transform origin = player.transform;
            Vector3 forward = Vector3.ProjectOnPlane(origin.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 position = origin.position + forward * _distance;
            int groundLayers = LayerMask.GetMask("Ground");
            if (Physics.Raycast(position + Vector3.up * 3, Vector3.down, out var hit, 30, groundLayers, QueryTriggerInteraction.Ignore))
                position = hit.point;
            else
            {
                Report("소환 실패: 전방에 Ground 지면이 없습니다. 위치/거리를 바꿔 주세요.");
                return;
            }
            SpawnAt(_prefab, origin, position, Quaternion.LookRotation(forward));
        }

        private ReactiveEnvironmentObject SpawnAt(GameObject prefab, Transform origin, Vector3 position, Quaternion rotation)
        {
            var target = ReactiveEnvironmentCheats.Spawn(prefab, origin, position, rotation, out string error);
            if (target == null) { Report(error); return null; }
            _spawned.Add(new SpawnedAsset { target = target, prefab = prefab });
            Select(target);
            Report("환경 소환: " + target.name + ". 등록이 False이면 날씨/지역/저작 구성을 확인하세요.");
            return target;
        }

        private void Select(ReactiveEnvironmentObject target)
        {
            _target = target;
            Selection.activeGameObject = target.gameObject;
            EditorGUIUtility.PingObject(target);
        }

        private void Apply(float damage, float heat, float water)
        {
            bool accepted = _target.TryApplyImpact(++_nextAttack, 0, damage, heat, water);
            Report(accepted ? $"환경 입력: 피해 {damage:0.##}, 열 {heat:0.##}, 물 {water:0.##}" : "환경 입력 거부: 등록 상태와 입력값을 확인하세요.");
        }

        private SpawnedAsset FindOwned(ReactiveEnvironmentObject target) => _spawned.Find(entry => entry.target == target);

        private void ReplaceSelected()
        {
            SpawnedAsset entry = FindOwned(_target);
            if (entry == null) return;
            var previous = entry.target;
            if (SpawnAt(entry.prefab, previous.transform, previous.transform.position, previous.transform.rotation) != null)
            {
                previous.gameObject.SetActive(false);
                Destroy(previous.gameObject);
                _spawned.Remove(entry);
            }
        }

        private void RemoveSelected()
        {
            SpawnedAsset entry = FindOwned(_target);
            if (entry == null) return;
            entry.target.gameObject.SetActive(false);
            Destroy(entry.target.gameObject);
            _spawned.Remove(entry);
            _target = null;
            Report("선택한 환경 테스트 개체 제거");
        }

        private void ClearSpawned()
        {
            foreach (SpawnedAsset entry in _spawned)
                if (entry.target != null) { entry.target.gameObject.SetActive(false); Destroy(entry.target.gameObject); }
            _spawned.Clear();
            _target = null;
            Report("이 창에서 소환한 환경 개체 제거");
        }

        private void Report(string message)
        {
            _message = message;
            CheatManager.Instance?.Log(CheatCategory.Spawn, message);
        }
    }
}
