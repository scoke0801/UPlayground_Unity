using System;
using System.Collections.Generic;

/// <summary>
/// 씬에 배치된 <see cref="MinimapMarkerRegistrar"/>를 런타임에 추적하는 정적 레지스트리.
/// 씬 전환 시 오브젝트가 파괴되면 OnDestroy에서 자동 해제됩니다.
/// </summary>
public static class MinimapMarkerRegistry
{
    private static readonly Dictionary<string, MinimapMarkerRegistrar> _map = new();

    public static event Action<MinimapMarkerRegistrar> OnMarkerAdded;
    public static event Action<MinimapMarkerRegistrar> OnMarkerRemoved;

    public static void Register(MinimapMarkerRegistrar registrar)
    {
        if (string.IsNullOrEmpty(registrar.LocationId)) return;
        _map[registrar.LocationId] = registrar;
        OnMarkerAdded?.Invoke(registrar);
    }

    public static void Unregister(MinimapMarkerRegistrar registrar)
    {
        if (string.IsNullOrEmpty(registrar.LocationId)) return;
        if (_map.TryGetValue(registrar.LocationId, out var stored) && stored == registrar)
        {
            _map.Remove(registrar.LocationId);
            OnMarkerRemoved?.Invoke(registrar);
        }
    }

    public static bool TryGet(string locationId, out MinimapMarkerRegistrar registrar)
        => _map.TryGetValue(locationId, out registrar);

    public static IEnumerable<MinimapMarkerRegistrar> GetAll() => _map.Values;
}
