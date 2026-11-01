using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.Data.Party;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Ability.Tests
{
    /// <summary>훈련 안내와 Raon의 실제 연계 데이터가 같은 분기·모션을 사용하는지 검사한다.</summary>
    public sealed class RaonComboTrainingTests
    {
        private const string SetPath = "Assets/10.Datas/Ability/Player/Raon/AbilitySet_Raon.asset";
        private AbilitySetSO Set => AssetDatabase.LoadAssetAtPath<AbilitySetSO>(SetPath);

        [Test]
        public void 강공격은_기본_5타에서_해금_6타로_확장되고_초기화해도_5타를_유지한다()
        {
            var tree = AssetDatabase.LoadAssetAtPath<CharacterSkillTreeSO>("Assets/10.Datas/Party/SkillTree/CharacterSkillTree_Raon.asset");
            var service = new CharacterSkillProgressionService();
            var type = global::UPlayGround.Data.EnumType.CharacterActorType.Raon;
            service.Configure(new[] { tree }, new SkillPointRule(), _ => 50);
            var sequence = Set.GetCombatSequence(PlayerCombatAbilitySlot.HeavyCombo);
            Assert.That(service.GetUnlockedComboCount(type, PlayerCombatAbilitySlot.HeavyCombo, sequence), Is.EqualTo(5));
            foreach (string node in new[] { "Bokusei.Attack.SharpenedEdge", "Bokusei.Attack.KeenEye", "Bokusei.Attack.FlowingNinth", "Bokusei.Attack.HeavyFinisher" })
                Assert.That(service.TryTakeNode(type, node), Is.True, node);
            Assert.That(service.GetUnlockedComboCount(type, PlayerCombatAbilitySlot.HeavyCombo, sequence), Is.EqualTo(6));
            Assert.That(service.TryRespec(type), Is.True);
            Assert.That(service.GetUnlockedComboCount(type, PlayerCombatAbilitySlot.HeavyCombo, sequence), Is.EqualTo(5));
        }

        [Test]
        public void 교체공격은_서로_다른_클립과_전체_타격_및_후속분기를_가진다()
        {
            var mapping = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bokusei_KatanaAnimationSet.asset");
            var clips = new HashSet<UnityEngine.AnimationClip>();
            clips.Add(mapping.GetAbilityMotionAsset(new MotionKey("Katana.Light.00")).motionSet.motions[0].motionClip);
            var view = PlayerCombatAbilityDataView.Build(Set);
            foreach (string key in new[] { "Katana.Entry", "Katana.EntryGroggy", "Katana.EntryAirborne", "Katana.SwapEvade", "Katana.SwapSpecial" })
            {
                var part = mapping.GetAbilityMotionAsset(new MotionKey(key)).motionSet.motions.Single();
                Assert.That(clips.Add(part.motionClip), Is.True, key);
                var hits = part.events.OfType<BeginCollisionEvent>().ToArray();
                CollectionAssert.AreEquivalent(Enumerable.Range(0, key == "Katana.SwapSpecial" ? 4 : 1), hits.Select(h => h.hitPhaseIndex));
                var window = part.events.OfType<ComboWindowEvent>().Single();
                Assert.That(window.startTime, Is.GreaterThanOrEqualTo(hits.Max(h => h.endTime)));
                Assert.That(window.endTime, Is.InRange(window.startTime, part.Duration));
                foreach (var token in new[] { ComboInputToken.LightAttack, ComboInputToken.HeavyAttack })
                    Assert.That(ComboRouteResolver.Resolve(new[] { token }, view.comboRoutes, null, true,
                        context: new ComboRouteContext(new MotionKey(key))), Is.Not.Null, key);
            }
            Assert.That(mapping.GetAbilityMotionAsset(new MotionKey("Katana.Light.09")).motionSet.motions[0].motionClip,
                Is.Not.SameAs(mapping.GetAbilityMotionAsset(new MotionKey("Katana.Light.00")).motionSet.motions[0].motionClip));
        }

        [Test]
        public void 빈입력의_훈련힌트만_시작패턴을_표시한다()
        {
            var view = PlayerCombatAbilityDataView.Build(Set);
            var hints = new List<ComboRouteResolver.ComboRouteHint>();
            ComboRouteResolver.CollectHints(new ComboInputToken[0], view.comboRoutes, null, true, null, hints);
            Assert.That(hints, Is.Empty);
            ComboRouteResolver.CollectHints(new ComboInputToken[0], view.comboRoutes, null, true, null, hints, true);
            Assert.That(hints.Count, Is.EqualTo(12));
            Assert.That(hints.All(h => h.MatchedLength == 0), Is.True);
            ComboRouteResolver.CollectHints(new ComboInputToken[0], view.comboRoutes, null, false, null, hints, true);
            Assert.That(hints.Count, Is.EqualTo(2));
            Assert.That(hints.All(h => h.Route.groundCondition == RouteGroundCondition.Airborne), Is.True);
        }

        [TestCase(1, "Raon.ShortFinish")]
        [TestCase(2, "Raon.LinkedFinish")]
        [TestCase(3, "Raon.FullFinish")]
        public void 약공격_횟수에_맞는_강공격_분기를_선택한다(int lightCount, string expected)
        {
            var view = PlayerCombatAbilityDataView.Build(Set);
            var tokens = Enumerable.Repeat(ComboInputToken.LightAttack, lightCount).ToList();
            tokens.Add(ComboInputToken.HeavyAttack);
            var route = ComboRouteResolver.Resolve(tokens, view.comboRoutes, null, true);
            Assert.That(route?.routeName, Is.EqualTo(expected));
            Assert.That(ComboRouteResolver.Resolve(tokens, view.comboRoutes, null, true, _ => false), Is.Null);
        }

        [TestCase(ComboInputToken.LightAttack, "Raon.SkillLight")]
        [TestCase(ComboInputToken.HeavyAttack, "Raon.SkillHeavy")]
        public void 스킬에서_시작한_후속입력을_해석한다(ComboInputToken followup, string expected)
        {
            var view = PlayerCombatAbilityDataView.Build(Set);
            var route = ComboRouteResolver.Resolve(new[] { ComboInputToken.Skill1, followup }, view.comboRoutes, null, true);
            Assert.That(route?.routeName, Is.EqualTo(expected));
        }

        [Test]
        public void 공유_체인과_궁극기_및_성장_해금을_보존한다()
        {
            Assert.That(Set.baseSet, Is.Not.Null);
            foreach (PlayerCombatAbilitySlot slot in System.Enum.GetValues(typeof(PlayerCombatAbilitySlot)))
            {
                if (slot == PlayerCombatAbilitySlot.DashAttack)
                {
                    Assert.That(Set.GetCombatAbility(slot).abilityId, Is.EqualTo("Player.Raon.DashEntry"));
                    Assert.That(Set.GetCombatAbility(slot, 1), Is.SameAs(Set.baseSet.GetCombatAbility(slot, 1)));
                }
                else
                    CollectionAssert.AreEqual(Set.baseSet.GetCombatSequence(slot), Set.GetCombatSequence(slot));
            }
            Assert.That(Set.GetPlayerAbility(PlayerSkillSlot.Ultimate), Is.SameAs(Set.baseSet.GetPlayerAbility(PlayerSkillSlot.Ultimate)));
            var definition = AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>("Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Raon.asset");
            Assert.That(definition.abilitySet, Is.SameAs(Set));
            var tree = AssetDatabase.LoadAssetAtPath<CharacterSkillTreeSO>("Assets/10.Datas/Party/SkillTree/CharacterSkillTree_Raon.asset");
            string json = UnityEngine.JsonUtility.ToJson(tree);
            StringAssert.DoesNotContain("Player.Katana.Ability\"", json);
            StringAssert.Contains("Player.Raon.Ability", json);
            StringAssert.Contains("Bokusei.Special.StormEdge", json);
        }

        [Test]
        public void 확장분기는_지상공중을_구분하고_독립된_후속창을_갖는다()
        {
            var view = PlayerCombatAbilityDataView.Build(Set);
            var mapping = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bokusei_KatanaAnimationSet.asset");
            foreach (var definition in Set.comboRoutes.Where(r => !r.routeId.StartsWith("Raon.Flow.") && r.ability.abilityId.StartsWith("Player.Raon.") && r.inputPattern[0] != ComboInputToken.Skill1))
            {
                bool grounded = definition.groundCondition == RouteGroundCondition.Grounded;
                var route = ComboRouteResolver.Resolve(definition.inputPattern, view.comboRoutes, null, grounded);
                Assert.That(route?.routeName, Is.EqualTo(definition.routeId));
                var motion = mapping.GetAbilityMotionAsset(route.attackInfo.motionKey);
                Assert.That(motion, Is.Not.Null);
                var events = motion.motionSet.motions[0].events;
                var hits = events.OfType<BeginCollisionEvent>().ToArray();
                Assert.That(hits, Is.Not.Empty);
                Assert.That(hits.All(e => e.hitPhaseIndex < route.attackInfo.baseInfo.hitPhases.Count), Is.True);
                var window = events.OfType<ComboWindowEvent>().Single();
                Assert.That(window.startTime, Is.GreaterThanOrEqualTo(hits.Max(h => h.endTime)));
                Assert.That(window.endTime, Is.GreaterThan(window.startTime));
                Assert.That(window.endTime, Is.LessThanOrEqualTo(motion.motionSet.TotalDuration));
                if (definition.inputPattern.Last() == ComboInputToken.HeavyAttack)
                    Assert.That(definition.ability.cost.value, Is.GreaterThan(0));
            }
        }

        [Test]
        public void 현재_모션의_후속이_오래된_입력패턴보다_우선한다()
        {
            var view = PlayerCombatAbilityDataView.Build(Set);
            var route = ComboRouteResolver.Resolve(new[] { ComboInputToken.LightAttack, ComboInputToken.LightAttack, ComboInputToken.LightAttack, ComboInputToken.HeavyAttack },
                view.comboRoutes, null, true, context: new ComboRouteContext(new MotionKey("Katana.Light.01"), true));
            Assert.That(route?.attackInfo.motionKey.value, Is.EqualTo("Katana.Heavy.01"));
        }

        [Test]
        public void 적중과_헛치기_및_미해금은_서로_다른_후속을_선택한다()
        {
            var view = PlayerCombatAbilityDataView.Build(Set);
            var input = new[] { ComboInputToken.HeavyAttack };
            var hit = new ComboRouteContext(new MotionKey("Katana.Light.05"), true);
            var miss = new ComboRouteContext(new MotionKey("Katana.Light.05"));
            Assert.That(ComboRouteResolver.Resolve(input, view.comboRoutes, null, true, context: hit)?.attackInfo.motionKey.value,
                Is.EqualTo("Katana.Heavy.05"));
            Assert.That(ComboRouteResolver.Resolve(input, view.comboRoutes, null, true, context: miss)?.attackInfo.motionKey.value,
                Is.EqualTo("Katana.Heavy.00"));
            Assert.That(ComboRouteResolver.Resolve(input, view.comboRoutes, null, true,
                r => r.ability.abilityId != "Player.Katana.Heavy.05", hit)?.attackInfo.motionKey.value, Is.EqualTo("Katana.Heavy.00"));
            Assert.That(ComboRouteResolver.Resolve(input, view.comboRoutes, null, true, context: default), Is.Null);
            Assert.That(ComboRouteResolver.Resolve(input, view.comboRoutes, null, false, context: hit), Is.Null);
        }

        [Test]
        public void 연계로_기록을_소비한_뒤에도_현재공격의_후속힌트를_표시한다()
        {
            var view = PlayerCombatAbilityDataView.Build(Set);
            var hints = new List<ComboRouteResolver.ComboRouteHint>();
            ComboRouteResolver.CollectHints(new ComboInputToken[0], view.comboRoutes, null, true, null, hints,
                context: new ComboRouteContext(new MotionKey("Raon.CrossCut"), true));
            Assert.That(hints.Any(h => h.NextToken == ComboInputToken.LightAttack), Is.True);
            Assert.That(hints.Any(h => h.NextToken == ComboInputToken.HeavyAttack), Is.True);
            Assert.That(hints.All(h => h.Route.HasSourceCondition), Is.True);
        }

        [Test]
        public void 모든_약강_체인은_후속창과_짧은_분기진입을_가진다()
        {
            var mapping = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bokusei_KatanaAnimationSet.asset");
            var view = PlayerCombatAbilityDataView.Build(Set);
            foreach (var slot in new[] { PlayerCombatAbilitySlot.LightCombo, PlayerCombatAbilitySlot.HeavyCombo })
            {
                var sequence = Set.GetCombatSequence(slot);
                for (int i = 0; i < sequence.Count; i++)
                {
                    var ability = sequence[i];
                    var payload = (UPlayGroundMotionAbilityPayloadSO)ability.variants[0].executionPayload;
                    var motion = mapping.GetAbilityMotionAsset(payload.attackInfo.motionKey);
                    StringAssert.Contains("/Raon/", AssetDatabase.GetAssetPath(motion));
                    foreach (var part in motion.motionSet.motions)
                    {
                        var hits = part.events.OfType<BeginCollisionEvent>().ToArray();
                        var window = part.events.OfType<ComboWindowEvent>().Single();
                        Assert.That(window.startTime, Is.GreaterThanOrEqualTo(hits.Max(h => h.endTime)), ability.name);
                        Assert.That(window.endTime, Is.GreaterThan(window.startTime), ability.name);
                        Assert.That(window.endTime, Is.LessThanOrEqualTo(part.Duration), ability.name);
                        Assert.That(hits.All(h => h.hitPhaseIndex < payload.attackInfo.baseInfo.hitPhases.Count), Is.True);
                    }
                    if (slot == PlayerCombatAbilitySlot.LightCombo && i < 3) continue;
                    bool hasShortEntry = view.comboRoutes.Any(r => r.HasSourceCondition && r.ability == ability);
                    if (slot == PlayerCombatAbilitySlot.LightCombo && i > 0)
                        hasShortEntry |= view.comboRoutes.Any(r => r.HasSourceCondition && r.ability == sequence[i - 1]);
                    Assert.That(hasShortEntry, Is.True, ability.name);
                }
            }
        }

        [Test]
        public void 스킬_후속창은_마지막_타격_뒤에_열리고_원본_모션은_유지한다()
        {
            var skill = Set.GetPlayerAbility(PlayerSkillSlot.Ability);
            Assert.That(skill.variants.Count, Is.EqualTo(1));
            Assert.That(skill.cooldown.durationSeconds, Is.EqualTo(Set.baseSet.GetPlayerAbility(PlayerSkillSlot.Ability).cooldown.durationSeconds));
            var payload = (UPlayGroundMotionAbilityPayloadSO)skill.variants[0].executionPayload;
            var mapping = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bokusei_KatanaAnimationSet.asset");
            var motion = mapping.GetAbilityMotionAsset(payload.attackInfo.motionKey);
            var events = motion.motionSet.motions[0].events;
            float lastHit = events.OfType<BeginCollisionEvent>().Max(e => e.endTime);
            var window = events.OfType<ComboWindowEvent>().Single();
            Assert.That(window.startTime, Is.GreaterThanOrEqualTo(lastHit));
            Assert.That(window.endTime, Is.GreaterThan(window.startTime));
            Assert.That(Set.GetEffectiveComboLinkWindow(), Is.GreaterThan(motion.motionSet.TotalDuration));
            var original = mapping.GetAbilityMotionAsset(new MotionKey("Katana.Ability"));
            Assert.That(original, Is.Not.SameAs(motion));
            Assert.That(original.motionSet.motions[0].events.OfType<ComboWindowEvent>(), Is.Empty);
            foreach (var route in PlayerCombatAbilityDataView.Build(Set).comboRoutes)
                Assert.That(mapping.GetAbilityMotionAsset(route.attackInfo.motionKey), Is.Not.Null, route.routeName);
        }
    }
}
