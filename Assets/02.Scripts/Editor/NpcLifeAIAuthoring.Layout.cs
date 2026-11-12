using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Components.LivingWorld;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor
{
    /// <summary>대표 주민 네 명의 작업 접점과 보호 장소·접근 경로를 저작한다.</summary>
    public static partial class NpcLifeAIAuthoring
    {
        [UPlaygroundTool("NpcLifeAI.RefineRepresentativeWorkContacts")]
        public static void RefineRepresentativeWorkContacts()
        {
            EnsureLayoutReady();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var props = scene.GetRootGameObjects().Single(g => g.name == "NpcLifeAI_Review").transform.Find("WorkContacts");
            if (props == null) throw new InvalidOperationException("작업물 연결이 선행되어야 합니다.");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("대표 작업 접점과 보행 폭 정합");
            try
            {
                var workers = UnityEngine.Object.FindObjectsByType<NpcLifeController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(l => l.gameObject.scene == scene && l.Profile != null && l.Profile.roleId.StartsWith("Worker", StringComparison.Ordinal));
                foreach (var life in workers)
                {
                    var routine = life.GetComponent<NpcRoutine>();
                    var routineData = new SerializedObject(routine);
                    var actor = (NpcActor)routineData.FindProperty("_actor").objectReferenceValue;
                    var motor = actor.GetComponent<KinematicCharacterController.KinematicCharacterMotor>();
                    var capsule = actor.GetComponent<CapsuleCollider>();
                    Undo.RecordObjects(new UnityEngine.Object[] { motor, capsule }, "대표 주민 작업 공간 정합");
                    var so = new SerializedObject(motor);
                    so.FindProperty("CapsuleRadius").floatValue = 0.2f;
                    so.FindProperty("CapsuleHeight").floatValue = 1.65f;
                    so.FindProperty("CapsuleYOffset").floatValue = 0.825f;
                    so.ApplyModifiedProperties();
                    capsule.radius = 0.2f; capsule.height = 1.65f; capsule.center = Vector3.up * 0.825f;
                    var steps = routineData.FindProperty("_steps");
                    steps.GetArrayElementAtIndex(life.Profile.roleId == "WorkerCook" ? 1 : 0)
                        .FindPropertyRelative("isContentPending").boolValue = true;
                    routineData.ApplyModifiedProperties();
                }
                var cook = UnityEngine.Object.FindObjectsByType<NpcWorkPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(p => p.gameObject.scene == scene && p.PointId == "lake.meal.prep");
                Undo.RecordObject(cook.transform, "조리 실물 작업대 접근점");
                Vector3 cookPosition = new(1031.5f, 0f, 406f); cookPosition.y = SampleGroundHeight(cookPosition) + 0.05f;
                cook.transform.position = cookPosition;
                if (props.Find("CookingTable") == null)
                    CreateFittedProp(props, "CookingTable", FarmPrefabs + "Props/Interior/Furniture/PF_Farm_WoodTable_01.prefab",
                        cook.transform.TransformPoint(new Vector3(-0.01f, 0.275f, 0.6f)), cook.transform.rotation, new Vector3(0.9f, 0.65f, 0.55f));
                MoveVisualCenter(props.Find("CookingBowl"), cook.transform.TransformPoint(new Vector3(-0.01f, 0.688f, 0.46f)));
                var point = UnityEngine.Object.FindObjectsByType<NpcWorkPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(p => p.gameObject.scene == scene && p.PointId == "lake.repair.work");
                MoveVisualCenter(props.Find("RepairTable"), point.transform.TransformPoint(new Vector3(0.23f, 0.35f, 0.43f)));
                MoveVisualCenter(props.Find("RepairTimber"), point.transform.TransformPoint(new Vector3(0.15f, 0.7275f, 0.43f)));
                ApplyRepairContactData(scene);
                // 작은 작업물은 손 도구처럼 장식이다. 이동 캡슐이 손 작업 면 때문에 뒤로 밀리지 않게 한다.
                foreach (var collider in props.Find("RepairTimber").GetComponentsInChildren<Collider>(true))
                { Undo.RecordObject(collider, "장식 작업물 충돌"); collider.enabled = false; }
                var marketVisit = UnityEngine.Object.FindObjectsByType<NpcWorkPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(p => p.gameObject.scene == scene && p.PointId == "life.visit.ResidentMarket");
                Undo.RecordObject(marketVisit.transform, "나무 충돌을 피한 방문점");
                Vector3 visitPosition = new(983f, 0f, 420f); visitPosition.y = SampleGroundHeight(visitPosition) + 0.05f;
                marketVisit.transform.position = visitPosition;
                var social = FindSocialPoint(scene);
                var socialData = new SerializedObject(social);
                for (int i = 0; i < 2; i++)
                {
                    var seat = (NpcWorkPoint)socialData.FindProperty(i == 0 ? "_first" : "_second").FindPropertyRelative("point").objectReferenceValue;
                    Undo.RecordObject(seat.transform, "교류 자리 충돌 정합");
                    Vector3 seatPosition = new(982f + i * 1.5f, 0f, 420.5f); seatPosition.y = SampleGroundHeight(seatPosition) + 0.05f;
                    seat.transform.position = seatPosition;
                }
                Physics.SyncTransforms(); Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        [UPlaygroundTool("NpcLifeAI.PrepareRepresentativeShelters")]
        public static void PrepareRepresentativeShelters()
        {
            EnsureLayoutReady();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var root = scene.GetRootGameObjects().Single(g => g.name == "NpcLifeAI_Review");
            if (root.transform.Find("Shelters") != null) return;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("대표 주민 실제 지붕과 출입 동선");
            try
            {
                var shelters = CreateChild(root.transform, "Shelters").transform;
                var routes = CreateChild(root.transform, "Routes").transform;
                var lives = UnityEngine.Object.FindObjectsByType<NpcLifeController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(l => l.gameObject.scene == scene && l.Profile != null).ToArray();
                foreach (var life in lives) PrepareShelterAndRoute(life, shelters, routes);
                ConfigureSocialRoutes(root, routes);
                Physics.SyncTransforms(); Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static void PrepareShelterAndRoute(NpcLifeController life, Transform shelters, Transform routes)
        {
            var routine = life.GetComponent<NpcRoutine>(); var so = new SerializedObject(routine);
            var rest = (NpcWorkPoint)so.FindProperty("_rest").FindPropertyRelative("point").objectReferenceValue;
            if (rest == null) throw new InvalidOperationException("휴식점이 없는 생활 바인딩입니다.");
            var roof = CreateFittedProp(shelters, life.Profile.roleId + "_Roof", FarmPrefabs + "BuildingModules/Roofs/PF_Farm_Roof_200x400x120_01.prefab",
                rest.transform.position + Vector3.up * 2.525f, Quaternion.identity, new Vector3(2.6f, 0.65f, 2.2f));
            Physics.SyncTransforms();
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    CreateShelterPillar(shelters, roof, rest.transform.position + new Vector3(x * 1.05f, 0f, z * 0.8f));
            var roofColliders = roof.GetComponentsInChildren<Collider>(true);
            if (!roofColliders.Any(c => c.Raycast(new Ray(rest.transform.position + Vector3.up * 1.8f, Vector3.up), out _, 3f)))
                throw new InvalidOperationException("보호점 위 실제 지붕을 확인하지 못했습니다.");
            Set(rest, "_isSheltered", true);
            ConfigureLifeRoutes(life, routes);
        }

        private static void ConfigureLifeRoutes(NpcLifeController life, Transform routes)
        {
            var routine = life.GetComponent<NpcRoutine>(); var so = new SerializedObject(routine);
            var rest = (NpcWorkPoint)so.FindProperty("_rest").FindPropertyRelative("point").objectReferenceValue;
            var route = CreateChild(routes, life.Profile.roleId).transform;
            Vector3 entry = rest.transform.position + Vector3.forward * 2f;
            if (life.Profile.roleId == "WorkerCook")
            {
                ConfigureRoute(routine, "_steps.Array.data[0]", route, new[] { entry, new Vector3(1033f, 0f, 405.2f), new Vector3(1031.2f, 0f, 405.2f) });
                ConfigureRoute(routine, "_rest", route, new[] { new Vector3(1031.2f, 0f, 405.2f), new Vector3(1033f, 0f, 405.2f), entry });
            }
            else if (life.Profile.roleId == "WorkerRepair")
            {
                ConfigureRoute(routine, "_steps.Array.data[0]", route, new[] { entry, new Vector3(985.6f, 0f, 431f) });
                ConfigureRoute(routine, "_steps.Array.data[1]", route, new[] { new Vector3(985.6f, 0f, 431f), new Vector3(988.7f, 0f, 431f) });
                ConfigureRoute(routine, "_rest", route, new[] { new Vector3(988.7f, 0f, 431f), entry });
            }
            else
            {
                var visit = (NpcWorkPoint)so.FindProperty("_steps").GetArrayElementAtIndex(0).FindPropertyRelative("point").objectReferenceValue;
                Vector3 corner = new(visit.transform.position.x, 0f, entry.z);
                ConfigureRoute(routine, "_steps.Array.data[0]", route, new[] { entry, corner });
                ConfigureRoute(routine, "_rest", route, new[] { corner, entry });
            }
        }

        private static void CreateShelterPillar(Transform parent, GameObject roof, Vector3 position)
        {
            position.y = SampleGroundHeight(position);
            var ray = new Ray(position + Vector3.up * 5f, Vector3.down);
            RaycastHit hit = default;
            bool found = false;
            foreach (var collider in roof.GetComponentsInChildren<Collider>(true))
                if (collider.Raycast(ray, out hit, 6f)) { found = true; break; }
            if (!found) throw new InvalidOperationException("지붕 지지 기둥의 실제 접점을 찾지 못했습니다.");
            float height = hit.point.y - position.y - 0.05f;
            CreateFittedProp(parent, "ShelterSupport", FarmPrefabs + "BuildingModules/Decoration/PF_Farm_StonePillar_01.prefab",
                position + Vector3.up * height * 0.5f, Quaternion.identity, new Vector3(0.18f, height, 0.22f));
        }

        [UPlaygroundTool("NpcLifeAI.RefreshRepresentativeRoutes")]
        public static void RefreshRepresentativeRoutes()
        {
            EnsureLayoutReady();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var root = scene.GetRootGameObjects().Single(g => g.name == "NpcLifeAI_Review");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("대표 주민 경로 갱신");
            try
            {
                var routes = CreateChild(root.transform, "Routes_Staging").transform;
                foreach (var life in UnityEngine.Object.FindObjectsByType<NpcLifeController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(l => l.gameObject.scene == scene && l.Profile != null)) ConfigureLifeRoutes(life, routes);
                ConfigureSocialRoutes(root, routes);
                var previous = root.transform.Find("Routes"); if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
                routes.name = "Routes";
                Physics.SyncTransforms(); Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static void ConfigureSocialRoutes(GameObject root, Transform routes)
        {
            var social = root.GetComponentInChildren<NpcSocialPoint>();
            var so = new SerializedObject(social); var participants = so.FindProperty("_participants");
            for (int i = 0; i < participants.arraySize; i++)
            {
                var life = (NpcLifeController)participants.GetArrayElementAtIndex(i).objectReferenceValue;
                var rest = (NpcWorkPoint)new SerializedObject(life.GetComponent<NpcRoutine>()).FindProperty("_rest").FindPropertyRelative("point").objectReferenceValue;
                string path = i == 0 ? "_first" : "_second";
                var seat = (NpcWorkPoint)so.FindProperty(path).FindPropertyRelative("point").objectReferenceValue;
                Vector3 entry = rest.transform.position + Vector3.forward * 2f;
                ConfigureRoute(social, path, routes, new[] { entry, new Vector3(entry.x, 0f, 420.5f) });
            }
        }

        private static void ConfigureRoute(UnityEngine.Object owner, string path, Transform parent, Vector3[] positions)
        {
            Undo.RecordObject(owner, "주민 접근 경로"); var so = new SerializedObject(owner);
            var waypoints = so.FindProperty(path).FindPropertyRelative("waypoints"); waypoints.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                var point = CreateChild(parent, "Approach").transform;
                Vector3 position = positions[i]; position.y = SampleGroundHeight(position) + 0.05f; point.position = position;
                waypoints.GetArrayElementAtIndex(i).objectReferenceValue = point;
            }
            so.ApplyModifiedProperties();
        }

        private static float SampleGroundHeight(Vector3 position)
        {
            var hit = Physics.RaycastAll(new Vector3(position.x, 200f, position.z), Vector3.down, 400f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider is TerrainCollider).OrderByDescending(h => h.point.y).FirstOrDefault();
            if (hit.collider == null) throw new InvalidOperationException("실제 Terrain 지면이 없는 접근점입니다.");
            return hit.point.y;
        }

        private static void MoveVisualCenter(Transform target, Vector3 center)
        { Undo.RecordObject(target, "작업물 접점 보정"); target.position += center - GetVisualBounds(target.gameObject).center; }

        private static void EnsureLayoutReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("편집 모드의 컴파일 완료 후 실행해야 합니다.");
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Any(g => g.name == "NpcLifeAI_Review"))
                throw new InvalidOperationException("대표 생활 바인딩이 있는 씬을 열어야 합니다.");
        }

        private static NpcSocialPoint FindSocialPoint(UnityEngine.SceneManagement.Scene scene)
            => scene.GetRootGameObjects().Single(g => g.name == "NpcLifeAI_Review").GetComponentInChildren<NpcSocialPoint>();

        /// <summary>실제 배치에서 대표 주민의 모든 활동 간 합류와 지붕·모션 참조를 검사한다.</summary>
        [UPlaygroundTool("NpcLifeAI.ValidateRepresentativeLayout")]
        public static void ValidateRepresentativeLayout()
        {
            EnsureLayoutReady();
            var rows = new System.Collections.Generic.List<string>();
            int failures = 0, residents = 0;
            foreach (var life in UnityEngine.Object.FindObjectsByType<NpcLifeController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (life.Profile == null) { rows.Add("FAIL profile " + life.name); failures++; continue; }
                residents++;
                var routine = life.GetComponent<NpcRoutine>(); var so = new SerializedObject(routine);
                float radius = life.Profile.roleId.StartsWith("Worker", StringComparison.Ordinal) ? 0.2f : 0.5f;
                float height = radius < 0.5f ? 1.65f : 2f;
                var steps = so.FindProperty("_steps");
                var activities = new System.Collections.Generic.List<SerializedProperty> { so.FindProperty("_rest") };
                for (int i = 0; i < steps.arraySize; i++)
                    if (!steps.GetArrayElementAtIndex(i).FindPropertyRelative("isContentPending").boolValue)
                        activities.Add(steps.GetArrayElementAtIndex(i));
                var socialData = new SerializedObject(FindSocialPoint(life.gameObject.scene));
                var participants = socialData.FindProperty("_participants");
                for (int i = 0; i < participants.arraySize; i++)
                    if (participants.GetArrayElementAtIndex(i).objectReferenceValue == life)
                        activities.Add(socialData.FindProperty(i == 0 ? "_first" : "_second"));
                var origins = activities.Select(a => ((NpcWorkPoint)a.FindPropertyRelative("point").objectReferenceValue).transform.position).ToList();
                var actor = so.FindProperty("_actor").objectReferenceValue as NpcActor;
                if (actor != null) origins.Add(actor.transform.position);
                foreach (var activity in activities)
                {
                    var point = (NpcWorkPoint)activity.FindPropertyRelative("point").objectReferenceValue;
                    if (point.Motion == null || point.Motion.motionSet.motions.Any(m => m == null || m.motionClip == null))
                    { rows.Add("FAIL motion " + point.PointId); failures++; }
                    var path = activity.FindPropertyRelative("waypoints");
                    var positions = new System.Collections.Generic.List<Vector3>();
                    for (int i = 0; i < path.arraySize; i++) positions.Add(((Transform)path.GetArrayElementAtIndex(i).objectReferenceValue).position);
                    positions.Add(point.transform.position);
                    foreach (var origin in origins)
                    {
                        int join = positions.Count - 1;
                        while (join >= 0 && !IsLayoutSegmentClear(origin, positions[join], radius, height, out _)) join--;
                        bool valid = join >= 0;
                        string obstacle = "no join";
                        for (int i = join; valid && i < positions.Count - 1; i++)
                            valid = IsLayoutSegmentClear(positions[i], positions[i + 1], radius, height, out obstacle);
                        rows.Add((valid ? "PASS " : "FAIL ") + life.Profile.roleId + " " + origin.ToString("F2")
                            + " -> " + point.PointId + " join=" + join + (valid ? "" : " obstacle=" + obstacle));
                        if (!valid) failures++;
                    }
                }
                var rest = (NpcWorkPoint)so.FindProperty("_rest").FindPropertyRelative("point").objectReferenceValue;
                bool covered = rest.IsSheltered && Physics.Raycast(rest.transform.position + Vector3.up * height, Vector3.up, 3f, ~0, QueryTriggerInteraction.Ignore);
                rows.Add((covered ? "PASS " : "FAIL ") + "roof " + life.Profile.roleId); if (!covered) failures++;
            }
            System.IO.Directory.CreateDirectory("output/NpcLifeAI");
            if (residents != 4) { rows.Add("FAIL representative count=" + residents); failures++; }
            rows.Add("failures=" + failures); System.IO.File.WriteAllLines("output/NpcLifeAI/representative-layout-paths.txt", rows);
            if (failures > 0) throw new InvalidOperationException("대표 배치 검사 실패 " + failures + "건. 경로/작업 승인 게이트를 열지 않습니다.");
        }

        /// <summary>원본 시나리오를 바꾸지 않고 생성 월드의 기존 수선점에 검수용 접촉 데이터를 복원한다.</summary>
        internal static void ApplyRepairContactData(UnityEngine.SceneManagement.Scene scene)
        {
            var points = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NpcWorkPoint>(true));
            var point = points.SingleOrDefault(p => p.PointId == "lake.repair.work");
            if (point == null) throw new InvalidOperationException("기존 수선 작업점이 없습니다.");
            var motion = LoadMotion("LivingWorld/LW_Repair_P09Contact");
            var end = LoadMotion("LivingWorld/LW_RepairEnd_P09Contact");
            var hammer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/LivingWorld/Props/LW_Hammer_P09Contact.prefab");
            var workstation = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/LivingWorld/NPC/P09RepairWorkstation.prefab");
            if (hammer == null || workstation == null) throw new InvalidOperationException("수선 접촉 보정 도구·작업대가 없습니다.");

            Set(point, "_motion", motion);
            Set(point, "_heldPropPrefab", hammer);
            foreach (var routine in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NpcRoutine>(true)))
            {
                Undo.RecordObject(routine, "수선 종료 모션 연결");
                var data = new SerializedObject(routine);
                var steps = data.FindProperty("_steps");
                for (int i = 0; i < steps.arraySize; i++)
                {
                    var step = steps.GetArrayElementAtIndex(i);
                    if (step.FindPropertyRelative("point").objectReferenceValue == point)
                        step.FindPropertyRelative("endMotion").objectReferenceValue = end;
                }
                data.ApplyModifiedProperties();
            }

            var review = scene.GetRootGameObjects().SingleOrDefault(g => g.name == "NpcLifeAI_Review");
            var timber = review != null ? review.transform.Find("WorkContacts/RepairTimber") : null;
            if (timber != null)
            {
                MoveVisualCenter(timber, point.transform.TransformPoint(new Vector3(0.15f, 0.7275f, 0.43f)));
                return;
            }
            if (point.transform.Find(workstation.name) != null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(workstation, scene);
            Undo.RegisterCreatedObjectUndo(instance, "수선 작업대 복원");
            instance.transform.SetParent(point.transform, false);
        }

        private static bool IsLayoutSegmentClear(Vector3 from, Vector3 to, float radius, float height, out string obstacle)
        {
            obstacle = ""; Vector3 offset = to - from;
            if (Mathf.Abs(offset.y) > 0.5f) { obstacle = "height"; return false; }
            if (offset.sqrMagnitude <= 0.0036f) return true;
            foreach (var hit in Physics.CapsuleCastAll(from + Vector3.up * radius,
                from + Vector3.up * (height - radius), radius, offset.normalized, offset.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<GameActor>() != null) continue;
                if (hit.collider is TerrainCollider && hit.normal.y >= 0.5f) continue;
                obstacle = hit.collider.name; return false;
            }
            return true;
        }
    }
}
