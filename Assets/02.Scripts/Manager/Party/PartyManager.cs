using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 파티 캐릭터 교체 시스템 매니저 (단일 PlayerActor + Model 교체 방식).
    ///
    /// 교체 규칙
    /// - 쿨다운 중 입력 버퍼에 보관, 쿨다운 해제 즉시 실행
    /// - 대기 캐릭터 HP 0이면 교체 불가
    /// - Death / Grabbed 상태에서는 교체 불가
    /// - 교체 어시스트: PerfectDodgeWindow 중 교체 성공 시 incoming 캐릭터 공격 자동 발동
    /// </summary>
    public class PartyManager : BaseManager<PartyManager>, IManager
    {
        private PartyConfigSO          _config;
        private PlayerActor            _player;
        private List<CharacterActorType> _partyOrder = new();
        private int                    _activeIndex  = 0;
        private float                  _lastSwapTime = -999f;
        private bool                   _isSwapping   = false;

        [SerializeField] private float _swapCooldown = 0.5f;

        public event Action<PlayerActor, PlayerActor> OnSwapStarted;
        public event Action<PlayerActor>              OnSwapCompleted;
        public event Action<CharacterActorType>       OnCharacterUnlocked;

        public PlayerActor               ActiveCharacter     => _player;
        public CharacterActorType        ActiveCharacterType => _player?.GetComponent<PlayerSwapBehaviour>()?.ActiveCharacterType ?? CharacterActorType.None;
        public int                       ActiveIndex         => _activeIndex;
        public IReadOnlyList<CharacterActorType> PartyOrder  => _partyOrder;

        private const string AddressableKey = "PartyConfig";

        // ─── IManager 구현 ────────────────────────────────────────────────

        public void Init()
        {
            LoadConfigSO();
            RegisterSwapInputs();
        }

        private async void LoadConfigSO()
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<PartyConfigSO>(AddressableKey);
                _config = await handle.Task;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PartyManager] ConfigSO 로드 실패: {e.Message}");
            }
        }

        public void AfterInit()
        {
            BuildPartyFromScene();

            if (_player == null)
            {
                Debug.LogWarning("[PartyManager] 씬에 PlayerActor가 없습니다.");
                return;
            }

            if (_partyOrder.Count == 0)
            {
                Debug.LogWarning("[PartyManager] 파티 순서가 비어있습니다.");
                return;
            }

            InitializePartyStates();
            NotifyActivePlayerChanged();

            Debug.Log($"[PartyManager] 파티 구성 완료: {_partyOrder.Count}명, 활성={ActiveCharacterType}");
        }

        public void Dispose()
        {
            UnregisterSwapInputs();
            _partyOrder.Clear();
        }

        public void OnUpdate()
        {
            if (!CanSwap()) return;

            var buffer = InputManager.Instance?.InputBuffer;
            if (buffer == null) return;

            if (buffer.HasInput(PlayerAction.PlayerSwap))
            {
                buffer.ConsumeInput(PlayerAction.PlayerSwap);
                RequestSwapNext();
            }
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }

        public void OnSceneChanged(string sceneType)
        {
            BuildPartyFromScene();
            if (_player != null && _partyOrder.Count > 0)
            {
                InitializePartyStates();
                NotifyActivePlayerChanged();
            }
        }

        // ─── 교체 요청 ────────────────────────────────────────────────────

        public bool RequestSwapNext() => RequestSwapTo((_activeIndex + 1) % _partyOrder.Count);

        public bool RequestSwapTo(int targetIndex)
        {
            if (!CanSwap())                                            return false;
            if (targetIndex == _activeIndex)                           return false;
            if (targetIndex < 0 || targetIndex >= _partyOrder.Count)  return false;

            var targetType = _partyOrder[targetIndex];

            if (_player.GetHealthForCharacter(targetType) <= 0f)      return false;

            bool isAssist = _player.GetCombat()?.IsPerfectDodgeWindow == true;

            _isSwapping = true;
            OnSwapStarted?.Invoke(_player, _player);

            var swap = _player.GetComponent<PlayerSwapBehaviour>();
            if (swap == null || !swap.SwapTo(targetType))
            {
                _isSwapping = false;
                return false;
            }

            if (isAssist)
                _player.QueueSwapAssist();

            _activeIndex  = targetIndex;
            _lastSwapTime = Time.time;
            _isSwapping   = false;

            NotifyActivePlayerChanged();
            OnSwapCompleted?.Invoke(_player);

            Debug.Log($"[PartyManager] 교체 → {targetType}{(isAssist ? " [어시스트]" : "")}");
            return true;
        }

        /// <summary>
        /// 처치 보상으로 파티 슬롯을 개방한다.
        /// 이미 파티에 있거나 PlayerSwapBehaviour에 모델이 없으면 무시.
        /// </summary>
        public void UnlockCharacter(CharacterActorType type)
        {
            if (type == CharacterActorType.None)    return;
            if (_partyOrder.Contains(type))         return;

            var swap = _player?.GetComponent<PlayerSwapBehaviour>();
            if (swap == null || swap.GetModelData(type) == null)
            {
                Debug.LogWarning($"[PartyManager] UnlockCharacter: {type} 모델이 PlayerActor 하위에 없습니다.");
                return;
            }

            _partyOrder.Add(type);
            OnCharacterUnlocked?.Invoke(type);
            Debug.Log($"[PartyManager] {type} 파티 합류!");
        }

        public bool CanSwap()
        {
            if (_isSwapping)                                return false;
            if (Time.time - _lastSwapTime < _swapCooldown) return false;
            if (_partyOrder.Count < 2)                     return false;

            var state = _player?.PlayerController?.CurrentState?.StateName;
            if (state == "Death")   return false;
            if (state == "Grabbed") return false;

            return true;
        }

        // ─── 내부: 파티 구성 ──────────────────────────────────────────────

        private void BuildPartyFromScene()
        {
            _player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            _partyOrder.Clear();

            if (_config != null && _config.partyOrder.Count > 0)
            {
                _partyOrder.AddRange(_config.partyOrder);
                _activeIndex = Mathf.Clamp(_config.startActiveIndex, 0, _partyOrder.Count - 1);
                return;
            }

            // SO 없으면 PlayerSwapBehaviour에서 폴백
            var swap = _player?.GetComponent<PlayerSwapBehaviour>();
            if (swap != null)
            {
                _partyOrder = swap.GetAllCharacterTypes();
                _activeIndex = 0;
            }
        }

        private void InitializePartyStates()
        {
            var swap = _player?.GetComponent<PlayerSwapBehaviour>();
            if (swap == null)
            {
                Debug.LogWarning("[PartyManager] PlayerActor에 PlayerSwapBehaviour가 없습니다.");
                return;
            }

            var initialType = _partyOrder.Count > _activeIndex
                ? _partyOrder[_activeIndex]
                : _partyOrder[0];

            swap.InitializeTo(initialType);
        }

        private void NotifyActivePlayerChanged()
        {
            GameObjectManager.Instance?.SetActivePartyPlayer(_player);
            CameraManager.Instance?.SetTarget(_player?.transform);
        }

        // ─── 입력 등록 ────────────────────────────────────────────────────

        private void RegisterSwapInputs()
        {
            if (!InputManager.Instance) return;

            InputManager.Instance.RegisterInputEvent(
                InputMapNames.PlayerAction, PlayerAction.PlayerSwap,
                null, OnPlayerSwapPerformed, null,
                null, null, InputLayer.Level_0);
        }

        private void UnregisterSwapInputs()
        {
            if (!InputManager.Instance) return;

            InputManager.Instance.UnRegisterInputEvent(
                InputMapNames.PlayerAction, PlayerAction.PlayerSwap,
                null, OnPlayerSwapPerformed, null);
        }

        private void OnPlayerSwapPerformed(InputAction.CallbackContext ctx)
        {
            if (RequestSwapNext())
                InputManager.Instance?.InputBuffer.ConsumeInput(PlayerAction.PlayerSwap);
        }
    }
}
