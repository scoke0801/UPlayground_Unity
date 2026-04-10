using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UPlayGround.Data.Save;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 게임 세이브/로드를 총괄하는 매니저.
    ///
    /// 사용 예시:
    ///   SaveManager.Instance.SaveGame(0);      // 슬롯 0에 저장
    ///   SaveManager.Instance.LoadGame(0);      // 슬롯 0에서 로드
    ///   SaveManager.Instance.HasSaveFile(0);   // 세이브 파일 존재 여부
    ///
    /// ISaveable을 구현한 매니저는 RegisterSaveable()로 자신을 등록해야 한다.
    /// GameManager.InitializeManagers()에서 SaveManager 등록 이후에 각 매니저가 등록한다.
    /// </summary>
    public class SaveManager : BaseManager<SaveManager>, IManager
    {
        private const string SAVE_FOLDER = "saves";
        private const string SAVE_FILE_PREFIX = "save_slot_";
        private const string SAVE_FILE_EXTENSION = ".json";
        private const string CURRENT_SAVE_VERSION = "1.0";

        private readonly List<ISaveable> _saveables = new List<ISaveable>();

        private string _saveFolder;

        #region IManager

        public void Init()
        {
            _saveFolder = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            Directory.CreateDirectory(_saveFolder);
        }

        public void AfterInit() { }
        public void Dispose() { }
        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) { }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 등록

        /// <summary>
        /// ISaveable 매니저를 세이브 시스템에 등록한다.
        /// Init() 이후 어느 시점에나 호출 가능하다.
        /// </summary>
        public void RegisterSaveable(ISaveable saveable)
        {
            if (saveable == null) return;
            if (!_saveables.Contains(saveable))
                _saveables.Add(saveable);
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 저장

        /// <summary>
        /// 현재 게임 상태를 지정한 슬롯에 저장한다.
        /// </summary>
        public void SaveGame(int slot = 0)
        {
            var data = new GameSaveData
            {
                saveVersion = CURRENT_SAVE_VERSION,
                saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            bool exportFailed = false;
            foreach (var saveable in _saveables)
            {
                try
                {
                    saveable.ExportSaveData(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveManager] {saveable.GetType().Name} 저장 실패: {e.Message}");
                    exportFailed = true;
                }
            }

            if (exportFailed)
            {
                Debug.LogError($"[SaveManager] 슬롯 {slot} 일부 데이터 수집 실패로 파일 쓰기를 중단합니다.");
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(GetSavePath(slot), json, System.Text.Encoding.UTF8);
                Debug.Log($"[SaveManager] 슬롯 {slot} 저장 완료 → {GetSavePath(slot)}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 슬롯 {slot} 파일 쓰기 실패: {e.Message}");
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 로드

        /// <summary>
        /// 지정한 슬롯에서 게임 상태를 로드한다.
        /// 각 ISaveable 매니저에 ImportSaveData()를 호출한다.
        /// DB 로드가 비동기인 매니저(InventoryManager, RecipeManager)는
        /// pending 데이터를 보관했다가 DB 준비 완료 시 자동 복원한다.
        /// </summary>
        /// <returns>세이브 파일이 존재하고 로드에 성공하면 true</returns>
        public bool LoadGame(int slot = 0)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] 슬롯 {slot} 세이브 파일 없음: {path}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var data = JsonConvert.DeserializeObject<GameSaveData>(json);

                if (data == null)
                {
                    Debug.LogError($"[SaveManager] 슬롯 {slot} 역직렬화 실패");
                    return false;
                }

                bool importFailed = false;
                foreach (var saveable in _saveables)
                {
                    try
                    {
                        saveable.ImportSaveData(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SaveManager] {saveable.GetType().Name} 로드 실패: {e.Message}");
                        importFailed = true;
                    }
                }

                if (importFailed)
                    Debug.LogWarning($"[SaveManager] 슬롯 {slot} 일부 데이터 복원 실패 (저장 일시: {data.saveDateTime})");
                else
                    Debug.Log($"[SaveManager] 슬롯 {slot} 로드 완료 (저장 일시: {data.saveDateTime})");

                return !importFailed;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 슬롯 {slot} 로드 중 예외: {e.Message}");
                return false;
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 슬롯 관리

        /// <summary> 해당 슬롯에 세이브 파일이 존재하는지 확인한다. </summary>
        public bool HasSaveFile(int slot = 0) => File.Exists(GetSavePath(slot));

        /// <summary> 해당 슬롯의 세이브 파일을 삭제한다. </summary>
        public void DeleteSaveFile(int slot = 0)
        {
            string path = GetSavePath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveManager] 슬롯 {slot} 삭제 완료");
            }
        }

        /// <summary>
        /// 슬롯의 메타 정보(저장 일시, 버전)를 빠르게 조회한다.
        /// 파일이 없거나 파싱 실패 시 null 반환.
        /// </summary>
        public SaveSlotInfo GetSaveSlotInfo(int slot)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var partial = JsonConvert.DeserializeObject<GameSaveData>(json);
                return new SaveSlotInfo
                {
                    slot = slot,
                    saveDateTime = partial?.saveDateTime ?? string.Empty,
                    saveVersion = partial?.saveVersion ?? string.Empty,
                    filePath = path
                };
            }
            catch
            {
                return null;
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 내부

        private string GetSavePath(int slot) =>
            Path.Combine(_saveFolder, $"{SAVE_FILE_PREFIX}{slot}{SAVE_FILE_EXTENSION}");

        #endregion
    }

    /// <summary> 세이브 슬롯 UI 표시용 메타 정보 </summary>
    public class SaveSlotInfo
    {
        public int slot;
        public string saveDateTime;
        public string saveVersion;
        public string filePath;
    }
}
