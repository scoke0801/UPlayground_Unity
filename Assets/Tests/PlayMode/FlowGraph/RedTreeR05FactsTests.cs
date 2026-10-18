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
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    /// <summary>격리 서비스에서 실제 R05 그래프의 선행 사실과 저장 복원 경계를 검사한다.</summary>
    public sealed class RedTreeR05FactsTests
    {
        private GameObject _root;
        private GameObject _eventsRoot;
        private IGameEventObservable _events;
        private Facts _facts;

        [SetUp]
        public void SetUp()
        {
            Services.Clear();
            _facts = new Facts();
            Services.Register(_facts);
            _eventsRoot = new GameObject("R05Events");
            _eventsRoot.SetActive(false);
            _events = (IGameEventObservable)_eventsRoot.AddComponent(Type.GetType("UPlayGround.Manager.EventManager, Assembly-CSharp", true));
            Services.Register(_events);
            _root = new GameObject("R05Facts");
            _root.SetActive(false);
            _root.AddComponent<FlowGraphRunner>().SetGraph(AssetDatabase.LoadAssetAtPath<FlowGraphSO>(
                "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R05/FLOW_RedTreeRecordsPassage.asset"), false);
            _root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            _root.SetActive(false);
            Services.Unregister(_facts);
            Services.Unregister(_events);
            UnityEngine.Object.Destroy(_root);
            UnityEngine.Object.Destroy(_eventsRoot);
        }

        [UnityTest]
        public IEnumerator 선행열람은_첫진실없이_결론을_만들지않고_수락뒤_누적된다()
        {
            foreach (string k in new[] { "evidence.inquiry_stopped", "evidence.warning_received", "evidence.offering_increase" }) _facts.Set(k);
            yield return null;
            Assert.IsFalse(_facts.Has("records_ready"));
            Assert.IsFalse(_facts.Has("truth.concealment"));
            Assert.IsEmpty(_facts.Accepted);
            _facts.Completed.Add("quest_main_red_tree_s05");
            _facts.Set("truth.cost"); _facts.Set("report_completed");
            yield return null;
            Assert.IsTrue(_facts.Has("records_ready"));
            CollectionAssert.Contains(_facts.Accepted, "quest_main_red_tree_s06");
            CollectionAssert.Contains(_facts.Events, "lake.red_tree.evidence.inquiry_stopped");
        }

        [UnityTest]
        public IEnumerator 기록공유와_리안동의가_없으면_둘째진실이_열리지않는다()
        {
            foreach (string k in new[] { "truth.cost", "report_completed", "evidence.warning_received", "evidence.offering_increase", "evidence.inquiry_stopped", "records_shared" }) _facts.Set(k);
            yield return null;
            Assert.IsFalse(_facts.Has("concealment_ready"));
            _facts.Set("lian_consent");
            yield return null;
            Assert.IsTrue(_facts.Has("concealment_ready"));
            Assert.IsFalse(_facts.Has("truth.concealment"));
            Assert.IsFalse(_facts.Has("myoryeong_consent"));
        }

        [UnityTest]
        public IEnumerator 진실로드는_관계나_영입을_자동완료하지않는다()
        {
            _facts.Values["lake.red_tree.truth.concealment"] = true;
            _facts.Reload();
            yield return null; yield return null;
            Assert.IsTrue(_facts.Has("truth.concealment"));
            Assert.IsFalse(_facts.Has("myoryeong_stance"));
            Assert.IsFalse(_facts.Has("myoryeong_consent"));
        }

        [UnityTest]
        public IEnumerator 명령거부는_길열림이나_레이네합류를_대신하지않는다()
        {
            _facts.Set("order_refused");
            yield return null;
            Assert.IsFalse(_facts.Has("passage_open"));
            Assert.IsFalse(_facts.Has("reine_consent"));
            _facts.Set("passage_open"); _facts.Reload();
            yield return null; yield return null;
            Assert.IsTrue(_facts.Has("passage_open"));
            Assert.IsFalse(_facts.Has("reine_consent"));
        }

        [UnityTest]
        public IEnumerator 우회로_세지점과_실제인계가_모여야_유라동의가_열린다()
        {
            foreach (string k in new[] { "route_near_checked", "route_far_checked", "yura_rescued", "truth.concealment" }) _facts.Set(k);
            yield return null;
            Assert.IsFalse(_facts.Has("yura_ready"));
            _facts.Set("handoff_point_checked");
            yield return null;
            Assert.IsTrue(_facts.Has("yura_ready"));
            Assert.IsFalse(_facts.Has("yura_consent"));
        }

        [UnityTest]
        public IEnumerator 선행구조는_보존되며_다른여행자_동의와_구별된다()
        {
            foreach (string k in new[] { "route_near_checked", "route_far_checked", "handoff_point_checked", "other_found", "other_rescued" }) _facts.Set(k);
            _facts.Reload();
            yield return null; yield return null;
            Assert.IsTrue(_facts.Has("other_rescued"));
            Assert.IsFalse(_facts.Has("other_ready"));
            _facts.Set("truth.concealment");
            yield return null;
            Assert.IsTrue(_facts.Has("other_ready"));
            Assert.IsFalse(_facts.Has("other_protagonist_consent"));
        }

        [UnityTest]
        public IEnumerator 다섯합류중_하나라도_없으면_다음목표를_만들지않는다()
        {
            foreach (string k in new[] { "truth.concealment", "lian_consent", "myoryeong_consent", "reine_consent", "yura_consent", "passage_open" }) _facts.Set(k);
            yield return null;
            Assert.IsFalse(_facts.Has("companions_ready"));
            _facts.Set("other_protagonist_consent");
            yield return null;
            Assert.IsTrue(_facts.Has("companions_ready"));
            Assert.IsFalse(_facts.Has("r05_complete"));
        }

        private sealed class Facts : IGlobalFlagService, IQuestFlowService
        {
            public readonly Dictionary<string, bool> Values = new();
            public readonly HashSet<string> Completed = new();
            public readonly HashSet<string> Accepted = new();
            public readonly List<string> Events = new();
            public event Action<string, bool> OnFlagChanged;
            public event Action OnFlagsReloaded;
            public bool Has(string key) => GetFlag("lake.red_tree." + key);
            public void Set(string key) => SetFlag("lake.red_tree." + key, true);
            public bool GetFlag(string key) => Values.TryGetValue(key, out bool value) && value;
            public void SetFlag(string key, bool value)
            {
                if (GetFlag(key) == value) return;
                Values[key] = value;
                OnFlagChanged?.Invoke(key, value);
            }
            public void Reload() => OnFlagsReloaded?.Invoke();
            public bool AcceptQuest(string id) => Accepted.Add(id);
            public bool CompleteQuest(string id) => Completed.Add(id);
            public bool FailQuest(string id) => true;
            public bool TrackQuest(string id) => true;
            public void NotifyStoryEvent(string id) => Events.Add(id);
            public QuestStatus GetQuestStatus(string id) => Completed.Contains(id) ? QuestStatus.Completed
                : Accepted.Contains(id) ? QuestStatus.Active : QuestStatus.Available;
        }
    }
}
#endif
