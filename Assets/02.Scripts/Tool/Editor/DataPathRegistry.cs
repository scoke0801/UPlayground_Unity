#if UNITY_EDITOR
using UnityEditor;

namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// 에디터 툴에서 공통으로 사용하는 데이터 저장/검색 경로 모음.
    /// 런타임 데이터 구조를 바꾸지 않고, 툴별 하드코딩 경로를 점진적으로 모으기 위한 기준점이다.
    /// </summary>
    public static class DataPathRegistry
    {
        public const string DataRoot = "Assets/10.Datas";

        public const string ActorRoot = DataRoot + "/Actor";
        public const string ActorDatabase = ActorRoot + "/DataBase";
        public const string ActorEnemy = ActorRoot + "/Enemy";
        public const string ActorNpc = ActorRoot + "/Npc";
        public const string ActorPlayer = ActorRoot + "/Player";
        public const string ActorMotion = ActorRoot + "/Animation/ActorMotion";
        public const string WeaponMotion = ActorRoot + "/Animation/WeaponMotion";

        public const string AIRoot = DataRoot + "/AI";
        public const string BehaviorTreeRoot = DataRoot + "/AI/BehaviorTree";
        public const string BehaviorTreeSourceJson = BehaviorTreeRoot + "/SourceJson";
        public const string BehaviorTreeGenerated = BehaviorTreeRoot + "/Generated";

        public const string BalanceRoot = DataRoot + "/Balance";
        public const string CameraRoot = DataRoot + "/Camera";
        public const string CraftRoot = DataRoot + "/Craft";
        public const string DialogueRoot = DataRoot + "/Dialogue";
        public const string ItemRoot = DataRoot + "/Item";
        public const string PartyRoot = DataRoot + "/Party";
        public const string PathRoot = DataRoot + "/Path";
        public const string QuestRoot = DataRoot + "/Quest";
        public const string StatRoot = DataRoot + "/Stat";
        public const string StatGenerated = StatRoot + "/Generated";
        public const string StatPlayer = StatRoot + "/Player";
        public const string StatTemplate = StatRoot + "/Template";
        public const string UIRoot = DataRoot + "/UI";

        public static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
