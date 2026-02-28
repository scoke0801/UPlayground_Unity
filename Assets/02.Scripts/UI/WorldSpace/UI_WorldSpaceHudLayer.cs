using System;
using UnityEngine;
using UPlayGround;
using UPlayGround.Manager;

/// <summary>
/// 월드 액터의 머리 위 HP Bar를 Screen Space Canvas 위에서 관리합니다.
/// Screen Space - Overlay Canvas에 부착합니다.
/// </summary>
public class UI_WorldSpaceHudLayer : MonoBehaviour
{
    private Canvas _parentCanvas;
    private Camera _mainCamera;
    
    // Addressable 키로 프리팹 로드
    private GameObject _hpBarPrefab;
    
    public void Init(Canvas parentCanvas)
    {
        _parentCanvas = parentCanvas;
        _mainCamera = Camera.main;
    }

    public void SetHpBarPrefab(GameObject hpBarPrefab)
    {
        _hpBarPrefab = hpBarPrefab;
    }
    
    public UI_ActorHpBar CreateHpBar(GameActor actor)
    {
        if (_hpBarPrefab == null) return null;
        
        if(_mainCamera == null) 
            _mainCamera = Camera.main;
        var hpBar = Instantiate(_hpBarPrefab, transform)?.GetComponent<UI_ActorHpBar>();
        hpBar?.Init(actor, _mainCamera, _parentCanvas);
        return hpBar;
    }
}