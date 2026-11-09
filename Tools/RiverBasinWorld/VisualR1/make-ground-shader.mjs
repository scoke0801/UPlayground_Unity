import fs from 'node:fs';
import assert from 'node:assert/strict';
const source=fs.readFileSync('Assets/06.Shaders/CreekGround.shader','utf8');
assert(source.includes('clip(input.color.a - max(grain,0.001));'));
let shader=source.replace('Shader "UPlayGround/Environment/Creek Ground"','Shader "UPlayGround/Environment/RiverBasin Ground Overlay"')
 .replace('"RenderType"="TransparentCutout" "Queue"="AlphaTest"','"RenderType"="Transparent" "Queue"="Transparent-50"')
 .replace('            // Deferred 깊이 선행 패스와 같은 픽셀을 잘라 실제 지형이 가장자리에 드러나게 한다.\n            float grain = InterleavedGradientNoise(input.positionCS.xy,0);\n            clip(input.color.a - max(grain,0.001));','            // 수관 아래 얇은 지표는 화면 점무늬 대신 실제 지형 위로 연속 혼합한다.\n            clip(input.color.a - 0.001);')
 .replace('            Name "CreekGround"','            Name "RiverBasinGround"\n            Blend SrcAlpha OneMinusSrcAlpha\n            ZWrite Off')
 .replace('return half4(MixFog(color,input.fog),1);','return half4(MixFog(color,input.fog),input.color.a);');
// 원본 파일의 CRLF와 LF를 모두 처리한다.
shader=shader.replace(/            \/\/ Deferred[^\r\n]*\r?\n            float grain = InterleavedGradientNoise\(input.positionCS.xy,0\);\r?\n            clip\(input.color.a - max\(grain,0.001\)\);/,'            // 수관 아래 얇은 지표를 실제 지형 위로 연속 혼합한다.\n            clip(input.color.a - 0.001);');
assert(!shader.includes('InterleavedGradientNoise'));assert(shader.includes('ZWrite Off'));
fs.writeFileSync('Assets/10.Datas/World/RiverBasinWorld/VisualR1/GroundOverlay.shader',shader);
