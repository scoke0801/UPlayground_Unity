using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class WorldPlanningPresetTests
    {
        [Test] public void BridgeCatalogSpanWidthAndPivotMustMatchIndependentOfSocketTolerance()
        {
            var catalog = ScriptableObject.CreateInstance<PlacementCatalog>(); var preset = ScriptableObject.CreateInstance<BridgePlanningPreset>();
            try
            {
                var serialized = new SerializedObject(catalog); var entries = serialized.FindProperty("_entries"); entries.arraySize = 1; var entry = entries.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("prefabGuid").stringValue = "00000000000000000000000000000500"; entry.FindPropertyRelative("halfExtents").vector2Value = new Vector2(3, 8); serialized.ApplyModifiedPropertiesWithoutUndo();
                preset.catalog = catalog; preset.prefabGuid = "00000000000000000000000000000500"; preset.span = 16; Assert.DoesNotThrow(() => preset.ToCore());
                preset.span = 32; preset.spanTolerance = 1000; Assert.Throws<System.InvalidOperationException>(() => preset.ToCore());
                preset.span = 16; preset.deckWidth = 8; Assert.Throws<System.InvalidOperationException>(() => preset.ToCore()); preset.deckWidth = 6;
                entry.FindPropertyRelative("pivotHeight").floatValue = 1; serialized.ApplyModifiedPropertiesWithoutUndo(); Assert.Throws<System.InvalidOperationException>(() => preset.ToCore());
            }
            finally { Object.DestroyImmediate(preset); Object.DestroyImmediate(catalog); }
        }
    }
}
