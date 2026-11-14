using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UPlayGround.Data.Pet;

namespace UPlayGround.UI
{
    public sealed partial class UI_Scene_PetMenu
    {
        [SerializeField] private RectTransform[] _skillSlots = System.Array.Empty<RectTransform>();
        [SerializeField] private UISkillTreeCurve[] _skillPaths = System.Array.Empty<UISkillTreeCurve>();
        [SerializeField] private RectTransform[] _skillHubs = System.Array.Empty<RectTransform>();
        [SerializeField] private Material _skillFlowMaterial;
        [SerializeField, Min(1f)] private float _skillPathWidth = 12f;
        [SerializeField] private Vector2 _skillNodeConnectionOffset = new(0f, 100f);
        private Material _runtimeSkillFlow;
        private Tween _skillFlowClock;
        private static readonly int SkillFlowTime = Shader.PropertyToID("_UnscaledTime");

        private void StartSkillFlow()
        {
            StopSkillFlow();
            Canvas.ForceUpdateCanvases();
            if (_runtimeSkillFlow == null && _skillFlowMaterial != null)
                _runtimeSkillFlow = new Material(_skillFlowMaterial);
            if (_runtimeSkillFlow == null) return;
            for (int i = 0; i < _skillPaths.Length && i < _skillSlots.Length; i++)
            {
                var hub = _skillHubs[i < 2 ? 0 : 1];
                var path = _skillPaths[i];
                path.material = _runtimeSkillFlow;
                path.color = new Color(.55f, .83f, .75f);
                path.Configure((Vector2)path.rectTransform.InverseTransformPoint(hub.position) + Vector2.down * 28f,
                    (Vector2)path.rectTransform.InverseTransformPoint(_skillSlots[i].position) + _skillNodeConnectionOffset,
                    _skillPathWidth, .5f, 40, true, Vector2.down);
            }
            _runtimeSkillFlow.SetFloat(SkillFlowTime, Time.unscaledTime);
            _skillFlowClock = DOTween.To(() => 0f,
                    _ => _runtimeSkillFlow.SetFloat(SkillFlowTime, Time.unscaledTime), 1f, 1f)
                .SetLoops(-1).SetUpdate(true);
        }

        private void StopSkillFlow()
        {
            _skillFlowClock?.Kill();
            _skillFlowClock = null;
        }

        private void RefreshSkillNode(Button button, string id, int level)
        {
            var rank = button.transform.Find("Rank")?.GetComponent<TextMeshProUGUI>();
            if (rank == null) return;
            var save = _pets.GetSnapshot();
            bool equipped = id == PetSkillCatalogSO.Pickup ? save.isAutoPickupEnabled
                : id == PetSkillCatalogSO.PickupRange ? save.isAutoPickupEnabled : save.SelectedSupportId == id;
            rank.text = $"Lv.{level} / 5" + (equipped ? "   적용 중" : "");
            rank.color = equipped ? new Color(.55f, .85f, .7f) : new Color(.62f, .7f, .78f);
            var frame = button.transform.Find("NodeFrame")?.GetComponent<Image>();
            if (frame != null) frame.color = id == _selectedSkillId ? new Color(.95f, .77f, .4f)
                : equipped ? new Color(.55f, .85f, .7f) : new Color(.5f, .6f, .66f);
        }

        private void ConfigureSkillGraphNavigation(Selectable action)
        {
            for (int i = 0; i < _skillButtons.Count; i++)
            {
                var button = _skillButtons[i];
                var nav = button.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnLeft = FindSkillNeighbor(i, Vector2.left) ?? button;
                nav.selectOnRight = FindSkillNeighbor(i, Vector2.right) ?? action;
                nav.selectOnUp = FindSkillNeighbor(i, Vector2.up) ?? _tabs.GetComponentInChildren<Button>();
                nav.selectOnDown = FindSkillNeighbor(i, Vector2.down) ?? _toggle;
                button.navigation = nav;
            }
        }

        private Selectable FindSkillNeighbor(int index, Vector2 direction)
        {
            if (index >= _skillSlots.Length) return null;
            Selectable nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < _skillButtons.Count && i < _skillSlots.Length; i++)
            {
                if (i == index) continue;
                Vector2 offset = _skillSlots[i].position - _skillSlots[index].position;
                float forward = Vector2.Dot(offset, direction);
                if (forward <= 0f) continue;
                float score = offset.sqrMagnitude / forward;
                if (score >= best) continue;
                best = score; nearest = _skillButtons[i];
            }
            return nearest;
        }
    }
}
