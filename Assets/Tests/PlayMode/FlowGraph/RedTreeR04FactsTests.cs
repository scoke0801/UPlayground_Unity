#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Event;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    /// <summary>본편 그래프의 선행 관찰 누적·영입 분리·저장 복원 계약을 격리 검증한다.</summary>
    public sealed class RedTreeR04FactsTests
    {
        private GameObject _root;
        private Facts _facts;
        private GameObject _eventRoot;
        private IGameEventPublisher _events;
        private UnityEngine.SceneManagement.Scene _neighborScene;
        private CurrentRegion _region;
        private FlowGraphSO _temporaryGraph;

        [SetUp]
        public void SetUp()
        {
            Services.Clear();
            _facts = new Facts();
            Services.Register(_facts);
            _eventRoot = new GameObject("R04EventTest");
            _eventRoot.SetActive(false);
            _events = (IGameEventPublisher)_eventRoot.AddComponent(Type.GetType("UPlayGround.Manager.EventManager, Assembly-CSharp", true));
            Services.Register((IGameEventObservable)_events);
            _root = new GameObject("R04FactsTest");
            _root.SetActive(false);
            var runner = _root.AddComponent<FlowGraphRunner>();
            runner.SetGraph(AssetDatabase.LoadAssetAtPath<FlowGraphSO>("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R04/FLOW_RedTreeVillageWaterReport.asset"), false);
            _root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            _root.SetActive(false);
            Services.Unregister(_facts);
            Services.Unregister((IGameEventObservable)_events);
            if (_region != null) Services.Unregister(_region);
            _region = null;
            UnityEngine.Object.Destroy(_eventRoot);
            UnityEngine.Object.Destroy(_root);
            if (_temporaryGraph != null) UnityEngine.Object.Destroy(_temporaryGraph);
        }

        [UnityTearDown]
        public IEnumerator UnloadNeighbor()
        {
            if (_neighborScene.IsValid() && _neighborScene.isLoaded)
                yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(_neighborScene);
        }

        [UnityTest]
        public IEnumerator 공유_R04가_두_지역에_있어도_현재_지역만_새_사건을_시작한다()
        {
            _neighborScene = UnityEngine.SceneManagement.SceneManager.CreateScene("R04Neighbor");
            var neighbor = new GameObject("R04NeighborRunner");
            neighbor.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(neighbor, _neighborScene);
            var runner = neighbor.AddComponent<FlowGraphRunner>();
            runner.SetGraph(_root.GetComponent<FlowGraphRunner>().Graph, false);
            runner.OriginSceneName = "DroughtVillage";
            _region = new CurrentRegion { Scene = _root.scene };
            Services.Register(_region);
            neighbor.SetActive(true);
            _facts.Set("well_examined");
            _facts.Set("handoff_acknowledged");
            yield return null;
            yield return null;
            var rejected = new List<FlowTraceEvent>();
            runner.GetTraceSnapshot(rejected);
            Assert.That(rejected, Is.Empty, "비현재 지역 러너는 반복 정책을 소비하거나 사건을 시작하면 안 된다.");
            CollectionAssert.Contains(_facts.Accepted, "quest_main_red_tree_s03");
            var accepted = new List<FlowTraceEvent>();
            _root.GetComponent<FlowGraphRunner>().GetTraceSnapshot(accepted);
            Assert.That(accepted, Is.Not.Empty);
        }

        private sealed class CurrentRegion : IWorldRegionService
        {
            public UnityEngine.SceneManagement.Scene Scene;
            public bool CanRunRegionEvents(UnityEngine.SceneManagement.Scene ownerScene) => ownerScene == Scene;
        }

        [UnityTest]
        public IEnumerator 복원_도착대기는_중복되지_않고_이탈시_완료없이_정리된다()
        {
            var runner = PrepareRegionWait(new WaitPlayerInVolumeNode { volumeId = "test.destination", pollSeconds = 0.1f });
            var volumeObject = new GameObject("Destination");
            volumeObject.transform.SetParent(_root.transform);
            volumeObject.AddComponent<BoxCollider>().isTrigger = true;
            var volume = volumeObject.AddComponent<FlowGraphTriggerVolume>();
            var serialized = new SerializedObject(volume);
            serialized.FindProperty("_volumeId").stringValue = "test.destination";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            runner.ResumeRegion();
            runner.ResumeRegion();
            Assert.That(runner.ActiveContexts.Count, Is.EqualTo(1));
            Assert.That(runner.HasRegionBlockingWork(), Is.False);
            var context = runner.ActiveContexts[0];
            bool? completed = null;
            context.RegisterCompletion(value => completed = value);
            runner.SuspendRegionWaits();
            yield return new WaitForSeconds(0.15f);
            Assert.That(runner.ActiveContexts, Is.Empty);
            Assert.That(completed, Is.False, "지역 이탈은 진행 완료가 아니다.");
            runner.ResumeRegion();
            Assert.That(runner.ActiveContexts.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator 진행중인_연출은_지역이탈로_취소하지_않는다()
        {
            var runner = PrepareRegionWait(new BlockingRegionNode());
            runner.ResumeRegion();
            runner.SuspendRegionWaits();
            yield return null;
            Assert.That(runner.HasRegionBlockingWork(), Is.True);
            Assert.That(runner.ActiveContexts.Count, Is.EqualTo(1));
            Assert.That(runner.ActiveContexts[0].Cancelled, Is.False);
        }

        private FlowGraphRunner PrepareRegionWait(FlowNode wait)
        {
            var runner = _root.GetComponent<FlowGraphRunner>();
            _root.SetActive(false);
            _temporaryGraph = ScriptableObject.CreateInstance<FlowGraphSO>();
            _temporaryGraph.graphId = "test.region." + Guid.NewGuid().ToString("N");
            var entry = new ManualEntryNode { entryId = "Resume", repeatPolicy = FlowRepeatPolicy.Always };
            _temporaryGraph.nodes.Add(entry);
            _temporaryGraph.nodes.Add(wait);
            _temporaryGraph.connections.Add(new FlowConnection { fromNodeId = entry.id, fromPort = FlowPort.Out, toNodeId = wait.id, toPort = FlowPort.In });
            runner.SetGraph(_temporaryGraph, false);
            var serialized = new SerializedObject(runner);
            serialized.FindProperty("_fireManualEntryOnStart").boolValue = true;
            serialized.FindProperty("_manualEntryIdOnStart").stringValue = "Resume";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _root.SetActive(true);
            return runner;
        }

        [Serializable]
        private sealed class BlockingRegionNode : FlowNode
        {
            public override IEnumerable<FlowPortDef> Ports { get { yield return FlowPortDef.Input(); } }
            public override IEnumerator Execute(FlowToken token)
            {
                while (!token.Context.Cancelled) yield return null;
            }
        }

        [UnityTest]
        public IEnumerator 인계플래그가_선행퀘스트보다_먼저_끝나도_완료이벤트로_수락한다()
        {
            _facts.IsHandoffQuestComplete = false;
            _events.GetType().GetMethod("OnSceneChanged").Invoke(_events, new object[] { "LakeOfLifeScenario" });
            _facts.Set("well_examined");
            _facts.Set("handoff_acknowledged");
            yield return null;
            CollectionAssert.DoesNotContain(_facts.Accepted, "quest_main_red_tree_s03");
            _facts.IsHandoffQuestComplete = true;
            _events.Send(QuestEvent.QuestCompleted, new QuestStateEventData { QuestId = "quest_main_red_tree_s02" });
            yield return null;
            CollectionAssert.Contains(_facts.Accepted, "quest_main_red_tree_s03");
            CollectionAssert.Contains(_facts.Events, "lake.red_tree.well_examined");
        }

        [UnityTest]
        public IEnumerator 선행조사는_나중에_수락해도_모두_반영된다()
        {
            foreach (string key in new[] { "field_examined", "home_examined", "survivors_found", "well_examined" }) _facts.Set(key);
            yield return null;
            Assert.IsTrue(_facts.Has("evidence.village_loss"));
            Assert.IsEmpty(_facts.Accepted);
            _facts.Set("handoff_acknowledged");
            yield return null;
            CollectionAssert.Contains(_facts.Accepted, "quest_main_red_tree_s03");
            CollectionAssert.Contains(_facts.Events, "lake.red_tree.field_examined");
            CollectionAssert.Contains(_facts.Events, "lake.red_tree.survivors_found");
        }

        [UnityTest]
        public IEnumerator 주민의사와_인계가_없으면_릴리동의는_열리지_않는다()
        {
            foreach (string key in new[] { "handoff_acknowledged", "evidence.village_loss", "resident_stays", "resident_leaves", "water_handed_over" }) _facts.Set(key);
            yield return null;
            Assert.IsFalse(_facts.Has("lili_ready"));
            _facts.Set("departure_prepared");
            yield return null;
            Assert.IsTrue(_facts.Has("lili_ready"));
            Assert.IsFalse(_facts.Has("lili_consent"));
        }

        [UnityTest]
        public IEnumerator 별도_증거와_조우가_모여야_진실을_확인할수있다()
        {
            foreach (string key in new[] { "lili_consent", "evidence.village_loss", "evidence.root_draw", "creek_cleared" }) _facts.Set(key);
            yield return null;
            Assert.IsFalse(_facts.Has("truth_ready"));
            _facts.Set("evidence.lake_path");
            yield return null;
            Assert.IsTrue(_facts.Has("truth_ready"));
            Assert.IsFalse(_facts.Has("truth.cost"));
            Assert.IsFalse(_facts.Has("seola_consent"));
        }

        [UnityTest]
        public IEnumerator 로드시_진실은_보존되고_영입실패를_완료로_만들지_않는다()
        {
            _facts.Values["lake.red_tree.truth.cost"] = true;
            _facts.Reload();
            yield return null;
            yield return null;
            Assert.IsTrue(_facts.Has("truth.cost"));
            Assert.IsFalse(_facts.Has("seola_consent"));
            CollectionAssert.DoesNotContain(_facts.Accepted, "quest_main_red_tree_s05");
        }

        [UnityTest]
        public IEnumerator 업무배정은_실제배송이나_공급대안_완료가_아니다()
        {
            foreach (string key in new[] { "report_completed", "support_assigned", "local_supply_checked", "outbound_supply_checked" }) _facts.Set(key);
            yield return null;
            Assert.IsTrue(_facts.Has("supply_review_ready"));
            Assert.IsFalse(_facts.Has("supplies_delivered"));
            Assert.IsFalse(_facts.Has("supply_need_confirmed"));
        }

        private sealed class Facts : IGlobalFlagService, IQuestFlowService
        {
            public readonly Dictionary<string, bool> Values = new();
            public readonly List<string> Accepted = new();
            public readonly List<string> Events = new();
            public bool IsHandoffQuestComplete = true;
            public event Action<string, bool> OnFlagChanged;
            public event Action OnFlagsReloaded;
            public bool Has(string key) => GetFlag("lake.red_tree." + key);
            public void Set(string key) => SetFlag("lake.red_tree." + key, true);
            public bool GetFlag(string key) => Values.TryGetValue(key, out bool value) && value;
            public void SetFlag(string key, bool value)
            {
                if (Values.TryGetValue(key, out bool old) && old == value) return;
                Values[key] = value;
                OnFlagChanged?.Invoke(key, value);
            }
            public void Reload() => OnFlagsReloaded?.Invoke();
            public bool AcceptQuest(string id) { Accepted.Add(id); return true; }
            public bool CompleteQuest(string id) => true;
            public bool FailQuest(string id) => true;
            public bool TrackQuest(string id) => true;
            public void NotifyStoryEvent(string id) => Events.Add(id);
            public QuestStatus GetQuestStatus(string id) => id == "quest_main_red_tree_s03" && !IsHandoffQuestComplete
                ? QuestStatus.Locked : Accepted.Contains(id) ? QuestStatus.Active : QuestStatus.Available;
        }
    }
}
#endif
