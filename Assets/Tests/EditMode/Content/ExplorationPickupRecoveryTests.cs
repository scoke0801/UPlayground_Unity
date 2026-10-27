using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.Content.Tests
{
    /// <summary>같은 씬에서 획득 전 상태를 불러올 때 탐험 보상이 사라진 채 남는 회귀를 검사한다.</summary>
    public sealed class ExplorationPickupRecoveryTests
    {
        private static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);

        [Test]
        public void 소비기록이_되돌아오면_배치_아이템도_복구된다()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var ownership = FindType("UPlayGround.Manager.World.RegionOwnership");
            try
            {
                ownership.GetMethod("Register").Invoke(null, new object[] { scene, "ExplorationRecoveryTest" });
                var worldType = FindType("UPlayGround.Manager.WorldStateManager");
                var persistenceType = FindType("UPlayGround.Manager.InteractionRespawnManager");
                var world = CreateObject(scene, "월드 상태").AddComponent(worldType);
                var persistence = CreateObject(scene, "소비 복구").AddComponent(persistenceType);
                var item = CreateObject(scene, "보상");
                item.AddComponent<SphereCollider>();
                var actorType = FindType("UPlayGround.DropItemActor");
                var actor = item.AddComponent(actorType);
                var idType = FindType("UPlayGround.Components.SceneEntityId");
                var id = item.AddComponent(idType);
                const string guid = "exploration-recovery-test";
                idType.GetMethod("EditorSetGuid").Invoke(id, new object[] { guid });
                // 프리뷰 씬은 일반 FindObjects에 나타나지 않아 실제 등록 데이터만 주입한다.
                var placementType = persistenceType.GetNestedType("PlacementInfo", BindingFlags.NonPublic);
                var placement = Activator.CreateInstance(placementType, true);
                placementType.GetField("mapId").SetValue(placement, "ExplorationRecoveryTest");
                placementType.GetField("guid").SetValue(placement, guid);
                placementType.GetField("dropItemActor").SetValue(placement, actor);
                var placements = (System.Collections.IDictionary)persistenceType.GetField("_placements", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(persistence);
                placements.Add(("ExplorationRecoveryTest", guid), placement);
                // Instance가 프리뷰 객체를 찾을 수 없으므로 테스트 수명 안에서만 연결한다.
                var singleton = worldType.BaseType.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
                var previous = singleton.GetValue(null);
                try
                {
                    singleton.SetValue(null, world);
                    worldType.GetMethod("RecordConsumedInteractable").Invoke(world, new object[] { "ExplorationRecoveryTest", guid, false });
                    persistenceType.GetMethod("ApplyConsumedStatesToScene").Invoke(persistence, null);
                    Assert.That(item.activeSelf, Is.False);
                    worldType.GetMethod("ClearConsumedInteractables").Invoke(world, new object[] { "ExplorationRecoveryTest" });
                    persistenceType.GetMethod("ApplyConsumedStatesToScene").Invoke(persistence, null);
                    Assert.That(item.activeSelf, Is.True);
                    Assert.That((bool)actorType.GetField("_isConsumed", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(actor), Is.False);
                    persistenceType.GetMethod("ApplyConsumedStatesToScene").Invoke(persistence, null);
                    Assert.That(item.activeSelf, Is.True, "같은 상태를 다시 적용해도 보상이 사라지지 않아야 한다.");
                }
                finally { singleton.SetValue(null, previous); }
            }
            finally
            {
                ownership.GetMethod("Unregister").Invoke(null, new object[] { scene });
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static GameObject CreateObject(Scene scene, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }
    }
}
