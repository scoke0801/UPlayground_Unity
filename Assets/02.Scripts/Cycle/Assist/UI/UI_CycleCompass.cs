using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Cycle;
using UPlayGround.Data.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public sealed class UI_CycleCompass : MonoBehaviour
    {
        [SerializeField] private RectTransform _container;
        [SerializeField] private Image _iconPrefab;
        [SerializeField] private MinimapIconConfigSO _iconConfig;
        [SerializeField] private float _horizontalRange = 300f;
        private readonly Dictionary<string, Image> _icons = new();
        private Image _remains;

        private void OnEnable()
        {
            CycleBossMarkerRegistry.OnMarkerAdded += Add;
            CycleBossMarkerRegistry.OnMarkerChanged += Change;
            CycleBossMarkerRegistry.OnMarkerRemoved += Remove;
            CycleRemainsMarkerRegistry.OnMarkerChanged += SetRemains;
            CycleRemainsMarkerRegistry.OnMarkerRemoved += ClearRemains;
            foreach (CycleBossMarkerData marker in CycleBossMarkerRegistry.GetAll()) Add(marker);
            if (CycleRemainsMarkerRegistry.HasMarker) SetRemains(CycleRemainsMarkerRegistry.Position);
        }
        private void OnDisable()
        {
            CycleBossMarkerRegistry.OnMarkerAdded -= Add; CycleBossMarkerRegistry.OnMarkerChanged -= Change; CycleBossMarkerRegistry.OnMarkerRemoved -= Remove;
            CycleRemainsMarkerRegistry.OnMarkerChanged -= SetRemains; CycleRemainsMarkerRegistry.OnMarkerRemoved -= ClearRemains;
            foreach (Image image in _icons.Values) if (image != null) Destroy(image.gameObject); _icons.Clear(); ClearRemains();
        }
        private void Add(CycleBossMarkerData marker) { if (_iconPrefab == null || _container == null || _icons.ContainsKey(marker.spawnId)) return; Image icon = Instantiate(_iconPrefab, _container); _icons[marker.spawnId] = icon; Apply(icon, marker); }
        private void Change(CycleBossMarkerData marker) { if (!_icons.TryGetValue(marker.spawnId, out Image icon)) Add(marker); else Apply(icon, marker); }
        private void Remove(string id) { if (!_icons.TryGetValue(id, out Image icon)) return; _icons.Remove(id); if (icon != null) Destroy(icon.gameObject); }
        private void SetRemains(Vector3 _) { if (_remains == null && _iconPrefab != null) _remains = Instantiate(_iconPrefab, _container); if (_remains != null && _iconConfig != null) _remains.sprite = _iconConfig.remains.sprite; }
        private void ClearRemains() { if (_remains != null) Destroy(_remains.gameObject); _remains = null; }
        private void Update()
        {
            PlayerActor player = UISvc.Actors?.Player; if (player == null) return;
            foreach ((string id, Image icon) in _icons) if (icon != null && CycleBossMarkerRegistry.TryGet(id, out CycleBossMarkerData marker)) Position(icon.rectTransform, player.transform, marker.worldPosition);
            if (_remains != null && CycleRemainsMarkerRegistry.HasMarker) Position(_remains.rectTransform, player.transform, CycleRemainsMarkerRegistry.Position);
        }
        private void Apply(Image icon, CycleBossMarkerData marker) { if (_iconConfig == null) return; icon.sprite = (marker.discovered ? (marker.isCentral ? _iconConfig.discoveredCentralBoss : _iconConfig.discoveredOuterBoss) : _iconConfig.unknownBoss).sprite; }
        private void Position(RectTransform icon, Transform player, Vector3 world) { Vector3 direction = world - player.position; direction.y = 0f; float angle = Vector3.SignedAngle(player.forward, direction, Vector3.up); icon.anchoredPosition = new Vector2(Mathf.Clamp(angle / 90f, -1f, 1f) * _horizontalRange, 0f); }
    }
}
