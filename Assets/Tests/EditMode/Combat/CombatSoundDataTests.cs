#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Data.Sound;

namespace UPlayGround.Combat.Tests
{
    public sealed class CombatSoundDataTests
    {
        private const string DatabasePath =
            "Assets/10.Datas/Sound/SoundDatabase.asset";

        [Test]
        public void 전투_임팩트_상위_티어는_고유한_클립_엔트리를_가진다()
        {
            SoundDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<SoundDatabaseSO>(DatabasePath);
            Assert.That(database, Is.Not.Null, DatabasePath);

            var entriesByKey = new Dictionary<string, SoundEntrySO>();
            foreach (SoundEntrySO entry in database.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;
                Assert.That(
                    entriesByKey.TryAdd(entry.key, entry),
                    Is.True,
                    $"중복 사운드 키: {entry.key}");
            }

            AssertPlayable(entriesByKey, GameSoundKey.CombatHitCritical);
            AssertPlayable(entriesByKey, GameSoundKey.CombatHitBreak);
            AssertPlayable(entriesByKey, GameSoundKey.CombatWallImpact);
        }

        private static void AssertPlayable(
            IReadOnlyDictionary<string, SoundEntrySO> entries,
            string key)
        {
            Assert.That(entries.TryGetValue(key, out SoundEntrySO entry),
                Is.True, $"필수 전투 사운드 엔트리 누락: {key}");
            Assert.That(entry.clip, Is.Not.Null, $"{key}: AudioClip 누락");
        }
    }
}
#endif
