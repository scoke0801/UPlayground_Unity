using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>월드 수치 설정의 단일 원본. 지역 C를 기본값으로 사용한다.</summary>
    [CreateAssetMenu(fileName = "WorldProfile", menuName = "UPlayGround/WorldBuilder/World Profile")]
    public sealed class WorldProfile : ScriptableObject
    {
        [SerializeField] private float _worldSize = 2048;
        [SerializeField] private int _sampleCount = 2049;
        [SerializeField] private float _originY;
        [SerializeField] private float _verticalRange = 700;
        [SerializeField] private int _tileCells = 512;
        [SerializeField] private RegionConcept[] _regionConcepts = Array.Empty<RegionConcept>();

        public float WorldSize => _worldSize;
        public int SampleCount => _sampleCount;
        public float OriginY => _originY;
        public float VerticalRange => _verticalRange;
        public int TileCells => _tileCells;
        public int TileCount => (_sampleCount - 1) / _tileCells;
        public IReadOnlyList<RegionConcept> RegionConcepts => _regionConcepts;

        /// <summary>Unity Terrain이 지원하는 타일 규격을 검사해 수치 스냅샷을 만든다.</summary>
        public WorldGrid CreateGrid()
        {
            if (_tileCells < 32 || _tileCells > 4096 || (_tileCells & (_tileCells - 1)) != 0 || _sampleCount < 2 || (_sampleCount - 1) % _tileCells != 0)
                throw new InvalidOperationException("Terrain 타일은 2의 거듭제곱 셀 수와 나누어지는 전역 규격이 필요합니다.");
            return new WorldGrid(_worldSize, _sampleCount, _originY, _verticalRange);
        }

        /// <summary>검증된 새 프로필 값만 반영하며 잘못된 입력은 원래 설정을 유지한다.</summary>
        public void Configure(float worldSize, int sampleCount, float originY, float verticalRange, int tileCells)
        {
            _ = new WorldGrid(worldSize, sampleCount, originY, verticalRange);
            if (tileCells < 32 || tileCells > 4096 || (tileCells & (tileCells - 1)) != 0 || (sampleCount - 1) % tileCells != 0) throw new ArgumentOutOfRangeException(nameof(tileCells));
            _worldSize = worldSize;
            _sampleCount = sampleCount;
            _originY = originY;
            _verticalRange = verticalRange;
            _tileCells = tileCells;
        }

        [Serializable]
        public struct RegionConcept
        {
            [SerializeField] private string _regionId;
            [SerializeField] private Texture2D _image;

            public string RegionId => _regionId;
            public Texture2D Image => _image;
        }
    }
}
