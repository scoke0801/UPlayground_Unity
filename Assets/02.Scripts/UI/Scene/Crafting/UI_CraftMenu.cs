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

    // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
    protected override bool BlocksLowerInput => true;

    protected override void OnDispose()
    {
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
