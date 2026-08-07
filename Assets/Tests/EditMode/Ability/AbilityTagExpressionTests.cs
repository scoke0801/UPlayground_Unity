using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Data.Ability;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Ability.Tests
{
    public sealed class AbilityTagExpressionTests
    {
        /// <summary>테스트용 태그 보유 스텁. 계층 일치를 실제 규칙대로 흉내낸다.</summary>
        private sealed class FakeTagReader : IGameplayTagReader
        {
            private readonly List<GameplayTag> _owned = new();

            public FakeTagReader With(params GameplayTag[] tags)
            {
                _owned.AddRange(tags);
                return this;
            }

            public bool HasTag(GameplayTag tag) => HasTag(tag, true);

            public bool HasTag(GameplayTag tag, bool matchHierarchy)
            {
                for (int i = 0; i < _owned.Count; i++)
                {
                    if (matchHierarchy
                        ? _owned[i].IsChildOf(tag)
                        : _owned[i].Equals(tag))
                        return true;
                }
                return false;
            }
        }

        private static AbilityTagLeafExpression Leaf(
            AbilityTagLeafMode mode,
            params GameplayTag[] tags)
        {
            return new AbilityTagLeafExpression
            {
                mode = mode,
                matchMode = AbilityTagMatchMode.Hierarchy,
                tags = new List<GameplayTag>(tags),
            };
        }

        [Test]
        public void 중첩표현식은_OR안의_AND를_평가한다()
        {
            // (Dash AND NOT Airborne) OR Guard
            var expression = new AbilityTagAnyExpression
            {
                children = new List<AbilityTagExpression>
                {
                    new AbilityTagAllExpression
                    {
                        children = new List<AbilityTagExpression>
                        {
                            Leaf(AbilityTagLeafMode.All, GameplayTags.State_Dash),
                            new AbilityTagNotExpression
                            {
                                child = Leaf(
                                    AbilityTagLeafMode.Any,
                                    GameplayTags.State_Airborne),
                            },
                        },
                    },
                    Leaf(AbilityTagLeafMode.All, GameplayTags.State_Combat_Guard),
                },
            };

            Assert.That(
                expression.Evaluate(new GameplayTagReaderQuerySource(
                    new FakeTagReader().With(GameplayTags.State_Dash))),
                Is.True,
                "Dash만 있으면 첫 번째 분기를 만족해야 한다.");
            Assert.That(
                expression.Evaluate(new GameplayTagReaderQuerySource(
                    new FakeTagReader().With(
                        GameplayTags.State_Dash,
                        GameplayTags.State_Airborne))),
                Is.False,
                "Airborne이 함께 있으면 Not이 첫 분기를 막고 Guard도 없어 실패해야 한다.");
            Assert.That(
                expression.Evaluate(new GameplayTagReaderQuerySource(
                    new FakeTagReader().With(
                        GameplayTags.State_Dash,
                        GameplayTags.State_Airborne,
                        GameplayTags.State_Combat_Guard))),
                Is.True,
                "두 번째 분기 Guard로 통과해야 한다.");
        }

        [Test]
        public void 계층일치_Leaf는_하위태그를_상위조건으로_인정한다()
        {
            AbilityTagLeafExpression leaf =
                Leaf(AbilityTagLeafMode.All, GameplayTags.State_Combat);
            var source = new GameplayTagReaderQuerySource(
                new FakeTagReader().With(GameplayTags.State_Combat_Attack));

            Assert.That(leaf.Evaluate(source), Is.True);

            leaf.matchMode = AbilityTagMatchMode.Exact;
            Assert.That(leaf.Evaluate(source), Is.False);
        }

        [Test]
        public void 평면조건과_중첩표현식은_AND로_결합된다()
        {
            var requirement = new AbilityTagRequirement
            {
                requireAll = new List<GameplayTag> { GameplayTags.State_Combat },
                expression = new AbilityTagNotExpression
                {
                    child = Leaf(AbilityTagLeafMode.Any, GameplayTags.State_Hit),
                },
            };

            Assert.That(
                AbilityTagRequirementEvaluator.Matches(
                    requirement,
                    new FakeTagReader().With(GameplayTags.State_Combat_Attack)),
                Is.True);
            Assert.That(
                AbilityTagRequirementEvaluator.Matches(
                    requirement,
                    new FakeTagReader().With(
                        GameplayTags.State_Combat_Attack,
                        GameplayTags.State_Hit)),
                Is.False,
                "중첩 Not 조건이 실패하면 평면 조건을 만족해도 전체가 실패해야 한다.");
            Assert.That(
                AbilityTagRequirementEvaluator.Matches(
                    requirement,
                    new FakeTagReader().With(GameplayTags.State_Hit)),
                Is.False,
                "평면 조건이 실패하면 전체가 실패해야 한다.");
        }

        [Test]
        public void 표현식이없는_기존Requirement는_동작이_바뀌지않는다()
        {
            var requirement = new AbilityTagRequirement
            {
                requireAll = new List<GameplayTag> { GameplayTags.State_Combat },
            };

            Assert.That(requirement.expression, Is.Null);
            Assert.That(
                AbilityTagRequirementEvaluator.Matches(
                    requirement,
                    new FakeTagReader().With(GameplayTags.State_Combat_Attack)),
                Is.True);
        }

        [Test]
        public void 깊이초과_표현식은_failclosed로_거부된다()
        {
            AbilityTagExpression node =
                Leaf(AbilityTagLeafMode.All, GameplayTags.State_Combat);
            // MaxDepth를 확실히 넘기도록 중첩한다.
            for (int i = 0; i <= AbilityTagExpression.MaxDepth; i++)
            {
                node = new AbilityTagAllExpression
                {
                    children = new List<AbilityTagExpression> { node },
                };
            }

            Assert.That(
                AbilityTagExpressionUtility.MeasureDepth(node),
                Is.GreaterThan(AbilityTagExpression.MaxDepth));
            Assert.That(
                node.Evaluate(new GameplayTagReaderQuerySource(
                    new FakeTagReader().With(GameplayTags.State_Combat))),
                Is.False,
                "깊이를 초과하면 조건을 통과시키지 않아야 한다.");
        }

        [Test]
        public void 깊이초과가_Not으로_감싸져도_failclosed로_거부된다()
        {
            AbilityTagExpression node =
                Leaf(AbilityTagLeafMode.All, GameplayTags.State_Combat);
            for (int i = 0; i <= AbilityTagExpression.MaxDepth; i++)
                node = new AbilityTagNotExpression { child = node };

            Assert.That(
                node.Evaluate(new GameplayTagReaderQuerySource(
                    new FakeTagReader().With(GameplayTags.State_Combat))),
                Is.False,
                "깊이 오류가 Not의 논리 부정으로 다시 true가 되면 안 된다.");
        }

        [Test]
        public void 빈노드는_조건을_걸지않은것으로_본다()
        {
            var reader = new FakeTagReader();
            var source = new GameplayTagReaderQuerySource(reader);

            Assert.That(new AbilityTagAllExpression().Evaluate(source), Is.True);
            Assert.That(new AbilityTagAnyExpression().Evaluate(source), Is.True);
            Assert.That(new AbilityTagNotExpression().Evaluate(source), Is.True);
            Assert.That(Leaf(AbilityTagLeafMode.Any).Evaluate(source), Is.True);
            Assert.That(
                new AbilityTagNotExpression
                {
                    child = Leaf(AbilityTagLeafMode.Any),
                }.Evaluate(source),
                Is.True,
                "빈 Leaf를 감싼 Not도 조건을 걸지 않아야 한다.");

            Assert.That(
                new AbilityTagAnyExpression
                {
                    children = new List<AbilityTagExpression>
                    {
                        Leaf(AbilityTagLeafMode.Any),
                        Leaf(AbilityTagLeafMode.All, GameplayTags.State_Dash),
                    },
                }.Evaluate(source),
                Is.False,
                "Any 안의 빈 자식이 유효한 다른 조건을 항상 참으로 만들면 안 된다.");

            var emptyRequirement = new AbilityTagRequirement
            {
                expression = new AbilityTagAnyExpression(),
            };
            Assert.That(emptyRequirement.IsEmpty, Is.True);
            Assert.That(
                AbilityTagRequirementEvaluator.Matches(emptyRequirement, null),
                Is.True,
                "빈 표현식만 있는 Requirement는 태그 소스가 없어도 조건을 걸지 않아야 한다.");
        }

        [Test]
        public void 캐시된_QuerySource는_재바인딩되어_재사용된다()
        {
            var requirement = new AbilityTagRequirement
            {
                expression = Leaf(AbilityTagLeafMode.All, GameplayTags.State_Dash),
            };
            var cached = new GameplayTagReaderQuerySource();

            Assert.That(
                AbilityTagRequirementEvaluator.Matches(
                    requirement,
                    new FakeTagReader().With(GameplayTags.State_Dash),
                    cached),
                Is.True);
            Assert.That(
                AbilityTagRequirementEvaluator.Matches(
                    requirement,
                    new FakeTagReader().With(GameplayTags.State_Jump),
                    cached),
                Is.False);
        }
    }
}
