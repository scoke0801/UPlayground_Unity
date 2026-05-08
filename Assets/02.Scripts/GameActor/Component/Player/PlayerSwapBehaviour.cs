using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.Component
{
    /// <summary>
    /// 단일 PlayerActor 하위의 Model 서브루트들을 관리한다.
    /// 교체 시 이전 Model을 비활성화하고 새 Model을 활성화한 뒤
    /// PlayerActor.RefreshForCharacter()를 호출해 컴포넌트들을 일괄 갱신한다.
    /// </summary>
    public class PlayerSwapBehaviour : MonoBehaviour
    {
        private PlayerActor _playerActor;

        private List<CharacterModelData> _models = new();
        private CharacterModelData       _activeModel;

        public CharacterActorType ActiveCharacterType =>
            _activeModel?.characterType ?? CharacterActorType.None;

        private void Awake()
        {
            _playerActor = GetComponent<PlayerActor>();
            _models.AddRange(GetComponentsInChildren<CharacterModelData>(includeInactive: true));

            if (_models.Count == 0)
                Debug.LogWarning($"[PlayerSwapBehaviour] {name}: CharacterModelData를 찾을 수 없습니다.");
        }

        /// <summary>
        /// PartyManager.AfterInit에서 호출. 첫 번째 파티 구성으로 초기화한다.
        /// </summary>
        public void InitializeTo(CharacterActorType type)
        {
            foreach (var m in _models)
                m.gameObject.SetActive(false);

            var target = _models.Find(m => m.characterType == type);
            if (target == null && _models.Count > 0)
            {
                Debug.LogWarning($"[PlayerSwapBehaviour] CharacterType={type} 모델 없음. 첫 번째로 대체.");
                target = _models[0];
            }

            if (target == null) return;

            _activeModel = target;
            _activeModel.gameObject.SetActive(true);
            _playerActor.RefreshForCharacter(_activeModel);
        }

        /// <summary>
        /// 지정한 캐릭터 타입으로 교체한다.
        /// </summary>
        public bool SwapTo(CharacterActorType type)
        {
            if (_activeModel?.characterType == type) return false;

            var target = _models.Find(m => m.characterType == type);
            if (target == null)
            {
                Debug.LogWarning($"[PlayerSwapBehaviour] CharacterType={type} 모델 없음.");
                return false;
            }

            ActorAnimator.MotionPlaybackSnapshot animationSnapshot =
                _playerActor?.Animator?.CaptureMovementPlaybackSnapshot()
                ?? ActorAnimator.MotionPlaybackSnapshot.Empty;

            _activeModel?.gameObject.SetActive(false);
            _activeModel = target;
            _activeModel.gameObject.SetActive(true);
            _playerActor.RefreshForCharacter(_activeModel, animationSnapshot);
            return true;
        }

        /// <summary>
        /// 보유한 모든 캐릭터 타입 목록을 반환한다.
        /// </summary>
        public List<CharacterActorType> GetAllCharacterTypes()
        {
            var types = new List<CharacterActorType>(_models.Count);
            foreach (var m in _models)
                types.Add(m.characterType);
            return types;
        }

        public CharacterModelData GetModelData(CharacterActorType type)
            => _models.Find(m => m.characterType == type);
    }
}
