using UnityEngine;

namespace Game.Editor.P09Builder
{
    public sealed class ToggleMagicaClothStep : IBuildStep
    {
        public void Execute(BuildContext ctx)
        {
            if (ctx.RootInstance == null) return;

            // MagicaCloth 컴포넌트 존재 여부만 로그
            var components = ctx.RootInstance.GetComponentsInChildren<Component>(true);
            int found = 0;
            foreach (var c in components)
            {
                if (c == null) continue;
                var typeName = c.GetType().Name;
                if (typeName.Contains("MagicaCloth"))
                    found++;
            }

            if (ctx.Config.UseMagicaCloth)
                Debug.Log($"[P09Builder] MagicaCloth components found: {found} (UseMagicaCloth=true)");
            else
                Debug.Log($"[P09Builder] No-Physics base used. MagicaCloth components: {found} (expected 0)");
        }
    }
}
