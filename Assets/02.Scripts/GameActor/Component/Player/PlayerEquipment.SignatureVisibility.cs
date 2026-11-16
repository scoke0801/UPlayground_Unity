using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>무기 모델의 이름에 의존하지 않는 장착 외형 슬롯.</summary>
    public enum WeaponVisibilitySlot
    {
        Main,
        Sub
    }

    public partial class PlayerEquipment
    {
        private sealed class HiddenWeaponSlot
        {
            public Renderer[] renderers = Array.Empty<Renderer>();
            public bool[] visibleStates = Array.Empty<bool>();
            public int requestCount;

            public void Cache(GameObject root)
            {
                renderers = root != null ? root.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
                visibleStates = new bool[renderers.Length];
            }

            public void Hide()
            {
                if (requestCount++ > 0) return;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    visibleStates[i] = renderer.enabled;
                    renderer.enabled = false;
                }
            }

            public void Restore()
            {
                if (requestCount == 0 || --requestCount > 0) return;
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].enabled = visibleStates[i];
            }

            public void RestoreAll()
            {
                if (requestCount == 0) return;
                requestCount = 1;
                Restore();
            }
        }

        private static readonly Dictionary<GameObject, PlayerEquipment> SignatureReceivers = new();
        private readonly HiddenWeaponSlot _signatureMainSlot = new();
        private readonly HiddenWeaponSlot _signatureSubSlot = new();
        private readonly Dictionary<int, WeaponVisibilitySlot> _signatureVisibilityRequests = new(4);
        private int _signatureVisibilityToken;
        private PlayerActor _signatureVisibilityActor;
        private GameObject _signatureVisibilityModel;

        /// <summary>미리 등록한 액터/모델 대상으로 장비를 조회해 이벤트 발화 중 계층 탐색을 피한다.</summary>
        public static bool TryGetForMotionEvent(GameObject target, out PlayerEquipment equipment)
        {
            equipment = null;
            return target != null && SignatureReceivers.TryGetValue(target, out equipment) && equipment != null;
        }

        /// <summary>다른 숨김 요청과 겹쳐도 마지막 요청이 끝날 때만 원래 외형을 복구한다.</summary>
        public int HideWeaponSlot(WeaponVisibilitySlot slot)
        {
            if (slot != WeaponVisibilitySlot.Main && slot != WeaponVisibilitySlot.Sub) return 0;
            do { _signatureVisibilityToken = unchecked(_signatureVisibilityToken + 1); }
            while (_signatureVisibilityToken == 0 || _signatureVisibilityRequests.ContainsKey(_signatureVisibilityToken));
            _signatureVisibilityRequests.Add(_signatureVisibilityToken, slot);
            GetSignatureSlot(slot).Hide();
            return _signatureVisibilityToken;
        }

        /// <summary>해당 요청만 해제하며 교체 전 모델에서 남은 토큰은 무시한다.</summary>
        public void RestoreWeaponSlot(int token)
        {
            if (!_signatureVisibilityRequests.TryGetValue(token, out WeaponVisibilitySlot slot)) return;
            _signatureVisibilityRequests.Remove(token);
            GetSignatureSlot(slot).Restore();
        }

        /// <summary>모델 비활성화·교체·사망 시 모션 종료 여부와 무관하게 외형을 복구한다.</summary>
        public void RestoreHiddenWeaponSlots()
        {
            RestoreWeaponGripOffsets();
            _signatureVisibilityRequests.Clear();
            _signatureMainSlot.RestoreAll();
            _signatureSubSlot.RestoreAll();
        }

        private HiddenWeaponSlot GetSignatureSlot(WeaponVisibilitySlot slot)
            => slot == WeaponVisibilitySlot.Main ? _signatureMainSlot : _signatureSubSlot;

        private void CacheSignatureWeaponRenderers()
        {
            _signatureMainSlot.Cache(_mainWeaponConstraint != null
                ? _mainWeaponConstraint.gameObject : _currentMainWeaponObj);
            _signatureSubSlot.Cache(_subWeaponConstraint != null
                ? _subWeaponConstraint.gameObject : _currentSubWeaponObj);
        }

        private void RegisterSignatureVisibility()
        {
            UnregisterSignatureVisibility();
            if (!isActiveAndEnabled) return;
            _signatureVisibilityActor = GetComponentInParent<PlayerActor>();
            CharacterModelData model = GetComponentInParent<CharacterModelData>();
            _signatureVisibilityModel = model != null ? model.gameObject : gameObject;
            SignatureReceivers[gameObject] = this;
            SignatureReceivers[_signatureVisibilityModel] = this;
            if (_signatureVisibilityActor == null) return;
            SignatureReceivers[_signatureVisibilityActor.gameObject] = this;
            _signatureVisibilityActor.OnHpChanged += OnSignatureOwnerHealthChanged;
        }

        private void UnregisterSignatureVisibility()
        {
            RestoreHiddenWeaponSlots();
            RemoveSignatureReceiver(gameObject);
            RemoveSignatureReceiver(_signatureVisibilityModel);
            if (_signatureVisibilityActor != null)
            {
                RemoveSignatureReceiver(_signatureVisibilityActor.gameObject);
                _signatureVisibilityActor.OnHpChanged -= OnSignatureOwnerHealthChanged;
            }
            _signatureVisibilityActor = null;
            _signatureVisibilityModel = null;
        }

        private void RemoveSignatureReceiver(GameObject target)
        {
            if (target != null && SignatureReceivers.TryGetValue(target, out PlayerEquipment receiver) && receiver == this)
                SignatureReceivers.Remove(target);
        }

        private void OnSignatureOwnerHealthChanged(float health, float maxHealth)
        {
            if (health <= 0f) RestoreHiddenWeaponSlots();
        }
    }
}
