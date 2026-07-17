using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// MotionEvent_MotionWarp 의 루트모션 총량(LocalTotal 방향 + PathLen 호길이)을 에디터에서 미리 베이크한다.
    ///
    /// [왜 필요한가] delta-warp 정확 모드는 런타임 지연 캐시(_rootTotalCache)에 의존하는데,
    /// 캐시 키가 motionSetName 기반이라 콤보/스킬처럼 매 단이 다른 모션셋이면 "세션 첫 시전 = 캐시 MISS = 약한 feel폴백"이 된다.
    /// 베이크로 윈도우 총량을 에셋에 굳혀두면 첫 시전부터 정확 모드로 진입한다.
    ///
    /// [정확성] 런타임 EvaluateVelocity 누적과 동일 정의로 측정한다(advisor 검토):
    ///   런타임: _accumRootPath += |DeltaPosition.xz| ,  _accumRootLocal += Inverse(rot) * DeltaPosition.xz
    ///   베이크: 동일. (rawHoriz*dt = DeltaPosition.xz 이므로 dt 가 소거되어 정확 일치)
    ///   - 소스: 라이브 프리뷰와 같은 ActorAnimator.DeltaPosition (raw 휴머노이드 커브 아님).
    ///   - seek 금지(Time 강제는 deltaPosition 을 흔든다) → 자연 재생을 결정적 고정 스텝(captureDeltaTime)으로 진행.
    ///   - 회전 진화: applyRotation 강제로 ON → 회전 루트 클립도 LocalTotal 방향이 런타임과 일치.
    ///   - 스케일: 시각 스케일(_rootMotionUniformScale) 1 강제 → 실제 액터 스케일의 DeltaPosition 그대로(런타임 단위).
    ///
    /// [검증] 베이크는 결정적(고정 스텝), 기존 라이브 프리뷰는 가변 dt — 두 독립 샘플링의 PathLen/|LocalTotal|
    ///   일치가 비런타임 정확성 증명이다. 결과를 UI/로그에 노출해 사용자가 라이브 프리뷰 트레일 거리와 대조한다.
    /// </summary>
    public partial class MotionSetEditorWindow
    {
        bool  _warpBakeActive;
        float _warpBakeMaxEnd;
        string _warpBakeSummary;

        class WarpBakeAccum
        {
            public MotionEvent_MotionWarp evt;
            public float   gStart, gEnd;   // 글로벌(모션셋 누적) 시간 구간
            public Vector3 local;          // Σ Inverse(rot)*DeltaPosition.xz  → 방향
            public float   path;           // Σ |DeltaPosition.xz|             → 호길이
        }
        List<WarpBakeAccum> _warpBakeAccums;

        // 베이크 중 임시 변경한 프리뷰/시간 설정 복원용 스냅샷.
        float _wbPrevScale, _wbPrevCaptureDelta, _wbPrevPlaybackSpeed, _wbPrevStart, _wbPrevEnd;
        bool  _wbPrevApplyRot, _wbPrevEnabled, _wbPrevDrawTrail, _wbPrevLooping;

        public bool IsWarpBaking => _warpBakeActive;
        public string WarpBakeSummary => _warpBakeSummary;

        public void StartWarpBake()
        {
            if (_warpBakeActive) return;

            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Warp Bake",
                    "베이크는 Play 모드에서 실행하세요.\nAnimancer graph 는 Play 모드에서만 루트모션(deltaPosition)을 평가합니다.",
                    "확인");
                return;
            }

            var set = GetCurrentMotionSet();
            if (set == null || _targetActor == null || _animancer == null)
            {
                EditorUtility.DisplayDialog("Warp Bake", "타겟 액터 또는 모션셋이 없습니다.", "확인");
                return;
            }

            // 워프 이벤트 → 글로벌 [start,end] 누적기. (MotionEventExecutor.CalculateEventOffsets 와 동일한 오프셋 규칙)
            _warpBakeAccums = new List<WarpBakeAccum>();
            float offset = 0f;
            if (set.motions != null)
            {
                foreach (var m in set.motions)
                {
                    if (m == null) continue;
                    if (m.events != null)
                        foreach (var e in m.events)
                            if (e is MotionEvent_MotionWarp w)
                                _warpBakeAccums.Add(new WarpBakeAccum { evt = w, gStart = offset + w.startTime, gEnd = offset + w.endTime });
                    offset += m.Duration;
                }
            }
            if (set.globalEvents != null) // 글로벌 이벤트는 오프셋 0
                foreach (var e in set.globalEvents)
                    if (e is MotionEvent_MotionWarp w)
                        _warpBakeAccums.Add(new WarpBakeAccum { evt = w, gStart = w.startTime, gEnd = w.endTime });

            if (_warpBakeAccums.Count == 0)
            {
                EditorUtility.DisplayDialog("Warp Bake", "이 모션셋에 MotionEvent_MotionWarp 이벤트가 없습니다.", "확인");
                return;
            }

            _warpBakeMaxEnd = 0f;
            foreach (var a in _warpBakeAccums) _warpBakeMaxEnd = Mathf.Max(_warpBakeMaxEnd, a.gEnd);

            // 설정 스냅샷 → 결정적·정합 설정으로 강제.
            _wbPrevScale         = _rootMotionUniformScale;
            _wbPrevApplyRot      = _rootMotionApplyRotation;
            _wbPrevEnabled       = _rootMotionEnabled;
            _wbPrevDrawTrail     = _rootMotionDrawTrail;
            _wbPrevCaptureDelta  = Time.captureDeltaTime;
            _wbPrevPlaybackSpeed = _playbackSpeed;
            _wbPrevStart         = _startTime;
            _wbPrevEnd           = _endTime;
            _wbPrevLooping       = _isLooping;

            _rootMotionUniformScale  = 1f;        // 런타임 단위(시각 스케일 배제)
            _rootMotionApplyRotation = true;      // 회전 진화 → Inverse(rot) 투영 정합
            _rootMotionEnabled       = true;
            _rootMotionDrawTrail     = false;
            Time.captureDeltaTime    = 1f / 120f; // 결정적 고정 스텝
            _playbackSpeed           = 1f;
            _isLooping               = false;
            _startTime               = 0f;
            _endTime                 = set.TotalDuration;

            StartPlayback(); // _playbackTime=0 부터, BeginRootMotionPreview 포함
            _warpBakeActive  = true;
            _warpBakeSummary = "베이크 중...";
            Debug.Log($"[WarpBake] 시작 — 워프 이벤트 {_warpBakeAccums.Count}개, maxEnd={_warpBakeMaxEnd:F3}s, set='{set.motionSetName}'");
        }

        /// <summary>
        /// OnEditorUpdate 에서 TickRootMotionPreview '직전' 호출.
        /// transform.rotation = 이번 프레임 회전 적용 전(이전 누적) → 런타임 EvaluateVelocity 상단 투영과 정합.
        /// DeltaPosition = 이번 graph 진행분(OnAnimatorMove) → 런타임과 동일 소스.
        /// </summary>
        void WarpBakeTick()
        {
            if (!_warpBakeActive) return;
            // 중도 실패(액터/애니메이터 소실)는 부분 누적을 valid 로 굳히면 안 된다 → 미저장 중단.
            if (_targetActor == null || _cachedActorAnimator == null) { AbortWarpBakeNoSave("타겟 액터/애니메이터 소실"); return; }

            Vector3 dp = _cachedActorAnimator.DeltaPosition;
            Vector3 horiz = new Vector3(dp.x, 0f, dp.z);
            if (horiz.sqrMagnitude > 1e-12f)
            {
                Quaternion invRot = Quaternion.Inverse(_targetActor.transform.rotation);
                float   pathStep  = horiz.magnitude;
                Vector3 localStep = invRot * horiz;
                foreach (var a in _warpBakeAccums)
                {
                    if (_playbackTime >= a.gStart && _playbackTime <= a.gEnd)
                    {
                        a.path  += pathStep;
                        a.local += localStep;
                    }
                }
            }

            // 모든 워프 구간을 지났으면 전체 재생을 기다리지 않고 조기 종료.
            if (_playbackTime >= _warpBakeMaxEnd)
                FinishWarpBake();
        }

        void FinishWarpBake()
        {
            if (!_warpBakeActive) return;
            _warpBakeActive = false;

            var sb = new StringBuilder();
            foreach (var a in _warpBakeAccums)
            {
                a.evt.bakedLocalTotal = a.local;
                a.evt.bakedPathLen    = a.path;
                a.evt.bakedValid      = a.path > 0.0001f;
                // 베이크 당시 윈도우 구간을 함께 굳힌다 → 런타임/Apply 에서 현재 start/end 와 대조해
                // 윈도우 시간이 편집된 stale 베이크는 자동 무효화(재베이크 누락 footgun 방지).
                a.evt.bakedStartTime  = a.evt.startTime;
                a.evt.bakedEndTime    = a.evt.endTime;

                Vector3 dir = a.local.sqrMagnitude > 1e-6f ? a.local.normalized : Vector3.zero;
                sb.AppendLine($"{a.evt.GetShortLabel()} [{a.gStart:F2}~{a.gEnd:F2}]  " +
                              $"PathLen={a.path:F4}  |Local|={a.local.magnitude:F4}  dir=({dir.x:F2},{dir.z:F2})  valid={a.evt.bakedValid}");
            }
            _warpBakeSummary = sb.ToString();

            if (_asset != null)
            {
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssetIfDirty(_asset);
            }

            // 설정 복원 (StopPlayback 의 EndRootMotionPreview 가 액터 위치를 시작점으로 되돌린다)
            RestoreWarpBakeSnapshot();
            StopPlayback();

            Debug.Log($"[WarpBake] 완료 — 에셋 저장:\n{_warpBakeSummary}");
        }

        /// <summary> 베이크 시작 시 강제한 프리뷰/시간 설정을 스냅샷에서 그대로 복원. </summary>
        void RestoreWarpBakeSnapshot()
        {
            Time.captureDeltaTime    = _wbPrevCaptureDelta;
            _rootMotionUniformScale  = _wbPrevScale;
            _rootMotionApplyRotation = _wbPrevApplyRot;
            _rootMotionEnabled       = _wbPrevEnabled;
            _rootMotionDrawTrail     = _wbPrevDrawTrail;
            _playbackSpeed           = _wbPrevPlaybackSpeed;
            _startTime               = _wbPrevStart;
            _endTime                 = _wbPrevEnd;
            _isLooping               = _wbPrevLooping;
        }

        /// <summary>
        /// 재생 진행 중 베이크가 실패(액터 소실 등)했을 때 결과를 저장하지 않고 중단.
        /// 부분 누적을 valid 로 굳히면 런타임이 그 truncated 시드를 1순위로 신뢰하므로 반드시 미저장.
        /// </summary>
        void AbortWarpBakeNoSave(string reason)
        {
            if (!_warpBakeActive) return;
            _warpBakeActive  = false;
            RestoreWarpBakeSnapshot();
            StopPlayback();   // EndRootMotionPreview 는 _targetActor null 가드가 있어 안전
            _warpBakeSummary = $"베이크 중단({reason}) — 결과 미저장";
            Debug.LogWarning($"[WarpBake] 중단됨({reason}) — 설정 복원, 결과 미저장");
        }

        /// <summary> 루트모션 프리뷰 패널 하단에 그리는 베이크 컨트롤. </summary>
        void DrawWarpBakeControls()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Warp 루트모션 베이크", EditorStyles.boldLabel);

            if (ShowPanelHelp)
                EditorGUILayout.HelpBox(
                    "Play 모드에서 실행. 현재 모션셋의 모든 MotionEvent_MotionWarp 윈도우 구간의 루트모션 총량(PathLen·방향)을 " +
                    "라이브 프리뷰와 동일한 DeltaPosition 으로 측정해 이벤트에 굳힌다. → 콤보/스킬도 첫 시전부터 정확 워프.\n" +
                    "정확도는 '루트 모션 적용'이 켜진 비플레이어 액터(레지스트리 스폰)에서 가장 신뢰할 수 있다(플레이어는 상태머신 이중 소비 위험).",
                    MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(_warpBakeActive || !Application.isPlaying || _targetActor == null);
                if (GUILayout.Button(_warpBakeActive ? "베이크 중..." : "Bake Warp Root Motion", GUILayout.Height(24)))
                    StartWarpBake();
                EditorGUI.EndDisabledGroup();
            }

            if (!Application.isPlaying)
                EditorGUILayout.LabelField("· Play 모드에서만 베이크 가능", EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(_warpBakeSummary))
            {
                EditorGUILayout.LabelField("최근 베이크 결과", EditorStyles.miniBoldLabel);
                EditorGUILayout.SelectableLabel(_warpBakeSummary,
                    EditorStyles.textArea,
                    GUILayout.Height(Mathf.Clamp(_warpBakeSummary.Split('\n').Length * 15f + 6f, 24f, 140f)));

                // 검증 안내: 라이브 프리뷰 '누적 이동'은 순변위(net, 시작~현재 직선거리),
                // 베이크 |Local| 도 순변위 → 이 둘을 대조한다. PathLen 은 경로길이(arc)라 굽은 모션에선 |Local| 보다 크다(정상).
                if (ShowPanelHelp)
                    EditorGUILayout.HelpBox(
                        "검증: 워프 윈도우 구간(start~end)만 라이브 프리뷰로 재생한 '누적 이동'(위) ≈ 베이크 |Local| 이면 정확. " +
                        "PathLen 은 경로길이라 직선 워프면 |Local|≈PathLen, 굽은 워프면 PathLen 이 더 큼(정상). " +
                        "|Local| 이 크게 어긋나면 스케일/회전/상태머신 이중소비 의심.",
                        MessageType.None);
            }
        }

        /// <summary> 베이크 중 윈도우/Play 모드가 끊기면 설정을 안전 복원(예외 가드). </summary>
        void AbortWarpBakeIfNeeded()
        {
            if (!_warpBakeActive) return;
            if (Application.isPlaying && _isPlaying) return; // 정상 진행 중
            // 비정상 종료 — 설정만 복원(_startTime/_endTime 포함, StopPlayback 은 이미 끊긴 상태라 생략).
            _warpBakeActive = false;
            RestoreWarpBakeSnapshot();
            _warpBakeSummary = "베이크 중단(Play 모드 종료 등) — 결과 미저장";
            Debug.LogWarning("[WarpBake] 중단됨 — 설정 복원");
        }
    }
}
