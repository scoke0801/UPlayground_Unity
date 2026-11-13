using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Data.Save;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>사전 파손 형상과 안전한 방향성 낙하를 적용한다. Update와 임의 메시 절단을 사용하지 않는다.</summary>
    public sealed class ReactiveEnvironmentDestruction : MonoBehaviour
    {
        [SerializeField] private Transform _fallPivot;
        [SerializeField] private GameObject _blockedFallback;
        [SerializeField] private Collider[] _shelterColliders;
        [SerializeField] private NavMeshObstacle[] _standingObstacles;
        [SerializeField] private NavMeshObstacle[] _remnantObstacles;
        private readonly Collider[] _overlaps = new Collider[32];
        private Quaternion _initialLocalRotation;
        private bool[] _shelterEnabled, _standingEnabled, _remnantEnabled;
        private bool _hasInitialized;
        private bool _isBroken;
        private EnvironmentDestructionSaveState _state;
        private EnvironmentDestructionRecipeSO _recipe;
        private Vector3 _impactDirection;

        /// <summary>소유 표현의 원래 상태를 한 번만 보관한다.</summary>
        public void Initialize()
        {
            if (_hasInitialized) return;
            _hasInitialized = true;
            if (_fallPivot != null) _initialLocalRotation = _fallPivot.localRotation;
            _shelterColliders ??= System.Array.Empty<Collider>();
            _standingObstacles ??= System.Array.Empty<NavMeshObstacle>();
            _remnantObstacles ??= System.Array.Empty<NavMeshObstacle>();
            _shelterEnabled = new bool[_shelterColliders.Length];
            _standingEnabled = new bool[_standingObstacles.Length];
            _remnantEnabled = new bool[_remnantObstacles.Length];
            for (int i = 0; i < _shelterColliders.Length; i++) _shelterEnabled[i] = _shelterColliders[i] != null && _shelterColliders[i].enabled;
            for (int i = 0; i < _standingObstacles.Length; i++) _standingEnabled[i] = _standingObstacles[i] != null && _standingObstacles[i].enabled;
            for (int i = 0; i < _remnantObstacles.Length; i++) _remnantEnabled[i] = _remnantObstacles[i] != null && _remnantObstacles[i].enabled;
        }

        /// <summary>직접 타격의 수평 방향을 낙하 후보로 기록한다.</summary>
        public void SetImpactDirection(Vector3 direction) => _impactDirection = Vector3.ProjectOnPlane(direction, Vector3.up);

        /// <summary>형상·충돌·지붕·길찾기 결과는 같은 논리 전이에 맞춰 적용한다.</summary>
        public void Apply(EnvironmentDestructionRecipeSO recipe, EnvironmentDestructionSaveState state, bool broken)
        {
            Initialize();
            bool changed = _isBroken != broken || !ReferenceEquals(_state, state);
            _state = state; _recipe = recipe; _isBroken = broken;
            if (recipe == null) return;
            if (broken && !state.initialized)
            {
                state.initialized = true;
                state.direction = new SerializableVector3(ChooseFallDirection(recipe));
                state.usedFallback = recipe.mode == EnvironmentDestructionMode.DirectionalFall && state.direction.ToVector3().sqrMagnitude < 0.5f;
                state.isFalling = recipe.mode == EnvironmentDestructionMode.DirectionalFall && !state.usedFallback;
            }
            if (changed)
            {
                for (int i = 0; i < _shelterColliders.Length; i++)
                    if (_shelterColliders[i] != null) _shelterColliders[i].enabled = !broken && _shelterEnabled[i];
                for (int i = 0; i < _standingObstacles.Length; i++)
                    if (_standingObstacles[i] != null) _standingObstacles[i].enabled = !broken && _standingEnabled[i];
                for (int i = 0; i < _remnantObstacles.Length; i++)
                    if (_remnantObstacles[i] != null) _remnantObstacles[i].enabled = broken && _remnantEnabled[i];
            }
            ApplyPose();
        }

        /// <summary>물리 스텝에서 각도 substep과 충돌을 확인하며 중요 잔해를 안전하게 정착시킨다.</summary>
        public void FixedTick(float deltaTime)
        {
            if (!_isBroken || _state == null || !_state.isFalling || _recipe == null || _fallPivot == null) return;
            float next = Mathf.Min(1, _state.progress + deltaTime / _recipe.fallSeconds);
            int samples = Mathf.Max(1, Mathf.CeilToInt((next - _state.progress) * _recipe.fallAngle / _recipe.sweepStepDegrees));
            float start = _state.progress;
            for (int i = 1; i <= samples; i++)
            {
                float progress = Mathf.Lerp(start, next, (float)i / samples);
                if (IsBlocked(_state.direction.ToVector3(), progress))
                {
                    _state.isFalling = false;
                    // 움직이는 액터를 콜라이더로 밀거나 관통시키지 않고 직전의 안전 포즈에서 멈춘다.
                    break;
                }
                _state.progress = progress;
            }
            if (_state.progress >= 1) _state.isFalling = false;
            ApplyPose();
        }

        /// <summary>방향성 레시피에 필요한 저작 결과와 소유 계층을 확인한다.</summary>
        public bool IsValid(EnvironmentDestructionRecipeSO recipe)
        {
            Initialize();
            if (recipe == null) return true;
            if (!recipe.IsValid) return false;
            if (recipe.mode == EnvironmentDestructionMode.DirectionalFall
                && (_fallPivot == null || _blockedFallback == null || !_fallPivot.IsChildOf(transform)
                    || _fallPivot == transform || !_blockedFallback.transform.IsChildOf(transform)
                    || _blockedFallback == gameObject)) return false;
            foreach (var collider in _shelterColliders)
                if (collider == null || !collider.transform.IsChildOf(transform)) return false;
            foreach (var obstacle in _standingObstacles)
                if (obstacle == null || !obstacle.transform.IsChildOf(transform)) return false;
            foreach (var obstacle in _remnantObstacles)
                if (obstacle == null || !obstacle.transform.IsChildOf(transform)) return false;
            return true;
        }

        private Vector3 ChooseFallDirection(EnvironmentDestructionRecipeSO recipe)
        {
            if (recipe.mode != EnvironmentDestructionMode.DirectionalFall) return Vector3.zero;
            Vector3 direction = _impactDirection.sqrMagnitude > 0.001f ? _impactDirection.normalized : transform.forward;
            // 충격 방향과 측면을 검사해 길막/액터 겹침이 없는 저작 궤적만 선택한다.
            for (int candidate = 0; candidate < 5; candidate++)
            {
                float yaw = candidate == 0 ? 0 : candidate == 1 ? 45 : candidate == 2 ? -45 : candidate == 3 ? 90 : -90;
                Vector3 option = Quaternion.AngleAxis(yaw, Vector3.up) * direction;
                bool blocked = false;
                int samples = Mathf.CeilToInt(recipe.fallAngle / recipe.sweepStepDegrees);
                for (int step = 0; step <= samples; step++)
                    if (IsBlocked(option, (float)step / samples)) { blocked = true; break; }
                if (!blocked) return option;
            }
            return Vector3.zero;
        }

        private bool IsBlocked(Vector3 direction, float progress)
        {
            if (_fallPivot == null || _recipe == null) return false;
            Vector3 axis = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 up = Quaternion.AngleAxis(_recipe.fallAngle * progress, axis) * Vector3.up;
            Vector3 start = _fallPivot.position + Vector3.up * (_recipe.trunkRadius * 1.1f);
            Vector3 end = _fallPivot.position + up * _recipe.trunkLength;
            int count = Physics.OverlapCapsuleNonAlloc(start, end, _recipe.trunkRadius, _overlaps,
                _recipe.fallBlockerLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) if (!_overlaps[i].transform.IsChildOf(transform)) return true;
            return count == _overlaps.Length;
        }

        private void ApplyPose()
        {
            if (_fallPivot == null) return;
            bool fallback = _isBroken && _state.usedFallback;
            _fallPivot.gameObject.SetActive(_isBroken && !fallback);
            if (_blockedFallback != null) _blockedFallback.SetActive(fallback);
            if (!_isBroken) { _fallPivot.localRotation = _initialLocalRotation; return; }
            Vector3 axis = Vector3.Cross(Vector3.up, _state.direction.ToVector3()).normalized;
            Vector3 localAxis = _fallPivot.parent.InverseTransformDirection(axis);
            _fallPivot.localRotation = Quaternion.AngleAxis(_recipe.fallAngle * _state.progress, localAxis) * _initialLocalRotation;
        }
    }
}
