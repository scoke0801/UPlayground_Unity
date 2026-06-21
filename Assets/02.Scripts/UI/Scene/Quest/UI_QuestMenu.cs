using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Crafting;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

public class UI_QuestMenu : UI_Base
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

    #region 퀘스트 추적

    /// <summary>
    /// 버튼 OnClick 또는 퀘스트 슬롯에서 호출해 해당 퀘스트를 HUD 추적 대상으로 지정한다.
    /// </summary>
    public void TrackQuest(string questId)
    {
        QuestManager.Instance?.TrackQuest(questId);
    }

    /// <summary>
    /// 현재 HUD 추적 퀘스트를 해제한다.
    /// </summary>
    public void UntrackQuest()
    {
        QuestManager.Instance?.UntrackQuest();
    }

    /// <summary>
    /// 같은 퀘스트를 누르면 해제하고, 다른 퀘스트를 누르면 추적 대상으로 바꾼다.
    /// </summary>
    public void ToggleTrackQuest(string questId)
    {
        if (QuestManager.Instance == null)
        {
            return;
        }

        if (QuestManager.Instance.IsQuestTracked(questId))
        {
            QuestManager.Instance.UntrackQuest();
            return;
        }

        QuestManager.Instance.TrackQuest(questId);
    }

    #endregion
}
