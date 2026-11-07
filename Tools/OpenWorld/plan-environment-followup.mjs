// 후속 저작 입력만 만든다. Unity 씬을 적용하거나 전체 월드를 생성하지 않는다.
import fs from 'node:fs';
const rows=fs.readFileSync('output/OpenWorld/EnvironmentFollowup/placement-before.txt','utf8').trim().split(/\r?\n/).map(line=>{const a=line.split('|');return {path:a[0],p:a[1].match(/-?[\d.]+/g).map(Number),prefab:a[6]};});
const plan={scene:'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity',heightFeatures:[],rocks:[],moves:[],paint:[],routes:[]};
const feature=(name,x,z,rx,rz,delta)=>plan.heightFeatures.push({name,center:[x,z],radius:[rx,rz],delta});
feature('채석장 절리 돌출',371,1144,19,31,8);
feature('채석장 침식 홈',391,1181,15,22,-6);
feature('채석장 상단 파쇄',354,1213,24,18,9);
feature('채석장 자갈 퇴적',413,1198,13,25,3.5);
feature('고갯길 능선 돌출',454,1362,22,29,8);
feature('고갯길 침식 홈',477,1397,16,20,-7);
feature('고갯길 돌출 어깨',440,1421,20,24,6);
const rock=(name,model,x,z,s,yaw,tilt,embed)=>plan.rocks.push({name,prefab:`Assets/03.Prefabs/Environment/PoorVillage/Terrain_Boulder_${model}.prefab`,position:[x,z],scale:s,rotation:[tilt,yaw,0],embed});
rock('채석장_주절리','A',364,1140,[10,16,8],24,-12,5);
rock('채석장_낮은층','C',382,1148,[8,10,6],101,14,3);
rock('채석장_파쇄상단','B',349,1214,[9,14,8],232,-18,5);
rock('채석장_퇴적어깨','A',390,1218,[5.5,7,4],317,18,4);
rock('채석장_하부파편','B',410,1193,[3.2,3,2.4],43,7,1.1);
rock('채석장_파편편석','C',428,1181,[2.7,2.5,2],171,-10,.7);
rock('채석장_고립전석','B',409,1209,[2,3,1.8],285,21,.8);
rock('고개_주암반','A',457,1361,[8,13,6],47,-13,5);
rock('고개_겹친층','C',469,1368,[7,9,5],130,17,3);
rock('고개_북쪽암반','B',410,1420,[12,18,10],257,-20,6);
rock('고개_북쪽편석','A',430,1428,[6,9,5],331,15,3);
rock('고개_하부전석','C',508,1370,[2.7,3.2,2.1],84,9,1);
rock('고개_하부자갈','B',520,1378,[1.8,2.2,1.7],219,-7,.6);
// 안벽의 양쪽 끝을 자연 해안에 묻는다. 잔교와 방파제의 보행면은 그대로 둔다.
rock('항구_서안석축발','A',1865,-330,[2.2,1.6,1.8],71,8,.9);
rock('항구_서안낮은돌','C',1860,-326,[1.7,1.3,1.5],203,13,.6);
rock('항구_동안석축발','B',1991,-330,[2.8,2,2.2],141,-8,1);
rock('항구_동안전석','C',1997,-326,[1.8,1.5,1.4],318,11,.8);
const move=(r,x,z,yaw,embed=0)=>plan.moves.push({path:r.path,from:[r.p[0],r.p[2]],position:[x,z],yaw,embed,prefab:r.prefab});
const quarry=rows.filter(r=>r.path.includes('/폐채석장 갱구와 작업 회랑/')&&r.path.split('/').length===4);
const cart=quarry.find(r=>r.path.includes('/cart_'));move(cart,519,1084,180,.04);
quarry.filter(r=>r.path.includes('/ore_')).forEach((r,i)=>move(r,...[[507.5,1115],[511,1115.5],[569,1115],[558,1116]][i],25+i*41,.18));
const crypt=rows.filter(r=>r.path.includes('/석관 묘역과 무너진 묘실/')&&r.path.split('/').length===4);
const coffins=crypt.filter(r=>r.path.includes('/coffin_'));
for(const r of coffins){move(r,r.p[0],r.p[2],r.p[0]<1525?90:270,.14);const grave=crypt.filter(g=>g.path.includes('/grave_')).sort((a,b)=>Math.hypot(a.p[0]-r.p[0],a.p[2]-r.p[2])-Math.hypot(b.p[0]-r.p[0],b.p[2]-r.p[2]))[0];move(grave,r.p[0]+(r.p[0]<1525?-2.1:2.1),r.p[2],r.p[0]<1525?90:270,.03);}
// 기존 판자 묶음을 측실의 침상과 하역 위치로 모아 통로를 비운다.
for(const r of rows.filter(r=>r.path.split('/').length===4&&r.path.includes('/planks_'))){if(r.path.includes('/숲 보루'))move(r,r.p[0]<1845?1821.5:1868.5,632,90,.03);if(r.path.includes('/버려진 역참'))move(r,r.p[0]<2025?1991.5:2058.5,191,0,.02);}
const patch=(name,x,z,rx,rz,weights,strength=.8)=>plan.paint.push({name,center:[x,z],radius:[rx,rz],weights,strength});
const dirt=[.04,.53,.14,0,0,.29,0],gravel=[.02,.22,.57,0,0,.19,0],sand=[.02,.17,.18,0,0,.23,.4];
patch('채석 운반 자국',520,1085,5,20,gravel,.9);patch('채석 적재장',512,1083,9,8,gravel,.9);
for(const r of coffins)patch('매장 바닥',r.p[0],r.p[2],3.3,2.2,[.04,.67,.18,0,0,.11,0],.95);
patch('참배 중앙길',1525,1075,4.5,34,[.07,.32,.18,0,0,.43,0],.85);
patch('병영 서측실',1819,627,12,13,dirt);patch('병영 동측실',1871,627,12,13,dirt);
patch('방어 전실',1845,581,18,7,gravel);patch('병영 훈련장',1845,611,13,18,[.03,.25,.36,0,0,.36,0],.7);
patch('숙영 서쪽 하역장',1994,181,10,19,dirt);patch('숙영 동쪽 하역장',2056,181,10,19,dirt);
patch('숙영 화덕',2025,205,6,5,[0,.73,.2,0,0,.07,0],.9);
patch('숙영 운송 자국',2025,166,8,23,[.03,.32,.2,0,0,.45,0]);
// 바닥 용도를 나누고 기존 포장과 빈 공간을 남긴다.
for(const r of rows.filter(r=>r.path.includes('/돌등성/시장/점포와 노점/')&&r.path.split('/').length===6))patch('장터 발길',r.p[0],r.p[2],7,6,[.06,.22,.12,0,0,.6,0],.94);
patch('서쪽 공방 작업마당',539,82,11,11,gravel,.94);patch('동쪽 공방 작업마당',619,138,11,12,gravel,.94);
patch('창고 하역장',536,63,9,9,dirt,.88);patch('동쪽 창고 하역장',626,120,10,8,dirt,.88);
patch('동문 주거 앞뜰',617,194,12,9,[.25,.25,.06,0,0,.44,0],.78);
patch('주거 문전',602,181,8,8,[.26,.24,.06,0,0,.44,0],.78);
patch('서쪽 여관 앞뜰',548,185,9,11,[.19,.25,.06,0,0,.5,0],.85);
patch('성문 진입 답압',580,230,8,13,[.08,.26,.28,0,0,.38,0],.8);
patch('안벽 육지 접합',1920,-319,77,5,sand,.92);
patch('서해안 전이',1864,-329,15,13,sand,.88);patch('동해안 전이',1990,-329,13,12,sand,.88);
patch('창고 운송 흔적',1882,-312,6,12,gravel,.9);patch('어시장 하역 흔적',1905,-307,6,16,dirt,.9);
patch('등대 접안 가장자리',1983,-334,5,12,sand,.85);
for(const f of plan.heightFeatures)patch(f.name+' 자갈',f.center[0]+13,f.center[1],f.radius[0]*1.1,f.radius[1]*1.1,gravel,.75);
plan.routes=[{name:'고갯길',points:[[548,1240],[560,1330],[580,1435]]},{name:'돌등성 중앙',points:[[580,235],[580,180],[580,130],[580,75]]},{name:'돌등성 서쪽',points:[[580,179],[556,179],[556,129],[551,83]]},{name:'돌등성 동쪽',points:[[580,187],[610,187],[610,146],[608,106]]},{name:'항구 하역',points:[[1920,-300],[1920,-320],[1884,-326],[1884,-350]]},{name:'항구 긴잔교',points:[[1920,-320],[1932,-326],[1932,-358]]},{name:'항구 등대',points:[[1920,-320],[1968,-326],[1980,-340]]}];
fs.writeFileSync('Tools/OpenWorld/environment-followup-plan.json',JSON.stringify(plan,null,2)+'\n');
console.log(JSON.stringify({features:plan.heightFeatures.length,rocks:plan.rocks.length,moves:plan.moves.length,paint:plan.paint.length,routes:plan.routes.length}));
