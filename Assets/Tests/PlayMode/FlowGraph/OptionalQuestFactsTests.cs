#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    /// <summary>실제 선택 사건 그래프의 역순 누적과 저장 복원 재개를 격리된 서비스로 검사한다.</summary>
    public sealed class OptionalQuestFactsTests
    {
        private GameObject _root;
        private FlowGraphRunner _runner;
        private FactsService _service;

        [SetUp]
        public void SetUp()
        {
            Services.Clear();
            _service = new FactsService();
            Services.Register(_service);
            _root = new GameObject("OptionalFactsTest");
            _root.SetActive(false);
            _runner = _root.AddComponent<FlowGraphRunner>();
            var graph = AssetDatabase.LoadAssetAtPath<FlowGraphSO>("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioOptionalFacts.asset");
            Assert.IsNotNull(graph);
            _runner.SetGraph(graph, false);
            _root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            _root.SetActive(false);
            Services.Unregister(_service);
            UnityEngine.Object.Destroy(_root);
        }

        [UnityTest]
        public IEnumerator 불러오기는_이전저장의_진행중인_흐름을_취소한다()
        {
            _root.SetActive(false);
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            graph.nodes.Add(new ManualEntryNode { id = "start", entryId = "Wait" });
            graph.nodes.Add(new WaitTimeNode { id = "wait", seconds = 60 });
            graph.nodes.Add(new OnFlagsReloadedEntryNode { id = "reload", cancelRunningContexts = true });
            graph.connections.Add(new FlowConnection { fromNodeId = "start", fromPort = "Out", toNodeId = "wait", toPort = "In" });
            _runner.SetGraph(graph, false);
            _root.SetActive(true);
            FlowContext context = null;
            _runner.FireManualEntries("Wait", current => context = current);
            Assert.IsFalse(context.Cancelled);
            _service.Reload();
            Assert.IsTrue(context.Cancelled);
            yield return null;
            _root.SetActive(false);
            UnityEngine.Object.Destroy(graph);
        }

        [UnityTest]
        public IEnumerator 등불_역순조사는_셋을_모두_확인했을때만_보고가능하다()
        {
            _service.SetFlag("lake.side.lantern_3_examined", true);
            _service.SetFlag("lake.side.lantern_1_examined", true);
            yield return null;
            Assert.IsFalse(_service.GetFlag("lake.side.lantern_cleared"));
            _service.SetFlag("lake.side.lantern_2_examined", true);
            yield return null;
            Assert.IsTrue(_service.GetFlag("lake.side.lantern_cleared"));
            CollectionAssert.Contains(_service.Events, "lake.side.lantern_3_examined");
            Assert.AreEqual(0, _service.TrackCount);
        }

        [UnityTest]
        public IEnumerator 전투전_수레조사는_보존되지만_처치전에는_보고할수없다()
        {
            _service.SetFlag("lake.side.driver_tracks_found", true);
            yield return null;
            Assert.IsFalse(_service.GetFlag("lake.side.west_road_ready"));
            _service.SetFlag("lake.side.west_road_cleared", true);
            yield return null;
            Assert.IsTrue(_service.GetFlag("lake.side.west_road_ready"));
        }

        [UnityTest]
        public IEnumerator 일괄로드는_다른서비스의_복원뒤_사실을_다시전달한다()
        {
            _service.Flags["lake.side.herb_bed_examined"] = true;
            _service.Reload();
            Assert.IsEmpty(_service.Events, "복원 디스패치 중에는 이전 퀘스트를 갱신하지 않는다.");
            yield return null;
            yield return null;
            CollectionAssert.Contains(_service.Events, "lake.side.herb_bed_examined");
            Assert.IsFalse(_service.GetFlag("lake.side.west_road_ready"));
            _root.SetActive(false);
            _service.Events.Clear();
            _service.Reload();
            yield return null;
            Assert.IsEmpty(_service.Events, "비활성 러너의 구독은 해제되어야 한다.");
        }

        private sealed class FactsService : IGlobalFlagService, IQuestFlowService
        {
            public readonly Dictionary<string, bool> Flags = new();
            public readonly List<string> Events = new();
            public int TrackCount;
            public event Action<string, bool> OnFlagChanged;
            public event Action OnFlagsReloaded;
            public bool GetFlag(string key) => Flags.TryGetValue(key, out bool value) && value;
            public void SetFlag(string key, bool value)
            {
                if (Flags.TryGetValue(key, out bool old) && old == value) return;
                Flags[key] = value;
                OnFlagChanged?.Invoke(key, value);
            }
            public void Reload() => OnFlagsReloaded?.Invoke();
            public bool AcceptQuest(string id) => true;
            public bool CompleteQuest(string id) => true;
            public bool FailQuest(string id) => true;
            public bool TrackQuest(string id) { TrackCount++; return true; }
            public void NotifyStoryEvent(string id) => Events.Add(id);
            public QuestStatus GetQuestStatus(string id) => QuestStatus.Active;
        }
    }
}
#endif
