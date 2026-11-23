using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Quest;
using UPlayGround.Data.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.Guidance
{
    /// <summary>
    /// 추적 중인 퀘스트의 목표 지점까지 길을 계산하고 연출을 재생하는 길안내 오케스트레이터.
    /// 입력은 받지 않고 <see cref="TryRequestGuide"/>만 노출한다(입력 연결은 HUD가 맡는다).
    /// 길을 확인하지 못하면 방향만 알려 주는 대체 연출도, "길 없음" 반응도 하지 않고 아무것도 재생하지 않는다.
    /// 경로 계산은 <see cref="IGuidePathProvider"/>, 연출은 <see cref="IGuideEffect"/>에 위임한다.
    /// </summary>
    public sealed class GuidanceController : MonoBehaviour
    {
        private const float RepathCheckInterval = 0.5f;
        private const string EffectRootName = "GuidanceEffectRoot";

        private GuideProfileSO _profile;
        private IGuidePathProvider _pathProvider;
        private IGuideEffect _effect;
        private Transform _effectRoot;

        private GuidePath _activePath = new();
        private GuidePath _sparePath = new();
        private Vector3 _targetPosition;
        private float _nextAllowedTime;
        private float _nextRepathCheckTime;
        private bool _wasPlaying;
        private bool _isGuiding;
        private bool _isSubscribed;

        /// <summary>프로필을 연결하고 경로 제공자·연출을 만든다. HUD가 표시될 때 한 번 호출한다.</summary>
        public void Initialize(GuideProfileSO profile)
        {
            _profile = profile;
            if (_profile == null)
                return;

            _pathProvider = new NavMeshGuidePathProvider(_profile.navMeshSampleDistance, _profile.smoothSubdivisions);
            _effect = GuideEffectFactory.Create(_profile.effectKind);
            _effectRoot = new GameObject(EffectRootName).transform;
            SubscribeQuestEvents();
        }

        /// <summary>
        /// 전투(교전) 중인지 판정한다. 길안내와 퀘스트 표시 입력은 전투 중에 항상 막는다.
        /// 플레이어의 전투 상태(<see cref="PlayerCombat.IsInCombat"/>)는 최근 교전 이벤트 후 일정 시간과
        /// 근거리 어그로 몬스터만 잡으므로, 플레이어를 노리는 몬스터가 더 먼 곳에서 추적 중인 경우를
        /// 위협 스캐너(<see cref="NpcThreatScanner"/>)로 보강한다.
        /// </summary>
        public bool IsBlockedByCombat()
        {
            if (_profile == null)
                return false;

            return IsEngagedInCombat(UISvc.Actors?.Player, _profile.combatThreatRadius);
        }

        /// <summary>플레이어가 전투 중이거나 threatRadius 안에 어그로 중인 몬스터가 있으면 true.</summary>
        public static bool IsEngagedInCombat(PlayerActor player, float threatRadius)
        {
            if (player == null)
                return false;
            if (player.GetCombat()?.IsInCombat == true)
                return true;

            return threatRadius > 0f
                   && NpcThreatScanner.TryGetNearest(player.transform.position, out _, out float distance)
                   && distance <= threatRadius;
        }

        /// <summary>길안내를 시작한다. 쿨다운·전투·경로 없음 등으로 시작하지 못하면 false. 길이 없으면 아무 연출도 하지 않는다.</summary>
        public bool TryRequestGuide()
        {
            if (_profile == null || _effect == null || !isActiveAndEnabled)
                return false;
            if (_effect.IsPlaying || Time.unscaledTime < _nextAllowedTime)
                return false;

            PlayerActor player = UISvc.Actors?.Player;
            if (player == null || !player.IsAlive())
                return false;
            if (IsEngagedInCombat(player, _profile.combatThreatRadius))
                return false;
            if (!TryBuildPathToTrackedObjective(player.transform.position))
                return false;

            PlaySfx(_profile.startSfxKey, player.transform.position);
            _effect.Begin(new GuideContext(player.transform, _effectRoot, _activePath, _profile));
            _isGuiding = true;
            _nextRepathCheckTime = Time.unscaledTime + RepathCheckInterval;
            return true;
        }

        private void Update()
        {
            if (_effect == null)
                return;

            _effect.Tick(Time.deltaTime);

            bool isPlaying = _effect.IsPlaying;
            if (_wasPlaying && !isPlaying)
            {
                _nextAllowedTime = Time.unscaledTime + _profile.cooldown;
                _isGuiding = false;
            }

            _wasPlaying = isPlaying;

            if (isPlaying && _isGuiding && Time.unscaledTime >= _nextRepathCheckTime)
            {
                _nextRepathCheckTime = Time.unscaledTime + RepathCheckInterval;
                ValidatePlayback();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeQuestEvents();
            _effect?.Dispose();
            _effect = null;
            if (_effectRoot != null)
                Destroy(_effectRoot.gameObject);
        }

        #region 목표와 경로

        // 활성 목표가 여럿이면 실제로 걸어갈 수 있는 가장 짧은 길의 목표를 고른다.
        private bool TryBuildPathToTrackedObjective(Vector3 from)
        {
            var quests = UISvc.Quest;
            if (quests == null || !quests.IsDBLoaded || quests.IsQuestTrackingSuppressed)
                return false;

            QuestRuntimeData quest = quests.GetTrackedQuestRuntime();
            if (quest?.QuestSO == null)
                return false;

            bool found = false;
            float bestLength = float.MaxValue;
            foreach (QuestObjectiveData objective in quest.GetActiveObjectives())
            {
                if (!TryGetObjectivePosition(objective, out Vector3 target))
                    continue;
                if (!_pathProvider.TryBuild(from, target, _sparePath) || _sparePath.TotalLength >= bestLength)
                    continue;

                bestLength = _sparePath.TotalLength;
                _targetPosition = target;
                (_activePath, _sparePath) = (_sparePath, _activePath);
                found = true;
            }

            return found;
        }

        private static bool TryGetObjectivePosition(QuestObjectiveData objective, out Vector3 position)
        {
            position = default;
            string locationId = QuestObjectiveMarker.ResolveLocationId(objective);
            if (string.IsNullOrEmpty(locationId))
                return false;
            if (!MinimapMarkerRegistry.TryGet(locationId, out MinimapMarkerRegistrar registrar) || registrar == null)
                return false;
            if (!registrar.IsWorldMarkerVisible)
                return false;

            position = registrar.transform.position;
            return true;
        }

        // 재생 중 주기적으로 확인한다: 플레이어가 사라지거나 죽으면 즉시 정리, 전투가 시작되면 페이드아웃, 길에서 벗어나면 다시 계산.
        private void ValidatePlayback()
        {
            PlayerActor player = UISvc.Actors?.Player;
            if (player == null || !player.IsAlive())
            {
                _effect.Stop(true);
                return;
            }

            if (IsEngagedInCombat(player, _profile.combatThreatRadius))
            {
                _effect.Stop(false);
                return;
            }

            _activePath.ProjectDistance(player.transform.position, 0f, out float deviation);
            if (deviation <= _profile.repathDistance)
                return;

            if (_pathProvider.TryBuild(player.transform.position, _targetPosition, _sparePath))
            {
                (_activePath, _sparePath) = (_sparePath, _activePath);
                _effect.ReplacePath(_activePath);
            }
            else
            {
                // 벗어난 위치에서 길을 확인하지 못하면 낡은 길을 계속 보여 주지 않고 사라진다.
                _effect.Stop(false);
            }
        }

        #endregion

        #region 연출

        private static void PlaySfx(string key, Vector3 position)
        {
            if (string.IsNullOrEmpty(key))
                return;

            Svc.Sound?.PlaySfx(key, position);
        }

        #endregion

        #region 퀘스트 이벤트

        // 추적 대상이나 목표가 바뀌면 이미 재생 중인 길은 더 이상 맞지 않으므로 사라지게 한다.
        private void SubscribeQuestEvents()
        {
            var events = Svc.Events;
            if (_isSubscribed || events == null)
                return;

            events.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestTracked, OnQuestStateChanged);
            events.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestUntracked, OnQuestStateChanged);
            events.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestStateChanged);
            events.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed, OnQuestStateChanged);
            events.Subscribe<QuestEvent, QuestObjectiveEventData>(QuestEvent.QuestObjectiveUpdated, OnQuestObjectiveChanged);
            _isSubscribed = true;
        }

        private void UnsubscribeQuestEvents()
        {
            var events = Svc.Events;
            if (!_isSubscribed || events == null)
                return;

            events.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestTracked, OnQuestStateChanged);
            events.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestUntracked, OnQuestStateChanged);
            events.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestStateChanged);
            events.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed, OnQuestStateChanged);
            events.Unsubscribe<QuestEvent, QuestObjectiveEventData>(QuestEvent.QuestObjectiveUpdated, OnQuestObjectiveChanged);
            _isSubscribed = false;
        }

        private void OnQuestStateChanged(QuestStateEventData _) => _effect?.Stop(false);
        private void OnQuestObjectiveChanged(QuestObjectiveEventData _) => _effect?.Stop(false);

        #endregion
    }
}
