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
    /// <summary>격리 서비스에서 실제 R07R08 그래프의 선행 사실과 저장 복원 경계를 검사한다.</summary>
    public sealed class RedTreeR07R08FactsTests
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
            _eventsRoot = new GameObject("R07R08Events");
            _eventsRoot.SetActive(false);
            _events = (IGameEventObservable)_eventsRoot.AddComponent(Type.GetType("UPlayGround.Manager.EventManager, Assembly-CSharp", true));
            Services.Register(_events);
            _root = new GameObject("R07R08Facts");
            _root.SetActive(false);
            _root.AddComponent<FlowGraphRunner>().SetGraph(AssetDatabase.LoadAssetAtPath<FlowGraphSO>(
                "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R07R08/FLOW_RedTreeResponseFinale.asset"), false);
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
        public IEnumerator 확보는_배급이_아니며_도착없는_대피는_완료되지않는다()
        {
            foreach(string k in new[]{"offering_handed_over","stored_food_checked","external_water_handed_over","response.front_clear","response.wishes"}) _facts.Set(k);
            yield return null;
            Assert.IsTrue(_facts.Has("response.evacuation_ready"));
            Assert.IsFalse(_facts.Has("response.evacuation_complete"));
            Assert.IsFalse(_facts.Has("response.distribution_complete"));
            Assert.IsFalse(_facts.Has("response.care_supplied"));
        }
        [UnityTest]
        public IEnumerator 선행배급은_목표공개후_반영하고_기존대기는_마감한다()
        {
            _facts.Accepted.Add("quest_main_red_tree_response_next");
            _facts.Set("response.care_supplied"); _facts.Set("response.remaining_supplied");
            _facts.Set("r06_complete"); _facts.Set("response_work_started");
            yield return null; yield return null;
            CollectionAssert.Contains(_facts.Completed,"quest_main_red_tree_response_next");
            CollectionAssert.Contains(_facts.Accepted,"quest_main_red_tree_s10");
            CollectionAssert.Contains(_facts.Events,"lake.red_tree.response.care_supplied");
            Assert.IsFalse(_facts.Has("response.affected_supplied"));
        }
        [UnityTest]
        public IEnumerator 부분중단은_로드후_보존하고_시험표식을_다시요구하지않는다()
        {
            _facts.Set("marker.trial_creek.disconnected"); _facts.Set("marker.west_relay.disconnected");
            _facts.Reload(); yield return null; yield return null;
            Assert.IsTrue(_facts.Has("marker.west_relay.disconnected"));
            Assert.IsFalse(_facts.Has("markers.remaining_disconnected"));
            _facts.Set("marker.south_channel.disconnected"); _facts.Set("marker.shrine_link.disconnected");
            yield return null;
            Assert.IsTrue(_facts.Has("markers.remaining_disconnected"));
            Assert.IsFalse(_facts.Has("markers.confirmed")); Assert.IsFalse(_facts.Has("lucia.accepted"));
        }
        [UnityTest]
        public IEnumerator 준비사실만으로_지정동료검사를_우회하지못한다()
        {
            foreach(string k in new[]{"r06_complete","truth.expansion","response_work_started","response.evacuation_complete","response.distribution_complete","response.access_complete"})_facts.Set(k);
            yield return null;
            Assert.IsFalse(_facts.Has("response.ready")); Assert.IsFalse(_facts.Has("lucia.accepted"));
        }
        [UnityTest]
        public IEnumerator 수용과_승리와_흡수중단과_관찰은_별도다()
        {
            _facts.Set("lucia.accepted"); _facts.Reload(); yield return null; yield return null;
            Assert.IsFalse(_facts.Has("lucia.victory"));
            _facts.Set("lucia.victory"); _facts.Reload(); yield return null; yield return null;
            Assert.IsFalse(_facts.Has("absorption.stopped")); Assert.IsFalse(_facts.Has("absorption.observed"));
            Assert.IsFalse(_facts.Accepted.Contains("quest_main_red_tree_return_next"));
        }
        [UnityTest]
        public IEnumerator 후반저장_복원은_실행한도를_넘지않고_활성목표에만_재통지한다()
        {
            var graph = _root.GetComponent<FlowGraphRunner>().Graph;
            foreach (var node in graph.nodes)
                if (node is CheckFlagNode check) _facts.Values[check.flagKey] = true;
            _facts.Completed.Add("quest_main_red_tree_response_next");
            _facts.Completed.Add("quest_main_red_tree_s10");
            _facts.Completed.Add("quest_main_red_tree_s11");
            _facts.Accepted.Add("quest_main_red_tree_s12");
            _facts.Reload(); yield return null; yield return null;
            Assert.IsTrue(_facts.Has("lucia.victory"));
            CollectionAssert.Contains(_facts.Events, "lake.red_tree.absorption.observed");
        }

        [UnityTest]
        public IEnumerator 관찰후에는_후속목표만_열고_처우나_엔딩을_정하지않는다()
        {
            _facts.Set("lucia.victory"); _facts.Set("lucia.attacks_cleared"); _facts.Set("absorption.stopped");
            _facts.Set("absorption.observed"); _facts.Reload(); yield return null; yield return null;
            CollectionAssert.Contains(_facts.Accepted,"quest_main_red_tree_return_next");
            Assert.IsFalse(_facts.Has("return_work_started")); Assert.IsFalse(_facts.Has("lucia.dead"));
            Assert.IsTrue(_facts.Has("lucia.victory"));
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


