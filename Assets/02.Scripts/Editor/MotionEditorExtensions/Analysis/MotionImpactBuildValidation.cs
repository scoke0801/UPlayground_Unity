using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UPlayGround.Animation.Editor
{
    /// <summary>원본이 변경된 운동 데이터를 Player에 실수로 포함하지 않도록 검사한다.</summary>
    public sealed class MotionImpactBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        /// <summary>연결된 측정 묶음의 모든 공격이 최신 런타임 표본을 가지는지 확인한다.</summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:CombatMotionCatalog"))
            {
                var source = AssetDatabase.LoadAssetAtPath<CombatMotionCatalog>(AssetDatabase.GUIDToAssetPath(guid));
                if (source.runtimeCatalog == null) continue;
                foreach (var entry in source.entries)
                {
                    CombatMotionAnalysis.Resolve(source, entry, out var payload, out var owner, out _);
                    if (owner.motionImpactCatalog != source.runtimeCatalog)
                        throw new BuildFailedException($"런타임 운동 데이터 연결 누락: {entry.ability.abilityId}");
                    string expected = CombatMotionImpactBaker.Fingerprint(source, entry);
                    bool found = false;
                    foreach (var window in source.runtimeCatalog.windows)
                    {
                        if (window.motionKey != payload.attackInfo.motionKey) continue;
                        found = true;
                        if (window.sourceFingerprint != expected)
                            throw new BuildFailedException($"운동 데이터가 오래되었습니다. 전투 모션 분석에서 재베이크하세요: {entry.ability.abilityId}");
                    }
                    if (!found)
                        throw new BuildFailedException($"런타임 운동 데이터 미측정: {entry.ability.abilityId}");
                }
            }
        }
    }
}
