using System.Collections;
using UnityEngine;
using UPlayGround.Manager;
using UPlayGround.Components;

namespace UPlayGround.Debugging
{
    /// <summary>시험장 액터를 서비스 준비 후 활성화하고 훈련 표적의 AI를 정지한다.</summary>
    public sealed class CameraTestMapRuntime : MonoBehaviour
    {
        [SerializeField] private SceneContext _sceneContext;
        [SerializeField] private GameObject _player;
        [SerializeField] private MonsterActor[] _targets;

        private IEnumerator Start()
        {
            // 씬 조립 계층에서 부팅을 시작한다. 비활성 액터의 Awake가 서비스 조회보다 앞서지 않게 한다.
            GameManager game = GameManager.Instance;
            while (game.BootState != GameBootState.Ready && game.BootState != GameBootState.Failed)
                yield return null;
            if (game.BootState == GameBootState.Failed)
            {
                Debug.LogError("[CameraTestMap] 게임 서비스 초기화 실패: " + game.InitializationFailure, this);
                yield break;
            }
            _player.SetActive(true);
            // Swap.Awake는 등록한 모델을 잠시 끈다. 이동 Start 이전에 선택해 Animancer까지 준비한다.
            CharacterModelData model = _player.GetComponentInChildren<CharacterModelData>(true);
            PlayerSwapBehaviour swap = _player.GetComponent<PlayerSwapBehaviour>();
            if (model == null || swap == null || !swap.InitializeTo(model.characterType))
            {
                Debug.LogError("[CameraTestMap] 시작 캐릭터 모델을 초기화하지 못했습니다.", this);
                yield break;
            }
            foreach (MonsterActor target in _targets)
                target.gameObject.SetActive(true);
            // MonsterActor.Start의 AI 활성화 정규화가 끝난 뒤 기존 Freeze 계약을 사용한다.
            yield return null;
            foreach (MonsterActor target in _targets)
                target.GroundAIController?.Freeze();
            _sceneContext.enabled = true;
        }
    }
}
