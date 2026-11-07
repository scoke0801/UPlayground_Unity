using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Ability
{
    /// <summary>
    /// 태그 조회 구현(Data의 <see cref="IGameplayTagReader"/>, Core의 <see cref="IAbilityTagPort"/>)에
    /// 의존하지 않고 표현식을 평가하기 위한 단일 조회 계약.
    /// </summary>
    public interface IAbilityTagQuerySource
    {
        bool HasTag(GameplayTag tag, bool matchHierarchy);
    }

    /// <summary>
    /// <see cref="IGameplayTagReader"/> 어댑터. 표현식 평가마다 새로 만들지 않도록
    /// 소비자가 인스턴스를 캐싱해서 재사용한다. Bind로 조회 대상을 바꾸는 가변 객체이므로
    /// 같은 인스턴스를 중첩 평가에 동시에 사용하면 안 된다.
    /// </summary>
    public sealed class GameplayTagReaderQuerySource : IAbilityTagQuerySource
    {
        private IGameplayTagReader _reader;

        public GameplayTagReaderQuerySource(IGameplayTagReader reader = null)
        {
            _reader = reader;
        }

        public GameplayTagReaderQuerySource Bind(IGameplayTagReader reader)
        {
            _reader = reader;
            return this;
        }

        public bool HasTag(GameplayTag tag, bool matchHierarchy) =>
            _reader != null && _reader.HasTag(tag, matchHierarchy);
    }

    /// <summary>
    /// Core <see cref="IAbilityTagPort"/> 어댑터. 소비자가 인스턴스를 캐싱해서 재사용한다.
    /// Bind로 조회 대상을 바꾸는 가변 객체이므로 같은 인스턴스를 중첩 평가에 동시에 사용하면 안 된다.
    /// </summary>
    public sealed class AbilityTagPortQuerySource : IAbilityTagQuerySource
    {
        private IAbilityTagPort _port;

        public AbilityTagPortQuerySource(IAbilityTagPort port = null)
        {
            _port = port;
        }

        public AbilityTagPortQuerySource Bind(IAbilityTagPort port)
        {
            _port = port;
            return this;
        }

        public bool HasTag(GameplayTag tag, bool matchHierarchy)
        {
            if (_port == null || string.IsNullOrEmpty(tag.TagName))
                return false;
            return _port.Has(tag.TagName, matchHierarchy);
        }
    }

    public enum AbilityTagLeafMode
    {
        /// <summary>모든 태그를 보유해야 참이다. (AND)</summary>
        All,
        /// <summary>하나라도 보유하면 참이다. (OR)</summary>
        Any,
        /// <summary>하나도 보유하지 않아야 참이다. (NONE)</summary>
        None,
    }

    /// <summary>
    /// 중첩 가능한 태그 조건 표현식 노드. <c>[SerializeReference]</c>로 직렬화하므로
    /// 파생 타입을 다른 어셈블리로 옮길 때는 <c>[MovedFrom]</c>을 반드시 유지한다.
    /// </summary>
    [Serializable]
    public abstract class AbilityTagExpression
    {
        /// <summary>
        /// 순환·과도 중첩 방어용 최대 깊이. 초과하면 평가는 fail-closed로 false를 반환하고
        /// 정적 검증(AbilityDataValidator)이 Error로 보고한다.
        /// </summary>
        public const int MaxDepth = 8;

        public bool Evaluate(IAbilityTagQuerySource source)
        {
            if (source == null
                || !AbilityTagExpressionUtility.IsWithinDepth(this))
                return false;

            // 빈 표현식은 기존 평면 조건의 빈 목록과 같이 조건을 걸지 않는다.
            return !AbilityTagExpressionUtility.HasCondition(this)
                   || EvaluateCore(source, 0);
        }

        internal abstract bool EvaluateCore(IAbilityTagQuerySource source, int depth);

        /// <summary>에디터 목록·검증 메시지에 쓰는 한 줄 요약.</summary>
        public abstract string Describe();
    }

    [Serializable]
    public sealed class AbilityTagLeafExpression : AbilityTagExpression
    {
        public AbilityTagLeafMode mode = AbilityTagLeafMode.All;
        public AbilityTagMatchMode matchMode = AbilityTagMatchMode.Hierarchy;
        public List<GameplayTag> tags = new();

        internal override bool EvaluateCore(IAbilityTagQuerySource source, int depth)
        {
            if (!AbilityTagExpressionUtility.IsWithinDepth(depth) || source == null)
                return false;

            bool hierarchy = matchMode == AbilityTagMatchMode.Hierarchy;
            bool sawValid = false;
            bool sawHit = false;
            for (int i = 0; i < (tags?.Count ?? 0); i++)
            {
                GameplayTag tag = tags[i];
                if (!tag.IsValid())
                    continue;
                sawValid = true;
                bool has = source.HasTag(tag, hierarchy);
                switch (mode)
                {
                    case AbilityTagLeafMode.All when !has:
                        return false;
                    case AbilityTagLeafMode.None when has:
                        return false;
                    case AbilityTagLeafMode.Any when has:
                        sawHit = true;
                        break;
                }
            }
            // 유효 태그가 없는 노드는 루트/부모에서 조건 없는 노드로 건너뛴다.
            if (mode == AbilityTagLeafMode.Any)
                return !sawValid || sawHit;
            return true;
        }

        public override string Describe()
        {
            int count = 0;
            for (int i = 0; i < (tags?.Count ?? 0); i++)
                if (tags[i].IsValid())
                    count++;
            return $"{mode}({count})";
        }
    }

    /// <summary>자식 노드를 결합하는 논리 노드의 공통 베이스.</summary>
    [Serializable]
    public abstract class AbilityTagCompositeExpression : AbilityTagExpression
    {
        [SerializeReference] public List<AbilityTagExpression> children = new();

        protected int ChildCount => children?.Count ?? 0;
    }

    [Serializable]
    public sealed class AbilityTagAllExpression : AbilityTagCompositeExpression
    {
        internal override bool EvaluateCore(IAbilityTagQuerySource source, int depth)
        {
            if (!AbilityTagExpressionUtility.IsWithinDepth(depth) || source == null)
                return false;
            for (int i = 0; i < ChildCount; i++)
            {
                AbilityTagExpression child = children[i];
                if (child == null
                    || !AbilityTagExpressionUtility.HasCondition(child))
                    continue;
                if (!child.EvaluateCore(source, depth + 1))
                    return false;
            }
            return true;
        }

        public override string Describe() => $"All[{ChildCount}]";
    }

    [Serializable]
    public sealed class AbilityTagAnyExpression : AbilityTagCompositeExpression
    {
        internal override bool EvaluateCore(IAbilityTagQuerySource source, int depth)
        {
            if (!AbilityTagExpressionUtility.IsWithinDepth(depth) || source == null)
                return false;
            bool sawChild = false;
            for (int i = 0; i < ChildCount; i++)
            {
                AbilityTagExpression child = children[i];
                if (child == null
                    || !AbilityTagExpressionUtility.HasCondition(child))
                    continue;
                sawChild = true;
                if (child.EvaluateCore(source, depth + 1))
                    return true;
            }
            // 자식이 없는 Any는 조건을 걸지 않은 것으로 본다.
            return !sawChild;
        }

        public override string Describe() => $"Any[{ChildCount}]";
    }

    [Serializable]
    public sealed class AbilityTagNotExpression : AbilityTagExpression
    {
        [SerializeReference] public AbilityTagExpression child;

        internal override bool EvaluateCore(IAbilityTagQuerySource source, int depth)
        {
            if (!AbilityTagExpressionUtility.IsWithinDepth(depth) || source == null)
                return false;
            // 자식이 없거나 의미 있는 태그가 없는 Not은 조건을 걸지 않은 것으로 본다.
            return child == null
                   || !AbilityTagExpressionUtility.HasCondition(child)
                   || !child.EvaluateCore(source, depth + 1);
        }

        public override string Describe() => $"Not({child?.Describe() ?? "-"})";
    }

    public static class AbilityTagExpressionUtility
    {
        public static bool IsWithinDepth(int depth) =>
            depth <= AbilityTagExpression.MaxDepth;

        public static bool IsWithinDepth(AbilityTagExpression expression) =>
            MeasureDepth(expression) <= AbilityTagExpression.MaxDepth;

        /// <summary>
        /// null 또는 유효한 태그가 전혀 없는 정상 깊이 표현식만 빈 조건으로 본다.
        /// 깊이 초과 표현식은 런타임에서 실패해야 하므로 비어 있다고 판정하지 않는다.
        /// </summary>
        public static bool IsEffectivelyEmpty(AbilityTagExpression expression) =>
            expression == null
            || (IsWithinDepth(expression) && !HasCondition(expression));

        /// <summary>
        /// 표현식 트리의 최대 깊이를 센다. <see cref="AbilityTagExpression.MaxDepth"/>를
        /// 넘어서면 더 내려가지 않고 초과 값을 반환한다.
        /// </summary>
        public static int MeasureDepth(AbilityTagExpression expression, int depth = 0)
        {
            if (expression == null)
                return depth;
            if (!IsWithinDepth(depth))
                return depth;

            switch (expression)
            {
                case AbilityTagNotExpression not:
                    return MeasureDepth(not.child, depth + 1);
                case AbilityTagCompositeExpression composite:
                {
                    int max = depth;
                    for (int i = 0; i < (composite.children?.Count ?? 0); i++)
                    {
                        int childDepth = MeasureDepth(composite.children[i], depth + 1);
                        if (childDepth > max)
                            max = childDepth;
                    }
                    return max;
                }
                default:
                    return depth;
            }
        }

        /// <summary>
        /// 표현식 아래에 실제로 판정할 유효 태그가 하나라도 있는지 확인한다.
        /// 빈 노드는 부모 논리식에서 중립적인 no-op으로 취급한다.
        /// </summary>
        public static bool HasCondition(
            AbilityTagExpression expression,
            int depth = 0)
        {
            if (expression == null || !IsWithinDepth(depth))
                return false;

            switch (expression)
            {
                case AbilityTagLeafExpression leaf:
                    for (int i = 0; i < (leaf.tags?.Count ?? 0); i++)
                        if (leaf.tags[i].IsValid())
                            return true;
                    return false;
                case AbilityTagNotExpression not:
                    return HasCondition(not.child, depth + 1);
                case AbilityTagCompositeExpression composite:
                    for (int i = 0; i < (composite.children?.Count ?? 0); i++)
                        if (HasCondition(composite.children[i], depth + 1))
                            return true;
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>표현식 트리의 모든 태그 리스트를 순회한다. 검증 도구가 사용한다.</summary>
        public static void CollectTagLists(
            AbilityTagExpression expression,
            List<List<GameplayTag>> destination,
            int depth = 0)
        {
            if (expression == null
                || destination == null
                || !IsWithinDepth(depth))
                return;

            switch (expression)
            {
                case AbilityTagLeafExpression leaf:
                    if (leaf.tags != null)
                        destination.Add(leaf.tags);
                    break;
                case AbilityTagNotExpression not:
                    CollectTagLists(not.child, destination, depth + 1);
                    break;
                case AbilityTagCompositeExpression composite:
                    for (int i = 0; i < (composite.children?.Count ?? 0); i++)
                        CollectTagLists(composite.children[i], destination, depth + 1);
                    break;
            }
        }
    }
}
