using UPlayGround.Ability.Core;

namespace UPlayGround.Data.Ability
{
    /// <summary>
    /// 저작 데이터(<see cref="GameplayEffectModifierDefinition"/>)의 크기 계산 방식을
    /// Core의 <see cref="IGameplayMagnitudeCalculation"/>으로 변환한다.
    /// 기본값 Fixed는 기존 저작과 동일하게 동작하므로 기존 Effect 에셋의 결과가 바뀌지 않는다.
    /// </summary>
    public static class GameplayEffectMagnitudeFactory
    {
        public static bool TryBuild(
            GameplayEffectModifierDefinition modifier,
            out IGameplayMagnitudeCalculation magnitude,
            out string error)
        {
            error = string.Empty;
            magnitude = null;
            if (modifier == null)
            {
                error = "Modifier 정의가 null입니다.";
                return false;
            }

            switch (modifier.magnitudeSource)
            {
                case GameplayEffectMagnitudeSource.Fixed:
                    magnitude = new FixedMagnitudeCalculation(modifier.value);
                    return true;

                case GameplayEffectMagnitudeSource.ScalableByLevel:
                    magnitude = new ScalableMagnitudeCalculation(
                        modifier.value,
                        modifier.perLevel);
                    return true;

                case GameplayEffectMagnitudeSource.AttributeBased:
                {
                    AttributeId sourceAttribute = modifier.SourceAttributeId;
                    if (!sourceAttribute.IsValid)
                    {
                        error = "AttributeBased 크기의 캡처 Attribute ID가 없습니다.";
                        return false;
                    }
                    if (modifier.captureSource == GameplayEffectCaptureSource.Target
                        && modifier.capturePolicy
                            == GameplayEffectCapturePolicy.SnapshotOnCreate)
                    {
                        // Core Spec 생성 시점에는 아직 적용 대상이 전달되지 않아 Target을
                        // 캡처할 수 없다. Apply 또는 Execute 시점 정책을 사용해야 한다.
                        error = "Target Attribute는 SnapshotOnCreate로 캡처할 수 없습니다.";
                        return false;
                    }
                    magnitude = new AttributeBasedMagnitudeCalculation(
                        new GameplayAttributeCaptureDefinition(
                            sourceAttribute,
                            modifier.captureSource,
                            modifier.capturePolicy),
                        modifier.coefficient,
                        modifier.preAdd,
                        modifier.postAdd);
                    return true;
                }

                case GameplayEffectMagnitudeSource.SetByCaller:
                {
                    if (string.IsNullOrWhiteSpace(modifier.setByCallerKey))
                    {
                        error = "SetByCaller 크기의 키가 비어 있습니다.";
                        return false;
                    }
                    magnitude = new SetByCallerMagnitudeCalculation(
                        new AbilityTagId(modifier.setByCallerKey),
                        modifier.allowMissingSetByCaller,
                        modifier.setByCallerDefaultValue);
                    return true;
                }

                default:
                    error = $"알 수 없는 크기 계산 방식입니다: {modifier.magnitudeSource}";
                    return false;
            }
        }
    }
}
