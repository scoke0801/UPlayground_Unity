using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Quest
{
    /// <summary>
    /// 모든 QuestSO를 담는 데이터베이스 ScriptableObject.
    /// Addressable 키 "QuestDatabase"로 로드한다.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "UPlayGround/퀘스트/Database")]
    public class QuestDatabase : ScriptableObject
    {
        [SerializeField] private List<QuestSO> _quests = new List<QuestSO>();

        private Dictionary<string, QuestSO> _questMap;

        public void Initialize()
        {
            _questMap = new Dictionary<string, QuestSO>();
            foreach (var q in _quests)
            {
                if (q == null) continue;
                if (_questMap.ContainsKey(q.questId))
                {
                    Debug.LogWarning($"[QuestDatabase] 중복 QuestID: {q.questId}");
                    continue;
                }
                _questMap[q.questId] = q;
            }
            Debug.Log($"[QuestDatabase] {_questMap.Count}개 퀘스트 초기화 완료");
        }

        public QuestSO GetQuest(string questId)
        {
            if (_questMap == null) return null;
            _questMap.TryGetValue(questId, out var q);
            return q;
        }

        public IEnumerable<QuestSO> GetAllQuests() => _quests;
        public List<QuestSO> QuestList => _quests;

        public IEnumerable<string> GetAllQuestIds()
        {
            if (_questMap == null) yield break;
            foreach (var id in _questMap.Keys)
                yield return id;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 전용: 지정 폴더의 모든 QuestSO를 스캔해서 _quests를 갱신한다.
        /// </summary>
        public void RefreshDatabase(string folderPath)
        {
            _quests.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:QuestSO", new[] { folderPath });
            foreach (var guid in guids)
            {
                var q = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestSO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (q != null) _quests.Add(q);
            }
            _quests.Sort((a, b) => string.Compare(a.questId, b.questId, System.StringComparison.Ordinal));
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[QuestDatabase] DB 갱신 완료 — {_quests.Count}개");
        }
#endif
    }
}
