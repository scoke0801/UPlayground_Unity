using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Stat
{
    /// <summary>
    /// 재사용 가능한 스탯 템플릿. StatType→값의 부분 집합을 정의하고,
    /// Stat Generator의 "스탯 재생성" 탭에서 선택해 여러 ActorStatSO에 일괄 적용한다.
    /// (ActorStatSO와 달리 모든 StatType을 가질 필요가 없으며, 정의된 항목만 적용 대상이 된다.)
    /// </summary>
    [CreateAssetMenu(fileName = "StatTemplate_", menuName = "UPlayGround/스탯/Template")]
    public class StatTemplateSO : ScriptableObject
    {
        [TextArea(2, 4)]
        public string description;

        [Tooltip("이 템플릿이 정의하는 스탯 항목. 여기 없는 StatType은 재생성 시 건드리지 않는다.")]
        [SerializeField] private List<ActorStatSO.StatEntry> _entries = new();

        public IReadOnlyList<ActorStatSO.StatEntry> Entries => _entries;

        /// <summary>해당 StatType이 이 템플릿에 명시되어 있으면 값을 반환.</summary>
        public bool TryGet(StatType type, out float value)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].statType == type)
                {
                    value = _entries[i].baseValue;
                    return true;
                }
            }
            value = 0f;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _entries.Sort((a, b) => a.statType.CompareTo(b.statType));
        }

        /// <summary>에디터 전용: 항목을 안전하게 설정한다(있으면 갱신, 없으면 추가).</summary>
        public void EditorSet(StatType type, float value)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].statType == type)
                {
                    var entry = _entries[i];
                    entry.baseValue = value;
                    _entries[i] = entry;
                    return;
                }
            }
            _entries.Add(new ActorStatSO.StatEntry { statType = type, baseValue = value });
        }

        public void EditorRemove(StatType type)
        {
            _entries.RemoveAll(e => e.statType == type);
        }

        public void EditorClear()
        {
            _entries.Clear();
        }

        /// <summary>
        /// 에디터 전용: 이 템플릿을 대상 ActorStatSO에 적용한다.
        /// </summary>
        /// <param name="target">적용 대상 ActorStatSO.</param>
        /// <param name="overwriteExisting">
        /// true면 대상에 이미 명시된 값도 템플릿 값으로 덮어쓴다.
        /// false면 대상에 없는(누락된) 항목만 템플릿 값으로 채운다.
        /// </param>
        /// <returns>실제로 변경된 StatType 개수.</returns>
        public int EditorApplyTo(ActorStatSO target, bool overwriteExisting)
        {
            if (target == null) return 0;

            int changed = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                bool hasExplicit = target.TryGetExplicit(entry.statType, out float current);

                if (hasExplicit && !overwriteExisting)
                    continue; // 덮어쓰기 끔 → 기존 명시값 보존
                if (hasExplicit && Mathf.Approximately(current, entry.baseValue))
                    continue; // 값이 같으면 변경 없음

                target.EditorSet(entry.statType, entry.baseValue);
                changed++;
            }
            return changed;
        }
#endif
    }
}
