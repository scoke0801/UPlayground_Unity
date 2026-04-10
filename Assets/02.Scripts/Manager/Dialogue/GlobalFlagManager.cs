using System.Collections.Generic;
using UPlayGround.Manager;
using UPlayGround.Data.Save;

namespace UPlayGround.Dialogue
{
    // 대화/퀘스트 플래그 단일 저장소
    public class GlobalFlagManager : BaseManager<GlobalFlagManager>, IManager, ISaveable
    {
        public static GlobalFlagManager Instance { get; private set; }

        private readonly Dictionary<string, bool> _flags = new();

        #region IManager
        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit()
        {
        }

        public void Dispose()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneChanged(string sceneType)
        {
        }
        #endregion

        public bool GetFlag(string key) => _flags.GetValueOrDefault(key, false);
        public void SetFlag(string key, bool value) => _flags[key] = value;

        // 세이브 연동 시 이 메서드로 일괄 복원
        public void LoadFlags(Dictionary<string, bool> saved)
        {
            _flags.Clear();
            foreach (var kv in saved) _flags[kv.Key] = kv.Value;
        }

        public Dictionary<string, bool> GetAllFlags() => new(_flags);

        #region ISaveable

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.flags.flags = GetAllFlags();
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            LoadFlags(saveData.flags.flags ?? new Dictionary<string, bool>());
        }

        #endregion
    }
}
