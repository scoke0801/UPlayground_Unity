using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UPlayGround.CameraSystem;
using UPlayGround.EditorTools;
using UPlayGround.Manager;

namespace UPlayGround.Tool.Editor
{
    /// <summary>저장된 시험장을 실제 Play Mode로 열어 카메라와 액터 연결을 검사한다.</summary>
    [InitializeOnLoad]
    public static class CameraTestMapSmoke
    {
        private const string PendingKey = "UPlayGround.CameraTestMap.Smoke";
        private const string ReportDirectory = "Logs/CameraTestMap";
        private static int _stage;
        private static double _nextCheck;
        private static double _startedAt;
        private static PlayerActor _player;
        private static CameraManager _camera;
        private static CameraLockOn _lockOn;
        private static Transform _selectedTarget;
        private static string _failure;
        private static bool _isFinishing;

        static CameraTestMapSmoke()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        /// <summary>충돌 당김·복귀, 락온 획득·전환·가림 해제를 순차 검증한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/카메라/카메라 테스트 맵 PlayMode 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode를 종료한 뒤 검증을 실행하세요.");
            CameraTestMapBuilder.OpenOrCreate();
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != CameraTestMapBuilder.ScenePath)
                return;
            CameraTestMapBuilder.Validate();
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(ReportDirectory + "/Smoke.txt", "카메라 시험장 Play Mode 검증 시작\n");
            SessionState.SetBool(PendingKey, true);
            EditorSceneManager.playModeStartScene = null;
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PendingKey, false))
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _stage = 0;
                _startedAt = EditorApplication.timeSinceStartup;
                _nextCheck = _startedAt;
                _failure = null;
                _isFinishing = false;
                Application.logMessageReceived += CaptureError;
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(PendingKey, false);
                EditorApplication.update -= Tick;
                Application.logMessageReceived -= CaptureError;
                if (Application.isBatchMode)
                    EditorApplication.Exit(File.ReadAllText(ReportDirectory + "/Smoke.txt").Contains("PASS") ? 0 : 1);
            }
        }

        private static void CaptureError(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _failure = message;
        }

        private static void Tick()
        {
            if (_isFinishing || !EditorApplication.isPlaying)
                return;
            double now = EditorApplication.timeSinceStartup;
            if (now - _startedAt > 120)
            {
                Finish("FAIL: 초기화/검증 시간 초과");
                return;
            }
            if (now < _nextCheck)
                return;
            try
            {
                ExecuteStage();
            }
            catch (Exception exception)
            {
                Finish("FAIL: " + exception);
            }
            _nextCheck = now + 3;
        }

        private static void ExecuteStage()
        {
            switch (_stage)
            {
                case 0:
                    if (GameManager.Instance.BootState == GameBootState.Failed)
                        throw new InvalidOperationException(GameManager.Instance.InitializationFailure);
                    if (!GameManager.Instance.IsInitialized)
                        return;
                    _player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
                    _camera = CameraManager.Instance;
                    if (_player?.PlayerController?.Motor == null || !_camera.IsSceneCameraInitialized)
                        return;
                    _lockOn = (CameraLockOn)typeof(CameraManager)
                        .GetField("_lockOn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_camera);
                    MovePlayer(new Vector3(-18, 0.1f, -10.5f), 0);
                    break;
                case 1:
                    float closeDistance = MeasureCameraDistance();
                    Require(closeDistance < 2.8f, "벽 앞 카메라 당김 실패: " + closeDistance);
                    Record("벽 앞 거리: " + closeDistance);
                    MovePlayer(new Vector3(-5, 0.1f, -10), 0);
                    break;
                case 2:
                    float restoredDistance = MeasureCameraDistance();
                    Require(restoredDistance > 3.4f, "개방 공간 거리 복귀 실패: " + restoredDistance);
                    Record("개방 공간 거리: " + restoredDistance);
                    MovePlayer(new Vector3(16, 0.1f, -7), 0);
                    break;
                case 3:
                    Require(_lockOn.TryActivate(), "실제 허수아비 락온 획득 실패");
                    _selectedTarget = _lockOn.CurrentTarget;
                    Record("획득: " + _selectedTarget.name);
                    break;
                case 4:
                    _lockOn.SwitchTarget(1);
                    Require(_lockOn.CurrentTarget != _selectedTarget, "오른쪽 표적 전환 실패");
                    Record("전환: " + _lockOn.CurrentTarget.name);
                    _lockOn.Release();
                    MovePlayer(new Vector3(22, 0.1f, 14), 180);
                    break;
                case 5:
                    Require(_lockOn.TryActivate(), "가림 검사 표적 획득 실패");
                    Require(_lockOn.CurrentTarget.name == "Target_Occluded", "가림 검사 표적 불일치");
                    MovePlayer(new Vector3(22, 0.1f, 3), 0);
                    break;
                case 6:
                    Require(!_lockOn.IsActive, "벽에 가려진 표적 유예 후 해제 실패");
                    Record("벽 가림 후 락온 해제 확인");
                    MovePlayer(new Vector3(0, 0.1f, 9), 0);
                    break;
                case 7:
                    Require(_lockOn.TryActivate(), "높은 발판 표적 획득 실패");
                    Require(_lockOn.CurrentTarget.name == "Target_Elevated", "높은 발판 표적 불일치");
                    Record("높은 발판 표적 획득 확인");
                    break;
                case 8:
                    Require(_lockOn.IsActive && _lockOn.CanTrack, "높은 발판 표적 추적 실패");
                    CaptureView();
                    break;
                case 9:
                    _lockOn.Release();
                    MovePlayer(new Vector3(-9, 0.1f, -8), 0);
                    _camera.SetRotation(0, -12);
                    break;
                case 10:
                    Require(_lockOn.TryActivate(), "공중 몬스터 락온 획득 실패");
                    Require(_lockOn.CurrentTarget.name == "Target_SkySentinel", "공중 몬스터 표적 불일치");
                    break;
                case 11:
                    ValidateMonsterTracking(4.2f);
                    CaptureView("SkySentinel");
                    Record("공중 감시자: 고도 유지·락온 추적 확인");
                    MovePlayer(new Vector3(-9, 0.1f, -1), 0);
                    break;
                case 12:
                    ValidateMonsterTracking(4.2f);
                    CaptureView("SkySentinelNear");
                    Record("공중 감시자: 근접 프레이밍 확인");
                    _lockOn.Release();
                    MovePlayer(new Vector3(11, 0.1f, 11), 0);
                    _camera.SetRotation(0, 0);
                    break;
                case 13:
                    Require(_lockOn.TryActivate(), "대형 몬스터 락온 획득 실패");
                    Require(_lockOn.CurrentTarget.name == "Target_StoneColossus", "대형 몬스터 표적 불일치");
                    break;
                case 14:
                    ValidateMonsterTracking(0.08f);
                    CaptureView("StoneColossus");
                    Record("석상 거인: 지면 유지·몸체 중심 락온 추적 확인");
                    MovePlayer(new Vector3(11, 0.1f, 17.5f), 0);
                    break;
                case 15:
                    ValidateMonsterTracking(0.08f);
                    CaptureView("StoneColossusNear");
                    Record("석상 거인: 근접 플레이어 크기 보호 확인");
                    break;
                default:
                    Finish(_failure == null ? "PASS" : "FAIL: 실행 중 오류: " + _failure);
                    return;
            }
            _stage++;
        }

        private static void MovePlayer(Vector3 position, float yaw)
        {
            _player.PlayerController.Motor.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            _camera.SnapToTarget(position);
            _camera.SetRotation(yaw, 10);
        }

        private static float MeasureCameraDistance()
        {
            Vector3 pivot = _player.transform.position + Vector3.up * 1.3f;
            return Vector3.Distance(Camera.main.transform.position, pivot);
        }

        private static void ValidateMonsterTracking(float expectedHeight)
        {
            Require(_lockOn.IsActive && _lockOn.CanTrack, "몬스터 표적 추적 실패");
            Require(Mathf.Abs(_lockOn.CurrentTarget.position.y - expectedHeight) < 0.2f, "몬스터 배치 고도 이탈");
            Vector3 viewport = Camera.main.WorldToViewportPoint(_lockOn.FocusPosition);
            Require(viewport.z > 0 && viewport.x > 0.1f && viewport.x < 0.9f &&
                viewport.y > 0.04f && viewport.y < 0.96f, "몬스터 포커스 화면 이탈: " + viewport);
            Transform model = _lockOn.CurrentTarget.Find("BlenderVisual");
            Require(model != null, "Blender 모델 누락");
            Animator animator = model.GetComponent<Animator>();
            Require(animator != null && animator.runtimeAnimatorController != null &&
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0, "Blender 대기 애니메이션 미재생");
            Record(_lockOn.CurrentTarget.name + " 포커스 화면 좌표: " + viewport);
            var context = (CameraContext)typeof(CameraManager)
                .GetField("_cameraContext", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_camera);
            float playerDepth = Camera.main.WorldToViewportPoint(
                _player.transform.position + context.State.CameraOffset).z;
            float playerScale = context.State.TargetDistance / playerDepth;
            Require(playerScale >= context.Settings.lockOnFramingMinPlayerScale - 0.02f,
                "프레이밍으로 플레이어가 지나치게 작아짐: " + playerScale);
            Record("기본 구도 대비 플레이어 투영 배율: " + playerScale);
            if (expectedHeight > 1f)
            {
                float pitch = Mathf.DeltaAngle(0f, Camera.main.transform.eulerAngles.x);
                Require(pitch >= 0f, "공중 프레이밍이 올려다보는 피치로 수렴: " + pitch);
                Vector3 feet = Camera.main.WorldToViewportPoint(_player.transform.position);
                Require(feet.z > 0f && feet.y > 0.02f && feet.y < 0.98f,
                    "공중 프레이밍에서 플레이어 발밑 이탈: " + feet);
                Require(_lockOn.TryGetTargetFramingPoints(0f, out _, out Vector3 targetTop),
                    "공중 대상 상단 좌표 조회 실패");
                Vector3 top = Camera.main.WorldToViewportPoint(targetTop);
                // 전신이 화면에 들어오는지를 성공 조건으로 삼으면 과도한 줌아웃을 다시 유도한다.
                Record("공중 프레이밍 피치: " + pitch + ", 발밑 화면 좌표: " + feet + ", 대상 상단: " + top);
            }
        }

        private static void CaptureView(string name = "PlayMode")
        {
            var renderTexture = RenderTexture.GetTemporary(1280, 720, 24);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = renderTexture };
                RenderPipeline.SubmitRenderRequest(Camera.main, request);
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes(ReportDirectory + "/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Record(string message)
        {
            File.AppendAllText(ReportDirectory + "/Smoke.txt", message + "\n");
        }

        private static void Finish(string result)
        {
            _isFinishing = true;
            Record(result);
            Debug.Log("[CameraTestMapSmoke] " + result);
            EditorApplication.isPlaying = false;
        }
    }
}
