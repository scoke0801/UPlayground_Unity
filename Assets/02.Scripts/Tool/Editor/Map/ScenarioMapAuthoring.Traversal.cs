using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string TraversalRequestedKey = "UPlayGround.ScenarioMap.TraversalRequested";
        private const string TraversalOutputKey = "UPlayGround.ScenarioMap.TraversalOutput";
        private static string TraversalDirectory => SessionState.GetString(TraversalOutputKey, "output/ScenarioGameplay/P02");
        private static bool IsTraversalSmoke => SessionState.GetBool(TraversalRequestedKey, false);
        private static Vector3[] s_traversalPoints;
        private static Camera s_traversalCamera;
        private static int s_traversalIndex;
        private static int s_traversalFrame;
        private static float s_traversalProgressTime;
        private static float s_traversalGroundTime;
        private static float s_traversalBestDistance;

        [Serializable] private sealed class TraversalReport
        {
            public string route;
            public int requestedWaypoints;
            public int reachedWaypoints;
            public bool roundTripCompleted;
            public Vector3 lastPosition;
            public List<Vector3> reachedPositions = new();
        }

        [Serializable] private sealed class TraversalSurvey
        {
            public List<TraversalPoint> points = new();
            public List<TraversalObject> objects = new();
        }

        [Serializable] private sealed class TraversalPoint
        {
            public string name;
            public Vector3 ground;
            public bool hasNavigation;
            public float slope;
        }

        [Serializable] private sealed class TraversalObject
        {
            public string path;
            public string sceneEntityId;
            public string globalObjectId;
            public Vector3 position;
            public Vector3 rotation;
            public Bounds bounds;
        }

        /// <summary>배치된 작업 전면·공터·주 동선의 실제 높이와 저장 식별자를 저장 없이 조사한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 P02 동선 조사")]
        public static void InspectScenarioTraversal()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            Layout layout = ReadLayout();
            Scene scene = EditorSceneManager.OpenScene(layout.scenePath);
            Transform environment = FindQualityTransform(scene, "AuthoredEnvironment");
            Terrain terrain = environment.GetComponentInChildren<Terrain>();
            var survey = new TraversalSurvey();
            foreach (Clearing clearing in layout.clearings)
                survey.points.Add(SurveyTraversalPoint(clearing.name, clearing.center, terrain));
            string probesPath = TraversalDirectory + "/ProbePoints.json";
            if (File.Exists(probesPath))
                foreach (TraversalPoint probe in JsonUtility.FromJson<TraversalSurvey>(File.ReadAllText(probesPath)).points)
                    survey.points.Add(SurveyTraversalPoint(probe.name, probe.ground, terrain));
            Route route = layout.routes[0];
            for (int index = 0; index < route.points.Length; index++)
                survey.points.Add(SurveyTraversalPoint(route.name + "/" + index, route.points[index], terrain));
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (SceneEntityId identity in root.GetComponentsInChildren<SceneEntityId>(true))
                survey.objects.Add(SurveyTraversalObject(identity.transform, identity.Guid));
            foreach (Transform prop in environment.Find("LandscapeDistrict"))
                survey.objects.Add(SurveyTraversalObject(prop, ""));
            foreach (Transform node in environment.GetComponentsInChildren<Transform>())
                if (node.name.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0)
                    survey.objects.Add(SurveyTraversalObject(node, ""));
            Directory.CreateDirectory(TraversalDirectory);
            File.WriteAllText(TraversalDirectory + "/Survey.json", JsonUtility.ToJson(survey, true));
        }

        private static TraversalPoint SurveyTraversalPoint(string name, Vector3 position, Terrain terrain)
        {
            Vector3 point = GroundPoint(position, terrain);
            Vector3 local = point - terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            return new TraversalPoint { name = name, ground = point,
                hasNavigation = NavMesh.SamplePosition(point, out _, 1.5f, NavMesh.AllAreas),
                slope = terrain.terrainData.GetSteepness(local.x / size.x, local.z / size.z) };
        }

        private static TraversalObject SurveyTraversalObject(Transform node, string identity)
        {
            return new TraversalObject { path = GetTransformPath(node), sceneEntityId = identity,
                globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(node.gameObject).ToString(),
                position = node.position, rotation = node.eulerAngles,
                bounds = GetRendererBounds(node.GetComponentsInChildren<Renderer>()) };
        }

        /// <summary>첫 저작 경로를 실제 게임패드 입력과 KCC로 왕복하고 도달 지점을 기록한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 P02 왕복 보행 검증")]
        public static void RunScenarioTraversalSmoke()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            Layout layout = ReadLayout();
            SessionState.EraseString(TraversalOutputKey);
            string[] outputArgs = Environment.GetCommandLineArgs();
            for (int index = 0; index + 1 < outputArgs.Length; index++)
                if (outputArgs[index] == "-scenarioTraversalOutput")
                    SessionState.SetString(TraversalOutputKey, Path.GetFullPath(outputArgs[index + 1]));
            InspectScenarioTraversal();
            Validate();
            Directory.CreateDirectory(TraversalDirectory);
            SessionState.EraseString(AcceptancePlanKey);
            string[] args = Environment.GetCommandLineArgs();
            for (int index = 0; index < args.Length - 1; index++)
                if (args[index] == "-scenarioTraversalPlan")
                    SessionState.SetString(AcceptancePlanKey, Path.GetFullPath(args[index + 1]));
            SessionState.SetBool(TraversalRequestedKey, true);
            SessionState.SetString(BaselineCharacterKey, layout.startingCharacter);
            StartSmoke(layout.scenePath);
        }

        private static void RequireSavedTraversalScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("미저장 씬이 있어 동선 검증을 중단합니다.");
        }

        private static void BeginTraversalSmoke(double now)
        {
            if (SessionState.GetString(AcceptancePlanKey, "") != "")
            {
                BeginAcceptance(now);
                return;
            }
            Route route = ReadLayout().routes[0];
            if (route.points == null || route.points.Length < 2)
                throw new InvalidOperationException("왕복 보행 경로에는 두 개 이상의 지점이 필요합니다.");
            s_traversalPoints = new Vector3[route.points.Length * 2 - 1];
            Array.Copy(route.points, s_traversalPoints, route.points.Length);
            for (int index = 1; index < route.points.Length; index++)
                s_traversalPoints[route.points.Length - 1 + index] = route.points[route.points.Length - 1 - index];
            s_traversalCamera = Camera.main;
            if (s_traversalCamera == null) throw new InvalidOperationException("실제 플레이 카메라가 없습니다.");
            s_smoke.traversal = new TraversalReport { route = route.name, requestedWaypoints = s_traversalPoints.Length };
            s_traversalIndex = 0;
            s_traversalFrame = -1;
            s_traversalProgressTime = Time.time;
            s_traversalGroundTime = Time.time;
            s_traversalBestDistance = float.PositiveInfinity;
            s_smokeStep = 40;
            s_smokeNextStep = now;
            // 장거리 쿼리 성공과 실제 보행을 구분한다. 이동 보정·무적·적 비활성화는 하지 않는다.
            s_smokeDeadline = now + 1800;
        }

        private static float s_traversalNextGuideInput;

        private static bool TryDismissTraversalGuide()
        {
            if (UPlayGround.Manager.Svc.GameTime?.IsPaused != true
                || UnityEngine.Object.FindFirstObjectByType<UPlayGround.UI.UI_Popup_Guide>() == null) return false;
            if (Time.unscaledTime >= s_traversalNextGuideInput)
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState().WithButton(GamepadButton.East));
                s_traversalNextGuideInput = Time.unscaledTime + 0.35f;
                s_smoke.guideCancelInputs++;
            }
            else InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            return true;
        }

        private static void TickTraversalSmoke()
        {
            if (s_traversalFrame == Time.frameCount) return;
            s_traversalFrame = Time.frameCount;
            if (TryDismissTraversalGuide()) return;
            if (s_smokeStep >= 41) { TickAcceptanceHold(); return; }
            ObserveAcceptance();
            if (s_smokeStep != 40) return;
            Vector3 position = s_smokePlayer.transform.position;
            s_smoke.traversal.lastPosition = position;
            Vector3 direction = Vector3.ProjectOnPlane(s_traversalPoints[s_traversalIndex] - position, Vector3.up);
            float distance = direction.magnitude;
            if (s_smokePlayer.ActorController.Motor.GroundingStatus.IsStableOnGround)
                s_traversalGroundTime = Time.time;
            if (Time.time - s_traversalGroundTime > 2)
                throw new InvalidOperationException("왕복 경로에서 접지 상실: " + s_traversalIndex);
            if (distance < s_traversalBestDistance - 0.1f)
            {
                s_traversalBestDistance = distance;
                s_traversalProgressTime = Time.time;
            }
            if (Time.time - s_traversalProgressTime > 10)
                throw new InvalidOperationException("왕복 경로에서 10초간 전진 실패: " + s_traversalIndex + " / " + position);
            if (distance < 1.2f)
            {
                s_smoke.traversal.reachedPositions.Add(position);
                s_smoke.traversal.reachedWaypoints = ++s_traversalIndex;
                s_traversalProgressTime = Time.time;
                s_traversalBestDistance = float.PositiveInfinity;
                if (s_traversalIndex == s_traversalPoints.Length)
                {
                    if (s_acceptance != null) { HoldAcceptanceLeg(); return; }
                    s_smoke.traversal.roundTripCompleted = true;
                    CaptureRuntimeView();
                    FinishSmoke();
                    return;
                }
                direction = Vector3.ProjectOnPlane(s_traversalPoints[s_traversalIndex] - position, Vector3.up);
            }
            Vector3 forward = Vector3.ProjectOnPlane(s_traversalCamera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            direction.Normalize();
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState
                { leftStick = new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)) });
        }
    }
}
