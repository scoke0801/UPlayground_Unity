using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AutoFootCurveGenerator : EditorWindow
{
    private GameObject _targetModel;
    private AnimationClip _targetClip;
    
    // 이 높이보다 낮으면 "땅에 닿았다"고 판단 (단위: 미터)
    private float _groundThreshold = 0.1f; 
    
    // 생성할 커브 이름 (Animancer 스크립트와 일치해야 함)
    private string _leftCurveName = "LeftFootIK";
    private string _rightCurveName = "RightFootIK";

    [MenuItem("Tools/Auto Foot IK Curve Generator")]
    public static void ShowWindow()
    {
        GetWindow<AutoFootCurveGenerator>("IK Curve Gen");
    }

    private void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);

        _targetModel = (GameObject)EditorGUILayout.ObjectField("Character Model", _targetModel, typeof(GameObject), true);
        _targetClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", _targetClip, typeof(AnimationClip), false);

        GUILayout.Space(10);
        GUILayout.Label("Bone Names (Hierarchy Search)", EditorStyles.boldLabel);

        GUILayout.Space(10);
        GUILayout.Label("Parameters", EditorStyles.boldLabel);
        _groundThreshold = EditorGUILayout.FloatField("Ground Threshold", _groundThreshold);
        _leftCurveName = EditorGUILayout.TextField("Left Curve Name", _leftCurveName);
        _rightCurveName = EditorGUILayout.TextField("Right Curve Name", _rightCurveName);

        GUILayout.Space(20);

        if (GUILayout.Button("Generate Curves"))
        {
            if (_targetModel == null || _targetClip == null)
            {
                Debug.LogError("모델과 애니메이션 클립을 모두 할당해주세요.");
                return;
            }
            
            GenerateCurves();
        }
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("주의: FBX에 포함된 'Read-Only' 클립은 수정할 수 없습니다. 클립을 선택하고 Ctrl+D로 복제한 뒤 사용하세요.", MessageType.Warning);
    }

    private void GenerateCurves()
    {
        // 1. 임시 모델 생성 (샘플링용)
        GameObject instance = Instantiate(_targetModel, Vector3.zero, Quaternion.identity);
        
        try 
        {
            // 본 트랜스폼 찾기
            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            if (leftFoot == null || rightFoot == null)
            {
                Debug.LogError($"본을 찾을 수 없습니다.");
                return;
            }

            // 커브 데이터 준비
            AnimationCurve curveL = new AnimationCurve();
            AnimationCurve curveR = new AnimationCurve();

            // 2. 프레임 단위 샘플링 (Frame Rate에 맞춤)
            float frameRate = _targetClip.frameRate;
            float length = _targetClip.length;
            int totalFrames = Mathf.FloorToInt(length * frameRate);

            for (int i = 0; i <= totalFrames; i++)
            {
                float time = i / frameRate;

                // 애니메이션 샘플링 (모델에 해당 시간의 포즈를 강제 적용)
                _targetClip.SampleAnimation(instance, time);

                // 높이 측정 (월드 좌표 Y) -> 임시 객체가 (0,0,0)에 있으므로 world pos 사용 가능
                float heightL = leftFoot.position.y;
                float heightR = rightFoot.position.y;

                // 3. 가중치 계산 (임계값보다 낮으면 1, 높으면 0)
                // 부드러운 전환을 위해 Mathf.InverseLerp 등을 사용할 수도 있음
                float weightL = heightL <= _groundThreshold ? 1.0f : 0.0f;
                float weightR = heightR <= _groundThreshold ? 1.0f : 0.0f;

                curveL.AddKey(new Keyframe(time, weightL));
                curveR.AddKey(new Keyframe(time, weightR));
            }
            
            // 4. 커브 부드럽게 만들기 (선택 사항 - 계단 현상 방지)
            for (int i = 0; i < curveL.length; i++) AnimationUtility.SetKeyLeftTangentMode(curveL, i, AnimationUtility.TangentMode.Auto);
            for (int i = 0; i < curveR.length; i++) AnimationUtility.SetKeyLeftTangentMode(curveR, i, AnimationUtility.TangentMode.Auto);

            // 5. 클립에 커브 쓰기
            // SetEditorCurve는 에디터 전용 함수입니다.
            AnimationUtility.SetEditorCurve(_targetClip, EditorCurveBinding.FloatCurve("", typeof(Animator), _leftCurveName), curveL);
            AnimationUtility.SetEditorCurve(_targetClip, EditorCurveBinding.FloatCurve("", typeof(Animator), _rightCurveName), curveR);
            
            EditorUtility.SetDirty(_targetClip);
            AssetDatabase.SaveAssets(); // 변경된 모든 에셋을 디스크에 저장
            AssetDatabase.Refresh();    // 프로젝트 창 새로고침
            
            Debug.Log($"성공적으로 커브를 추가했습니다: {_targetClip.name}");
        }
        finally
        {
            // 임시 모델 삭제
            DestroyImmediate(instance);
        }
    }

    // 자식 오브젝트 재귀 검색 헬퍼 함수
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}