#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

/// <summary>
/// ParentConstraint의 특정 source 하나의 오프셋만 현재 포즈 기준으로 재계산하는 유틸.
/// 사용: ParentConstraint 컴포넌트 우클릭(⋮) → "소스 N 오프셋만 재계산".
///
/// ■ 배경
///   인스펙터의 Activate 버튼은 모든 source의 오프셋을 현재 포즈 기준으로 다시 계산한다.
///   무기 소켓처럼 source 0(손) / source 1(등)을 weight 스왑으로 전환하는 constraint에서
///   손에 쥔 포즈로 Activate를 누르면 등 소켓 오프셋까지 덮어써 망가진다.
///   이 유틸은 지정한 source의 오프셋만 갱신하고 나머지는 건드리지 않는다.
///
/// ■ 절차 (무기 소켓 기준)
///   1. 손 더미 본(R_Hand_Weapon)을 기본 그립 포즈로 둔다
///   2. 소켓/무기를 올바른 그립으로 보이도록 정렬한다
///   3. 소스 0 오프셋 재계산 실행 → 등 소켓(source 1) 오프셋은 그대로 유지됨
/// </summary>
public static class ParentConstraintSourceOffsetUtility
{
    [MenuItem("CONTEXT/ParentConstraint/소스 0 오프셋만 재계산 (현재 포즈 유지)")]
    private static void RecalculateSource0(MenuCommand command)
    {
        Recalculate((ParentConstraint)command.context, 0);
    }

    [MenuItem("CONTEXT/ParentConstraint/소스 1 오프셋만 재계산 (현재 포즈 유지)")]
    private static void RecalculateSource1(MenuCommand command)
    {
        Recalculate((ParentConstraint)command.context, 1);
    }

    [MenuItem("CONTEXT/ParentConstraint/소스 2 오프셋만 재계산 (현재 포즈 유지)")]
    private static void RecalculateSource2(MenuCommand command)
    {
        Recalculate((ParentConstraint)command.context, 2);
    }

    private static void Recalculate(ParentConstraint constraint, int sourceIndex)
    {
        if (constraint == null)
            return;

        if (sourceIndex >= constraint.sourceCount)
        {
            Debug.LogError(
                $"[ParentConstraintOffset] source {sourceIndex}가 없습니다. (sourceCount: {constraint.sourceCount})",
                constraint);
            return;
        }

        Transform sourceTransform = constraint.GetSource(sourceIndex).sourceTransform;
        if (sourceTransform == null)
        {
            Debug.LogError($"[ParentConstraintOffset] source {sourceIndex}의 Transform이 비어 있습니다.", constraint);
            return;
        }

        Undo.RecordObject(constraint, "Recalculate ParentConstraint Source Offset");

        // 해당 source가 weight 1일 때 constraint 오브젝트가 현재 포즈를 유지하도록 오프셋 계산.
        // (Activate와 동일한 수식이지만 지정한 source 하나에만 적용)
        Transform constrained = constraint.transform;
        Vector3[] translationOffsets = constraint.translationOffsets;
        Vector3[] rotationOffsets = constraint.rotationOffsets;

        translationOffsets[sourceIndex] = sourceTransform.InverseTransformPoint(constrained.position);
        rotationOffsets[sourceIndex] =
            (Quaternion.Inverse(sourceTransform.rotation) * constrained.rotation).eulerAngles;

        constraint.translationOffsets = translationOffsets;
        constraint.rotationOffsets = rotationOffsets;

        EditorUtility.SetDirty(constraint);
        PrefabUtility.RecordPrefabInstancePropertyModifications(constraint);

        Debug.Log(
            $"[ParentConstraintOffset] {constraint.name} source {sourceIndex} ({sourceTransform.name}) 오프셋 재계산 완료.\n" +
            $"  translation: {translationOffsets[sourceIndex]}\n" +
            $"  rotation(euler): {rotationOffsets[sourceIndex]}",
            constraint);
    }
}
#endif
