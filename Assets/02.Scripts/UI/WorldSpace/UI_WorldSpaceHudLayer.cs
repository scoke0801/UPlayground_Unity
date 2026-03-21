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

    private GameObject              _floaterPrefab;
    private DamageFloaterConfigSO   _floaterConfig;
    private Queue<UI_DamageFloater> _floaterPool = new Queue<UI_DamageFloater>();

    // 풀 준비 완료 여부 — 미준비 상태에서 호출 시 조용히 스킵
    private bool _isPoolReady = false;

    public void Init(Canvas parentCanvas)
    {
        _parentCanvas = parentCanvas;
        _mainCamera   = Camera.main;
    }

    // ── HP Bar ────────────────────────────────────────────────────────

    public void SetHpBarPrefab(GameObject hpBarPrefab) => _hpBarPrefab = hpBarPrefab;

    public UI_ActorHpBar CreateHpBar(GameActor actor)
    {
        if (_hpBarPrefab == null) return null;
        if (_mainCamera  == null) _mainCamera = Camera.main;

        var hpBar = Instantiate(_hpBarPrefab, transform)?.GetComponent<UI_ActorHpBar>();
        hpBar?.Init(actor, _mainCamera, _parentCanvas);
        return hpBar;
    }

    // ── 데미지 플로터 ─────────────────────────────────────────────────

    public void SetupFloaterPool(GameObject floaterPrefab, DamageFloaterConfigSO config)
    {
        _floaterPrefab = floaterPrefab;
        _floaterConfig = config;

        if (_mainCamera == null) _mainCamera = Camera.main;

        for (int i = 0; i < config.initialPoolSize; i++)
            _floaterPool.Enqueue(CreateFloater());

        _isPoolReady = true;
    }

    public void ShowFloater(Vector3 worldPos, float damage, FloatStyle style)
    {
        if (!_isPoolReady) return;
        GetFloaterFromPool().Play(worldPos, Mathf.RoundToInt(damage).ToString(), style);
    }

    public void ShowFloaterMiss(Vector3 worldPos)
    {
        if (!_isPoolReady) return;
        GetFloaterFromPool().Play(worldPos, "MISS", FloatStyle.Miss);
    }

    /// <param name="style">Heal 또는 MonsterHeal — 호출자가 구분해서 전달</param>
    public void ShowFloaterHeal(Vector3 worldPos, float amount, FloatStyle style = FloatStyle.Heal)
    {
        if (!_isPoolReady) return;
        GetFloaterFromPool().Play(worldPos, $"+{Mathf.RoundToInt(amount)}", style);
    }

    public void ReturnFloaterToPool(UI_DamageFloater floater) => _floaterPool.Enqueue(floater);

    private UI_DamageFloater GetFloaterFromPool() =>
        _floaterPool.Count > 0 ? _floaterPool.Dequeue() : CreateFloater();

    private UI_DamageFloater CreateFloater()
    {
        var go      = Instantiate(_floaterPrefab, transform);
        var floater = go.GetComponent<UI_DamageFloater>();
        floater.Init(_mainCamera, _parentCanvas, _floaterConfig, this);
        go.SetActive(false);
        return floater;
    }
}
