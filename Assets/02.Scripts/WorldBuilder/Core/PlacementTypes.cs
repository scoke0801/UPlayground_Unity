using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace UPlayGround.WorldBuilder.Core
{
    public enum PlacementRejection { None, OutsideRegion, RuleDisabled, DensityRejected, HeightRejected, SlopeRejected, HoleOverlap, ReservedArea, FootprintOverlap, SpacingRejected, Unsupported, SurfaceMismatch, OrphanOverride, ManualConflict, DuplicateId, MissingPrefab, TargetCount }
    public enum PlacementOverrideKind { Manual, Pin, Transform, Delete, ExcludeRegion, ReplacePrefab }
    public enum PlacementAnchor { Terrain, Absolute }

    /// <summary>Unity 직렬화와 수치 평가가 공유하는 배치 스냅샷.</summary>
    [Serializable]
    public sealed class PlacementRecord
    {
        public string id, sourceIdentity, layerGuid, ruleGuid, prefabGuid, surfaceId = "terrain";
        public string lineage;
        public int epoch, seed, ownerX, ownerZ;
        public double x, y, z, yaw, scaleX = 1, scaleY = 1, scaleZ = 1, heightOffset;
        public double halfX, halfZ, spacing, maxSupportDelta;
        public PlacementAnchor anchor;
        public bool isFixed;
        public PlacementRecord Clone() => (PlacementRecord)MemberwiseClone();
    }

    /// <summary>대상 정체성과 저작 의도를 분리해 seed 변경 시 오적용을 막는다.</summary>
    [Serializable]
    public sealed class PlacementOverride
    {
        public string overrideGuid, targetId, targetSourceIdentity, prefabGuid, ruleTag;
        public int epoch, seed;
        public PlacementOverrideKind kind;
        public PlacementRecord placement;
        public double minX, minZ, maxX, maxZ;
        public PlacementOverride Clone() { var copy = (PlacementOverride)MemberwiseClone(); copy.placement = placement?.Clone(); return copy; }
    }

    /// <summary>프리팹의 바닥 pivot·평면 footprint·배율 지원 계약.</summary>
    public sealed class PlacementPrefab
    {
        public string Guid;
        public double HalfX, HalfZ, PivotHeight;
        public bool SupportsNonUniformScale;
    }

    /// <summary>SO를 평가 전에 검증 가능한 순수 값으로 복사한 규칙.</summary>
    public sealed class PlacementRule
    {
        public string LayerGuid, RuleGuid, PrefabGuid, Tag, SurfaceId = "terrain";
        public bool Enabled = true, Mandatory;
        public int SeedSalt, Slots = 1, MinimumCount;
        public double CellSize = 16, Jitter = 1, Density = 1, MinHeight = -1000, MaxHeight = 10000, MaxSlope = 45;
        public double MinX, MinZ, MaxX = 2048, MaxZ = 2048, Spacing = 2, MinScale = 1, MaxScale = 1, MaxSupportDelta = 2;
    }

    /// <summary>P03의 명시적 사각 예약 영역. 후속 수계·바이옴을 성공으로 흉내내지 않는다.</summary>
    [Serializable]
    public sealed class PlacementReservation
    {
        public string guid;
        public string ownerPlanGuid;
        public double minX, minZ, maxX, maxZ;
        public PlanPoint[] boundary;
        // Unity Undo/직렬화는 사각 예약의 null 배열을 빈 배열로 복원할 수 있다.
        public PlacementReservation Clone() { var copy = (PlacementReservation)MemberwiseClone(); copy.boundary = boundary == null || boundary.Length == 0 ? null : (PlanPoint[])boundary.Clone(); return copy; }
    }

    public sealed class PlacementDiagnostic
    {
        public string RuleGuid, CandidateId, OverrideGuid;
        public PlacementRejection Reason;
        public bool IsError;
    }

    /// <summary>거절 후보·집계·오류와 정규화된 배치 결과를 함께 제공한다.</summary>
    public sealed class PlacementBuildResult
    {
        public readonly List<PlacementRecord> Records = new List<PlacementRecord>();
        public readonly List<PlacementDiagnostic> Diagnostics = new List<PlacementDiagnostic>();
        public bool HasErrors => Diagnostics.Exists(value => value.IsError);
        public int Count(PlacementRejection reason) => Diagnostics.FindAll(value => value.Reason == reason).Count;
        public void Reject(string rule, string id, PlacementRejection reason, bool error = false, string overrideGuid = null)
            => Diagnostics.Add(new PlacementDiagnostic { RuleGuid = rule, CandidateId = id, Reason = reason, IsError = error, OverrideGuid = overrideGuid });

        /// <summary>리스트 순서·시각·Unity 메타데이터를 제외한 논리 결과 SHA-256.</summary>
        public string ComputeHash()
        {
            var sorted = new List<PlacementRecord>(Records);
            sorted.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                foreach (var p in sorted)
                {
                    writer.Write(p.id); writer.Write(p.sourceIdentity); writer.Write(p.prefabGuid);
                    writer.Write(p.layerGuid ?? ""); writer.Write(p.ruleGuid ?? ""); writer.Write(p.surfaceId);
                    writer.Write(p.epoch); writer.Write(p.seed); writer.Write(p.lineage ?? "");
                    writer.Write(p.x); writer.Write(p.y); writer.Write(p.z); writer.Write(p.yaw);
                    writer.Write(p.scaleX); writer.Write(p.scaleY); writer.Write(p.scaleZ);
                    writer.Write(p.heightOffset); writer.Write((int)p.anchor); writer.Write(p.isFixed);
                    writer.Write(p.halfX); writer.Write(p.halfZ); writer.Write(p.spacing); writer.Write(p.maxSupportDelta);
                    writer.Write(p.ownerX); writer.Write(p.ownerZ);
                }
                writer.Flush(); return PlacementIdentity.Hash(stream.ToArray());
            }
        }
    }

    /// <summary>버전이 있는 little-endian・長さ付き文字列の canonical key。永続値にランタイムhashを使わない。</summary>
    public static class PlacementIdentity
    {
        public static byte[] Encode(string world, int epoch, string layer, string rule, int cellX, int cellZ, int slot)
        {
            ValidateGuid(world); ValidateGuid(layer); ValidateGuid(rule);
            if (epoch < 0 || slot < 0) throw new ArgumentOutOfRangeException(nameof(epoch));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(1); writer.Write(world); writer.Write(epoch); writer.Write(layer); writer.Write(rule);
                writer.Write(cellX); writer.Write(cellZ); writer.Write(slot); writer.Flush(); return stream.ToArray();
            }
        }
        public static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        public static string Id(string identity) => Hash(Convert.FromBase64String(identity)).Substring(0, 32);
        public static byte[] RandomKey(byte[] key, int seed, int salt)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(key); writer.Write(seed); writer.Write(salt); writer.Flush();
                using (var sha = SHA256.Create()) return sha.ComputeHash(stream.ToArray());
            }
        }
        public static double Unit(byte[] hash, int offset)
        {
            uint value = (uint)(hash[offset] | hash[offset + 1] << 8 | hash[offset + 2] << 16 | hash[offset + 3] << 24);
            return value / 4294967296.0;
        }
        public static void ValidateGuid(string value)
        {
            if (!System.Guid.TryParseExact(value, "N", out var parsed) || parsed.ToString("N") != value) throw new ArgumentException("소문자 N 형식 GUID가 필요합니다.");
        }
        public static void RequireFinite(params double[] values)
        {
            foreach (double value in values) if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentException("유한 수치가 필요합니다.");
        }
    }
}

