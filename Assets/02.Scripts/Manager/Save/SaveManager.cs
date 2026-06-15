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
        private const string SAVE_FILE_EXTENSION = ".sav";   // 암호화 바이너리
        private const string CURRENT_SAVE_VERSION = "2.0";    // 1.0=평문 JSON, 2.0=AES 암호화

        /// <summary> 지원하는 세이브 슬롯 개수 (0 ~ MAX_SLOTS-1). </summary>
        public const int MAX_SLOTS = 3;

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
            if (!IsValidSlot(slot))
            {
                Debug.LogError($"[SaveManager] 잘못된 슬롯 번호 {slot} (유효 범위 0~{MAX_SLOTS - 1}). 저장 중단.");
                return;
            }

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
                byte[] encrypted = SaveCrypto.Encrypt(json);
                File.WriteAllBytes(GetSavePath(slot), encrypted);
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
            return LoadGameInternal(slot, out _);
        }

        /// <summary>
        /// 슬롯을 로드한 뒤 저장된 씬으로 진입한다. 타이틀/다른 맵에서 호출하는 경우 사용.
        /// 흐름: 데이터 복원(각 매니저가 pending 보관) → 저장된 씬 로드 →
        ///       씬 준비 시 OnSceneChanged에서 파티·위치·월드 상태가 적용된다.
        /// </summary>
        public bool LoadGameToScene(int slot = 0)
        {
            if (!LoadGameInternal(slot, out var data))
                return false;

            string sceneName = data?.party?.loadSceneName;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning($"[SaveManager] 슬롯 {slot}에 진입할 씬 정보가 없습니다. 씬 전환을 건너뜁니다.");
                return true;
            }

            SceneManager.Instance.LoadScene(sceneName);
            return true;
        }

        /// <summary>
        /// 슬롯을 읽어 복호화·역직렬화하고 모든 ISaveable에 ImportSaveData를 디스패치한다.
        /// </summary>
        private bool LoadGameInternal(int slot, out GameSaveData data)
        {
            data = null;

            if (!IsValidSlot(slot))
            {
                Debug.LogError($"[SaveManager] 잘못된 슬롯 번호 {slot} (유효 범위 0~{MAX_SLOTS - 1}). 로드 중단.");
                return false;
            }

            string path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] 슬롯 {slot} 세이브 파일 없음: {path}");
                return false;
            }

            try
            {
                string json = SaveCrypto.Decrypt(File.ReadAllBytes(path));
                data = JsonConvert.DeserializeObject<GameSaveData>(json);

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
        public bool HasSaveFile(int slot = 0) => IsValidSlot(slot) && File.Exists(GetSavePath(slot));

        /// <summary> 해당 슬롯의 세이브 파일을 삭제한다. </summary>
        public void DeleteSaveFile(int slot = 0)
        {
            if (!IsValidSlot(slot))
            {
                Debug.LogError($"[SaveManager] 잘못된 슬롯 번호 {slot} (유효 범위 0~{MAX_SLOTS - 1}). 삭제 중단.");
                return;
            }

            string path = GetSavePath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveManager] 슬롯 {slot} 삭제 완료");
            }
        }

        /// <summary>
        /// 슬롯의 메타 정보(저장 일시, 버전, 맵, 진행도)를 빠르게 조회한다.
        /// 파일이 없거나 파싱 실패 시 null 반환.
        /// </summary>
        public SaveSlotInfo GetSaveSlotInfo(int slot)
        {
            if (!IsValidSlot(slot)) return null;

            string path = GetSavePath(slot);
            if (!File.Exists(path)) return null;

            try
            {
                string json = SaveCrypto.Decrypt(File.ReadAllBytes(path));
                var partial = JsonConvert.DeserializeObject<GameSaveData>(json);
                return new SaveSlotInfo
                {
                    slot = slot,
                    saveDateTime = partial?.saveDateTime ?? string.Empty,
                    saveVersion = partial?.saveVersion ?? string.Empty,
                    mapId = partial?.party?.mapId ?? string.Empty,
                    storyProgress = partial?.story?.progress ?? 0,
                    filePath = path
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 슬롯 {slot} 메타 조회 실패: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 모든 슬롯(0 ~ MAX_SLOTS-1)의 메타 정보를 조회한다.
        /// 비어 있는 슬롯은 해당 인덱스가 null이다. 슬롯 선택 UI에서 사용.
        /// </summary>
        public SaveSlotInfo[] GetAllSlotInfos()
        {
            var infos = new SaveSlotInfo[MAX_SLOTS];
            for (int i = 0; i < MAX_SLOTS; i++)
                infos[i] = GetSaveSlotInfo(i);
            return infos;
        }

        /// <summary>
        /// 가장 최근에 저장된 슬롯 번호를 반환한다(이어하기용). 저장이 하나도 없으면 -1.
        /// </summary>
        public int GetMostRecentSlot()
        {
            int best = -1;
            string bestDate = null;
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                var info = GetSaveSlotInfo(i);
                if (info == null) continue;
                // saveDateTime은 "yyyy-MM-dd HH:mm:ss" 고정 포맷이라 문자열 비교로 정렬 가능.
                if (bestDate == null || string.CompareOrdinal(info.saveDateTime, bestDate) > 0)
                {
                    bestDate = info.saveDateTime;
                    best = i;
                }
            }
            return best;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 내부

        private string GetSavePath(int slot) =>
            Path.Combine(_saveFolder, $"{SAVE_FILE_PREFIX}{slot}{SAVE_FILE_EXTENSION}");

        /// <summary> 슬롯 번호가 유효 범위(0 ~ MAX_SLOTS-1)인지 검사한다. </summary>
        private static bool IsValidSlot(int slot) => slot >= 0 && slot < MAX_SLOTS;

        #endregion
    }

    /// <summary> 세이브 슬롯 UI 표시용 메타 정보 </summary>
    public class SaveSlotInfo
    {
        public int slot;
        public string saveDateTime;
        public string saveVersion;
        public string mapId;        // 저장 당시 맵 식별자
        public int storyProgress;   // 스토리 진행도
        public string filePath;
    }
}
