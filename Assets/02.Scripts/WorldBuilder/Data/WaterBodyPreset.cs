using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>수계 단면·조회·URP 표현 규칙의 저작 원본.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/WorldBuilder/Water Body Preset")]
    public sealed class WaterBodyPreset : ScriptableObject
    {
        public float width = 24, requiredDepth = 2, shoreWidth = 2, bankWidth = 2, bankClearance = .5f, shoreFalloff = 4;
        public float flowSpeed = 1, maxCutFill = 12, uvRepeat = 8, queryHeight = 3;
        public float searchStep = 4;
        public int maxVisited = 120000;
        public Material material;
        public Texture2D normalMap;
        public FootstepSurfaceProfileSO footstepProfile;
        public Color shallowColor = new Color(.13f, .45f, .39f, .55f), deepColor = new Color(.025f, .15f, .19f, .85f);
        public float normalScale = .25f, normalTiling = .15f, normalScroll = .1f, depthFade = 3, foamWidth = .6f, foamStrength = .45f;
        public Vector2 waveDrift = new Vector2(.1f, .05f);
        public void Apply(Material target)
        {
            target.SetTexture("_NormalMap", normalMap); target.SetColor("_ShallowColor", shallowColor); target.SetColor("_DeepColor", deepColor);
            target.SetFloat("_NormalScale", normalScale); target.SetFloat("_NormalTiling", normalTiling); target.SetFloat("_ScrollSpeed", normalScroll);
            target.SetFloat("_DepthFade", depthFade); target.SetFloat("_FoamWidth", foamWidth); target.SetFloat("_FoamStrength", foamStrength);
            target.SetVector("_WaveDrift", new Vector4(waveDrift.x, waveDrift.y, 0, 0));
        }
    }
}
