import fs from 'node:fs';
import assert from 'node:assert/strict';
const dir = 'output/TerrainBoundaryAudit';
const data = JSON.parse(fs.readFileSync(`${dir}/world-audit.json`));
const provenance = JSON.parse(fs.readFileSync(`${dir}/world-provenance.json`));
const first = new Set([16,19,25,26,29,30,31,33,35,38,40,41,42]);
const outside = new Set([1,2,3,4,6,7,9,15]);
for (const g of data.groups) {
  const id = +g.id.slice(1);
  g.priority = first.has(id) ? '1. 초반 동선' : outside.has(id) ? '4. 외곽 사면' : id === 5 || [10,12,13,27,32,48,49].includes(id) ? '3. 채석장·고개' : '2. 거점·호안';
  g.action = first.has(id) ? '주변 지면과 완만하게 연결. 길·상호작용 발밑을 고립된 평탄 패치로 고정하지 말고 주변 높이 전이를 함께 조정.' : outside.has(id) ? '의도된 산지/외곽 경계인지 확인 후 유지 또는 실루엣·암반 보정. 급경사 수치만으로 평탄화하지 않음.' : id === 5 ? '절개 단과 보호 영역 사이의 급격한 높이 전이를 재저작. 보행 경계는 완만하게, 절벽은 실제 암반 면과 상단/하단 접합으로 처리.' : [8,14,17,18,20,21,23,28,36,44,47].includes(id) ? '건축 기단·굴착면의 경계인지 대조. 통행하는 면은 경사/계단으로 연결하고 비보행 면은 노출된 Terrain 벽을 암반·축대로 마감.' : id === 24 ? '북쪽 호수/사당 지형의 긴 수평 절개 경계. 지형 전이 폭과 인접 기단/바위 접합을 함께 조정.' : '저장된 지형과 실제 소품·호안의 접합을 확인해 경사 연결 또는 절벽 마감. 관련 보행/복귀 경로 보존.';
  if (id === 1) g.region = '북부 외곽 산지';
  if (id === 2) g.region = '북동 외곽 산지';
  if (id === 3) g.region = '서부 외곽 산지';
  if (id === 4) g.region = '북서 외곽 산지';
  if ([6,7].includes(id)) g.region = '북부 고지대';
  if (id === 9) g.region = '호수 북동 사면';
}
assert.equal(data.groups.filter(g=>g.priority.startsWith('1.')).length,13);
assert.equal(data.groups.length,50);
fs.writeFileSync(`${dir}/world-review.json`, JSON.stringify({...data,provenance},null,2));
const csvRows=[['ID','우선순위','지역','대표X','대표Y','대표Z','최소X','최대X','최소Z','최대Z','최대경사','한셀최대높이차_m','급경사평면투영면적_m2','수정방향'],...data.groups.map(g=>[g.id,g.priority,g.region,...g.peak,g.minX,g.maxX,g.minZ,g.maxZ,g.maxSlope,g.maxCellStep,g.area,g.action])];
fs.writeFileSync(`${dir}/world-boundaries.csv`,'\uFEFF'+csvRows.map(r=>r.map(v=>'"'+String(v).replaceAll('"','""')+'"').join(',')).join('\n'));
const esc = s => String(s).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('"','&quot;');
const rows = data.groups.map(g=>`<tr data-id="${g.id}"><td>${g.id}</td><td>${g.priority}</td><td>${g.region}</td><td>${g.peak[0].toFixed(1)} / ${g.peak[2].toFixed(1)}</td><td>${g.maxSlope.toFixed(1)}°</td><td>${g.area.toFixed(1)}</td></tr>`).join('');
const html = String.raw`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>LakeVillageWorld 지형 경계 전수조사</title><style>body{font:15px/1.6 system-ui,sans-serif;color:#24251f;background:#faf9f5;margin:24px}h1{font-size:26px;margin-bottom:8px}h2{font-size:20px;margin-top:32px}p{max-width:1100px}a{color:#245e73}table{border-collapse:collapse;width:100%;font-size:13px}td,th{padding:8px;border-bottom:1px solid #c8c7be;text-align:left}button,select{padding:8px;margin:4px}button{cursor:pointer}main{display:grid;grid-template-columns:minmax(550px,1fr) minmax(550px,1fr);gap:20px;align-items:start}canvas{width:100%;height:auto;border:1px solid #888;display:block}aside{position:sticky;top:12px}tr[data-id]{cursor:pointer}tr[data-id]:hover{background:#e7e4d8}#detail{white-space:pre-wrap;min-height:230px;padding:16px;background:#eeede7}small{color:#555}.note{border:1px solid #aaa;padding:12px;max-width:1100px}@media(max-width:1150px){main{display:block}aside{position:static}canvas{max-width:800px}}</style>
<h1>LakeVillageWorld 지형 경계 전수조사</h1><p>대상: <code>Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity</code><br>읽기 전용 조사 · 2026-11-06 · 씬·Terrain·NavMesh 수정 없음</p>
<p><strong>수정 우선순위는 시작 마을·붉은나무숲의 낮은 턱 13곳 → 거점과 호안 → 채석장과 고개 → 외곽 산지입니다.</strong> 낮은 턱은 주변 지면과 경사를 연결하고, 의도된 절벽은 암반 면과 상·하단 접합을 저작하는 방향을 권합니다.</p>
<p>2400×2800m 전체, 높이 표본 <strong>16,785,409개</strong> 조사. 55° 이상 급경사를 4m 격자에서 연결한 후보 <strong>50개</strong>, 이 중 2㎡ 이상 <strong>44개</strong>. 45° 이상은 ${Math.round(data.area45).toLocaleString()}㎡, 55° 이상은 ${Math.round(data.area55).toLocaleString()}㎡, 75° 이상은 ${Math.round(data.area75).toLocaleString()}㎡입니다. 면적은 경사면의 <strong>평면 투영 면적</strong>이며, 후보 수는 수정이 확정된 결함 수가 아닙니다.</p>
<p class="note">이 보고서는 통합 월드의 현재 저장 Terrain을 직접 역직렬화한 결과입니다. 폐기 대상 LakeOfLifeScenario의 32개 후보는 집계에서 제외했습니다. Unity 직접 추출 표본은 바이너리 높이 단위 검증에만 사용했습니다. 사진 3장의 정확한 촬영 좌표를 확보하지 못했으므로 특정 후보와 일대일 대응했다고 단정하지 않습니다. 수목·건물에 가린 면, 물속, 실제 카메라 노출, 보행·카메라 충돌 여부는 현장 검수가 남습니다.</p>
<h2>먼저 고칠 구간</h2><table><thead><tr><th>대상</th><th>좌표 범위 X / Z</th><th>확인한 상태</th><th>권장 처리</th></tr></thead><tbody>
<tr><td>W016 숲의 긴 경계</td><td>949~1031 / 542~560</td><td>최대 80.6°, 급경사 362.5㎡, 한 셀 높이 차 최대 4.13m</td><td>긴 단차의 상·하단을 넓게 연결. 길과 전투 빈터 양쪽에서 확인</td></tr>
<tr><td>W019·W025·W035 호수 진입 숲길</td><td>1066~1153 / 483~561</td><td>최대 79.6°, 굽은 길 주변으로 여러 턱</td><td>각 패치를 따로 누르지 말고 길 주변의 연속 지형으로 정리</td></tr>
<tr><td>W030 마을 북측 길</td><td>1051~1075 / 435~453</td><td>최대 75.3°, 한 셀 높이 차 최대 2.21m</td><td>마을 바닥과 숲 지면의 경사 연결</td></tr>
<tr><td>W026·W029·W041·W042 시작 마을</td><td>1041~1145 / 347~422</td><td>작은 평탄부 주위의 급한 턱. 최대 73.5°</td><td>잔디만 칠하지 말고 평탄부 외곽을 완만하게 재저작</td></tr>
<tr><td>W031·W033·W038·W040 구출·채집 주변</td><td>916~979 / 488~560</td><td>최대 78.1°, 좁고 짧은 경계</td><td>발밑 높이·트리거를 보존하며 주변 전이 폭 확보</td></tr>
<tr><td>W024 북쪽 호수·사당 경계</td><td>879~1059 / 1118~1121</td><td>약 180m 폭의 얇은 경계, 최대 79.0°</td><td>긴 직선 절개를 완화하고 소품 접합을 동반 보정</td></tr>
<tr><td>W005·W012 채석장·옛 고갯길</td><td>302~522 / 1023~1483</td><td>최대 88.1°, 한 셀 높이 차 최대 13.48m</td><td>큰 사면 재저작. 이동 보호 영역과 절개 단의 접합을 우선 확인</td></tr>
<tr><td>W011·W020·W021·W023·W028·W036 동부 유적</td><td>1836~1965 / 836~964</td><td>직선·각진 단차, 최대 82.2°</td><td>통행면은 경사/계단, 건축 바깥 노출면은 기단·암반으로 마감</td></tr>
<tr><td>W008·W014·W044·W047 돌등성</td><td>480~664 / 10~222</td><td>장방형 기단 주변, 최대 80.8°</td><td>의도된 건축 경계와 Terrain 노출을 구분해 접합 보정</td></tr>
</tbody></table>
<h2>전체 후보 지도와 좌표</h2><p>빨강 ≥75° · 주황 ≥55° · 노랑 ≥45°. 북쪽은 위입니다. 지도 클릭 또는 목록 클릭으로 후보를 선택합니다. 후보 ID는 면적 순이며 우선순위 순서가 아닙니다. 대표 좌표는 각 후보의 최대 경사 표본이며 후보 전체의 중심이 아닙니다.</p>
<label>목록 필터 <select id="filter"><option value="">전체 50개</option><option>1. 초반 동선</option><option>2. 거점·호안</option><option>3. 채석장·고개</option><option>4. 외곽 사면</option></select></label><button id="all">월드 전체</button><button id="start">시작 마을·붉은나무숲 확대</button><button id="quarry">채석장·고개 확대</button>
<main><aside><canvas width="960" height="1120" aria-label="월드 경사와 후보 위치"></canvas><div id="detail" aria-live="polite">후보를 선택하세요.</div></aside><table id="candidates"><thead><tr><th>ID</th><th>우선순위</th><th>지역</th><th>대표 X / Z</th><th>최대 경사</th><th>㎡</th></tr></thead><tbody>${rows}</tbody></table></main>
<h2>원인과 수정 원칙</h2><p><strong>형상:</strong> 통합 Terrain 안의 높이 변화가 좁은 폭에 몰린 구간이 실제로 확인됐습니다. 타일 사이 균열을 수정하는 작업으로 해결할 유형이 아닙니다. 전역 Smooth로 지형 전체를 흐리면 전투 빈터·길·건축물의 지지면도 움직이므로, 대상 경계의 전이 폭과 높이를 지역별로 조정해야 합니다.</p>
<p><strong>저작 경로의 위험:</strong> ScenarioMapAuthoring.Build.cs의 CalculateHeight/SampleRoute는 가장 가까운 길의 높이와 평탄 지점을 반복 반영합니다. 서로 높이가 다른 길의 영향권이 바뀌는 지점은 높이 연속성을 확인해야 합니다. Settlement.cs의 LevelPropFootprint에는 blend=0 호출이 있어 건물 발밑 보정이 한 셀 턱을 만들 수 있습니다. District.cs의 바닥 보호와 지형 변경 가중치도 보호 영역 테두리에 변화를 집중시킬 수 있습니다. 이는 코드에서 확인한 발생 가능 경로이며, 후보 하나하나의 원인으로 확정한 것은 아닙니다.</p>
<p><strong>최신 사면 저작:</strong> Tools/OpenWorld/shape-environment-escarpment.csx는 단차·절개 형상을 만들었고, 후속 soften-environment-escarpment.csx는 기존 보존 표본에는 필터를 확장하지 않습니다. 현재 W005의 극단적인 경사와 해당 저작 영역이 겹칩니다. 보존 영역에 인접한 단차를 먼저 확인해야 합니다. 기존 RESULT에도 절개 단 반복·급경사 텍스처 늘어짐이 미해결로 명시돼 있습니다. 이 일회성 적용 스크립트들을 재실행하지 않습니다.</p>
<p><strong>재질:</strong> 급한 면의 형태를 먼저 정리한 뒤 최종 경사에 맞춰 잔디·흙·암반 혼합을 다시 검사해야 합니다. 가파른 절벽을 유지할 곳은 별도 암반 메시나 수직 면에 맞는 투영 방식을 검토합니다. 작은 바위와 수풀로 긴 경계를 덮는 것만으로 완료 판정하지 않습니다. 이번 전체 집계는 높이 데이터 기준이며 늘어짐 자체의 자동 합격 판정은 하지 않았습니다.</p>
<h2>검증과 남은 확인</h2><p>전체 높이 표본을 읽었고 셀 간격 X=${dxText(data.spacing[0])}m, Z=${dxText(data.spacing[1])}m로 경사를 계산했습니다. 각 셀 네 변의 최대 높이차를 축별로 사용한 보수적 후보 추출값으로, Unity GetSteepness 값이나 KCC 보행 가능 각도 판정을 그대로 대체하지 않습니다. Terrain hole 420셀은 후보 집계에서 제외했습니다. 55° 이상 표본이 모든 후보에 정확히 한 번 포함되는 합계 검사를 통과했습니다. 45° 이상 전체 후보는 별도 20m 격자 CSV에도 기록했습니다.</p>
<p>Unity GetHeights 대조 표본 1,050,625개와 바이너리 해석의 최대 정규화 오차는 ${provenance.unity_reference_max_error.toExponential(3)}입니다. 대상 씬과 Terrain은 추출 전후 SHA256이 동일합니다. Unity가 종료돼 현재 맵을 순회하는 Play Mode·렌더 검수와 NavMesh 연결성 검사는 수행하지 않았습니다. 최종 화면에서 각 후보의 노출 여부를 확인하고 수정 범위를 확정해야 합니다.</p>
<p>수정 후에는 상·하단/측면에서 잔디 늘어짐·직선 턱·공중에 뜨는 소품을 확인하고, 기존 길의 보행·대시·회피·전투·적 추격/복귀·호안 복귀·저장 재진입을 검수합니다. SceneEntityId·진행 트리거·상호작용 위치와 건물 지지면을 보존하고, 형상 변경에 필요한 NavMesh 재검증을 별도 수행해야 합니다.</p>
<p><a href="world-boundaries.csv">전체 50개 좌표 CSV</a> · <a href="world-slope45-tiles.csv">45° 이상 20m 격자 CSV</a> · <a href="world-review.json">상세 조사 JSON</a> · <a href="world-provenance.json">원본 해시·추출 근거</a></p>
<script>const data=${JSON.stringify(data)},canvas=document.querySelector('canvas'),ctx=canvas.getContext('2d'),image=new Image;let selected=null,view=[0,-700,2400,2800];const filtered=()=>data.groups.filter(g=>!document.querySelector('#filter').value||g.priority===document.querySelector('#filter').value);function draw(){if(!image.complete)return;ctx.clearRect(0,0,960,1120);const [vx,vz,vw,vh]=view;ctx.drawImage(image,vx/2400*image.width,(2100-vz-vh)/2800*image.height,vw/2400*image.width,vh/2800*image.height,0,0,960,1120);ctx.font='13px system-ui';for(const g of filtered()){const x=(g.peak[0]-vx)/vw*960,y=(vz+vh-g.peak[2])/vh*1120;if(x<0||y<0||x>960||y>1120)continue;ctx.fillStyle=g===selected?'#fff':'#101010';ctx.fillRect(x-3,y-3,6,6);if(g.area>15||g===selected){ctx.strokeStyle='#111';ctx.lineWidth=3;ctx.strokeText(g.id,x+5,y-5);ctx.fillStyle='#fff';ctx.fillText(g.id,x+5,y-5)}}if(selected){const g=selected;ctx.strokeStyle='#fff';ctx.lineWidth=2;ctx.strokeRect((g.minX-vx)/vw*960,(vz+vh-g.maxZ)/vh*1120,(g.maxX-g.minX)/vw*960,(g.maxZ-g.minZ)/vh*1120)}}function select(g){selected=g;document.querySelector('#detail').textContent=g.id+' · '+g.region+' · '+g.priority+'\n대표 X/Y/Z '+g.peak.map(v=>v.toFixed(2)).join(' / ')+'\n범위 X '+g.minX.toFixed(1)+'~'+g.maxX.toFixed(1)+' / Z '+g.minZ.toFixed(1)+'~'+g.maxZ.toFixed(1)+'\n최대 경사 '+g.maxSlope.toFixed(1)+'°, 한 셀 최대 높이 차 '+g.maxCellStep.toFixed(2)+'m\n저작 데이터의 인접 경로: '+g.route.name+' (대표점 기준 '+g.route.distance.toFixed(1)+'m, 폭 제외)\n수정 방향: '+g.action+'\n※ 화면 노출과 통행 여부는 현장 검수 필요';draw()}document.querySelectorAll('tr[data-id]').forEach(row=>row.onclick=()=>select(data.groups.find(g=>g.id===row.dataset.id)));document.querySelector('#filter').onchange=()=>{document.querySelectorAll('tr[data-id]').forEach(row=>row.hidden=!filtered().some(g=>g.id===row.dataset.id));draw()};document.querySelector('#all').onclick=()=>{view=[0,-700,2400,2800];draw()};document.querySelector('#start').onclick=()=>{view=[880,330,310,280];draw()};document.querySelector('#quarry').onclick=()=>{view=[200,1000,360,500];draw()};canvas.onclick=e=>{const r=canvas.getBoundingClientRect(),x=view[0]+(e.clientX-r.left)/r.width*view[2],z=view[1]+view[3]-(e.clientY-r.top)/r.height*view[3];const g=filtered().sort((a,b)=>Math.hypot(a.peak[0]-x,a.peak[2]-z)-Math.hypot(b.peak[0]-x,b.peak[2]-z))[0];if(g)select(g)};image.onload=draw;image.src='data:image/png;base64,${fs.readFileSync(`${dir}/world-slope-map.png`).toString('base64')}';</script></html>`;
function dxText(n){return n.toFixed(6)}
fs.writeFileSync(`${dir}/LakeVillageWorld-REPORT.html`,html);
console.log('LakeVillageWorld 보고서 생성: 전체 50개, 초반 동선 13개');
