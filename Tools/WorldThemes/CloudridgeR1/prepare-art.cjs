const fs=require('fs');
const survey=JSON.parse(fs.readFileSync('output/CloudridgeR1/survey.json','utf8').replace(/^\uFEFF/,''));
const scene=[];
for(const o of survey){
 const [x,y,z]=o.position;
 const area=((x-1240)/112)**2+((z-1285)/69)**2;
 const highland=o.path.includes('북서 고원 혼합림')||o.path.includes('북쪽 고원 숲과 쉼터');
 if(!highland||area>1||!o.active)continue;
 if((/FullTree/.test(o.prefab)&&!/impostor/.test(o.prefab))||/Bush/.test(o.prefab)||/Terrain_Boulder/.test(o.prefab))scene.push({id:o.id,path:o.path,position:o.position,prefab:o.prefab,property:'m_IsActive',before:true,after:false,reason:'기존 발견 순환로의 낮은 어깨와 남향 조망을 여는 선택 솎기'});
}
const plan={coordinateSpace:'Unity world metres',bounds:[1115,1405,1218,1382],ellipse:[1260,1300,145,82],routeProtection:6,contentProtection:10,colliderProtection:2,shoulders:[[1218,1325,35,24,2.4],[1295,1345,34,24,3.2],[1190,1275,24,19,1.5]],routes:[[[1040,1410],[1080,1370],[1145,1300],[1225,1265],[1310,1310],[1400,1365],[1360,1420]],[[800,1230],[1040,1410],[1190,1450],[1360,1420],[1490,1330],[1580,1220],[1630,1100]]],sceneChanges:scene,scope:'고원 남향 어깨와 복귀길. 다른 지역 수목·원경은 읽기 전용. 기존 발견 표식 Explore_0625/0626과 모든 콘텐츠는 변경하지 않음'};
fs.writeFileSync('Tools/WorldThemes/CloudridgeR1/art-plan.json',JSON.stringify(plan,null,2));
fs.writeFileSync('Tools/WorldThemes/CloudridgeR1/disable-ids.json',JSON.stringify(scene.map(o=>o.id)));
console.log(JSON.stringify({selected:scene.length,trees:scene.filter(o=>o.prefab.includes('FullTree')).length,rocks:scene.filter(o=>o.prefab.includes('Boulder')).length}));
