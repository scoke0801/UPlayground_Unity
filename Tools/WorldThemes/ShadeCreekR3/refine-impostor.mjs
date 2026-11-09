import fs from 'node:fs';
const source='Assets/10.Datas/World/ThemeStudies/ShadeCreekR2/CanopyImpostor.shader';
const target=source.replace('ShadeCreekR2','ShadeCreekR3');
let s=fs.readFileSync(source,'utf8').replace('ShadeCreekR2Impostor','ShadeCreekR3Impostor');
const start=s.indexOf('                float yaw =');
const end=s.indexOf('                UvLB =',start);
if(start<0||end<start)throw Error('아틀라스 함수 위치 변경');
s=s.slice(0,start)+`                // 주기 경계에서 음수 셀을 만들지 않고 각 축의 인접 뷰를 보간한다.
                float yawCoordinate = frac((atan2(viewDir.z, viewDir.x) + trYawOffset) / TWO_PI) * _HorizontalSegments;
                float yaw0 = floor(yawCoordinate);
                float yaw1 = fmod(yaw0 + 1.0, _HorizontalSegments);
                float elevationCoordinate = clamp(asin(clamp(viewDir.y, -1.0, 1.0)) / max(_VerticalStep * DEG2RAD, 0.001) - _VerticalOffset, -_VerticalSegments, _VerticalSegments) + _VerticalSegments;
                float elevation0 = floor(elevationCoordinate);
                float elevation1 = min(elevation0 + 1.0, 2.0 * _VerticalSegments);
                alpha = frac(yawCoordinate);
                beta = frac(elevationCoordinate);
                float subdLB = yaw0 + elevation0 * _HorizontalSegments;
                float subdRB = yaw1 + elevation0 * _HorizontalSegments;
                float subdLT = yaw0 + elevation1 * _HorizontalSegments;
                float subdRT = yaw1 + elevation1 * _HorizontalSegments;

`+s.slice(end);
s=s.replaceAll('lerp(cRB, cLB, beta)','lerp(cLB, cRB, alpha)').replaceAll('lerp(cRT, cLT, beta)','lerp(cLT, cRT, alpha)').replaceAll('lerp(cT, cB, alpha)','lerp(cB, cT, beta)');
fs.writeFileSync(target,s);
// 경계의 셀 인덱스와 가중치가 항상 유효 범위에 있는지 확인한다.
for(let h of [8,16])for(let degrees=-720;degrees<=720;degrees+=.5){const coordinate=((degrees/360)%1+1)%1*h;const a=Math.floor(coordinate),b=(a+1)%h,w=coordinate-a;if(a<0||b<0||a>=h||b>=h||w<0||w>=1)throw Error('경계 실패');}
console.log('R3 전용 아틀라스 보간 수정. 경계 산술 검사 통과. 렌더 검증 대기.');
