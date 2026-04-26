using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UPlayGround.Component;
using UPlayGround.Data.Party;
using UPlayGround.Dialogue;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 파티 캐릭터 교체 시스템 매니저.
    /// 씬 참조 없이 Resources/Data/PartyConfig.asset 의 SO 데이터와
    /// FindObjectsByType 으로 파티를 구성한다.
    ///
    /// 교체 규칙
    /// - 쿨다운 중에도 입력 버퍼에 보관했다가 쿨다운 해제 즉시 실행
    /// - 대기 캐릭터 HP 0 이면 교체 불가 (부활 없음)
    /// - Death / Grabbed 상태에서는 교체 불가
    /// - 교체 어시스트: PerfectDodgeWindow 중 교체 성공 시 incoming 캐릭터 공격 자동 발동
    /// </summary>
    public class PartyManager : BaseManager<PartyManager>, IManager
    {
        private PartyConfigSO     _config;
        private List<PlayerActor> _partyMembers = new();
        private int               _activeIndex  = 0;
        private float             _lastSwapTime = -999f;
        private bool              _isSwapping   = false;

        [SerializeField] private float _swapCooldown = 0.5f;

        public event Action<PlayerActor, PlayerActor> OnSwapStarted;
        public event Action<PlayerActor>              OnSwapCompleted;

        public PlayerActor                ActiveCharacter => _partyMembers.Count > 0 ? _partyMembers[_activeIndex] : null;
        public int                        ActiveIndex     => _activeIndex;
        public IReadOnlyList<PlayerActor> PartyMembers    => _partyMembers;

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

                try
                {
                    _config = await handle.Task;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PartyManager] ConfigSO 로드 실패: {e.Message}");
                }
            }
            catch (Exception e)
            {
                throw; // TODO 예외 처리
            }
        }
        public void AfterInit()
        {
            BuildPartyFromScene();

            if (_partyMembers.Count == 0)
            {
                Debug.LogWarning("[PartyManager] 파티에 포함할 PlayerActor가 씬에 없습니다.");
                return;
            }

            InitializePartyStates();
            NotifyActivePlayerChanged();

            Debug.Log($"[PartyManager] 파티 구성 완료: {_partyMembers.Count}명, 활성={ActiveCharacter?.name}");
        }

        public void Dispose()
        {
            UnregisterSwapInputs();
            _partyMembers.Clear();
        }

        /// <summary>
        /// 매 프레임: 쿨다운 중 눌렸던 교체 입력을 버퍼에서 재시도한다.
        /// </summary>
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
            if (_partyMembers.Count > 0)
            {
                InitializePartyStates();
                NotifyActivePlayerChanged();
            }
        }

        // ─── 교체 요청 ────────────────────────────────────────────────────

        public bool RequestSwapNext() => RequestSwapTo((_activeIndex + 1) % _partyMembers.Count);
       
        public bool RequestSwapTo(int targetIndex)
        {
            if (!CanSwap())                                             return false;
            if (targetIndex == _activeIndex)                            return false;
            if (targetIndex < 0 || targetIndex >= _partyMembers.Count) return false;

            var incoming = _partyMembers[targetIndex];
            if (incoming == null)                    return false;
            if (incoming.CurrentHealth <= 0f)        return false;  // HP 0: 부활 없음, 교체 불가

            var outgoing = _partyMembers[_activeIndex];

            // 어시스트 조건: 교체 시점에 PerfectDodgeWindow 가 열려 있으면 카운터 발동
            bool isAssist = outgoing.GetCombat()?.IsPerfectDodgeWindow == true;

            _isSwapping = true;
            OnSwapStarted?.Invoke(outgoing, incoming);

            Vector3    pos = outgoing.transform.position;
            Quaternion rot = outgoing.transform.rotation;

            outgoing.GetComponent<PlayerSwapBehaviour>()?.EnterStandby();
            incoming.GetComponent<PlayerSwapBehaviour>()?.EnterActive(pos, rot);

            if (isAssist)
                incoming.QueueSwapAssist();

            _activeIndex  = targetIndex;
            _lastSwapTime = Time.time;
            _isSwapping   = false;

            NotifyActivePlayerChanged();
            OnSwapCompleted?.Invoke(incoming);

            Debug.Log($"[PartyManager] 교체: {outgoing.name} → {incoming.name}{(isAssist ? " [어시스트]" : "")}");
            return true;
        }

        public bool CanSwap()
        {
            if (_isSwapping)                                return false;
            if (Time.time - _lastSwapTime < _swapCooldown) return false;
            if (_partyMembers.Count < 2)                   return false;

            var state = ActiveCharacter?.PlayerController?.CurrentState?.StateName;
            if (state == "Death")   return false;
            if (state == "Grabbed") return false;

            return true;
        }

        // ─── 내부: 파티 구성 ──────────────────────────────────────────────

        private void BuildPartyFromScene()
        {
            var allActors = UnityEngine.Object.FindObjectsByType<PlayerActor>(FindObjectsSortMode.None);
            _partyMembers.Clear();

            if (_config != null && _config.partyOrder.Count > 0)
            {
                foreach (var type in _config.partyOrder)
                {
                    PlayerActor found = null;
                    foreach (var actor in allActors)
                    {
                        if (actor.CharacterType == type) { found = actor; break; }
                    }

                    if (found != null)
                        _partyMembers.Add(found);
                    else
                        Debug.LogWarning($"[PartyManager] CharacterType={type} 인 PlayerActor를 씬에서 찾을 수 없습니다.");
                }

                _activeIndex = Mathf.Clamp(_config.startActiveIndex, 0, Mathf.Max(0, _partyMembers.Count - 1));
            }
            else
            {
                _partyMembers.AddRange(allActors);
                _activeIndex = 0;
            }
        }

        private void InitializePartyStates()
        {
            for (int i = 0; i < _partyMembers.Count; i++)
            {
                var member = _partyMembers[i];
                if (member == null) continue;

                var swap = member.GetComponent<PlayerSwapBehaviour>();
                if (swap == null)
                {
                    Debug.LogWarning($"[PartyManager] {member.name} 에 PlayerSwapBehaviour 가 없습니다.");
                    continue;
                }

                if (i == _activeIndex)
                    swap.EnterActive(member.transform.position, member.transform.rotation);
                else
                    swap.EnterStandby();
            }
        }

        private void NotifyActivePlayerChanged()
        {
            GameObjectManager.Instance?.SetActivePartyPlayer(ActiveCharacter);
            CameraManager.Instance?.SetTarget(ActiveCharacter?.transform);
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
            // 즉시 실행 시도: 성공하면 버퍼에서 제거, 실패하면 OnUpdate()에서 재시도
            if (RequestSwapNext())
                InputManager.Instance?.InputBuffer.ConsumeInput(PlayerAction.PlayerSwap);
        }
    }
}
