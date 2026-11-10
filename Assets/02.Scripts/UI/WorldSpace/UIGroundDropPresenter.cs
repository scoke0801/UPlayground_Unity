using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Item;
using UPlayGround.Manager;
using UPlayGround.InputDefine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.UI
{
    /// <summary>기존 월드 HUD 틱에서 이름·가림·포인터·패드 아이템 포커스를 관리한다.</summary>
    public sealed class UIGroundDropPresenter : IDisposable, IWorldPointerInputService
    {
        private sealed class Entry
        {
            public GroundDropView Drop;
            public UIItemDropLabel Label;
            public bool Visible;
        }
        private readonly Dictionary<string, Entry> _entries = new();
        private readonly List<string> _removed = new();
        private readonly List<Rect> _occupied = new();
        private readonly Transform _root;
        private readonly RectTransform _canvas;
        private Camera _camera;
        private IGroundDropService _drops;
        private IInputService _input;
        private string _focused;
        private float _nextRefresh;
        private bool _enabled;

        public UIGroundDropPresenter(Transform root, RectTransform canvas, Camera camera)
        {
            _root = root; _canvas = canvas; _camera = camera;
            Services.Register(this);
        }
        public void ManagedLateTick()
        {
            if (_drops == null)
            {
                if (!Services.TryGet(out _drops)) return;
                _drops.Collected += RemoveCollected;
            }
            if (_input == null) Services.TryGet(out _input);
            _enabled = CanPresent();
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.25f;
                if (Services.TryGet<ICameraViewService>(out var cameras)) _camera = cameras.GetMainCamera();
                Synchronize();
            }
            _occupied.Clear();
            foreach (var entry in _entries.Values) UpdateLabel(entry);
            if (_focused == null || !_entries.TryGetValue(_focused, out var focused) || !focused.Visible
                || !_drops.CanPlayerCollect(_focused))
                FocusNearest();
            foreach (var pair in _entries) pair.Value.Label.SetFocused(pair.Key == _focused && _input?.ActiveDevice == ActiveInputDevice.Gamepad);
        }
        private bool CanPresent() => _drops?.Settings?.labelPrefab != null && _camera != null
            && _input != null && _input.CurrentLayer == InputLayer.Level_0
            && !_input.IsPlayerActionInputSuppressed && Time.timeScale > 0f
            && Svc.Dialogue?.IsDialogueActive != true && _root.gameObject.activeInHierarchy;

        private void Synchronize()
        {
            var drops = _drops.VisibleDrops;
            _removed.Clear();
            foreach (var pair in _entries)
            {
                bool exists = false;
                for (int i = 0; i < drops.Count; i++) if (drops[i].Id == pair.Key) { exists = true; break; }
                if (!exists) _removed.Add(pair.Key);
            }
            foreach (string id in _removed)
            { UnityEngine.Object.Destroy(_entries[id].Label.gameObject); _entries.Remove(id); }
            var settings = _drops.Settings;
            if (settings?.labelPrefab == null) return;
            for (int i = 0; i < drops.Count; i++)
            {
                GroundDropView drop = drops[i];
                if (_entries.ContainsKey(drop.Id) || drop.Transform == null || drop.Item == null) continue;
                if (ActorSvc.Objects?.Player == null || (drop.Transform.position - ActorSvc.Objects.Player.transform.position).sqrMagnitude
                    > settings.labelDistance * settings.labelDistance) continue;
                var view = UnityEngine.Object.Instantiate(settings.labelPrefab, _root).GetComponent<UIItemDropLabel>();
                string captured = drop.Id;
                view.Initialize(drop.Count > 1 ? $"{drop.Item.itemName} ×{drop.Count}" : drop.Item.itemName,
                    drop.Item.icon, drop.Item.itemRarity.ToColor(), settings.labelFadeDuration,
                    () => _drops.TryPlayerCollect(captured));
                _entries.Add(drop.Id, new Entry { Drop = drop, Label = view });
            }
        }
        private void UpdateLabel(Entry entry)
        {
            var transform = entry.Drop.Transform;
            var player = ActorSvc.Objects?.Player;
            bool visible = _enabled && transform != null && transform.gameObject.activeInHierarchy && player != null;
            Vector3 screen = default;
            if (visible)
            {
                Vector3 point = transform.position + Vector3.up * _drops.Settings.labelHeight;
                screen = _camera.WorldToScreenPoint(point);
                visible = screen.z > 0f && screen.x > 0f && screen.x < Screen.width && screen.y > 0f && screen.y < Screen.height
                    && (transform.position - player.transform.position).sqrMagnitude <= _drops.Settings.labelDistance * _drops.Settings.labelDistance
                    && !Physics.Linecast(_camera.transform.position, point, _drops.Settings.visibilityMask, QueryTriggerInteraction.Ignore);
            }
            entry.Visible = visible;
            entry.Label.gameObject.SetActive(visible);
            if (!visible) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screen, null, out var position);
            Vector2 size = entry.Label.Rect.sizeDelta;
            var rect = new Rect(position - size * 0.5f, size);
            for (int attempt = 0; attempt <= _occupied.Count; attempt++)
            {
                bool overlap = false;
                for (int i = 0; i < _occupied.Count; i++)
                    if (rect.Overlaps(_occupied[i])) { overlap = true; break; }
                if (!overlap) break;
                position.y += size.y; rect.position = position - size * 0.5f;
            }
            entry.Label.Rect.anchoredPosition = position;
            _occupied.Add(rect);
        }
        private void FocusNearest()
        {
            _focused = null;
            var player = ActorSvc.Objects?.Player;
            if (player == null) return;
            float nearest = float.MaxValue;
            foreach (var pair in _entries)
            {
                var entry = pair.Value;
                if (!entry.Visible || !_drops.CanPlayerCollect(pair.Key)) continue;
                float distance = (entry.Drop.Transform.position - player.transform.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance; _focused = pair.Key;
            }
        }
        public bool TryConsumeDirection(Vector2 direction, bool performed)
        {
            if (!CanPresent() || _input?.ActiveDevice != ActiveInputDevice.Gamepad || _focused == null
                || !_entries.TryGetValue(_focused, out var current) || !current.Visible
                || !_drops.CanPlayerCollect(_focused)) return false;
            if (Services.TryGet<ICombatEncounterService>(out var encounter)
                && encounter.CurrentEncounter != UPlayGround.Data.Combat.CombatEncounterKind.Explore) return false;
            if (!performed || direction.sqrMagnitude < 0.5f) return true;
            string next = null; float nearest = float.MaxValue;
            foreach (var pair in _entries)
            {
                if (pair.Key == _focused || !pair.Value.Visible || !_drops.CanPlayerCollect(pair.Key)) continue;
                Vector2 delta = pair.Value.Label.Rect.anchoredPosition - current.Label.Rect.anchoredPosition;
                if (Vector2.Dot(delta.normalized, direction.normalized) < 0.5f || delta.sqrMagnitude >= nearest) continue;
                next = pair.Key; nearest = delta.sqrMagnitude;
            }
            if (next != null) _focused = next;
            return true;
        }
        public bool TryConsume(Vector2 position, bool performed)
        {
            if (!CanPresent()) return false;
            foreach (var entry in _entries.Values)
            {
                if (!entry.Visible || !RectTransformUtility.RectangleContainsScreenPoint(entry.Label.Rect, position)) continue;
                if (performed) _drops.TryPlayerCollect(entry.Drop.Id);
                return true;
            }
            if (!Physics.Raycast(_camera.ScreenPointToRay(position), out var hit, _drops.Settings.labelDistance,
                ~0, QueryTriggerInteraction.Collide)) return false;
            var drops = _drops.VisibleDrops;
            for (int i = 0; i < drops.Count; i++)
            {
                if (drops[i].Transform == null || !hit.transform.IsChildOf(drops[i].Transform)) continue;
                if (performed) _drops.TryPlayerCollect(drops[i].Id);
                return true;
            }
            return false;
        }
        public bool TryConsumeInteraction(bool performed, bool preferNearestItem = false)
        {
            if (!CanPresent()) return false;
            if (preferNearestItem) FocusNearest();
            if ((!preferNearestItem && _input.ActiveDevice != ActiveInputDevice.Gamepad) || _focused == null
                || !_entries.TryGetValue(_focused, out var entry) || !entry.Visible || !_drops.CanPlayerCollect(_focused)) return false;
            var target = ActorSvc.Objects?.InteractionHandler?.CurrentClosestInteractable;
            if (!preferNearestItem && target != null && target is not DropItemActor) return false;
            if (performed) _drops.TryPlayerCollect(_focused);
            return true;
        }
        public void HideAll()
        {
            _enabled = false;
            foreach (var entry in _entries.Values) { entry.Visible = false; entry.Label.gameObject.SetActive(false); }
        }
        public void Dispose()
        {
            Services.Unregister(this);
            if (_drops != null) _drops.Collected -= RemoveCollected;
            foreach (var entry in _entries.Values) if (entry.Label != null) UnityEngine.Object.Destroy(entry.Label.gameObject);
            _entries.Clear();
        }
        private void RemoveCollected(string id)
        {
            if (!_entries.Remove(id, out var entry)) return;
            entry.Label.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(entry.Label.gameObject);
            if (_focused == id) _focused = null;
        }
    }
}
