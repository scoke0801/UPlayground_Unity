using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 타이틀 UI
    /// </summary>
    public class UI_TitleMenu : UI_Base
    {
        [Header("UI 버튼")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button optionButton;

        [Header("게임 흐름")]
        [Tooltip("새 게임 시작 씬을 읽어올 맵 설정 DB. DefaultStartMapId 를 사용한다.")]
        [SerializeField] private MapConfigDatabaseSO _mapConfigDB;

        // 새 게임 진입 시 띄운 캐릭터 선택 UI 인스턴스. 확정/취소 후 구독 해제에 사용.
        private UI_CharacterSelect _characterSelect;

        protected override void Awake()
        {
            base.Awake();

            if (continueButton)
            {
                continueButton.onClick.AddListener(OnClickContinueButton);
            }

            if (loadButton)
            {
                loadButton.onClick.AddListener(OnClickLoadButton);
            }
            if (newGameButton)
            {
                newGameButton.onClick.AddListener(OnClickNewGameButton);
            }
            if (optionButton)
            {
                optionButton.onClick.AddListener(OnClickOptionButton);
            }
        }

        private void OnClickContinueButton()
        {
            // 이어하기: 가장 최근 슬롯을 로드하고 저장된 씬으로 진입.
            // 저장이 없으면 새 게임으로 폴백.
            int recent = UISvc.Save.GetMostRecentSlot();

            if (recent >= 0)
            {
                UISvc.UI.HideAllUI();
                UISvc.Save.LoadGameToScene(recent);
            }
            else
            {
                // 시작 파티는 반드시 캐릭터 선택 결과로 구성한다.
                OnClickNewGameButton();
            }
        }

        private void OnClickLoadButton()
        {
            // 슬롯 선택 UI를 로드 모드로 띄운다. 슬롯 선택 시 저장된 씬으로 진입한다.
            var go = UISvc.UI.ShowUI(UI_SaveSlotMenu.UIKey);
            go?.GetComponent<UI_SaveSlotMenu>()?.SetMode(UI_SaveSlotMenu.SaveSlotMode.Load);
        }

        private void OnClickNewGameButton()
        {
            // 새 게임: 곧바로 시작하지 않고 캐릭터 선택 UI를 띄운다.
            // 캐릭터를 확정하면 CharacterConfirmed 이벤트로 실제 시작 흐름을 진행한다.
            // (취소 시 캐릭터 선택 UI만 닫히고 타이틀로 복귀.)
            var go = UISvc.UI.ShowUI(UI_CharacterSelect.UIKey);
            _characterSelect = go != null ? go.GetComponent<UI_CharacterSelect>() : null;
            if (_characterSelect == null)
            {
                Debug.LogError("[UI_TitleMenu] 캐릭터 선택 UI가 없어 새 게임을 시작할 수 없습니다.");
                return;
            }

            // 재사용되는 UI 인스턴스이므로 중복 구독을 방지한 뒤 확정/취소 이벤트를 연결한다.
            _characterSelect.CharacterConfirmed -= OnCharacterSelected;
            _characterSelect.CharacterConfirmed += OnCharacterSelected;
            _characterSelect.Cancelled -= OnCharacterSelectCancelled;
            _characterSelect.Cancelled += OnCharacterSelectCancelled;

            // 캐릭터 선택이 뜨는 동안 타이틀 메뉴는 숨긴다(겹침 방지). 취소 시 복귀.
            Hide();
        }

        /// <summary>
        /// 캐릭터 선택 화면에서 캐릭터를 확정했을 때. 선택 캐릭터로 새 게임을 시작한다.
        /// </summary>
        private void OnCharacterSelected(CharacterActorType selected)
        {
            UnsubscribeCharacterSelect();
            StartNewGame(selected);
        }

        /// <summary>
        /// 캐릭터 선택을 취소하고 뒤로 나왔을 때. 타이틀 메뉴를 다시 표시한다.
        /// </summary>
        private void OnCharacterSelectCancelled()
        {
            UnsubscribeCharacterSelect();
            Show();
        }

        private void UnsubscribeCharacterSelect()
        {
            if (_characterSelect != null)
            {
                _characterSelect.CharacterConfirmed -= OnCharacterSelected;
                _characterSelect.Cancelled -= OnCharacterSelectCancelled;
                _characterSelect = null;
            }
        }

        /// <summary>
        /// 새 게임 시작. 이전 세션의 진행 상태(처치 몬스터·레벨·플래그 등)가 누수되지 않도록
        /// 모든 ISaveable 매니저의 인메모리 상태를 초기화한 뒤 진입한다.
        /// </summary>
        private void StartNewGame(CharacterActorType selectedCharacter)
        {
            if (selectedCharacter == CharacterActorType.None)
            {
                Debug.LogError("[UI_TitleMenu] 시작 캐릭터가 선택되지 않아 새 게임을 중단합니다.");
                Show();
                return;
            }

            UISvc.Save.ResetForNewGame();
            UISvc.Party.PrepareNewGameStartingCharacter(selectedCharacter);
            UISvc.Cycle.RequestStartNewCycleOnNextWorld();
            UISvc.UI.HideAllUI();
            LoadStartScene();
        }

        /// <summary>
        /// 새 게임 시작 씬으로 진입한다. 씬 이름은 코드 상수가 아닌
        /// MapConfigDatabaseSO.DefaultStartMapId(데이터)에서 읽는다.
        /// </summary>
        private void LoadStartScene()
        {
            string startScene = _mapConfigDB != null ? _mapConfigDB.DefaultStartMapId : null;
            if (string.IsNullOrEmpty(startScene))
            {
                Debug.LogError("[UI_TitleMenu] 시작 씬이 지정되지 않았습니다. " +
                               "MapConfigDatabaseSO를 할당하고 DefaultStartMapId를 설정하세요.");
                return;
            }

            UISvc.Scene.LoadScene(startScene);
        }

        private void OnClickOptionButton()
        {
            UISvc.UI.ShowUI(UIKeyType.Config);
        }

        protected override void OnDispose()
        {
            // 씬 전환 등으로 파괴될 때 캐릭터 선택 UI 구독이 남지 않도록 정리.
            UnsubscribeCharacterSelect();
            base.OnDispose();
        }
    }
}
