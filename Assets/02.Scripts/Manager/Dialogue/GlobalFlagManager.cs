using System.Collections.Generic;
using UnityEngine;

namespace Dialogue
{
    // 대화/퀘스트 플래그 단일 저장소
    // 세이브 시 이 딕셔너리 전체를 직렬화하면 됩니다
    public class GlobalFlagManager : MonoBehaviour
    {
        public static GlobalFlagManager Instance { get; private set; }

        private readonly Dictionary<string, bool> _flags = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool GetFlag(string key) => _flags.GetValueOrDefault(key, false);
        public void SetFlag(string key, bool value) => _flags[key] = value;

        // 세이브 연동 시 이 메서드로 일괄 복원
        public void LoadFlags(Dictionary<string, bool> saved)
        {
            _flags.Clear();
            foreach (var kv in saved) _flags[kv.Key] = kv.Value;
        }

        public Dictionary<string, bool> GetAllFlags() => new(_flags);
    }
}
