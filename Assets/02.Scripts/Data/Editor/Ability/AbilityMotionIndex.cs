using System.Collections.Generic;
using UnityEditor;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Data.Editor.Ability
{
    /// <summary>
    /// Motion Key를 액터 소유 MotionSetAsset으로 되짚는 역인덱스.
    /// GAS는 키만 들고 실제 모션은 ActorAnimationMotionSet이 소유하므로, 에디터 도구가
    /// 키 하나를 조회할 때마다 프로젝트 전체를 스캔하지 않도록 1회 로드해 재사용한다.
    ///
    /// 생성 이후의 에셋 변경은 반영하지 않는다. 한 번의 검증/스캔/그리기 작업 단위로
    /// 새로 만들어 쓰고 버린다.
    /// </summary>
    public sealed class AbilityMotionIndex
    {
        private readonly List<ActorAnimationMotionSet> _owners = new();
        private readonly Dictionary<MotionKey, List<MotionSetAsset>> _byKey =
            new();

        public AbilityMotionIndex()
        {
            foreach (string guid in AssetDatabase.FindAssets(
                         $"t:{nameof(ActorAnimationMotionSet)}"))
            {
                ActorAnimationMotionSet owner =
                    AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(
                        AssetDatabase.GUIDToAssetPath(guid));
                if (owner != null)
                    _owners.Add(owner);
            }
        }

        public IReadOnlyList<ActorAnimationMotionSet> Owners => _owners;

        /// <summary>키가 해석되는 서로 다른 모션 후보. 무기별로 다른 모션이면 여러 개가 된다.</summary>
        public IReadOnlyList<MotionSetAsset> Candidates(MotionKey key)
        {
            if (!key.IsValid)
                return System.Array.Empty<MotionSetAsset>();
            if (_byKey.TryGetValue(key, out List<MotionSetAsset> cached))
                return cached;

            var resolved = new List<MotionSetAsset>();
            for (int i = 0; i < _owners.Count; i++)
            {
                MotionSetAsset candidate = _owners[i].GetAbilityMotionAsset(key);
                if (candidate != null && !resolved.Contains(candidate))
                    resolved.Add(candidate);
            }
            _byKey[key] = resolved;
            return resolved;
        }

        /// <summary>후보가 여럿이면 액터/무기 컨텍스트 없이 고를 수 없으므로 모호로 본다.</summary>
        public bool IsAmbiguous(MotionKey key) => Candidates(key).Count > 1;

        /// <summary>단일 후보일 때만 해석한다. 모호하거나 없으면 null.</summary>
        public MotionSetAsset ResolveUnique(MotionKey key)
        {
            IReadOnlyList<MotionSetAsset> candidates = Candidates(key);
            return candidates.Count == 1 ? candidates[0] : null;
        }

        /// <summary>
        /// 액터/무기 범위를 특정할 수 없는 표시·분석용 대표 모션.
        /// 모호한 키도 첫 후보를 돌려주므로, 수치를 확정하는 경로에는 쓰지 않는다.
        /// </summary>
        public MotionSetAsset ResolveRepresentative(MotionKey key)
        {
            IReadOnlyList<MotionSetAsset> candidates = Candidates(key);
            return candidates.Count > 0 ? candidates[0] : null;
        }

        /// <summary>이 키가 프로젝트 어딘가에서 해당 모션으로 해석되는가.</summary>
        public bool Matches(MotionKey key, MotionSetAsset motionAsset)
        {
            if (motionAsset == null)
                return false;
            IReadOnlyList<MotionSetAsset> candidates = Candidates(key);
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i] == motionAsset)
                    return true;
            return false;
        }

        /// <summary>
        /// 해당 키를 fallback 상속이 아니라 자기 abilityMotions에 직접 가진 세트만 모은다.
        /// 매핑을 복제·수정하는 도구는 상속 매핑을 자식 세트로 평탄화하면 안 되므로 이 목록을 쓴다.
        /// </summary>
        public List<ActorAnimationMotionSet> FindDirectOwners(MotionKey key)
        {
            var result = new List<ActorAnimationMotionSet>();
            if (!key.IsValid)
                return result;
            for (int i = 0; i < _owners.Count; i++)
            {
                ActorAnimationMotionSet owner = _owners[i];
                if (owner.abilityMotions != null
                    && owner.abilityMotions.TryGetValue(key, out MotionSetAsset motion)
                    && motion != null)
                    result.Add(owner);
            }
            return result;
        }
    }
}
