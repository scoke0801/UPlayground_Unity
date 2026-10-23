using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UPlayGround.Data.Save;
using UPlayGround.Manager.Save;

namespace UPlayGround.Manager
{
    public partial class SaveManager
    {
        /// <summary>검토된 맵 분리를 별도 빈 슬롯에 적용하며 원본·현재 플레이·활성 슬롯을 보존한다.</summary>
        public bool TryCreateLivingWorldCopy(int sourceSlot, int destinationSlot,
            LivingWorldMapMigrationPlan plan, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(_saveFolder) || _isSaving || IsPreparingSceneLoad
                || !IsValidSlot(sourceSlot) || !IsValidSlot(destinationSlot) || sourceSlot == destinationSlot)
            {
                error = "저장 준비 상태와 서로 다른 슬롯을 확인하세요.";
                return false;
            }
            string destination = GetSavePath(destinationSlot);
            if (HasLivingWorldDestinationFiles(destinationSlot))
            {
                error = "대상 슬롯의 원본/백업/임시 파일을 덮어쓸 수 없습니다.";
                return false;
            }
            string staging = destination + ".livingworld." + Guid.NewGuid().ToString("N") + ".tmp";
            _isSaving = true;
            try
            {
                if (!TryReadSaveData(GetSavePath(sourceSlot), out var source, out error, migrate: false)
                    || !LivingWorldMapMigration.TryCreateCopy(source, plan, out var copy, out error)) return false;
                WriteLivingWorldStaging(staging, copy, FileMode.CreateNew);
                if (!ValidateLivingWorldStaging(staging, copy, out error)) return false;
                copy.flags ??= new FlagSaveData();
                copy.flags.flags ??= new Dictionary<string, bool>();
                copy.flags.flags[LivingWorldMapMigration.CompletedFlag] = true;
                WriteLivingWorldStaging(staging, copy, FileMode.Truncate);
                if (!ValidateLivingWorldStaging(staging, copy, out error)) return false;
                if (HasLivingWorldDestinationFiles(destinationSlot))
                {
                    error = "복사 중 대상 슬롯 상태가 바뀌었습니다.";
                    return false;
                }
                File.Move(staging, destination);
                return true;
            }
            catch (Exception exception) { error = exception.Message; return false; }
            finally
            {
                _isSaving = false;
                try { if (File.Exists(staging)) File.Delete(staging); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private bool HasLivingWorldDestinationFiles(int slot) => File.Exists(GetSavePath(slot))
            || File.Exists(GetBackupPath(slot)) || File.Exists(GetTempPath(slot));

        private static void WriteLivingWorldStaging(string path, GameSaveData data, FileMode mode)
        {
            byte[] bytes = SaveCrypto.Encrypt(JsonConvert.SerializeObject(data, Formatting.Indented));
            using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        private bool ValidateLivingWorldStaging(string path, GameSaveData expected, out string error)
        {
            if (!TryReadSaveData(path, out var actual, out error, migrate: false)) return false;
            if (JToken.DeepEquals(JToken.FromObject(expected), JToken.FromObject(actual))) return true;
            error = "복사본 재독해 결과가 변환 결과와 다릅니다.";
            return false;
        }
    }
}
