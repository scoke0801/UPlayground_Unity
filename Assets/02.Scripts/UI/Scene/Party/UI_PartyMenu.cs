using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// 파티원 선택 / 편성 화면.
/// - 스왑 모드(기본): 출전 슬롯 클릭 시 즉시 활성 캐릭터 교체.
/// - 편성 모드: 출전 슬롯 / 후보 슬롯을 조작해 BattleOrder 를 변경. 즉시 반영.
/// 자세한 규칙: docs/party-formation-system.md
/// </summary>
public class UI_PartyMenu : UI_Base
{
    [Serializable]
    private class CharacterPreview
    {
        public RawImage _previewImage;
        public UICharacterPreviewRenderer _previewRenderer;
    }
    
    [Header("Character Preview")]
    [SerializeField] private List<CharacterPreview> _characterPreviews;
    
    protected override void Awake()
    {
        base.Awake();

        foreach (var preview in _characterPreviews)
        {
            preview._previewImage.enabled = true;
            preview._previewImage.texture = preview._previewRenderer.GetRenderTexture();
        }
    }
    protected override void OnShow()
    {
        InputManager.Instance.SetInputLayer(_layer.ToInputLayer());
        
        foreach (var preview in _characterPreviews)
        {
            preview._previewRenderer.ShowPreview();
        }
        
    }

    protected override void OnHide()
    {
        InputManager.Instance.SetInputLayer(InputLayer.None);

        // 캐릭터 프리뷰 비활성화
        foreach (var preview in _characterPreviews)
        {
            preview._previewRenderer.HidePreview();
        }
    }
    
    public override bool PerformBackFunction()
    {
        // ESC 키 입력 시 닫는다.
        Hide();
        return false;
    }
}
