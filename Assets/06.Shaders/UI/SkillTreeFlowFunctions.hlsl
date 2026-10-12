#ifndef UPLAYGROUND_SKILL_TREE_FLOW_INCLUDED
#define UPLAYGROUND_SKILL_TREE_FLOW_INCLUDED

// UV.x는 곡선을 따라 누적된 픽셀 길이, UV.z는 선행 노드의 해금 상태다.
void SkillTreeFlow_float(float4 UV, float Clock, float Speed, float Spacing,
    float Intensity, out float3 Color, out float Alpha)
{
    float across = abs(UV.y * 2.0 - 1.0);
    float core = 1.0 - smoothstep(0.12, 0.3, across);
    float halo = pow(saturate(1.0 - across), 2.0);
    float phase = frac((UV.x - Clock * Speed) / max(Spacing, 1.0));
    float pulse = smoothstep(0.62, 0.94, phase) * (1.0 - smoothstep(0.94, 1.0, phase));
    pulse *= saturate(UV.z);
    float dash = lerp(0.35, 0.7, step(0.3, frac(UV.x / 16.0)));
    float track = lerp(dash, 0.65, saturate(UV.z));
    Color = (0.8 + pulse * Intensity).xxx;
    Alpha = saturate(core * (track + pulse * 0.65) + halo * (0.12 + pulse * 0.3));
}
#endif
