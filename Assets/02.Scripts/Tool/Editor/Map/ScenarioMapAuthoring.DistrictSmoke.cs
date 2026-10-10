using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        [Serializable] private sealed class DistrictWalkResult
        {
            public string route;
            public float distance;
            public bool grounded;
        }

        private static Route[] s_districtWalks;
        private static int s_districtWalkIndex;
        private static Terrain s_districtTerrain;

        private static bool BeginDistrictSmoke(double now)
        {
            if (s_smoke.scene == SourceScenePath) return false;
            s_districtWalks = ReadLayout().district?.walkChecks;
            if (s_districtWalks == null || s_districtWalks.Length == 0) return false;
            s_districtWalkIndex = 0;
            s_districtTerrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            s_smokeStep = 30;
            s_smokeNextStep = now;
            return true;
        }

        private static void TickDistrictSmoke(double now)
        {
            Route route = s_districtWalks[s_districtWalkIndex];
            if (s_smokeStep == 30)
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                Vector3 point = GroundPoint(route.points[0], s_districtTerrain);
                var motor = s_smokePlayer.ActorController.Motor;
                motor.BaseVelocity = Vector3.zero;
                motor.SetPositionAndRotation(point + Vector3.up * 0.2f, Quaternion.LookRotation(route.points[1] - route.points[0]));
                s_smokeStep = 31;
                s_smokeNextStep = now + 1.5;
                return;
            }
            if (s_smokeStep == 31)
            {
                s_walkStart = s_smokePlayer.transform.position;
                Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                Vector3 direction = Vector3.ProjectOnPlane(route.points[1] - route.points[0], Vector3.up).normalized;
                Vector2 stick = new(Vector3.Dot(direction, right), Vector3.Dot(direction, forward));
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState { leftStick = stick });
                s_smokeStep = 32;
                s_smokeNextStep = now + 1.5;
                return;
            }
            Vector3 movement = s_smokePlayer.transform.position - s_walkStart;
            movement.y = 0;
            var result = new DistrictWalkResult
            {
                route = route.name, distance = movement.magnitude,
                grounded = s_smokePlayer.ActorController.Motor.GroundingStatus.IsStableOnGround
            };
            s_smoke.districtWalks.Add(result);
            if (result.distance < 1 || !result.grounded)
                s_smoke.errors.Add("확장 구역 실제 보행 실패: " + route.name);
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            s_districtWalkIndex++;
            if (s_districtWalkIndex == s_districtWalks.Length)
            {
                FinishSmoke();
                return;
            }
            s_smokeStep = 30;
            s_smokeNextStep = now;
        }
    }
}
