using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Crafting;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// 제작(크래프팅) UI — Popup 레이어
///
/// 레이아웃:
///   왼쪽 패널  : 카테고리 탭 + 레시피 리스트 (스크롤)
///   오른쪽 패널: 레시피 상세 (결과 아이콘/이름/설명 + 재료 목록 + 비용/시간)
///   하단 패널  : 수량 스텝퍼 + 제작 버튼 + 진행 바
///
/// UIPrefabDatabase 키: "Crafting"
/// 호출 예: UIManager.Instance.ShowUI("Crafting");
/// </summary>
public class UI_CraftMenu : UI_Base
{
    #region UI_Base 생명주기

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnShow()
    {
        InputManager.Instance.SetInputLayer(_layer.ToInputLayer());
    }

    protected override void OnHide()
    {
        InputManager.Instance.SetInputLayer(InputLayer.None);

      //  RecipeManager.Instance.OnRecipeUnlocked    -= OnRecipeUnlocked;
      //  RecipeManager.Instance.OnCraftingStarted   -= OnCraftingStarted;
      //  RecipeManager.Instance.OnCraftingCompleted -= OnCraftingCompleted;
      //  RecipeManager.Instance.OnCraftingCancelled -= OnCraftingCancelled;
    }

    protected override void OnDispose()
    {
        // 혹시 구독이 남아있으면 정리
       // if (RecipeManager.Instance != null)
       // {
       //     RecipeManager.Instance.OnRecipeUnlocked    -= OnRecipeUnlocked;
       //     RecipeManager.Instance.OnCraftingStarted   -= OnCraftingStarted;
       //     RecipeManager.Instance.OnCraftingCompleted -= OnCraftingCompleted;
       //     RecipeManager.Instance.OnCraftingCancelled -= OnCraftingCancelled;
       // }
    }

    public override bool PerformBackFunction()
    {
        Hide();
        return false;
    }

    protected override void Update()
    {
        base.Update();
    }

    #endregion
}
