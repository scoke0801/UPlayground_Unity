using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 샘플 데이터 프리셋 생성 유틸리티
/// 메뉴: Tools/RPG/Create Sample Presets
/// </summary>
public class SampleDataPresets
{
#if UNITY_EDITOR
    private const string DATA_PATH = "Assets/ScriptableObjects/";
    
    [MenuItem("Tools/RPG/Create Sample Presets/Create All")]
    public static void CreateAllPresets()
    {
        CreateCharacterStatsPresets();
        CreateMovementSettingsPresets();
        CreatePlayerSettingsPreset();
        
        Debug.Log("✅ 모든 샘플 프리셋이 생성되었습니다!");
        AssetDatabase.Refresh();
    }
    
    [MenuItem("Tools/RPG/Create Sample Presets/1. Character Stats")]
    public static void CreateCharacterStatsPresets()
    {
        string path = DATA_PATH + "CharacterStats/";
        EnsureDirectoryExists(path);
        
        // 전사 스탯
        var warriorStats = ScriptableObject.CreateInstance<CharacterStatsData>();
        warriorStats.maxHP = 200f;
        warriorStats.maxMP = 50f;
        warriorStats.maxStamina = 150f;
        warriorStats.hpRegenRate = 5f;
        warriorStats.mpRegenRate = 8f;
        warriorStats.staminaRegenRate = 25f;
        warriorStats.jumpStaminaCost = 20f;
        warriorStats.rollStaminaCost = 30f;
        warriorStats.runStaminaCostPerSecond = 10f;
        AssetDatabase.CreateAsset(warriorStats, path + "WarriorStats.asset");
        
        // 마법사 스탯
        var mageStats = ScriptableObject.CreateInstance<CharacterStatsData>();
        mageStats.maxHP = 80f;
        mageStats.maxMP = 200f;
        mageStats.maxStamina = 80f;
        mageStats.hpRegenRate = 3f;
        mageStats.mpRegenRate = 20f;
        mageStats.staminaRegenRate = 15f;
        mageStats.jumpStaminaCost = 25f;
        mageStats.rollStaminaCost = 35f;
        mageStats.runStaminaCostPerSecond = 12f;
        AssetDatabase.CreateAsset(mageStats, path + "MageStats.asset");
        
        // 도적 스탯
        var rogueStats = ScriptableObject.CreateInstance<CharacterStatsData>();
        rogueStats.maxHP = 120f;
        rogueStats.maxMP = 100f;
        rogueStats.maxStamina = 180f;
        rogueStats.hpRegenRate = 4f;
        rogueStats.mpRegenRate = 12f;
        rogueStats.staminaRegenRate = 30f;
        rogueStats.jumpStaminaCost = 15f;
        rogueStats.rollStaminaCost = 20f;
        rogueStats.runStaminaCostPerSecond = 8f;
        AssetDatabase.CreateAsset(rogueStats, path + "RogueStats.asset");
        
        Debug.Log("✅ Character Stats 프리셋 생성 완료");
    }
    
    [MenuItem("Tools/RPG/Create Sample Presets/2. Movement Settings")]
    public static void CreateMovementSettingsPresets()
    {
        string path = DATA_PATH + "MovementSettings/";
        EnsureDirectoryExists(path);
        
        // 느린 이동 (중장비 캐릭터)
        var heavyMovement = ScriptableObject.CreateInstance<MovementSettingsData>();
        heavyMovement.walkSpeed = 2.5f;
        heavyMovement.runSpeed = 5f;
        heavyMovement.rotationSpeed = 300f;
        heavyMovement.jumpPower = 4f;
        heavyMovement.groundCheckDistance = 0.1f;
        heavyMovement.rollDistance = 2.5f;
        heavyMovement.rollDuration = 0.6f;
        AssetDatabase.CreateAsset(heavyMovement, path + "HeavyMovement.asset");
        
        // 표준 이동
        var standardMovement = ScriptableObject.CreateInstance<MovementSettingsData>();
        standardMovement.walkSpeed = 3f;
        standardMovement.runSpeed = 6f;
        standardMovement.rotationSpeed = 360f;
        standardMovement.jumpPower = 5f;
        standardMovement.groundCheckDistance = 0.1f;
        standardMovement.rollDistance = 3f;
        standardMovement.rollDuration = 0.5f;
        AssetDatabase.CreateAsset(standardMovement, path + "StandardMovement.asset");
        
        // 빠른 이동 (경량 캐릭터)
        var fastMovement = ScriptableObject.CreateInstance<MovementSettingsData>();
        fastMovement.walkSpeed = 3.5f;
        fastMovement.runSpeed = 7f;
        fastMovement.rotationSpeed = 450f;
        fastMovement.jumpPower = 5.5f;
        fastMovement.groundCheckDistance = 0.1f;
        fastMovement.rollDistance = 4f;
        fastMovement.rollDuration = 0.4f;
        AssetDatabase.CreateAsset(fastMovement, path + "FastMovement.asset");
        
        Debug.Log("✅ Movement Settings 프리셋 생성 완료");
    }
    
    [MenuItem("Tools/RPG/Create Sample Presets/3. Player Settings")]
    public static void CreatePlayerSettingsPreset()
    {
        string path = DATA_PATH + "PlayerSettings/";
        EnsureDirectoryExists(path);
        
        var playerSettings = ScriptableObject.CreateInstance<PlayerSettingsData>();
        playerSettings.mouseSensitivity = 2f;
        playerSettings.cameraDistance = 5f;
        playerSettings.cameraHeight = 2f;
        playerSettings.cameraMinVerticalAngle = -80f;
        playerSettings.cameraMaxVerticalAngle = 80f;
        playerSettings.cameraSmoothness = 10f;
        AssetDatabase.CreateAsset(playerSettings, path + "DefaultPlayerSettings.asset");
        
        Debug.Log("✅ Player Settings 프리셋 생성 완료");
    }
    
    private static void EnsureDirectoryExists(string path)
    {
        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
    }
#endif
}
