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
    /// <summary>격리 서비스에서 실제 R06 그래프의 선행 사실과 저장 복원 경계를 검사한다.</summary>
    public sealed class RedTreeR06FactsTests
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
            _eventsRoot = new GameObject("R06Events");
            _eventsRoot.SetActive(false);
            _events = (IGameEventObservable)_eventsRoot.AddComponent(Type.GetType("UPlayGround.Manager.EventManager, Assembly-CSharp", true));
            Services.Register(_events);
            _root = new GameObject("R06Facts");
            _root.SetActive(false);
            _root.AddComponent<FlowGraphRunner>().SetGraph(AssetDatabase.LoadAssetAtPath<FlowGraphSO>(
                "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R06/FLOW_RedTreeMarksSupply.asset"), false);
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
        public IEnumerator 선행조사와_대조와_진실은_별도이고_수락후_목표가_반영된다()
        {
            foreach (string k in new[] { "cargo_list_read", "cargo_label_read" }) _facts.Set(k);
            yield return null; Assert.IsFalse(_facts.Has("cargo_comparison_ready"));
            _facts.Set("install_bundle_read"); yield return null;
            Assert.IsTrue(_facts.Has("cargo_comparison_ready")); Assert.IsFalse(_facts.Has("evidence.cargo_match"));
            _facts.Completed.Add("quest_main_red_tree_s07"); _facts.Accepted.Add("quest_main_red_tree_marks_next"); _facts.Reload();
            yield return null; yield return null;
            CollectionAssert.Contains(_facts.Completed, "quest_main_red_tree_marks_next");
            CollectionAssert.Contains(_facts.Accepted, "quest_main_red_tree_s08");
            CollectionAssert.Contains(_facts.Events, "lake.red_tree.cargo_list_read");
            Assert.IsFalse(_facts.Has("truth.expansion"));
        }

        [UnityTest]
        public IEnumerator 주민안전과_사전관찰이_없으면_표식을_끊을수없다()
        {
            _facts.Set("truth.concealment"); _facts.Set("evidence.cargo_match"); yield return null;
            Assert.IsFalse(_facts.Has("trial_disconnect_ready"));
            _facts.Set("trial_resident_safe"); yield return null; Assert.IsFalse(_facts.Has("trial_disconnect_ready"));
            _facts.Set("marker_before_observed"); yield return null; Assert.IsTrue(_facts.Has("trial_disconnect_ready"));
            Assert.IsFalse(_facts.Has("marker.trial_creek.disconnected"));
        }

        [UnityTest]
        public IEnumerator 분리후_로드해도_관찰과_증거는_자동완료되지않는다()
        {
            _facts.Set("marker_before_observed"); _facts.Set("marker.trial_creek.disconnected"); _facts.Reload();
            yield return null; yield return null;
            Assert.IsTrue(_facts.Has("marker.trial_creek.disconnected"));
            Assert.IsFalse(_facts.Has("marker_after_observed")); Assert.IsFalse(_facts.Has("evidence.marker_draw"));
            _facts.Set("marker_after_observed"); yield return null; Assert.IsTrue(_facts.Has("evidence.marker_draw"));
        }

        [UnityTest]
        public IEnumerator 원문과_실제기능을_모아도_진실과_책임과_동의는_자동완료되지않는다()
        {
            foreach (string k in new[] { "truth.concealment", "evidence.cargo_match", "evidence.marker_draw", "trial_resident_safe" }) _facts.Set(k);
            yield return null; Assert.IsFalse(_facts.Has("expansion_ready"));
            _facts.Set("evidence.install_order"); yield return null; Assert.IsTrue(_facts.Has("expansion_ready"));
            _facts.Set("truth.expansion"); _facts.Reload(); yield return null; yield return null;
            Assert.IsFalse(_facts.Has("myomyo_responsibility")); Assert.IsFalse(_facts.Has("myomyo_consent"));
        }

        [UnityTest]
        public IEnumerator 봉헌철회와_물확보는_인계와_동의를_대신하지않는다()
        {
            _facts.Set("truth.expansion"); _facts.Set("offering_found"); yield return null;
            Assert.IsTrue(_facts.Has("withdrawal_ready")); _facts.Set("offering_withdrawn"); _facts.Reload(); yield return null;
            Assert.IsFalse(_facts.Has("offering_handed_over")); Assert.IsFalse(_facts.Has("yeonhoa_consent"));
            foreach (string k in new[] { "stored_food_checked", "external_water_checked", "external_water_secured" }) _facts.Set(k);
            yield return null; Assert.IsFalse(_facts.Has("sera_alternative_ready"));
            _facts.Set("external_water_handed_over"); yield return null; Assert.IsTrue(_facts.Has("sera_alternative_ready"));
            Assert.IsFalse(_facts.Has("sera_stop_consent")); Assert.IsFalse(_facts.Has("sera_consent"));
        }

        [UnityTest]
        public IEnumerator 마지막세명만_합류해도_앞선사건을_대신하지않는다()
        {
            foreach (string k in new[] { "truth.expansion", "myomyo_consent", "yeonhoa_consent", "sera_consent" }) _facts.Set(k);
            yield return null; Assert.IsFalse(_facts.Has("r06_events_ready")); Assert.IsFalse(_facts.Has("r06_complete"));
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

