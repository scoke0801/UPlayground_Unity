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
    /// <summary>격리 서비스에서 실제 R09 그래프의 선행 사실과 저장 복원 경계를 검사한다.</summary>
    public sealed class RedTreeR09FactsTests
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
            _eventsRoot = new GameObject("R09Events");
            _eventsRoot.SetActive(false);
            _events = (IGameEventObservable)_eventsRoot.AddComponent(Type.GetType("UPlayGround.Manager.EventManager, Assembly-CSharp", true));
            Services.Register(_events);
            _root = new GameObject("R09Facts");
            _root.SetActive(false);
            _root.AddComponent<FlowGraphRunner>().SetGraph(AssetDatabase.LoadAssetAtPath<FlowGraphSO>(
                "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R09/FLOW_RedTreeReturn.asset"), false);
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

        private void PrepareObserved()
        {
            foreach (string k in new[] { "lucia.victory", "lucia.attacks_cleared", "absorption.stopped", "absorption.observed" }) _facts.Set(k);
        }
        [UnityTest]
        public IEnumerator 승리만으로_관찰이나_반환을_대체하지않는다()
        {
            _facts.Set("lucia.victory"); _facts.Set("lucia.attacks_cleared"); _facts.Set("absorption.stopped");
            _facts.Reload(); yield return null; yield return null;
            Assert.IsFalse(_facts.Has("return.entry_ready")); Assert.IsFalse(_facts.Has("absorption.observed"));
            Assert.IsFalse(_facts.Accepted.Contains("quest_main_red_tree_s13")); Assert.IsFalse(_facts.Has("return_work_started"));
        }
        [UnityTest]
        public IEnumerator 관찰완료는_준비만_열고_대기는_시작입력뒤_마감한다()
        {
            _facts.Accepted.Add("quest_main_red_tree_return_next"); PrepareObserved(); yield return null;
            Assert.IsTrue(_facts.Has("return.entry_ready")); Assert.IsFalse(_facts.Has("return_work_started"));
            Assert.IsFalse(_facts.Completed.Contains("quest_main_red_tree_return_next"));
            _facts.Set("return_work_started"); yield return null;
            CollectionAssert.Contains(_facts.Completed,"quest_main_red_tree_return_next");
            CollectionAssert.Contains(_facts.Accepted,"quest_main_red_tree_s13");
        }
        [UnityTest]
        public IEnumerator 선행외부관찰과_부분작업은_목표공개와_로드에서_반영한다()
        {
            PrepareObserved(); _facts.Set("return.damage_observed"); _facts.Set("return.water_checked"); _facts.Set("return.packed");
            _facts.Set("return_work_started"); _facts.Reload(); yield return null; yield return null;
            CollectionAssert.Contains(_facts.Events,"lake.red_tree.return.damage_observed");
            CollectionAssert.Contains(_facts.Events,"lake.red_tree.return.packed");
            Assert.IsFalse(_facts.Has("return.loaded")); Assert.IsFalse(_facts.Has("return.departed"));
        }
        [UnityTest]
        public IEnumerator 기존배급은_새적재나_주민의사를_자동완료하지않는다()
        {
            PrepareObserved();
            foreach(string k in new[]{"response.care_supplied","response.remaining_supplied","response.affected_supplied","response.share_checked","return.stock_checked"}) _facts.Set(k);
            yield return null;
            Assert.IsTrue(_facts.Has("return.pack_ready")); Assert.IsFalse(_facts.Has("return.packed"));
            Assert.IsFalse(_facts.Has("return.loaded")); Assert.IsFalse(_facts.Has("return.handed_over")); Assert.IsFalse(_facts.Has("return.receipt_ready"));
            _facts.Set("return.recipient_wishes"); yield return null;
            Assert.IsTrue(_facts.Has("return.receipt_ready")); Assert.IsFalse(_facts.Has("return.handed_over"));
        }
        [UnityTest]
        public IEnumerator 작업완료는_출발과_결말을_대신하지않는다()
        {
            PrepareObserved();
            foreach(string k in new[]{"return_work_started","return.loaded","return.handed_over","return.water_checked","return.channel_supported","return.route_supported","return.damage_observed"}) _facts.Set(k);
            yield return null;
            Assert.IsTrue(_facts.Has("return.ready")); Assert.IsFalse(_facts.Has("return.departed")); Assert.IsFalse(_facts.Has("return.common_ending"));
            _facts.Set("return.departed"); _facts.Reload(); yield return null; yield return null;
            Assert.IsTrue(_facts.Has("return.departed")); Assert.IsFalse(_facts.Has("return.common_ending"));
        }
        [UnityTest]
        public IEnumerator 완료저장은_재수락_재보상_흡수재개가없다()
        {
            PrepareObserved(); _facts.Set("return_work_started"); _facts.Set("return.departed"); _facts.Set("return.common_ending");
            _facts.Completed.Add("quest_main_red_tree_return_next"); _facts.Completed.Add("quest_main_red_tree_s13");
            _facts.Accepted.Clear(); _facts.Events.Clear(); _facts.Reload(); yield return null; yield return null;
            Assert.IsEmpty(_facts.Accepted); Assert.IsEmpty(_facts.Events);
            Assert.IsTrue(_facts.Has("lucia.victory")); Assert.IsTrue(_facts.Has("absorption.stopped"));
            Assert.IsTrue(_facts.Has("return.common_ending")); Assert.IsFalse(_facts.Has("lucia.dead"));
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



