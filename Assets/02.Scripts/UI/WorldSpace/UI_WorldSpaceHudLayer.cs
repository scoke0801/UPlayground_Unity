using System.Collections.Generic;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.UI;
using UPlayGround.Manager;
using UPlayGround.UI;

/// <summary>
/// 월드 액터의 머리 위 HP Bar + 데미지 플로터를
/// Screen Space Canvas 위에서 관리합니다.
/// Screen Space - Overlay Canvas에 부착합니다.
/// </summary>
public class UI_WorldSpaceHudLayer : MonoBehaviour
{
    private Canvas _parentCanvas;
    private Camera _mainCamera;

    private GameObject _hpBarPrefab;

    // ── 데미지 플로터 풀 ──────────────────────────────────────────────
    private GameObject               _floaterPrefab;
    private DamageFloaterConfigSO    _floaterConfig;
    private Queue<UI_DamageFloater>  _floaterPool = new Queue<UI_DamageFloater>();

    public void Init(Canvas parentCanvas)
    {
        _parentCanvas = parentCanvas;
        _mainCamera   = Camera.main;
    }

    // ── HP Bar ────────────────────────────────────────────────────────

    public void SetHpBarPrefab(GameObject hpBarPrefab)
    {
        _hpBarPrefab = hpBarPrefab;
    }

    public UI_ActorHpBar CreateHpBar(GameActor actor)
    {
        if (_hpBarPrefab == null) return null;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        var hpBar = Instantiate(_hpBarPrefab, transform)?.GetComponent<UI_ActorHpBar>();
        hpBar?.Init(actor, _mainCamera, _parentCanvas);
        return hpBar;
    }

    // ── 데미지 플로터 ─────────────────────────────────────────────────

    /// <summary>
    /// UIManager.Init() 흐름에서 HpBar 프리팹 세팅 직후에 호출
    /// </summary>
    public void SetupFloaterPool(GameObject floaterPrefab, DamageFloaterConfigSO config)
    {
        _floaterPrefab  = floaterPrefab;
        _floaterConfig  = config;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        for (int i = 0; i < config.initialPoolSize; i++)
            _floaterPool.Enqueue(CreateFloater());
    }

    public void ShowFloater(Vector3 worldPos, float damage, FloatStyle style)
    {
        string label = Mathf.RoundToInt(damage).ToString();
        GetFloaterFromPool().Play(worldPos, label, style);
    }

    public void ShowFloaterMiss(Vector3 worldPos)
    {
        GetFloaterFromPool().Play(worldPos, "MISS", FloatStyle.Miss);
    }

    public void ShowFloaterHeal(Vector3 worldPos, float amount)
    {
        string label = $"+{Mathf.RoundToInt(amount)}";
        GetFloaterFromPool().Play(worldPos, label, FloatStyle.Heal);
    }

    public void ReturnFloaterToPool(UI_DamageFloater floater)
    {
        _floaterPool.Enqueue(floater);
    }

    private UI_DamageFloater GetFloaterFromPool()
    {
        return _floaterPool.Count > 0 ? _floaterPool.Dequeue() : CreateFloater();
    }

    private UI_DamageFloater CreateFloater()
    {
        var go      = Instantiate(_floaterPrefab, transform);
        var floater = go.GetComponent<UI_DamageFloater>();
        floater.Init(_mainCamera, _parentCanvas, _floaterConfig, this);
        go.SetActive(false);
        return floater;
    }
}
