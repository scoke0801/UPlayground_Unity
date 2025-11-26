using UnityEngine;
using UnityEditor;
using Game.Skills;

/// <summary>
/// 스킬 데이터 생성 유틸리티
/// </summary>
public class SkillDataCreator : EditorWindow
{
    private string skillName = "새 스킬";
    private SkillType skillType = SkillType.Instant;
    private float cooldownTime = 5f;
    
    [MenuItem("Tools/Skill/스킬 데이터 생성기")]
    public static void ShowWindow()
    {
        GetWindow<SkillDataCreator>("스킬 생성기");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("스킬 데이터 생성", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(10);
        
        skillName = EditorGUILayout.TextField("스킬 이름", skillName);
        skillType = (SkillType)EditorGUILayout.EnumPopup("스킬 타입", skillType);
        cooldownTime = EditorGUILayout.FloatField("쿨다운 시간", cooldownTime);
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("스킬 데이터 생성", GUILayout.Height(30)))
        {
            CreateSkillData();
        }
        
        EditorGUILayout.Space(20);
        
        if (GUILayout.Button("테스트 스킬 4개 생성", GUILayout.Height(30)))
        {
            CreateTestSkills();
        }
    }
    
    private void CreateSkillData()
    {
        SkillData skillData = CreateInstance<SkillData>();
        
        string path = "Assets/Data/Skills";
        
        // 폴더가 없으면 생성
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Skills"))
            AssetDatabase.CreateFolder("Assets/Data", "Skills");
        
        // 파일명 생성
        string fileName = $"{skillName.Replace(" ", "_")}.asset";
        string fullPath = $"{path}/{fileName}";
        
        // 중복 체크
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
        
        // 저장
        AssetDatabase.CreateAsset(skillData, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // 선택
        Selection.activeObject = skillData;
        EditorGUIUtility.PingObject(skillData);
        
        Debug.Log($"[SkillDataCreator] 스킬 데이터 생성 완료: {fullPath}");
    }
    
    private void CreateTestSkills()
    {
        string path = "Assets/Data/Skills";
        
        // 폴더가 없으면 생성
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Skills"))
            AssetDatabase.CreateFolder("Assets/Data", "Skills");
        
        // 테스트 스킬 1: 파이어볼 (즉시 발동)
        CreateTestSkill(path, "파이어볼", SkillType.Instant, 3f, "화염 구체를 발사합니다.");
        
        // 테스트 스킬 2: 차징 샷 (차징)
        CreateTestSkill(path, "차징_샷", SkillType.Charged, 5f, "차징하여 강력한 공격을 합니다.");
        
        // 테스트 스킬 3: 실드 (토글)
        CreateTestSkill(path, "보호막", SkillType.Toggle, 10f, "일정 시간 동안 보호막을 생성합니다.");
        
        // 테스트 스킬 4: 힐링 (지속 시전)
        CreateTestSkill(path, "힐링", SkillType.Channeling, 8f, "지속적으로 체력을 회복합니다.");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[SkillDataCreator] 테스트 스킬 4개 생성 완료");
    }
    
    private void CreateTestSkill(string path, string name, SkillType type, float cooldown, string description)
    {
        SkillData skillData = CreateInstance<SkillData>();
        
        string fileName = $"TestSkill_{name}.asset";
        string fullPath = $"{path}/{fileName}";
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
        
        AssetDatabase.CreateAsset(skillData, fullPath);
    }
}
