using UnityEditor;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.Editor
{
    [CustomEditor(typeof(CameraShakeData))]
    public class CameraShakerDataEditor : UnityEditor.Editor
    {
        private CameraShakeData _data;
        private CameraShaker    _testShaker;
        private float           _elapsedTime;
        private bool            _isTesting;
        private double          _lastUpdateTime;

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
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("에디터 테스트", EditorStyles.boldLabel);

            if (_isTesting)
            {
                EditorGUILayout.HelpBox(
                    $"테스트 진행 중...\n경과 시간: {_elapsedTime:F2}초 / {_data.Duration + _data.Delay:F2}초",
                    MessageType.Info);
            }

            GUI.enabled = !_isTesting;
            if (GUILayout.Button("쉐이크 테스트 시작", GUILayout.Height(30)))
                StartTest();
            GUI.enabled = true;

            GUI.enabled = _isTesting;
            if (GUILayout.Button("테스트 중지", GUILayout.Height(25)))
                StopTest();
            GUI.enabled = true;

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

            var tempGo = new GameObject("_TempCameraShaker") { hideFlags = HideFlags.HideAndDontSave };
            _testShaker = tempGo.AddComponent<CameraShaker>();

            // _shakeData 리플렉션으로 주입
            typeof(CameraShaker)
                .GetField("_shakeData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_testShaker, _data);

            // Scene View 카메라를 _data.Cameras에 직접 등록
            // (FetchCameras 제거 후 에디터 프리뷰는 SO의 Cameras 리스트를 직접 활용)
            var sceneCamera = SceneView.lastActiveSceneView.camera;
            if (sceneCamera != null && !_data.Cameras.Contains(sceneCamera))
                _data.Cameras.Add(sceneCamera);

            _elapsedTime    = 0f;
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            _isTesting      = true;

            CameraShaker.EditorPreview = true;
            Debug.Log("[CameraShakerDataEditor] 쉐이크 테스트 시작");
        }

        private void StopTest()
        {
            if (_testShaker != null)
            {
                _testShaker.StopShake();
                DestroyImmediate(_testShaker.gameObject);
                _testShaker = null;
            }

            _isTesting   = false;
            _elapsedTime = 0f;

            if (SceneView.lastActiveSceneView != null)
                _data.Cameras.Remove(SceneView.lastActiveSceneView.camera);

            SceneView.RepaintAll();
            Debug.Log("[CameraShakerDataEditor] 쉐이크 테스트 종료");
        }

        private void OnEditorUpdate()
        {
            if (!_isTesting || _testShaker == null) return;

            double currentTime = EditorApplication.timeSinceStartup;
            float  delta       = (float)(currentTime - _lastUpdateTime);
            _lastUpdateTime    = currentTime;

            _elapsedTime += delta;
            _testShaker.Animate(_elapsedTime);

            SceneView.RepaintAll();
            Repaint();

            if (_elapsedTime >= _data.Duration + _data.Delay)
                StopTest();
        }
    }
}
