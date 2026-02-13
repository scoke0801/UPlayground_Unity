using UnityEditor;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.Editor
{
    [CustomEditor(typeof(CameraShakeData))]
    public class CameraShakerDataEditor : UnityEditor.Editor
    {
        private CameraShakeData _data;
        private CameraShaker _testShaker;
        private float _elapsedTime;
        private bool _isTesting;
        private double _lastUpdateTime;

        private void OnEnable()
        {
            _data = (CameraShakeData)target;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopTest();
        }

        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 그리기
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("에디터 테스트", EditorStyles.boldLabel);
            
            // 테스트 상태 표시
            if (_isTesting)
            {
                EditorGUILayout.HelpBox(
                    $"테스트 진행 중...\n경과 시간: {_elapsedTime:F2}초 / {_data.Duration + _data.Delay:F2}초", 
                    MessageType.Info
                );
            }

            // 테스트 버튼
            GUI.enabled = !_isTesting;
            if (GUILayout.Button("쉐이크 테스트 시작", GUILayout.Height(30)))
            {
                StartTest();
            }
            GUI.enabled = true;

            GUI.enabled = _isTesting;
            if (GUILayout.Button("테스트 중지", GUILayout.Height(25)))
            {
                StopTest();
            }
            GUI.enabled = true;

            // Scene View 카메라 정보
            EditorGUILayout.Space(5);
            if (SceneView.lastActiveSceneView != null)
            {
                var sceneCamera = SceneView.lastActiveSceneView.camera;
                EditorGUILayout.LabelField("Scene View 카메라", sceneCamera != null ? "감지됨" : "없음");
            }
            else
            {
                EditorGUILayout.HelpBox("Scene View를 열어주세요.", MessageType.Warning);
            }
        }

        private void StartTest()
        {
            if (SceneView.lastActiveSceneView == null)
            {
                EditorUtility.DisplayDialog("오류", "Scene View가 열려있지 않습니다.", "확인");
                return;
            }

            // 임시 게임오브젝트 생성
            var tempGo = new GameObject("_TempCameraShaker");
            tempGo.hideFlags = HideFlags.HideAndDontSave;
            
            _testShaker = tempGo.AddComponent<CameraShaker>();
            
            // 리플렉션으로 private 필드 설정
            var shakerType = typeof(CameraShaker);
            var shakeDataField = shakerType.GetField("_shakeData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            shakeDataField?.SetValue(_testShaker, _data);

            // Scene View 카메라 추가
            var sceneCamera = SceneView.lastActiveSceneView.camera;
            if (!_data.Cameras.Contains(sceneCamera))
            {
                _data.Cameras.Add(sceneCamera);
            }
            
            _testShaker.FetchCameras();
            
            _elapsedTime = 0f;
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            _isTesting = true;
            
            CameraShaker.EditorPreview = true;
            
            Debug.Log("카메라 쉐이크 테스트 시작");
        }

        private void StopTest()
        {
            if (_testShaker != null)
            {
                _testShaker.StopShake();
                DestroyImmediate(_testShaker.gameObject);
                _testShaker = null;
            }

            _isTesting = false;
            _elapsedTime = 0f;
            
            // Scene View 카메라 제거
            if (SceneView.lastActiveSceneView != null)
            {
                var sceneCamera = SceneView.lastActiveSceneView.camera;
                _data.Cameras.Remove(sceneCamera);
            }
            
            SceneView.RepaintAll();
            Debug.Log("카메라 쉐이크 테스트 종료");
        }

        private void OnEditorUpdate()
        {
            if (!_isTesting || _testShaker == null)
                return;

            // deltaTime 계산
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - _lastUpdateTime);
            _lastUpdateTime = currentTime;
            
            _elapsedTime += deltaTime;
            
            // 매 프레임 animate 호출
            _testShaker.Animate(_elapsedTime);

            // Scene View 갱신
            SceneView.RepaintAll();
            
            // 인스펙터 갱신
            Repaint();

            // 종료 조건
            if (_elapsedTime >= _data.Duration + _data.Delay)
            {
                StopTest();
            }
        }
    }
}