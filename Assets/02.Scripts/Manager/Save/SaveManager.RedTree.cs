using System;
using System.IO;
using Newtonsoft.Json;
using UPlayGround.Data.Save;

namespace UPlayGround.Manager
{
    public partial class SaveManager
    {
        /// <summary>원본과 현재 플레이를 유지하며 빈 슬롯에만 붉은 나무 이관 복사본을 만든다.</summary>
        public bool TryCreateRedTreeCopy(
            int sourceSlot,
            int destinationSlot,
            string resumeMapId,
            string resumeSceneName,
            out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(_saveFolder) || _isSaving
                || !IsValidSlot(sourceSlot) || !IsValidSlot(destinationSlot)
                || sourceSlot == destinationSlot)
            {
                error = "저장 준비 상태와 서로 다른 원본·대상 슬롯을 확인하세요.";
                return false;
            }
            string destination = GetSavePath(destinationSlot);
            if (File.Exists(destination) || File.Exists(GetBackupPath(destinationSlot))
                || File.Exists(GetTempPath(destinationSlot)))
            {
                error = "대상 슬롯에 기존 파일이 있어 복사하지 않았습니다.";
                return false;
            }
            if (!TryReadSaveData(GetSavePath(sourceSlot), out GameSaveData copy, out error)
                || !RedTreeSaveMigration.TryApplyToCopy(copy, resumeMapId, resumeSceneName, out error))
            {
                return false;
            }

            // 다른 저장 작업과 경합해도 기존 대상 슬롯을 대체하지 않는 임시 파일을 사용한다.
            string stagingPath = destination + ".redtree." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                byte[] encrypted = SaveCrypto.Encrypt(JsonConvert.SerializeObject(copy, Formatting.Indented));
                using (var stream = new FileStream(stagingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    stream.Write(encrypted, 0, encrypted.Length);
                if (!TryReadSaveData(stagingPath, out _, out error))
                    return false;
                if (File.Exists(GetBackupPath(destinationSlot)) || File.Exists(GetTempPath(destinationSlot)))
                {
                    error = "복사 중 대상 슬롯 상태가 바뀌었습니다.";
                    return false;
                }
                File.Move(stagingPath, destination);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(stagingPath))
                        File.Delete(stagingPath);
                }
                catch (IOException)
                {
                    // 유일 이름의 잔여 임시 파일 때문에 복사 실패 원인을 덮어쓰지 않는다.
                }
                catch (UnauthorizedAccessException)
                {
                    // 기존 슬롯은 보존되고, 접근이 복구되면 이 임시 파일만 정리할 수 있다.
                }
            }
        }
    }
}
