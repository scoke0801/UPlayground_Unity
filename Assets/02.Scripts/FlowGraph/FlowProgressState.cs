using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Save;

namespace UPlayGround.FlowGraph
{
    /// <summary>
    /// FlowGraph 진행 기록 저장소.
    ///
    /// 두 스코프를 구분해 보관한다.
    ///   · 세션 스코프(OncePerSession) — 플레이 세션 동안만 유지. 세이브에 남기지 않는다.
    ///   · 세이브 스코프(OncePerSave)   — 세이브 파일에 기록돼 로드 후에도 유지된다.
    /// 여기에 더해 진입점별 발화/완주 횟수를 남겨 "흐름이 어디까지 진행됐는지"를 조회할 수 있게 한다.
    ///
    /// 실행 중인 토큰 위치는 저장하지 않는다(대사·컷신·스폰 등 부수효과를 중간부터 재현할 수 없다).
    /// 노드/러너 인스턴스가 아닌 static 저장소인 이유는 씬 재로드·러너 재생성을 건너 살아남아야 하기 때문이며,
    /// 도메인 리로드 비활성화 환경에서도 매 플레이 진입 시 초기화되도록 SubsystemRegistration에서 리셋한다
    /// (ManagerLifecycle 패턴 계승).
    /// </summary>
    public static class FlowProgressState
    {
        private sealed class EntryProgress
        {
            public int FireCount;
            public int CompleteCount;
        }

        private static readonly HashSet<string> SessionFiredKeys = new();
        private static readonly HashSet<string> SavedFiredKeys = new();
        private static readonly Dictionary<string, EntryProgress> Entries = new();

        /// <summary>세이브 로드/새 게임으로 기록 전체가 교체된 뒤 발화한다.</summary>
        public static event Action OnProgressReloaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            SessionFiredKeys.Clear();
            SavedFiredKeys.Clear();
            Entries.Clear();
        }

        // ──────────────────────────────────────────────────────────
        #region 발화 기록

        /// <summary>세션 스코프 1회 발화 기록. 처음이면 true.</summary>
        public static bool TryMarkFired(string key)
        {
            return !string.IsNullOrEmpty(key) && SessionFiredKeys.Add(key);
        }

        /// <summary>세이브 스코프 1회 발화 기록. 처음이면 true.</summary>
        public static bool TryMarkFiredPersistent(string key)
        {
            return !string.IsNullOrEmpty(key) && SavedFiredKeys.Add(key);
        }

        public static bool IsFiredPersistent(string key)
        {
            return !string.IsNullOrEmpty(key) && SavedFiredKeys.Contains(key);
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 진입점 진행도

        public static string MakeEntryKey(string graphId, string nodeId) => $"{graphId}:{nodeId}";

        public static void MarkEntryStarted(string graphId, string nodeId)
        {
            GetOrCreate(MakeEntryKey(graphId, nodeId)).FireCount++;
        }

        public static void MarkEntryCompleted(string graphId, string nodeId)
        {
            GetOrCreate(MakeEntryKey(graphId, nodeId)).CompleteCount++;
        }

        /// <summary>진입점이 한 번이라도 발화된 적 있는가.</summary>
        public static bool IsEntryStarted(string graphId, string nodeId)
        {
            return Entries.TryGetValue(MakeEntryKey(graphId, nodeId), out EntryProgress progress)
                   && progress.FireCount > 0;
        }

        /// <summary>진입점에서 시작한 흐름이 한 번이라도 끝까지 완주한 적 있는가.</summary>
        public static bool IsEntryCompleted(string graphId, string nodeId)
        {
            return Entries.TryGetValue(MakeEntryKey(graphId, nodeId), out EntryProgress progress)
                   && progress.CompleteCount > 0;
        }

        /// <summary>발화됐지만 아직 완주 기록이 없는 진행 중(또는 중단된) 흐름.</summary>
        public static bool IsEntryInProgress(string graphId, string nodeId)
        {
            return Entries.TryGetValue(MakeEntryKey(graphId, nodeId), out EntryProgress progress)
                   && progress.FireCount > progress.CompleteCount;
        }

        private static EntryProgress GetOrCreate(string key)
        {
            if (!Entries.TryGetValue(key, out EntryProgress progress))
            {
                progress = new EntryProgress();
                Entries[key] = progress;
            }
            return progress;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 세이브 연동

        public static FlowProgressSaveData Export()
        {
            var data = new FlowProgressSaveData();
            data.firedKeys.AddRange(SavedFiredKeys);
            data.firedKeys.Sort(StringComparer.Ordinal); // 세이브 파일 diff 안정화

            foreach (KeyValuePair<string, EntryProgress> pair in Entries)
            {
                data.entries.Add(new FlowEntryProgressSave
                {
                    key = pair.Key,
                    fireCount = pair.Value.FireCount,
                    completeCount = pair.Value.CompleteCount,
                });
            }
            data.entries.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
            return data;
        }

        /// <summary>
        /// 세이브 기록을 복원한다. 세션 스코프(OncePerSession) 기록은 세이브에 속하지 않으므로
        /// 건드리지 않는다 — 로드는 플레이 세션을 끝내지 않는다.
        /// </summary>
        public static void Import(FlowProgressSaveData data)
        {
            SavedFiredKeys.Clear();
            Entries.Clear();

            if (data != null)
            {
                if (data.firedKeys != null)
                {
                    for (int i = 0; i < data.firedKeys.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(data.firedKeys[i]))
                            SavedFiredKeys.Add(data.firedKeys[i]);
                    }
                }

                if (data.entries != null)
                {
                    for (int i = 0; i < data.entries.Count; i++)
                    {
                        FlowEntryProgressSave entry = data.entries[i];
                        if (entry == null || string.IsNullOrEmpty(entry.key))
                            continue;

                        Entries[entry.key] = new EntryProgress
                        {
                            FireCount = entry.fireCount,
                            CompleteCount = entry.completeCount,
                        };
                    }
                }
            }

            OnProgressReloaded?.Invoke();
        }

        /// <summary>새 게임 초기화. 세션 기록까지 포함해 전부 비운다.</summary>
        public static void ResetAll()
        {
            SessionFiredKeys.Clear();
            SavedFiredKeys.Clear();
            Entries.Clear();
            OnProgressReloaded?.Invoke();
        }

        #endregion
    }
}
