// 생성 전에 잘못된 경로·범위·에셋 참조를 차단하는 빠른 보조 검사다.
import fs from 'node:fs';
import assert from 'node:assert/strict';
const recipe=JSON.parse(fs.readFileSync('Tools/OpenWorld/city-layout.json','utf8'));
const {worldOrigin:origin,worldSize:size,props}=recipe.landscape;
const inside=p=>p.x>=origin.x&&p.x<=origin.x+size.x&&p.z>=origin.z&&p.z<=origin.z+size.z;
const routes=[...recipe.cities.map(c=>({id:c.mapId,points:c.road})),...recipe.routes];
const results=[];
for(const route of routes){
 let length=0,grade=0;
 for(let i=0;i<route.points.length;i++){
  const p=route.points[i];assert(Object.values(p).every(Number.isFinite),`${route.id}: 유효하지 않은 좌표`);
  assert(inside(p),`${route.id}: 지형 밖 경로`);
  assert(p.y>=origin.y&&p.y<=origin.y+size.y,`${route.id}: 지형 고도 범위 밖`);
  if(!i)continue;
  const a=route.points[i-1],horizontal=Math.hypot(p.x-a.x,p.z-a.z);
  assert(horizontal>.1,`${route.id}: 중복 점`);
  length+=Math.hypot(horizontal,p.y-a.y);grade=Math.max(grade,Math.abs(p.y-a.y)/horizontal);
 }
 assert(grade<.35,`${route.id}: 경로 경사 35% 초과`);
 results.push({id:route.id,lengthMeters:Math.round(length),maximumGrade:Number(grade.toFixed(3))});
}
const ids=new Set();
for(const p of props){
 assert(!ids.has(p.id),`중복 배치 ID: ${p.id}`);ids.add(p.id);
 assert(inside(p.position),`지형 밖 배치: ${p.id}`);
 assert(fs.existsSync(p.prefab),`프리팹 누락: ${p.prefab}`);
 assert(Object.values(p.scale).every(x=>Number.isFinite(x)&&x>0),`배율 오류: ${p.id}`);
}
const report={routes:results,propCount:props.length,distinctPrefabs:new Set(props.map(p=>p.prefab)).size,
 terrainAreaSquareKilometers:size.x*size.z/1e6,note:'설계 데이터 검사. Unity 접지·충돌·보행·미술 품질 검사를 대신하지 않음'};
fs.mkdirSync('output/OpenWorld/Expansion',{recursive:true});
fs.writeFileSync('output/OpenWorld/Expansion/layout-validation.json',JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
