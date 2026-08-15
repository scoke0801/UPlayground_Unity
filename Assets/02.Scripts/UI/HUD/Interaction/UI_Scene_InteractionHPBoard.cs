using System.Collections;
 using TMPro;
 using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UPlayGround.Manager;
using UPlayGround.Data.Actor;

namespace UPlayGround.UI
{
    public class UI_Scene_InteractionHPBoard : UI_Base
    {
        [SerializeField] private Image _boardHpFill;
        [SerializeField] private Image _boardHpWhiteFill;
        [SerializeField] private TextMeshProUGUI _textName;
        [SerializeField] private TextMeshProUGUI _textDesc;

        [SerializeField] private float _fillTimeScale = 5.0f;

        private Coroutine _fillCoroutine;

        #region UI_Base
        protected override void OnShow()
        {
            _boardHpFill.fillAmount = 1.0f;
            _boardHpWhiteFill.fillAmount = 1.0f;

            AnimationChange("On");
        }

        protected override void OnHide()
        {
            AnimationChange("Out");
        }

        protected override void OnClose()
        {
        }
        #endregion

        public void BoardFill(float hp, float maxHp)
        {
            _boardHpFill.fillAmount = hp / maxHp;
            if (_fillCoroutine != null)
            {
                StopCoroutine(_fillCoroutine);
            }

            _fillCoroutine = StartCoroutine(FillCoroutine());
        }

        public void SetInteractionData(InteractableActorSO data)
        {
            _textName.text = data.actorName;
            _textDesc.text = data.description;
        }

        private IEnumerator FillCoroutine()
        {
            while (_boardHpWhiteFill.fillAmount > _boardHpFill.fillAmount)
            {
                _boardHpWhiteFill.fillAmount = Mathf.Lerp(_boardHpWhiteFill.fillAmount,
                    _boardHpFill.fillAmount, Time.deltaTime * _fillTimeScale);

                yield return null;
            }
        }

        private void OnInteractionOut()
        {
            AnimationChange("Out");
        }
    }
}
