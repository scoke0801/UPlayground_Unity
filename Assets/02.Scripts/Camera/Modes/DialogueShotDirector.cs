using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 라인 단위로 "어떤 샷을, 어떤 전환으로" 잡을지 결정한다.
    /// 데이터 우선 — 노드가 샷/전환을 지정했으면 그대로 쓰고, Auto일 때만 규칙이 개입한다.
    /// </summary>
    public static class DialogueShotDirector
    {
        public struct Decision
        {
            public DialogueShotType Shot;
            public DialogueShotTransition Transition;
            public Transform Subject;
            public bool PlayIntro;

            /// <summary>이번 라인 기준으로 갱신된 연속 짧은 라인 수. 세션 커밋용.</summary>
            public int ConsecutiveShortLines;
        }

        public static Decision Decide(
            DialogueCameraSettingsSO settings,
            DialogueShotSession session,
            in DialogueShotRequest request)
        {
            int consecutiveShortLines = ComputeConsecutiveShortLines(settings, session, request);

            bool isFirstLine = session == null || session.LineIndex == 0;

            // 활성 pair가 바뀌어 가상선이 크게 돌았으면 컷이 아니라 이동으로 넘어간다.
            // 선을 넘는 것 자체가 아니라 "컷으로 넘는 것"이 관객의 공간 감각을 깨뜨리기 때문이다.
            bool isAxisChange = session != null
                                && !isFirstLine
                                && settings.axisChangePolicy != DialogueAxisChangePolicy.None
                                && session.LastAxisChangeAngle >= settings.axisEstablishAngle;

            DialogueShotType shot;
            if (request.ShotType != DialogueShotType.Auto)
                shot = request.ShotType;
            else if (isAxisChange && settings.axisChangePolicy == DialogueAxisChangePolicy.EstablishWide)
                shot = DialogueShotType.Wide;
            else
                shot = DecideShot(settings, session, request, consecutiveShortLines);

            shot = ResolveFramingFallback(settings, request, shot);

            // 샷이 확정된 뒤에 주시 대상을 해석한다. 축 전환 Wide 승격이나 프레이밍 폴백으로 대상이 달라진다.
            Transform subject = DialogueShotComposer.ResolveSubject(request, shot);

            bool playIntro = isFirstLine
                             && settings.enableIntroSequence
                             && session != null
                             && !session.IntroConsumed
                             && session.HasActivePair
                             && request.Speaker != null
                             && request.Listener != null
                             && request.Speaker != request.Listener
                             && CanPlayIntroBetweenActors(
                                 settings,
                                 request.Speaker,
                                 request.Listener)
                             && !settings.ResolvePreset(shot).framesBothActors;

            DialogueShotTransition transition;
            if (request.Transition != DialogueShotTransition.Auto)
                transition = request.Transition;
            else if (isAxisChange)
                transition = DialogueShotTransition.Establish;
            else
                transition = DecideTransition(session, shot, subject, isFirstLine);

            return new Decision
            {
                Shot = shot,
                Transition = transition,
                Subject = subject,
                PlayIntro = playIntro,
                ConsecutiveShortLines = consecutiveShortLines
            };
        }

        private static DialogueShotType ResolveFramingFallback(
            DialogueCameraSettingsSO settings,
            in DialogueShotRequest request,
            DialogueShotType shot)
        {
            DialogueShotPreset preset = settings.ResolvePreset(shot);
            if (!preset.framesBothActors)
                return shot;

            Transform subject = DialogueShotComposer.ResolveSubject(request, shot);
            Transform anchor = DialogueShotComposer.ResolveAnchor(request, subject);
            if (subject == null || anchor == null)
                return shot;

            Vector3 separation = subject.position - anchor.position;
            separation.y = 0f;
            return settings.CanFrameBothActors(
                preset,
                separation.magnitude,
                request.DistanceOverride)
                ? shot
                : DialogueShotType.OverTheShoulderSpeaker;
        }

        private static bool CanPlayIntroBetweenActors(
            DialogueCameraSettingsSO settings,
            Transform speaker,
            Transform listener)
        {
            Vector3 separation = speaker.position - listener.position;
            separation.y = 0f;
            return settings.CanFrameBothActors(
                settings.ResolvePreset(DialogueShotType.TwoShot),
                separation.magnitude);
        }

        private static DialogueShotType DecideShot(
            DialogueCameraSettingsSO settings,
            DialogueShotSession session,
            in DialogueShotRequest request,
            int consecutiveShortLines)
        {
            // 리액션 대상이 지정된 라인은 그 인물을 잡는다.
            if (request.ReactionSubject != null)
                return DialogueShotType.Reaction;

            // 선택지 구간은 두 인물을 함께 담아 상황을 읽기 쉽게 한다.
            if (request.IsChoicePhase && settings.choicePhaseTwoShot)
                return DialogueShotType.TwoShot;

            // 짧은 대사가 연속되면 화자마다 컷하지 않고 투샷으로 묶는다.
            // 한 줄짜리 주고받기에서 컷이 난사되는 것을 막는 영화 문법 쪽 처리.
            if (settings.shortLineTwoShotCount > 0
                && consecutiveShortLines >= settings.shortLineTwoShotCount
                && session != null
                && session.HasActivePair)
            {
                return DialogueShotType.TwoShot;
            }

            // 기본: 화자를 잡는 OTS. 화자가 바뀌면 대상이 반대편으로 바뀌므로
            // 가상선이 고정된 상태에서 자연히 shot-reverse-shot이 성립한다.
            return DialogueShotType.OverTheShoulderSpeaker;
        }

        private static DialogueShotTransition DecideTransition(
            DialogueShotSession session,
            DialogueShotType shot,
            Transform subject,
            bool isFirstLine)
        {
            if (isFirstLine)
                return DialogueShotTransition.Establish;

            if (session == null)
                return DialogueShotTransition.Blend;

            // 주시 대상이나 구도 자체가 바뀌면 컷, 같은 대상 유지면 미세 보정 블렌드.
            bool subjectChanged = session.LastSubject != subject;
            bool shotChanged = session.LastShotType != shot;

            return subjectChanged || shotChanged
                ? DialogueShotTransition.Cut
                : DialogueShotTransition.Blend;
        }

        private static int ComputeConsecutiveShortLines(
            DialogueCameraSettingsSO settings,
            DialogueShotSession session,
            in DialogueShotRequest request)
        {
            if (session == null || settings.shortLineTwoShotCount <= 0)
                return 0;

            bool isShort = request.LineLength > 0 && request.LineLength <= settings.shortLineThreshold;
            if (!isShort)
                return 0;

            // 컷 난사는 "짧은 대사 + 화자 교대"에서 생긴다. 같은 화자가 짧은 대사를 이어가는 동안은
            // 컷이 일어나지 않으므로 누적치를 올리지 않고 유지만 한다.
            bool speakerChanged = session.LastSpeaker != request.Speaker;
            return speakerChanged
                ? session.ConsecutiveShortLines + 1
                : session.ConsecutiveShortLines;
        }

        /// <summary>전환 종류 → 실제 블렌드 시간(초).</summary>
        public static float ResolveBlendTime(DialogueCameraSettingsSO settings, DialogueShotTransition transition)
        {
            switch (transition)
            {
                case DialogueShotTransition.Cut:
                    return Mathf.Max(0f, settings.cutInstantTime);

                case DialogueShotTransition.Establish:
                    return Mathf.Max(0f, settings.establishBlendTime);

                case DialogueShotTransition.Blend:
                default:
                    return Mathf.Max(0f, settings.softBlendTime);
            }
        }
    }
}
