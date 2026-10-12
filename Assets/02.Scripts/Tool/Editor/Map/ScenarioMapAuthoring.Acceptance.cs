using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UPlayGround.Components;
using UPlayGround.Manager;
using UPlayGround.Dialogue;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string AcceptancePlanKey = "UPlayGround.ScenarioMap.AcceptancePlan";
        private static AcceptancePlan s_acceptance;
        private static int s_acceptanceIndex;
        private static float s_acceptanceHoldUntil;
        private static float s_acceptanceHoldHealth;
        private static float s_acceptanceProbeTime;
        private static NpcActor s_acceptanceNpc;
        private static int s_acceptanceDialogueStage;
        private static readonly Dictionary<string, EnemyDetection> s_acceptanceEnemies = new();
        private static readonly HashSet<string> s_acceptanceAlerts = new();

        [Serializable] private sealed class AcceptancePlan
        {
            public AcceptanceLeg[] legs;
        }

        private enum AcceptanceDialogueExit { Skip, CancelInput, Advance }

        [Serializable] private sealed class AcceptanceLeg
        {
            public string name;
            public string route;
            public Vector3[] points;
            public bool roundTrip;
            public bool placeAtStart;
            public bool useNavigation;
            public float holdSeconds = 2;
            public string[] observeIds = Array.Empty<string>();
            public bool requireAlert;
            public bool requireCalm;
            public string npcPath;
            public string expectedSpeaker;
            public AcceptanceDialogueExit dialogueExit;
        }

        [Serializable] private sealed class AcceptanceResult
        {
            public string name;
            public bool placedAtStart;
            public int reached;
            public int requested;
            public string[] alertedIds;
            public int remainingTargets;
            public float healthBeforeHold;
            public float healthAfterHold;
            public bool dialogueStarted;
            public bool dialogueEnded;
            public string dialogueExit;
            public bool portraitAndHistoryMatched;
            public bool backlogPortraitMatched;
            public Vector3 position;
        }

        private static void BeginAcceptance(double now)
        {
            string path = SessionState.GetString(AcceptancePlanKey, "");
            s_acceptance = JsonUtility.FromJson<AcceptancePlan>(File.ReadAllText(path));
            if (s_acceptance?.legs == null || s_acceptance.legs.Length == 0)
                throw new InvalidOperationException("인수 검증 구간이 없습니다.");
            s_acceptanceEnemies.Clear();
            foreach (EnemyDetection enemy in UnityEngine.Object.FindObjectsByType<EnemyDetection>(FindObjectsSortMode.None))
            {
                SceneEntityId id = enemy.GetComponentInParent<SceneEntityId>();
                if (id != null) s_acceptanceEnemies[id.Guid] = enemy;
            }
            CheckAcceptanceRespawnMigration();
            CheckAcceptanceWaterBoundary();
            s_acceptanceIndex = 0;
            s_smokeDeadline = now + 3600;
            BeginAcceptanceLeg();
        }

        private static void BeginAcceptanceLeg()
        {
            AcceptanceLeg leg = s_acceptance.legs[s_acceptanceIndex];
            Vector3[] points = leg.points;
            if (!string.IsNullOrEmpty(leg.route))
            {
                Route route = Array.Find(ReadLayout().routes, item => item.name == leg.route);
                if (route == null) throw new InvalidOperationException("없는 검증 경로: " + leg.route);
                points = route.points;
            }
            if (points == null || points.Length < 2)
                throw new InvalidOperationException("인수 검증에는 두 지점 이상이 필요합니다: " + leg.name);
            Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            var grounded = new List<Vector3>();
            foreach (Vector3 point in points) grounded.Add(GroundPoint(point, terrain));
            s_acceptanceNpc = null;
            if (!string.IsNullOrEmpty(leg.npcPath))
            {
                GameObject npcObject = GameObject.Find(leg.npcPath);
                if (npcObject == null || !npcObject.TryGetComponent(out s_acceptanceNpc))
                    throw new InvalidOperationException("대화 검사 NPC가 없습니다: " + leg.npcPath);
                grounded[grounded.Count - 1] = s_acceptanceNpc.transform.position
                    + Vector3.ProjectOnPlane(grounded[0] - s_acceptanceNpc.transform.position, Vector3.up).normalized * 1.7f;
            }
            if (!leg.placeAtStart) grounded[0] = s_smokePlayer.transform.position;
            if (leg.useNavigation) grounded = ResolveAcceptancePath(grounded);
            if (leg.roundTrip)
                for (int index = grounded.Count - 2; index >= 0; index--) grounded.Add(grounded[index]);
            s_traversalPoints = grounded.ToArray();
            if (leg.placeAtStart)
            {
                // 독립 사례의 시작 위치만 설정한다. 검사 경로 내부 이동은 실제 KCC 입력을 사용한다.
                var motor = s_smokePlayer.ActorController.Motor;
                motor.BaseVelocity = Vector3.zero;
                motor.SetPositionAndRotation(s_traversalPoints[0] + Vector3.up * 0.15f,
                    Quaternion.LookRotation(s_traversalPoints[1] - s_traversalPoints[0]));
            }
            foreach (string id in leg.observeIds)
                if (!s_acceptanceEnemies.ContainsKey(id)) throw new InvalidOperationException("검사할 적 ID가 없습니다: " + id);
            s_acceptanceAlerts.Clear();
            s_traversalCamera = Camera.main;
            if (s_traversalCamera == null) throw new InvalidOperationException("플레이 카메라가 없습니다.");
            s_traversalIndex = leg.placeAtStart ? 1 : 0;
            s_traversalFrame = -1;
            s_traversalProgressTime = s_traversalGroundTime = Time.time;
            s_traversalBestDistance = float.PositiveInfinity;
            s_smoke.traversal = new TraversalReport { route = leg.name, requestedWaypoints = s_traversalPoints.Length,
                reachedWaypoints = s_traversalIndex };
            s_smokeStep = 40;
            s_smokeNextStep = EditorApplication.timeSinceStartup + 0.5;
            Debug.Log("[P02Acceptance] 시작: " + leg.name);
        }

        private static List<Vector3> ResolveAcceptancePath(List<Vector3> points)
        {
            var result = new List<Vector3>();
            var path = new NavMeshPath();
            for (int index = 1; index < points.Count; index++)
            {
                if (!NavMesh.SamplePosition(points[index - 1], out NavMeshHit from, 2, NavMesh.AllAreas)
                    || !NavMesh.SamplePosition(points[index], out NavMeshHit to, 2, NavMesh.AllAreas)
                    || !NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path)
                    || path.status != NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("인수 검사 접근 경로 단절: " + index);
                foreach (Vector3 corner in path.corners) result.Add(corner);
            }
            return result;
        }

        private static void ObserveAcceptance()
        {
            if (s_acceptance == null) return;
            if (s_traversalIndex == 1 && s_smoke.traversal.reachedPositions.Count == 0
                && s_acceptance.legs[s_acceptanceIndex].placeAtStart)
                s_smoke.traversal.reachedPositions.Add(s_smokePlayer.transform.position);
            if (s_acceptanceNpc != null && s_smokeStep == 40 && s_traversalIndex == s_traversalPoints.Length - 1)
                s_traversalPoints[s_traversalIndex] = s_acceptanceNpc.transform.position
                    + Vector3.ProjectOnPlane(s_smokePlayer.transform.position - s_acceptanceNpc.transform.position, Vector3.up).normalized * 1.7f;
            if (s_smokePlayer.CurrentHealth <= 0) throw new InvalidOperationException("검증 중 플레이어 사망");
            foreach (string id in s_acceptance.legs[s_acceptanceIndex].observeIds)
                if (s_acceptanceEnemies[id].HasTarget) s_acceptanceAlerts.Add(id);
            if (s_smokeStep == 40 && s_acceptance.legs[s_acceptanceIndex].requireAlert && s_acceptanceAlerts.Count > 0)
            {
                s_smoke.traversal.reachedPositions.Add(s_smokePlayer.transform.position);
                s_traversalPoints = s_smoke.traversal.reachedPositions.ToArray();
                s_traversalIndex = s_traversalPoints.Length;
                s_smoke.traversal.requestedWaypoints = s_smoke.traversal.reachedWaypoints = s_traversalIndex;
                HoldAcceptanceLeg();
            }
        }

        private static void HoldAcceptanceLeg()
        {
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            AcceptanceLeg leg = s_acceptance.legs[s_acceptanceIndex];
            float hold = leg.holdSeconds;
            if (s_acceptanceNpc != null) hold = 0;
            else if (leg.requireAlert) hold = Mathf.Min(0.5f, hold);
            s_acceptanceHoldUntil = Time.time + hold;
            s_acceptanceHoldHealth = s_smokePlayer.CurrentHealth;
            s_acceptanceProbeTime = 0;
            s_smokeStep = 41;
        }

        private static void TickAcceptanceHold()
        {
            ObserveAcceptance();
            AcceptanceLeg leg = s_acceptance.legs[s_acceptanceIndex];
            if (leg.requireCalm && Time.time >= s_acceptanceProbeTime)
            {
                s_acceptanceProbeTime = Time.time + 0.2f;
                foreach (ProjectileRuntime projectile in UnityEngine.Object.FindObjectsByType<ProjectileRuntime>(FindObjectsSortMode.None))
                    if (projectile.IsActive && projectile.Owner is MonsterActor
                        && Vector3.Distance(projectile.transform.position, s_smokePlayer.transform.position) < 5)
                        throw new InvalidOperationException("생활권 대기 지점에 적 투사체 접근: " + leg.name);
                if (s_smokePlayer.CurrentHealth < s_acceptanceHoldHealth - 0.01f)
                    throw new InvalidOperationException("생활권 대기 중 피격: " + leg.name);
            }
            if (Time.time < s_acceptanceHoldUntil) return;
            if (s_smokeStep == 42)
            {
                if (!Svc.Dialogue.IsDialogueActive) throw new InvalidOperationException("NPC 대화 시작 실패");
                if (!string.IsNullOrEmpty(leg.expectedSpeaker))
                {
                    var dialogue = DialogueManager.Instance;
                    Sprite portrait = dialogue.PortraitTable?.GetPortrait(leg.expectedSpeaker);
                    var history = dialogue.History;
                    if (portrait == null || history.Count == 0
                        || history[history.Count - 1].SpeakerName != leg.expectedSpeaker
                        || history[history.Count - 1].Portrait != portrait)
                        throw new InvalidOperationException("주민 화자·초상화·대화 이력 불일치");
                    bool hasVisiblePortrait = false;
                    foreach (var uiImage in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None))
                        if (uiImage.isActiveAndEnabled && uiImage.sprite == portrait) hasVisiblePortrait = true;
                    if (!hasVisiblePortrait) throw new InvalidOperationException("대화 화면 초상화 누락");
                    s_smoke.acceptance[s_smoke.acceptance.Count - 1].portraitAndHistoryMatched = true;
                }
                CaptureAcceptanceView(leg.name + "_Dialogue");
                s_smoke.acceptance[s_smoke.acceptance.Count - 1].dialogueStarted = true;
                s_smoke.acceptance[s_smoke.acceptance.Count - 1].dialogueExit = leg.dialogueExit.ToString();
                s_acceptanceDialogueStage = 0;
                s_acceptanceHoldUntil = Time.time + 1;
                s_smokeStep = 43;
                return;
            }
            if (s_smokeStep == 43)
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                // 프레임 끝 캡처가 완료된 다음에 이력 창이나 종료 입력을 처리한다.
                if (s_acceptanceDialogueStage == 0 && !string.IsNullOrEmpty(leg.expectedSpeaker)
                    && leg.dialogueExit == AcceptanceDialogueExit.Advance)
                {
                    UIManager.Instance.ShowUI(DialogueUIKeys.DialogueBacklog);
                    s_acceptanceDialogueStage = 1;
                    s_acceptanceHoldUntil = Time.time + 1;
                    return;
                }
                if (s_acceptanceDialogueStage == 1)
                {
                    var backlog = UIManager.Instance.GetUI<UPlayGround.UI.UI_Popup_DialogueBacklog>(DialogueUIKeys.DialogueBacklog);
                    Sprite portrait = DialogueManager.Instance.PortraitTable.GetPortrait(leg.expectedSpeaker);
                    bool hasPortrait = false;
                    if (backlog != null && backlog.IsVisible)
                        foreach (var uiImage in backlog.GetComponentsInChildren<UnityEngine.UI.Image>())
                            if (uiImage.isActiveAndEnabled && uiImage.sprite == portrait) hasPortrait = true;
                    if (!hasPortrait) throw new InvalidOperationException("이력 화면 주민 초상화 누락");
                    s_smoke.acceptance[s_smoke.acceptance.Count - 1].backlogPortraitMatched = true;
                    CaptureAcceptanceView(leg.name + "_Backlog");
                    s_acceptanceDialogueStage = 2;
                    s_acceptanceHoldUntil = Time.time + 1;
                    return;
                }
                if (s_acceptanceDialogueStage < 3)
                {
                    if (s_acceptanceDialogueStage == 2) UIManager.Instance.HideUI(DialogueUIKeys.DialogueBacklog);
                    if (leg.dialogueExit == AcceptanceDialogueExit.CancelInput)
                        InputSystem.QueueStateEvent(s_smokePad, new GamepadState().WithButton(GamepadButton.East));
                    else if (leg.dialogueExit == AcceptanceDialogueExit.Advance)
                        DialogueManager.Instance.Advance();
                    else
                        DialogueManager.Instance.RequestSkip();
                    s_acceptanceDialogueStage = 3;
                    s_acceptanceHoldUntil = Time.time + 1;
                    return;
                }
                if (leg.dialogueExit == AcceptanceDialogueExit.Advance && Svc.Dialogue.IsDialogueActive)
                {
                    DialogueManager.Instance.Advance();
                    s_acceptanceHoldUntil = Time.time + 1;
                    return;
                }
                if (Svc.Dialogue.IsDialogueActive || s_acceptanceNpc.IsInteracting())
                    throw new InvalidOperationException("NPC 대화 종료·조작 복귀 실패");
                s_smoke.acceptance[s_smoke.acceptance.Count - 1].dialogueEnded = true;
                AdvanceAcceptance();
                return;
            }
            int targets = 0;
            foreach (string id in leg.observeIds) if (s_acceptanceEnemies[id].HasTarget) targets++;
            if (leg.requireAlert && s_acceptanceAlerts.Count == 0)
                throw new InvalidOperationException("실제 감지가 발생하지 않아 유인 검증 불성립: " + leg.name + DescribeAcceptanceEnemies(leg));
            if (leg.requireCalm && targets != 0)
                throw new InvalidOperationException("귀환 이후 남아 있는 추격 대상: " + leg.name);
            s_smoke.acceptance.Add(new AcceptanceResult { name = leg.name, placedAtStart = leg.placeAtStart,
                reached = s_traversalIndex, requested = s_traversalPoints.Length,
                alertedIds = new List<string>(s_acceptanceAlerts).ToArray(), remainingTargets = targets,
                healthBeforeHold = s_acceptanceHoldHealth, healthAfterHold = s_smokePlayer.CurrentHealth,
                position = s_smokePlayer.transform.position });
            CaptureAcceptanceView(leg.name);
            if (s_acceptanceNpc != null)
            {
                if (Vector3.Distance(s_smokePlayer.transform.position, s_acceptanceNpc.transform.position) > 3
                    || !s_acceptanceNpc.CanInteract()) throw new InvalidOperationException($"NPC 접근·상호작용 불가: 거리={Vector3.Distance(s_smokePlayer.transform.position, s_acceptanceNpc.transform.position)}, 가능={s_acceptanceNpc.CanInteract()}");
                s_acceptanceNpc.Interact(s_smokePlayer);
                s_smokeStep = 42;
                s_acceptanceHoldUntil = Time.time + 2;
                return;
            }
            AdvanceAcceptance();
        }

        private static void CaptureAcceptanceView(string name)
        {
            CaptureRuntimeView();
            File.Copy(TraversalDirectory + "/Traversal.png", TraversalDirectory + "/Acceptance_" + name + ".png", true);
            CaptureAcceptanceCanvas(name);
        }

        private static void CaptureAcceptanceCanvas(string name)
        {
            // 배치 모드는 프레임 끝 스크린샷을 만들지 않아 Overlay Canvas를 촬영 중에만 카메라에 연결한다.
            var restoredCanvases = new List<(Canvas canvas, Camera camera, float distance)>();
            Camera camera = Camera.main;
            try
            {
                foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    restoredCanvases.Add((canvas, canvas.worldCamera, canvas.planeDistance));
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = camera.nearClipPlane + 0.1f;
                }
                Canvas.ForceUpdateCanvases();
                CaptureRuntimeView();
                File.Copy(TraversalDirectory + "/Traversal.png", TraversalDirectory + "/Acceptance_" + name + "_Canvas.png", true);
            }
            finally
            {
                foreach (var saved in restoredCanvases)
                {
                    saved.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    saved.canvas.worldCamera = saved.camera;
                    saved.canvas.planeDistance = saved.distance;
                }
                Canvas.ForceUpdateCanvases();
            }
        }


        private static string DescribeAcceptanceEnemies(AcceptanceLeg leg)
        {
            var result = new System.Text.StringBuilder();
            foreach (string id in leg.observeIds)
            {
                EnemyDetection enemy = s_acceptanceEnemies[id];
                result.Append($" / {id}: 위치={enemy.transform.position}, 활성={enemy.isActiveAndEnabled}, 반경={enemy.DetectionRadius}, 방향={enemy.transform.eulerAngles}");
            }
            return result.ToString();
        }

        private static void AdvanceAcceptance()
        {
            File.WriteAllText(TraversalDirectory + "/Acceptance.progress.json", JsonUtility.ToJson(s_smoke, true));
            if (++s_acceptanceIndex < s_acceptance.legs.Length) BeginAcceptanceLeg();
            else { s_smoke.traversal.roundTripCompleted = true; FinishSmoke(); }
        }
    }
}
