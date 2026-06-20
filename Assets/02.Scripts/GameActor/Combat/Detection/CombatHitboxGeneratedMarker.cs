using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 에디터 자동 생성 HitBox의 출처와 재생성 정책을 기록한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHitboxGeneratedMarker : MonoBehaviour
    {
        [SerializeField] private int _generatorVersion;
        [SerializeField] private string _profileGuid;
        [SerializeField] private string _sourcePath;
        [SerializeField] private string _generatedAt;
        [SerializeField] private bool _manuallyModified;

        public int GeneratorVersion => _generatorVersion;
        public string ProfileGuid => _profileGuid;
        public string SourcePath => _sourcePath;
        public string GeneratedAt => _generatedAt;
        public bool ManuallyModified => _manuallyModified;

        public void Configure(int version, string profileGuid, string sourcePath, string generatedAt)
        {
            _generatorVersion = version;
            _profileGuid = profileGuid;
            _sourcePath = sourcePath;
            _generatedAt = generatedAt;
            _manuallyModified = false;
        }

        public void MarkManuallyModified() => _manuallyModified = true;
    }
}
