using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 추적 중(tracked) 퀘스트의 목표 지점을 자동으로 <see cref="WorldMarkerRegistry"/>에 등록/해제하는 브리지.
    /// 인게임 HUD 월드 마커(<see cref="UI_HUD_WorldMarker"/>)가 이를 화면에 투영해 원신식 웨이포인트로 노출한다.
    ///
    /// 타겟 월드 위치는 미니맵과 동일하게 씬 배치 <see cref="MinimapMarkerRegistrar"/>(LocationId == 목표 targetStringId)에서 가져온다.
    /// 목표에 대응하는 Registrar가 아직 없으면 그 목표는 마커를 만들지 않고, Registrar가 스폰되면 자동 반영한다.
    ///
    /// 구독:
    ///   - 퀘스트 상태 이벤트(수락/완료/실패/추적/추적해제/목표갱신) → 대상 재계산
    ///   - MinimapMarkerRegistry Add/Remove → Registrar 스폰·파괴 반영
    /// </summary>
    public sealed class QuestWorldMarkerBridge : MonoBehaviour
    {
        private const string MarkerIdPrefix = "quest:";

        [Tooltip("퀘스트 목표 마커 아이콘. 비워두면 UI_HUD_WorldMarker가 아이콘 없이 거리만 표시한다.")]
        [SerializeField] private Sprite _questIcon;

        [Tooltip("퀘스트 목표 마커 색상")]
        [SerializeField] private Color _questColor = new Color(1f, 0.85f, 0.2f, 1f);

        private bool _subscribed;
        // 현재 이 브리지가 등록해 둔 마커 id 집합(마크-스윕용).
        private readonly HashSet<string> _owned = new();
        private readonly List<string> _seen = new();
        private readonly List<string> _toRemove = new();

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearOwned();
        }

        #region 구독

        private void Subscribe()
        {
            if (_subscribed) return;

            var ev = Svc.Events;
            if (ev != null)
            {
                ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestAccepted, OnQuestStateChanged);
                ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestStateChanged);
                ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed, OnQuestStateChanged);
                ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestTracked, OnQuestStateChanged);
                ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestUntracked, OnQuestStateChanged);
                ev.Subscribe<QuestEvent, QuestObjectiveEventData>(QuestEvent.QuestObjectiveUpdated, OnQuestObjectiveChanged);
            }

            MinimapMarkerRegistry.OnMarkerAdded += OnRegistrarChanged;
            MinimapMarkerRegistry.OnMarkerRemoved += OnRegistrarChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;

            var ev = Svc.Events;
            if (ev != null)
            {
                ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestAccepted, OnQuestStateChanged);
                ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestStateChanged);
                ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed, OnQuestStateChanged);
                ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestTracked, OnQuestStateChanged);
                ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestUntracked, OnQuestStateChanged);
                ev.Unsubscribe<QuestEvent, QuestObjectiveEventData>(QuestEvent.QuestObjectiveUpdated, OnQuestObjectiveChanged);
            }

            MinimapMarkerRegistry.OnMarkerAdded -= OnRegistrarChanged;
            MinimapMarkerRegistry.OnMarkerRemoved -= OnRegistrarChanged;
            _subscribed = false;
        }

        private void OnQuestStateChanged(QuestStateEventData _) => Refresh();
        private void OnQuestObjectiveChanged(QuestObjectiveEventData _) => Refresh();
        private void OnRegistrarChanged(MinimapMarkerRegistrar _) => Refresh();

        #endregion

        #region 마커 재계산

        // 추적 퀘스트의 미완료 목표 중 Registrar가 존재하는 것만 마커로 등록하고, 나머지는 해제한다.
        private void Refresh()
        {
            _seen.Clear();

            var qm = UISvc.Quest;
            if (qm != null && qm.IsDBLoaded && !qm.IsQuestTrackingSuppressed)
            {
                QuestRuntimeData quest = qm.GetTrackedQuestRuntime();
                if (quest?.QuestSO != null)
                {
                    var objectives = quest.QuestSO.objectives;
                    for (int i = 0; i < objectives.Count; i++)
                    {
                        QuestObjectiveData obj = objectives[i];
                        if (obj == null || quest.IsObjectiveComplete(obj)) continue;

                        string locationId = ResolveQuestLocationId(obj);
                        if (string.IsNullOrEmpty(locationId)) continue;
                        if (!MinimapMarkerRegistry.TryGet(locationId, out MinimapMarkerRegistrar registrar) || registrar == null)
                            continue;

                        string markerId = MarkerIdPrefix + locationId;
                        // Registrar transform을 추종 → NPC/오브젝트가 움직여도 따라간다. 파괴되면 자동 정리.
                        WorldMarkerRegistry.Register(markerId, registrar.transform, _questIcon, _questColor);
                        _owned.Add(markerId);
                        _seen.Add(markerId);
                    }
                }
            }

            // 이번 재계산에서 빠진 소유 마커는 해제한다.
            if (_owned.Count == _seen.Count) return;

            _toRemove.Clear();
            foreach (string id in _owned)
                if (!_seen.Contains(id))
                    _toRemove.Add(id);

            for (int i = 0; i < _toRemove.Count; i++)
            {
                WorldMarkerRegistry.Remove(_toRemove[i]);
                _owned.Remove(_toRemove[i]);
            }
        }

        private void ClearOwned()
        {
            foreach (string id in _owned)
                WorldMarkerRegistry.Remove(id);
            _owned.Clear();
        }

        // 미니맵과 동일한 규칙: ReachLocation은 targetStringId, ItemDeliver는 npc_{npcId}.
        private static string ResolveQuestLocationId(QuestObjectiveData obj) => obj.type switch
        {
            QuestObjectiveType.ReachLocation => obj.targetStringId,
            QuestObjectiveType.ItemDeliver   => $"npc_{obj.npcId}",
            _                               => null,
        };

        #endregion
    }
}
