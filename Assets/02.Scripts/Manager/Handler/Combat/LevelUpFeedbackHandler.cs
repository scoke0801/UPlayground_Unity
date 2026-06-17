using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager.Handler;
using UPlayGround.UI;

namespace UPlayGround.Manager.Combat
{
    /// <summary>
    /// 레벨업 순간의 연출(전신 VFX + "LEVEL UP" 플로터)을 담당한다.
    /// PartyManager.OnLevelUp을 구독하며, 활성 캐릭터만 전신 연출을 재생한다.
    /// 포스트프로세스/타임스케일 슬로우는 사용하지 않는다(전투 흐름 유지).
    /// </summary>
    public sealed class LevelUpFeedbackHandler : GameHandlerBase
    {
        // FX 프리팹 키. 해당 키의 FX가 등록되어 있지 않으면 ShowFX가 무시한다(안전).
        private const string LevelUpFxKey = "LevelUp";
        private const string LevelUpSoundKey = "LevelUp";
        private const float  FxDuration   = 3f;
        private const float  HeadOffsetY  = 2.2f;

        // 다단/동시 레벨업 연출 디바운스: 연출은 1회로 합치고 표기는 항상 최종 레벨을 읽는다.
        private const float MinInterval = 0.35f;
        private float _lastPlayTime = -999f;
        private bool  _subscribed;

        // OnLevelUp은 AddExp 루프 내부에서 발화하므로 이 시점의 _levels는 아직 커밋 전이다.
        // 플래그만 세우고, 다음 Update에서 커밋된 최종 레벨로 1회 연출한다(같은 프레임 다단/다캐릭터 자동 합산).
        private bool _pendingActiveFx;

        public override void AfterInit() => TrySubscribe();

        // PartyManager가 AfterInit 시점에 아직 준비되지 않았을 수 있어 지연 구독을 보장한다.
        public override void Update()
        {
            if (!_subscribed) TrySubscribe();
            if (!_pendingActiveFx) return;

            float now = Time.unscaledTime;
            if (now - _lastPlayTime < MinInterval) return;   // 버스트 스로틀

            _pendingActiveFx = false;
            _lastPlayTime = now;
            PlayActiveCharacterFx(PartyManager.Instance);
        }

        private void TrySubscribe()
        {
            var pm = PartyManager.Instance;
            if (pm == null) return;
            pm.OnLevelUp += OnLevelUp;
            _subscribed = true;
        }

        public override void Dispose()
        {
            if (PartyManager.Instance != null)
                PartyManager.Instance.OnLevelUp -= OnLevelUp;
            _subscribed = false;
        }

        public override void OnSceneChanged(string sceneType)
        {
            _lastPlayTime = -999f;
            _pendingActiveFx = false;
        }

        private void OnLevelUp(CharacterActorType type, int newLevel)
        {
            var pm = PartyManager.Instance;
            if (pm == null || type != pm.ActiveCharacterType) return;
            _pendingActiveFx = true;   // 커밋 후(다음 Update) 최종 레벨로 연출
        }

        private void PlayActiveCharacterFx(PartyManager pm)
        {
            if (pm == null) return;
            var player = pm.ActiveCharacter;
            if (player == null) return;

            // 다운된 활성 캐릭터 위에는 레벨업 연출을 띄우지 않는다(부활 미발생과 일관).
            if (!player.IsAlive()) return;

            int     level   = pm.GetLevel(pm.ActiveCharacterType); // 커밋된 최종 레벨
            Vector3 basePos = player.transform.position;

            // 전신 VFX — player.transform에 부착되어 따라다닌다. FX 미등록 시 무시.
            GameObjectManager.Instance?.ShowFX(
                LevelUpFxKey, basePos, Quaternion.identity, player.transform, FxDuration);

            // "LEVEL UP" 플로터 — 골드(Critical 스타일 재사용).
            UIManager.Instance?.ShowDamageFloaterLabel(
                basePos + Vector3.up * HeadOffsetY, $"LEVEL UP!  Lv.{level}", FloatStyle.Critical);

            SoundManager.Instance?.PlayUi(LevelUpSoundKey);
        }
    }
}
