using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Enemy와 NPC 샘플 프리셋 생성 유틸리티
/// </summary>
public class EnemyNPCPresets
{
#if UNITY_EDITOR
    private const string DATA_PATH = "Assets/ScriptableObjects/";
    
    [MenuItem("Tools/RPG/Create Sample Presets/Enemy & NPC/Create All")]
    public static void CreateAllEnemyNPCPresets()
    {
        CreateEnemyPresets();
        CreateNPCPresets();
        
        Debug.Log("✅ Enemy & NPC 프리셋이 모두 생성되었습니다!");
        AssetDatabase.Refresh();
    }
    
    #region Enemy Presets
    
    [MenuItem("Tools/RPG/Create Sample Presets/Enemy & NPC/1. Enemy Presets")]
    public static void CreateEnemyPresets()
    {
        CreateEnemyAIPresets();
        CreateEnemyCombatPresets();
        CreateEnemyRewardPresets();
        
        Debug.Log("✅ Enemy 프리셋 생성 완료");
    }
    
    private static void CreateEnemyAIPresets()
    {
        string path = DATA_PATH + "Enemy/AI/";
        EnsureDirectoryExists(path);
        
        // 기본 AI
        var standardAI = ScriptableObject.CreateInstance<EnemyAIData>();
        standardAI.detectionRange = 10f;
        standardAI.chaseRange = 15f;
        standardAI.lostTargetTime = 3f;
        standardAI.patrolRange = 5f;
        standardAI.patrolWaitTimeMin = 1f;
        standardAI.patrolWaitTimeMax = 3f;
        standardAI.attackRange = 2f;
        standardAI.attackCooldown = 2f;
        AssetDatabase.CreateAsset(standardAI, path + "StandardAI.asset");
        
        // 공격적인 AI
        var aggressiveAI = ScriptableObject.CreateInstance<EnemyAIData>();
        aggressiveAI.detectionRange = 15f;
        aggressiveAI.chaseRange = 20f;
        aggressiveAI.lostTargetTime = 5f;
        aggressiveAI.patrolRange = 8f;
        aggressiveAI.patrolWaitTimeMin = 0.5f;
        aggressiveAI.patrolWaitTimeMax = 2f;
        aggressiveAI.attackRange = 2.5f;
        aggressiveAI.attackCooldown = 1.5f;
        AssetDatabase.CreateAsset(aggressiveAI, path + "AggressiveAI.asset");
        
        // 소극적인 AI
        var passiveAI = ScriptableObject.CreateInstance<EnemyAIData>();
        passiveAI.detectionRange = 5f;
        passiveAI.chaseRange = 8f;
        passiveAI.lostTargetTime = 2f;
        passiveAI.patrolRange = 3f;
        passiveAI.patrolWaitTimeMin = 2f;
        passiveAI.patrolWaitTimeMax = 5f;
        passiveAI.attackRange = 1.5f;
        passiveAI.attackCooldown = 3f;
        AssetDatabase.CreateAsset(passiveAI, path + "PassiveAI.asset");
    }
    
    private static void CreateEnemyCombatPresets()
    {
        string path = DATA_PATH + "Enemy/Combat/";
        EnsureDirectoryExists(path);
        
        // 약한 적 전투
        var weakCombat = ScriptableObject.CreateInstance<EnemyCombatData>();
        weakCombat.baseAttackDamage = 10f;
        weakCombat.attackDamagePerLevel = 3f;
        weakCombat.hitInvincibilityTime = 0.1f;
        weakCombat.attackPatterns = new List<AttackPattern>();
        AssetDatabase.CreateAsset(weakCombat, path + "WeakCombat.asset");
        
        // 표준 전투
        var standardCombat = ScriptableObject.CreateInstance<EnemyCombatData>();
        standardCombat.baseAttackDamage = 20f;
        standardCombat.attackDamagePerLevel = 5f;
        standardCombat.hitInvincibilityTime = 0.1f;
        standardCombat.attackPatterns = new List<AttackPattern>
        {
            new AttackPattern("기본 공격", 1, 1f, 0.5f),
            new AttackPattern("강공격", 1, 1.5f, 0.7f)
        };
        AssetDatabase.CreateAsset(standardCombat, path + "StandardCombat.asset");
        
        // 보스 전투
        var bossCombat = ScriptableObject.CreateInstance<EnemyCombatData>();
        bossCombat.baseAttackDamage = 40f;
        bossCombat.attackDamagePerLevel = 10f;
        bossCombat.hitInvincibilityTime = 0.2f;
        bossCombat.attackPatterns = new List<AttackPattern>
        {
            new AttackPattern("연속 공격", 3, 1f, 0.3f),
            new AttackPattern("강력한 일격", 1, 2f, 1f),
            new AttackPattern("광역 공격", 1, 1.5f, 0.8f)
        };
        AssetDatabase.CreateAsset(bossCombat, path + "BossCombat.asset");
    }
    
