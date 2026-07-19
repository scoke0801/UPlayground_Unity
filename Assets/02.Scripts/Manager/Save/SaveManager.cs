using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UPlayGround.Data.Save;
using UPlayGround.Data.World;

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
    public class SaveManager : BaseManager<SaveManager>, IManager, UPlayGround.UI.IUISaveService
    {
        private const string SAVE_FOLDER = "saves";
        private const string SAVE_FILE_PREFIX = "save_slot_";
        private const string SAVE_FILE_EXTENSION = ".sav";   // 암호화 바이너리

        /// <summary>
        /// 저장 데이터를 현재 씬에 즉시 적용하지 않고, 저장된 씬 진입 시점까지 보류해야 하는지 여부.
        /// LoadGameToScene의 ImportSaveData 디스패치 동안만 true다.
        /// </summary>
        public bool IsPreparingSceneLoad { get; private set; }
        private const string TEMP_FILE_EXTENSION = ".tmp";
        private const string BACKUP_FILE_EXTENSION = ".bak";
        private const string CURRENT_SAVE_VERSION = "2.0";    // 1.0=평문 JSON, 2.0=AES 암호화

        private static readonly Regex SaveFileRegex = new Regex(
            $"^{Regex.Escape(SAVE_FILE_PREFIX)}(?<slot>\\d+){Regex.Escape(SAVE_FILE_EXTENSION)}(?:{Regex.Escape(BACKUP_FILE_EXTENSION)})?$",
            RegexOptions.Compiled);

        private readonly List<ISaveable> _saveables = new List<ISaveable>();

        private string _saveFolder;
        private bool _isSaving;

        public SaveOperationResult LastOperationResult { get; private set; }
        public int? ActiveSlot { get; private set; }

        #region IManager

        public void Init()
        {
            ActiveSlot = null;
            _saveFolder = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            Directory.CreateDirectory(_saveFolder);
        }

        public void AfterInit() { }
        public void Dispose()
        {
            ActiveSlot = null;
        }
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
            LastOperationResult = SaveGameDetailed(slot);
        }

        public SaveOperationResult SaveGameDetailed(int slot = 0)
        {
            var result = new SaveOperationResult(slot, SaveOperationType.Save);
            if (!IsValidSlot(slot))
            {
                return CompleteWithFailure(
                    result,
                    $"잘못된 슬롯 번호 {slot} (0 이상의 정수만 허용)");
            }

            if (_isSaving)
            {
                return CompleteWithFailure(result, "이미 다른 저장 작업이 진행 중입니다.");
            }

            _isSaving = true;
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
                    result.Failures.Add(new SaveParticipantFailure(
                        saveable.GetType().Name,
                        SaveParticipantStage.Export,
                        e.Message));
                    exportFailed = true;
                }
            }

            if (exportFailed)
            {
                _isSaving = false;
                return CompleteWithFailure(
                    result,
                    $"슬롯 {slot} 일부 데이터 수집 실패로 파일 쓰기를 중단합니다.");
            }

            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                byte[] encrypted = SaveCrypto.Encrypt(json);
                WriteSaveAtomically(slot, encrypted);
                // 저장 성공 직후 UI 제외 화면 캡처를 슬롯 썸네일로 저장(부가 기능, 실패해도 저장은 유효).
                SaveThumbnail.Capture(_saveFolder, slot);
                Debug.Log($"[SaveManager] 슬롯 {slot} 저장 완료 → {GetSavePath(slot)}");
                result.Succeeded = true;
                result.FilePath = GetSavePath(slot);
                ActiveSlot = slot;
                LastOperationResult = result;
                return result;
            }
            catch (Exception e)
            {
                result.Failures.Add(new SaveParticipantFailure(
                    nameof(SaveManager),
                    SaveParticipantStage.FileWrite,
                    e.Message));
                return CompleteWithFailure(result, $"슬롯 {slot} 파일 쓰기 실패: {e.Message}");
            }
            finally
            {
                _isSaving = false;
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 새 게임

        /// <summary>
        /// 새 게임을 시작하기 전에 모든 ISaveable 매니저의 인메모리 상태를 초기화한다.
        ///
        /// 로드와 달리 파일을 읽지 않고, 각 매니저를 신규 실행(fresh launch) 직후와
        /// 동일한 기본 상태로 되돌린다. 한 세션 안에서 플레이 → 타이틀 복귀 → 새 게임을
        /// 했을 때, 이전 플레이의 상태(처치 몬스터·레벨·경험치·플래그·인벤토리 등)가
        /// 새 게임에 누수되는 것을 막는다.
        ///
        /// 호출 시점: 타이틀에서 새 게임 버튼 → 대상 씬 로드 직전.
        /// 이후 씬 초기화 훅(PartyManager.BuildPartyFromScene 등)이 기본값을 재시딩한다.
        /// </summary>
        public void ResetForNewGame()
        {
            // 새 게임이 직전 플레이 슬롯을 자동 저장으로 덮어쓰지 않게 슬롯 귀속도 초기화한다.
            ActiveSlot = null;

            foreach (var saveable in _saveables)
            {
                try
                {
                    saveable.ResetForNewGame();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveManager] {saveable.GetType().Name} 새 게임 초기화 실패: {e.Message}");
                }
            }

            Debug.Log("[SaveManager] 새 게임 상태 초기화 완료");
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
            bool succeeded = LoadGameInternal(slot, out _, out SaveOperationResult result);
            LastOperationResult = result;
            return succeeded;
        }

        /// <summary>
        /// 슬롯을 로드한 뒤 저장된 씬으로 진입한다. 타이틀/다른 맵에서 호출하는 경우 사용.
        /// 흐름: 데이터 복원(각 매니저가 pending 보관) → 저장된 씬 로드 →
        ///       씬 준비 시 OnSceneChanged에서 파티·위치·월드 상태가 적용된다.
        /// </summary>
        public bool LoadGameToScene(int slot = 0)
        {
            GameSaveData data;
            SaveOperationResult result;

            IsPreparingSceneLoad = true;
            try
            {
                if (!LoadGameInternal(slot, out data, out result))
                {
                    LastOperationResult = result;
                    return false;
                }
            }
            finally
            {
                IsPreparingSceneLoad = false;
            }

            LastOperationResult = result;

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
        private bool LoadGameInternal(
            int slot,
            out GameSaveData data,
            out SaveOperationResult result)
        {
            data = null;
            result = new SaveOperationResult(slot, SaveOperationType.Load);

            if (!IsValidSlot(slot))
            {
                CompleteWithFailure(
                    result,
                    $"잘못된 슬롯 번호 {slot} (0 이상의 정수만 허용)");
                return false;
            }

            string path = GetSavePath(slot);
            string backupPath = GetBackupPath(slot);
            if (!File.Exists(path) && !File.Exists(backupPath))
            {
                Debug.LogWarning($"[SaveManager] 슬롯 {slot} 세이브 파일 없음: {path}");
                CompleteWithFailure(result, $"슬롯 {slot} 세이브 파일이 없습니다.");
                return false;
            }

            if (!TryReadSaveData(path, out data, out string primaryError))
            {
                Debug.LogWarning($"[SaveManager] 슬롯 {slot} 본 파일 로드 실패: {primaryError}");
                if (!TryReadSaveData(backupPath, out data, out string backupError))
                {
                    Debug.LogError(
                        $"[SaveManager] 슬롯 {slot} 백업 복구 실패: {backupError}");
                    result.Failures.Add(new SaveParticipantFailure(
                        nameof(SaveManager),
                        SaveParticipantStage.FileRead,
                        $"본 파일={primaryError}, 백업={backupError}"));
                    CompleteWithFailure(result, "본 파일과 백업 파일을 모두 읽지 못했습니다.");
                    return false;
                }

                Debug.LogWarning($"[SaveManager] 슬롯 {slot} 백업 파일로 복구합니다.");
                result.UsedBackup = true;
                TryRestoreBackup(slot);
            }

            try
            {
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
                        result.Failures.Add(new SaveParticipantFailure(
                            saveable.GetType().Name,
                            SaveParticipantStage.Import,
                            e.Message));
                        importFailed = true;
                    }
                }

                if (importFailed)
                    Debug.LogWarning($"[SaveManager] 슬롯 {slot} 일부 데이터 복원 실패 (저장 일시: {data.saveDateTime})");
                else
                    Debug.Log($"[SaveManager] 슬롯 {slot} 로드 완료 (저장 일시: {data.saveDateTime})");

                result.Succeeded = !importFailed;
                result.FilePath = result.UsedBackup ? backupPath : path;
                result.Message = importFailed
                    ? "일부 데이터 복원에 실패했습니다."
                    : "로드가 완료되었습니다.";
                if (result.Succeeded)
                    ActiveSlot = slot;
                return result.Succeeded;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 슬롯 {slot} 데이터 적용 중 예외: {e.Message}");
                result.Failures.Add(new SaveParticipantFailure(
                    nameof(SaveManager),
                    SaveParticipantStage.Import,
                    e.Message));
                CompleteWithFailure(result, "데이터 적용 중 예외가 발생했습니다.");
                return false;
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 슬롯 관리

        /// <summary>
        /// 마지막으로 정상 저장하거나 로드한 슬롯에 현재 상태를 저장한다.
        /// 활성 슬롯이 없으면 임의 슬롯을 덮어쓰지 않고 실패한다.
        /// </summary>
        public bool TrySaveActiveSlot()
        {
            if (!ActiveSlot.HasValue)
                return false;

            return SaveGameDetailed(ActiveSlot.Value).Succeeded;
        }

        /// <summary> 해당 슬롯에 세이브 파일이 존재하는지 확인한다. </summary>
        public bool HasSaveFile(int slot = 0) =>
            IsValidSlot(slot) &&
            (File.Exists(GetSavePath(slot)) || File.Exists(GetBackupPath(slot)));

        /// <summary> 해당 슬롯의 세이브 파일을 삭제한다. </summary>
        public void DeleteSaveFile(int slot = 0)
        {
            if (!IsValidSlot(slot))
            {
                Debug.LogError($"[SaveManager] 잘못된 슬롯 번호 {slot} (0 이상의 정수만 허용). 삭제 중단.");
                return;
            }

            string path = GetSavePath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            string backupPath = GetBackupPath(slot);
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            string tempPath = GetTempPath(slot);
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            SaveThumbnail.Delete(_saveFolder, slot);

            if (ActiveSlot == slot)
                ActiveSlot = null;

            Debug.Log($"[SaveManager] 슬롯 {slot} 삭제 완료");
        }

        /// <summary>
        /// 슬롯 썸네일 Sprite를 반환한다(세이브 슬롯 UI용). 캡처된 파일이 없으면 null.
        /// 결과는 슬롯별로 캐시되며 저장/삭제 시 갱신된다.
        /// </summary>
        public Sprite GetSlotThumbnail(int slot) => SaveThumbnail.GetSprite(_saveFolder, slot);

        /// <summary>
        /// 슬롯의 메타 정보(저장 일시, 버전, 맵, 진행도)를 빠르게 조회한다.
        /// 파일이 없거나 파싱 실패 시 null 반환.
        /// </summary>
        public SaveSlotInfo GetSaveSlotInfo(int slot)
        {
            if (!IsValidSlot(slot)) return null;

            string path = GetSavePath(slot);
            if (!TryReadSaveData(path, out var partial, out _))
            {
                path = GetBackupPath(slot);
                if (!TryReadSaveData(path, out partial, out _))
                    return null;
            }

            return new SaveSlotInfo
            {
                slot = slot,
                saveDateTime = partial?.saveDateTime ?? string.Empty,
                saveVersion = partial?.saveVersion ?? string.Empty,
                mapId = partial?.party?.mapId ?? string.Empty,
                storyProgress = partial?.story?.progress ?? 0,
                elapsedGameDays = CalcElapsedGameDays(partial?.time),
                mainQuestName = QuestManager.Instance.ResolveMainQuestName(partial?.quest) ?? string.Empty,
                filePath = path
            };
        }

        /// <summary>저장 파일이 존재하는 모든 슬롯의 메타 정보를 슬롯 번호 오름차순으로 조회한다.</summary>
        public SaveSlotInfo[] GetAllSlotInfos()
        {
            var indices = GetExistingSlotIndices();
            var infos = new List<SaveSlotInfo>(indices.Count);
            foreach (int index in indices)
            {
                var info = GetSaveSlotInfo(index);
                if (info != null)
                    infos.Add(info);
            }

            return infos.ToArray();
        }

        /// <summary>
        /// 슬롯 선택 UI에 표시할 슬롯 번호 목록을 반환한다.
        /// 저장 모드에서는 기존 저장 슬롯 전체와 가장 낮은 빈 슬롯 1개를 함께 노출한다.
        /// </summary>
        public List<int> GetSlotIndicesForMenu(bool includeNextEmptySlot)
        {
            var indices = GetExistingSlotIndices();
            if (!includeNextEmptySlot)
                return indices;

            int nextEmpty = 0;
            var used = new HashSet<int>(indices);
            while (used.Contains(nextEmpty))
                nextEmpty++;

            indices.Add(nextEmpty);
            indices.Sort();
            return indices;
        }

        /// <summary>
        /// 가장 최근에 저장된 슬롯 번호를 반환한다(이어하기용). 저장이 하나도 없으면 -1.
        /// </summary>
        public int GetMostRecentSlot()
        {
            int best = -1;
            string bestDate = null;
            foreach (int i in GetExistingSlotIndices())
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

        private string GetTempPath(int slot) => GetSavePath(slot) + TEMP_FILE_EXTENSION;
        private string GetBackupPath(int slot) => GetSavePath(slot) + BACKUP_FILE_EXTENSION;

        private void WriteSaveAtomically(int slot, byte[] encrypted)
        {
            string savePath = GetSavePath(slot);
            string tempPath = GetTempPath(slot);
            string backupPath = GetBackupPath(slot);

            if (File.Exists(tempPath))
                File.Delete(tempPath);

            File.WriteAllBytes(tempPath, encrypted);

            if (!TryReadSaveData(tempPath, out _, out string validationError))
            {
                File.Delete(tempPath);
                throw new InvalidDataException(
                    $"임시 세이브 검증 실패: {validationError}");
            }

            if (!File.Exists(savePath))
            {
                File.Move(tempPath, savePath);
                return;
            }

            try
            {
                File.Replace(tempPath, savePath, backupPath);
            }
            catch (PlatformNotSupportedException)
            {
                ReplaceWithPortableFallback(tempPath, savePath, backupPath);
            }
            catch (IOException)
            {
                ReplaceWithPortableFallback(tempPath, savePath, backupPath);
            }
        }

        private static void ReplaceWithPortableFallback(
            string tempPath,
            string savePath,
            string backupPath)
        {
            File.Copy(savePath, backupPath, overwrite: true);
            File.Delete(savePath);
            File.Move(tempPath, savePath);
        }

        private bool TryReadSaveData(
            string path,
            out GameSaveData data,
            out string error)
        {
            data = null;
            error = null;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                error = "파일 없음";
                return false;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                string json = IsPlainJson(bytes)
                    ? Encoding.UTF8.GetString(bytes)
                    : SaveCrypto.Decrypt(bytes);

                data = JsonConvert.DeserializeObject<GameSaveData>(json);
                if (data == null)
                    throw new InvalidDataException("역직렬화 결과가 null입니다.");

                MigrateToCurrentVersion(data);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                data = null;
                return false;
            }
        }

        private static bool IsPlainJson(byte[] bytes)
        {
            if (bytes == null)
                return false;

            for (int i = 0; i < bytes.Length; i++)
            {
                char c = (char)bytes[i];
                if (char.IsWhiteSpace(c))
                    continue;
                return c == '{';
            }

            return false;
        }

        private static void MigrateToCurrentVersion(GameSaveData data)
        {
            string sourceVersion = string.IsNullOrWhiteSpace(data.saveVersion)
                ? "1.0"
                : data.saveVersion;

            if (!Version.TryParse(sourceVersion, out Version parsedSource))
                throw new InvalidDataException($"잘못된 세이브 버전: {sourceVersion}");
            if (!Version.TryParse(CURRENT_SAVE_VERSION, out Version current))
                throw new InvalidOperationException(
                    $"잘못된 현재 세이브 버전: {CURRENT_SAVE_VERSION}");
            if (parsedSource > current)
                throw new InvalidDataException(
                    $"현재 빌드보다 새로운 세이브 버전입니다: {sourceVersion}");

            // 1.x → 2.0: 암호화 포맷 전환. 데이터 필드는 기본 객체로 보정한다.
            data.inventory ??= new InventorySaveData();
            data.story ??= new StorySaveData();
            data.flags ??= new FlagSaveData();
            data.recipe ??= new RecipeSaveData();
            data.quest ??= new QuestSaveData();
            data.party ??= new PartySaveData();
            data.world ??= new WorldStateSaveData();
            data.cycle ??= new CycleSaveData();
            data.cycle.assists ??= new UPlayGround.Data.Cycle.AssistProgressSaveData();
            data.cycle.history ??= new UPlayGround.Data.Cycle.CycleHistorySaveData();
            data.monsterCodex ??= new List<MonsterCodexEntrySave>();
            data.saveVersion = CURRENT_SAVE_VERSION;
        }

        private void TryRestoreBackup(int slot)
        {
            string backupPath = GetBackupPath(slot);
            string savePath = GetSavePath(slot);

            try
            {
                File.Copy(backupPath, savePath, overwrite: true);
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[SaveManager] 슬롯 {slot} 백업 파일 복원 쓰기 실패: {e.Message}");
            }
        }

        private List<int> GetExistingSlotIndices()
        {
            var slots = new SortedSet<int>();
            if (string.IsNullOrEmpty(_saveFolder) || !Directory.Exists(_saveFolder))
                return new List<int>();

            foreach (string path in Directory.EnumerateFiles(_saveFolder))
            {
                var match = SaveFileRegex.Match(Path.GetFileName(path));
                if (!match.Success)
                    continue;

                if (int.TryParse(match.Groups["slot"].Value, out int slot))
                    slots.Add(slot);
            }

            return new List<int>(slots);
        }

        /// <summary> 슬롯 번호가 유효한지 검사한다. 세이브 슬롯은 개수 제한 없이 0 이상의 정수를 허용한다. </summary>
        private static bool IsValidSlot(int slot) => slot >= 0;

        private static int CalcElapsedGameDays(TimeSaveData time)
        {
            if (time == null || time.totalGameMinutes < 0f)
                return 0;

            return Mathf.Max(0, Mathf.FloorToInt(time.totalGameMinutes / WorldTimeSettingsSO.MinutesPerDay));
        }

        private SaveOperationResult CompleteWithFailure(
            SaveOperationResult result,
            string message)
        {
            result.Succeeded = false;
            result.Message = message;
            LastOperationResult = result;
            Debug.LogError($"[SaveManager] {message}");
            return result;
        }

        #endregion
    }

    public enum SaveOperationType
    {
        Save,
        Load,
    }

    public enum SaveParticipantStage
    {
        Export,
        FileWrite,
        FileRead,
        Migration,
        Import,
    }

    public sealed class SaveParticipantFailure
    {
        public string Participant { get; }
        public SaveParticipantStage Stage { get; }
        public string Message { get; }

        public SaveParticipantFailure(
            string participant,
            SaveParticipantStage stage,
            string message)
        {
            Participant = participant;
            Stage = stage;
            Message = message;
        }
    }

    public sealed class SaveOperationResult
    {
        public int Slot { get; }
        public SaveOperationType OperationType { get; }
        public bool Succeeded { get; internal set; }
        public bool UsedBackup { get; internal set; }
        public string FilePath { get; internal set; }
        public string Message { get; internal set; }
        public List<SaveParticipantFailure> Failures { get; } = new();

        public SaveOperationResult(int slot, SaveOperationType operationType)
        {
            Slot = slot;
            OperationType = operationType;
        }
    }
}
