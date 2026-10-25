using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Event;
using UPlayGround.Data.Save;
using UPlayGround.Data.UI;
using UPlayGround.FlowGraph;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>도시 길 안내의 결말 조건과 저장 후 중복 노출 방지를 검사한다.</summary>
    public sealed class CityRoadGuideTests
    {
        private const string GuideId = "Guide.CityRoadsOpened";
        private const string ConfigPath = "Assets/10.Datas/Guide/FirstTimeGuideConfig.asset";
        private const string GraphPath = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R09/FLOW_RedTreeReturn.asset";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void 결말_이벤트가_로드후_반복되어도_본_안내는_다시_쌓이지않는다(bool shown, bool pending)
        {
            Type type = Type.GetType("UPlayGround.Manager.GameGuideManager, Assembly-CSharp", true);
            var owner = new GameObject("CityRoadGuideSaveTest");
            owner.SetActive(false);
            try
            {
                var manager = owner.AddComponent(type);
                var config = AssetDatabase.LoadAssetAtPath<FirstTimeGuideConfigSO>(ConfigPath);
                type.GetField("_config", PrivateInstance).SetValue(manager, config);
                var input = new GameSaveData
                {
                    firstTimeGuide = new FirstTimeGuideSaveData
                    {
                        shownGuideIds = shown ? new List<string> { GuideId } : new List<string>(),
                        pendingGuideIds = pending ? new List<string> { GuideId, GuideId } : new List<string>()
                    }
                };
                type.GetMethod("ImportSaveData").Invoke(manager, new object[] { input });
                type.GetMethod("OnEvent").Invoke(manager, new object[] { GameMilestoneEvent.CityRoadsOpened });
                type.GetMethod("OnEvent").Invoke(manager, new object[] { GameMilestoneEvent.CityRoadsOpened });
                var output = new GameSaveData();
                type.GetMethod("ExportSaveData").Invoke(manager, new object[] { output });
                Assert.That(output.firstTimeGuide.pendingGuideIds.Count, Is.EqualTo(shown ? 0 : 1));
                Assert.That(output.firstTimeGuide.shownGuideIds.Contains(GuideId), Is.EqualTo(shown));

                type.GetMethod("ImportSaveData").Invoke(manager, new object[] { output });
                type.GetMethod("OnEvent").Invoke(manager, new object[] { GameMilestoneEvent.CityRoadsOpened });
                type.GetMethod("ExportSaveData").Invoke(manager, new object[] { output });
                Assert.That(output.firstTimeGuide.pendingGuideIds.Count, Is.EqualTo(shown ? 0 : 1));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test]
        public void 도시_길_안내는_연출뒤_이동을_요구하고_기존_자료로_방향을_알린다()
        {
            var config = AssetDatabase.LoadAssetAtPath<FirstTimeGuideConfigSO>(ConfigPath);
            Assert.IsTrue(config.TryGet(GameMilestoneEvent.CityRoadsOpened, out var entry));
            Assert.That(entry.GuideId, Is.EqualTo(GuideId));
            Assert.That(entry.TravelDistanceBeforeShow, Is.EqualTo(4f));
            Assert.That(entry.Popup.Pages.Count, Is.EqualTo(1));
            var page = entry.Popup.Pages[0];
            Assert.NotNull(page.Image, "기존 UI 팩의 나침반 참조가 필요하다.");
            StringAssert.Contains("서쪽", page.Body);
            StringAssert.Contains("남동쪽", page.Body);
            foreach (string term in new[] { "포탈", "포털", "해금", "플래그" })
                StringAssert.DoesNotContain(term, page.Title + page.Body);
        }

        [Test]
        public void 안내대기중_로딩으로_옮겨진_거리는_자유이동으로_세지않는다()
        {
            Type guideType = Type.GetType("UPlayGround.Manager.GameGuideManager, Assembly-CSharp", true);
            Type sceneType = Type.GetType("UPlayGround.Manager.SceneManager, Assembly-CSharp", true);
            var instanceField = sceneType.BaseType.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            object previousSceneManager = instanceField.GetValue(null);
            Services.TryGet<IActorQueryService>(out var previousActors);
            var owner = new GameObject("CityRoadGuideTravelTest");
            owner.SetActive(false);
            var player = new GameObject("CityRoadGuideTravelPlayer");
            var actors = new GuideActors(player.transform);
            try
            {
                var guide = owner.AddComponent(guideType);
                var scene = owner.AddComponent(sceneType);
                instanceField.SetValue(null, scene);
                var config = AssetDatabase.LoadAssetAtPath<FirstTimeGuideConfigSO>(ConfigPath);
                config.TryGet(GameMilestoneEvent.CityRoadsOpened, out var entry);
                guideType.GetField("_config", PrivateInstance).SetValue(guide, config);
                guideType.GetMethod("OnEvent").Invoke(guide, new object[] { GameMilestoneEvent.CityRoadsOpened });
                if (previousActors != null) Services.Unregister(previousActors);
                Services.Register(actors);
                var canShow = guideType.GetMethod("CanShowAfterTravel", PrivateInstance);
                Assert.IsFalse((bool)canShow.Invoke(guide, new object[] { entry }));
                player.transform.position = new Vector3(3f, 10f, 0f);
                Assert.IsFalse((bool)canShow.Invoke(guide, new object[] { entry }), "수직 이동은 수평4m 조건을 채우지 않는다.");

                sceneType.GetField("_isLoading", PrivateInstance).SetValue(scene, true);
                guideType.GetField("_presentationIdleSeconds", PrivateInstance).SetValue(guide, 1f);
                guideType.GetMethod("OnUpdate").Invoke(guide, null);
                Assert.That(guideType.GetField("_presentationIdleSeconds", PrivateInstance).GetValue(guide), Is.EqualTo(0f));
                sceneType.GetField("_isLoading", PrivateInstance).SetValue(scene, false);
                player.transform.position = new Vector3(30f, 10f, 0f);
                Assert.IsFalse((bool)canShow.Invoke(guide, new object[] { entry }), "로딩 후 새 위치에서 이동을 다시 측정한다.");
                player.transform.position += Vector3.right * 4f;
                Assert.IsTrue((bool)canShow.Invoke(guide, new object[] { entry }));
                var saved = new GameSaveData();
                guideType.GetMethod("ExportSaveData").Invoke(guide, new object[] { saved });
                CollectionAssert.AreEqual(new[] { GuideId }, saved.firstTimeGuide.pendingGuideIds);
                Assert.IsEmpty(saved.firstTimeGuide.shownGuideIds, "조건 확인만으로 안내를 본 것으로 저장하지 않는다.");
            }
            finally
            {
                Services.Unregister(actors);
                if (previousActors != null) Services.Register(previousActors);
                instanceField.SetValue(null, previousSceneManager);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void 결말_복원과_실시간_완료는_같은_호수_안내분기로_이어진다()
        {
            var graph = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(GraphPath);
            var issues = new List<string>();
            Assert.IsTrue(graph.Validate(issues), string.Join("\n", issues));
            foreach (string source in new[] { "resume", "reload", "change24" })
                Assert.IsTrue(graph.connections.Any(c => c.fromNodeId == source && c.toNodeId == "sync"));
            var flag = (CheckFlagNode)graph.GetNode("city_roads_ending");
            Assert.That(flag.flagKey, Is.EqualTo("lake.red_tree.return.common_ending"));
            Assert.IsTrue(flag.expectedValue);
            var scene = (RunnerSceneCondition)((BranchNode)graph.GetNode("city_roads_lake")).condition;
            Assert.That(scene.sceneName, Is.EqualTo("LakeOfLifeScenario"));
            var publish = (PublishGameEventNode)graph.GetNode("city_roads_publish");
            Assert.IsTrue(publish.gameEvent.TryResolve(out var eventType, out var eventValue));
            Assert.That(eventType, Is.EqualTo(typeof(GameMilestoneEvent)));
            Assert.That(eventValue, Is.EqualTo(GameMilestoneEvent.CityRoadsOpened));
            AssertConnection(graph, "sync", "3", flag.id);
            AssertConnection(graph, flag.id, FlowPort.True, "city_roads_lake");
            AssertConnection(graph, "city_roads_lake", FlowPort.True, publish.id);
        }

        private static void AssertConnection(FlowGraphSO graph, string source, string port, string target)
        {
            Assert.IsTrue(graph.connections.Any(c => c.fromNodeId == source && c.fromPort == port && c.toNodeId == target));
        }

        private sealed class GuideActors : IActorQueryService
        {
            public GuideActors(Transform player) => PlayerTransform = player;
            public IWorldActor Player => null;
            public Transform PlayerTransform { get; }
            public IEnumerable<IWorldActor> AllActors => Array.Empty<IWorldActor>();
            public IWorldActor FindActor(string actorId) => null;
        }
    }
}
