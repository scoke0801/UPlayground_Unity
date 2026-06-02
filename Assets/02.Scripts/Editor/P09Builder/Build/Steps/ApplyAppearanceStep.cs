using UnityEngine;

namespace Game.Editor.P09Builder
{
    /// <summary>
    /// AvatarView의 SetActive 토글 / 머티리얼 교체 로직을 에디터에서 적용한다.
    /// AppearanceApplier에 위임.
    /// </summary>
    public sealed class ApplyAppearanceStep : IBuildStep
    {
        public void Execute(BuildContext ctx)
        {
            if (ctx == null || ctx.RootInstance == null)
            {
                Debug.LogWarning("[P09Builder] ApplyAppearanceStep: ctx or RootInstance is null");
                return;
            }

            // BuildContext에 catalog이 없으므로 새로 생성
            var catalog = new P09AssetCatalog();
            catalog.Refresh();

            AppearanceApplier.Apply(ctx.RootInstance, ctx.Config, catalog);

            Debug.Log($"[P09Builder] AppearanceApplier 적용 완료: {ctx.PrefabName}");
        }
    }
}
