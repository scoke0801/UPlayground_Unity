using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Manager;

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>종료 중 틱 서비스가 없어도 펫 해제가 다른 매니저 정리를 막지 않게 검사한다.</summary>
    public sealed class PetShutdownPlayModeTests
    {
        [UnityTest]
        public IEnumerator 종료_중_틱_싱글톤_없이_펫을_해제하고_반복_정리할_수_있다()
        {
            var lifecycle = typeof(IManager).Assembly.GetType("UPlayGround.Manager.ManagerLifecycle", true);
            FieldInfo quitting = lifecycle.GetField("ApplicationIsQuitting", BindingFlags.Public | BindingFlags.Static);
            bool previous = (bool)quitting.GetValue(null);
            var root = new GameObject("펫 종료 수명 검사");
            root.SetActive(false);
            var actorObject = new GameObject("종료할 펫");
            actorObject.transform.SetParent(root.transform, false);
            var actor = actorObject.AddComponent<PetActor>();
            Type managerType = Type.GetType("UPlayGround.Manager.PetManager, Assembly-CSharp", true);
            var manager = (IManager)root.AddComponent(managerType);
            FieldInfo actorField = managerType.GetField("_actor", BindingFlags.NonPublic | BindingFlags.Instance);
            actorField.SetValue(manager, actor);
            int tickManagers = UnityEngine.Object.FindObjectsByType<AgentTickManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            try
            {
                // 실제 OnApplicationQuit과 같은 접근자 경계를 짧게 재현한 뒤 즉시 복구한다.
                quitting.SetValue(null, true);
                try
                {
                    Assert.That(AgentTickManager.Instance, Is.Null);
                    Assert.DoesNotThrow(manager.Dispose);
                    Assert.That(actorField.GetValue(manager), Is.Null);
                    Assert.DoesNotThrow(manager.Dispose);
                }
                finally { quitting.SetValue(null, previous); }

                yield return null;
                Assert.That(actor == null, Is.True, "펫 파괴 예약이 완료돼야 한다.");
                Assert.That(UnityEngine.Object.FindObjectsByType<AgentTickManager>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(tickManagers),
                    "종료 정리 중 새 틱 싱글톤을 만들면 안 된다.");
            }
            finally
            {
                quitting.SetValue(null, previous);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