    private static void CreateEnemyRewardPresets()
    {
        string path = DATA_PATH + "Enemy/Rewards/";
        EnsureDirectoryExists(path);
        
        // 일반 적 보상
        var normalReward = ScriptableObject.CreateInstance<EnemyRewardData>();
        normalReward.baseExperience = 50;
        normalReward.experiencePerLevel = 25;
        normalReward.minGoldDrop = 10;
        normalReward.maxGoldDrop = 30;
        normalReward.goldIncreasePerLevel = 10f;
        normalReward.dropItems = new List<DropItem>
        {
            new DropItem("potion_hp", "체력 포션", 30f, 1, 1),
            new DropItem("potion_mp", "마나 포션", 20f, 1, 1)
        };
        AssetDatabase.CreateAsset(normalReward, path + "NormalReward.asset");
        
        // 엘리트 보상
        var eliteReward = ScriptableObject.CreateInstance<EnemyRewardData>();
        eliteReward.baseExperience = 150;
        eliteReward.experiencePerLevel = 50;
        eliteReward.minGoldDrop = 50;
        eliteReward.maxGoldDrop = 100;
        eliteReward.goldIncreasePerLevel = 15f;
        eliteReward.dropItems = new List<DropItem>
        {
            new DropItem("potion_hp", "체력 포션", 50f, 1, 2),
            new DropItem("gem_small", "작은 보석", 40f, 1, 3),
            new DropItem("equipment_rare", "희귀 장비", 10f, 1, 1)
        };
        AssetDatabase.CreateAsset(eliteReward, path + "EliteReward.asset");
        
        // 보스 보상
        var bossReward = ScriptableObject.CreateInstance<EnemyRewardData>();
        bossReward.baseExperience = 500;
        bossReward.experiencePerLevel = 100;
        bossReward.minGoldDrop = 200;
        bossReward.maxGoldDrop = 500;
        bossReward.goldIncreasePerLevel = 20f;
        bossReward.dropItems = new List<DropItem>
        {
            new DropItem("potion_hp", "체력 포션", 100f, 3, 5),
            new DropItem("gem_large", "큰 보석", 80f, 2, 5),
            new DropItem("equipment_epic", "영웅 장비", 50f, 1, 1),
            new DropItem("equipment_legendary", "전설 장비", 5f, 1, 1)
        };
        AssetDatabase.CreateAsset(bossReward, path + "BossReward.asset");
    }
    
    #endregion
    
    #region NPC Presets
    
    [MenuItem("Tools/RPG/Create Sample Presets/Enemy & NPC/2. NPC Presets")]
    public static void CreateNPCPresets()
    {
        string path = DATA_PATH + "NPC/";
        EnsureDirectoryExists(path);
        
        // 일반 NPC
        var normalNPC = ScriptableObject.CreateInstance<NPCSettingsData>();
        normalNPC.npcType = NPCType.Normal;
        normalNPC.interactionRange = 3f;
        normalNPC.patrolRange = 5f;
        normalNPC.npcWalkSpeed = 2f;
        normalNPC.idleTimeMin = 2f;
        normalNPC.idleTimeMax = 5f;
        normalNPC.dialogues = new List<string>
        {
            "안녕하세요!",
            "좋은 날씨네요.",
            "여기는 평화로운 마을입니다."
        };
        AssetDatabase.CreateAsset(normalNPC, path + "NormalNPC.asset");
        
        // 퀘스트 제공자
        var questGiver = ScriptableObject.CreateInstance<NPCSettingsData>();
        questGiver.npcType = NPCType.QuestGiver;
        questGiver.interactionRange = 3f;
        questGiver.patrolRange = 3f;
        questGiver.npcWalkSpeed = 1.5f;
        questGiver.idleTimeMin = 3f;
        questGiver.idleTimeMax = 7f;
        questGiver.dialogues = new List<string>
        {
            "도움이 필요하신가요?",
            "마을을 위해 일을 맡아주실 수 있나요?"
        };
        questGiver.availableQuests = new List<Quest>
        {
            new Quest("quest_001", "슬라임 퇴치", "마을 근처 슬라임 10마리를 처치하세요.", 100, 50)
        };
        AssetDatabase.CreateAsset(questGiver, path + "QuestGiverNPC.asset");
        
        // 상인 NPC
        var merchant = ScriptableObject.CreateInstance<NPCSettingsData>();
        merchant.npcType = NPCType.Merchant;
        merchant.interactionRange = 3f;
        merchant.patrolRange = 2f;
        merchant.npcWalkSpeed = 1f;
        merchant.idleTimeMin = 5f;
        merchant.idleTimeMax = 10f;
        merchant.dialogues = new List<string>
        {
            "어서오세요!",
            "좋은 물건들이 많습니다."
        };
        merchant.shopItems = new List<ShopItem>
        {
            new ShopItem("potion_hp", "체력 포션", 50, 1, true),
            new ShopItem("potion_mp", "마나 포션", 40, 1, true),
            new ShopItem("weapon_sword", "철검", 200, 1, false),
            new ShopItem("armor_leather", "가죽 갑옷", 150, 1, false)
        };
        merchant.shopBuyBackPercentage = 50;
        AssetDatabase.CreateAsset(merchant, path + "MerchantNPC.asset");
        
        Debug.Log("✅ NPC 프리셋 생성 완료");
    }
    
    #endregion
    
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
