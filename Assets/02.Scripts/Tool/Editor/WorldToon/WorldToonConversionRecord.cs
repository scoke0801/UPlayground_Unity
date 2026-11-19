using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UPlayGround.Tool.Editor.WorldToon
{
    /// <summary>생성 재질 한 건의 기록. 같은 원본+프리셋은 항상 같은 항목·같은 파일로 대응한다.</summary>
    [Serializable]
    public sealed class WorldToonGeneratedMaterialEntry
    {
        public string sourceMaterialId;
        public string sourceDependencyHash;
        public string sourceShaderName;
        public string[] sourceKeywords;
        public string presetId;
        public string targetMaterialGuid;
        public string targetPath;
        public string generatedContentHash;
        public string status;
        public string[] warnings;
        public string toolVersion;
    }

    /// <summary>렌더러 슬롯 적용 한 건의 기록. Rollback은 이 기록의 슬롯만 되돌린다.</summary>
    [Serializable]
    public sealed class WorldToonAppliedSlotEntry
    {
        public string rendererId;
        public string rendererPath;
        public int materialSlot;
        public string sourceMaterialId;
        public string targetMaterialGuid;
        public string status;
    }

    /// <summary>변환·적용 기록 파일(ConversionReport.json). 원본 매핑의 영구 저장소다.</summary>
    [Serializable]
    public sealed class WorldToonConversionRecord
    {
        public const string ToolVersion = "worldtoon-1.0";
        public const string DefaultPath = "docs/work-status/worldtoon/ConversionReport.json";

        public string toolVersion = ToolVersion;
        public List<WorldToonGeneratedMaterialEntry> generated = new List<WorldToonGeneratedMaterialEntry>();
        public List<WorldToonAppliedSlotEntry> applied = new List<WorldToonAppliedSlotEntry>();

        /// <summary>파일이 없으면 빈 기록을 반환한다.</summary>
        public static WorldToonConversionRecord Load(string path = DefaultPath)
        {
            if (!File.Exists(path))
                return new WorldToonConversionRecord();
            var record = JsonUtility.FromJson<WorldToonConversionRecord>(File.ReadAllText(path));
            return record ?? new WorldToonConversionRecord();
        }

        /// <summary>사람이 diff로 볼 수 있게 들여쓰기 JSON으로 저장한다.</summary>
        public void Save(string path = DefaultPath)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(this, true));
        }

        /// <summary>원본+프리셋의 생성 기록을 찾는다.</summary>
        public WorldToonGeneratedMaterialEntry FindGenerated(string sourceMaterialId, string presetId)
        {
            return generated.Find(e => e.sourceMaterialId == sourceMaterialId && e.presetId == presetId);
        }

        /// <summary>렌더러 슬롯의 현재 적용 기록을 찾는다.</summary>
        public WorldToonAppliedSlotEntry FindApplied(string rendererId, int slot)
        {
            return applied.Find(e => e.rendererId == rendererId && e.materialSlot == slot);
        }
    }
}
