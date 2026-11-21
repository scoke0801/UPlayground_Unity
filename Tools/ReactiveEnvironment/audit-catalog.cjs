// 전체 카탈로그의 경로와 저작 상태를 CSV로 기록한다. 저장소 루트에서 실행한다.
const fs=require('node:fs');
const full=JSON.parse(fs.readFileSync('docs/asset-catalog/manifest.json','utf8'));
const selected=JSON.parse(fs.readFileSync('output/asset-catalog/manifest.json','utf8'));
const selectedSources=new Map(selected.map(row=>[row.source,row.path]));
const folder='output/ReactiveEnvironment/AllTreeAudit/';
const unity=new Map(['tree','prop'].flatMap(category=>JSON.parse(fs.readFileSync(folder+'unity-'+category+'-prefabs.json','utf8')))
 .map(prefab=>[prefab.path,prefab]));
const installed=full.filter(row=>fs.existsSync(row.path));
const skipped=fs.readFileSync('docs/asset-catalog/skipped.txt','utf8').trim().split(/\r?\n/).filter(Boolean).map(line=>{const i=line.indexOf('|');return{path:line.slice(0,i),reason:line.slice(i+1)};});
const paired=JSON.parse(fs.readFileSync('output/ReactiveEnvironment/AllTreeAudit/eastlands-geometry.json','utf8'));
const pairPaths=new Map(paired.map(row=>[row.path,row.upper.length]));
const part=/(?:Stump|Trunk|_Log|^Log|^Branch|Chopped|Remnant|Impostor)/i;
const nonTree=/GroundLeaves|^PF_Farm_GenTree_Bush|^PF_Farm_GenTree_Sapling|_Sapling_|^TGV_.*_Ivy$/i;
const wood=/Barrel|Crate|Box|Basket|Cart|Carriage|Wooden|WoodPile|Woodpile|Firewood/i;
const ceramic=/Vase|Pottery|Ceramic|Earthen|ClayPot|^vase_/i;
const treeNames={blossom:/^BlossomTree_(\d\d)$/,broadleaf:/^BroadleafTree_(\d\d)_/,fir:/^Fir_(\d\d)$/,willow:/^WillowTree_(\d\d)_/};
function classify(row){
 const {name,cat,path:asset}=row;
 const present=fs.existsSync(asset), wrapper=selectedSources.get(name)||'';
 if(!present)return {...row,status:'경로 확인 필요',wrapper};
 if(cat==='tree'){
  if(wrapper)return {...row,wrapper,status:'반응 프리팹 연결',parts:pairPaths.get(asset)||0};
  if(asset.includes('/ReactiveTrees/'))return {...row,status:'프로젝트 반응 프리팹'};
  if(pairPaths.has(asset))return {...row,status:'구매 절단 부품 있음',parts:pairPaths.get(asset)};
  if(part.test(name)||asset.includes('/Trunks and Stumps/')||asset.includes('/TreeParts/'))return {...row,status:'절단·잔해·임포스터 부품'};
  if(nonTree.test(name))return {...row,status:'묘목·관목·별도 덩굴·낙엽'};
  if(/PF_Farm_GenTree_[A-D]_FullTree/.test(name))return {...row,status:'기존 Farmlands 벌목 배치'};
  const osmanthusTint=name.match(/^(PF_EastLands_Osmanthus_\d\d)_(Copper|Gold|Green)_/);
  if(asset.includes('/RiverBasinWorld/')&&osmanthusTint)return {...row,status:'기본형과 다른 월드 재질 변형',geometry:osmanthusTint[1]};
  if(asset.includes('/Reactive/'))return {...row,status:'기존 프로젝트 반응 프리팹'};
  if(name==='ChoppableTree')return {...row,status:'구매 채집 전용 원형·ChoppedTree 별도 확인'};
  if(asset.includes('/Toon Golden Valley/'))return {...row,status:'새 수관·절단 형상 저작 후보 · TGV'};
  if(/_Young_|DeadTree|Dead_Tree|DriedTree/.test(name))return {...row,status:'어린 나무·고사목 · 별도 파손 저작 후보'};
  if(asset.includes('/LowPolyFantasyArena2/'))return {...row,status:'소형 나무 · 파손 형상 후보'};
  if(name==='PF_EastLands_HeroTree')return {...row,status:'상징 수목 · 개별 파손 저작 후보'};
  const shape=Object.entries(treeNames).find(([,pattern])=>pattern.test(name));
  if(asset.includes('/Idyllic Fantasy Nature/')&&shape)return {...row,status:'절단 저작 후보',geometry:shape[0]+name.match(shape[1])[1]};
  return {...row,status:'절단·기존 상호작용 확인 대상'};
 }
 if(cat==='prop'){
  if(wrapper)return {...row,wrapper,status:'반응 프리팹 연결'};
  if(unity.get(asset)?.gathering>0)return {...row,status:'기존 채집 소유 · 직접 전환 제외'};
  if(unity.get(asset)?.reactive>0)return {...row,status:'기존 프로젝트 반응 프리팹'};
  if(wood.test(name))return {...row,status:'목재 소품 후보 · 모델·배치 소유 확인'};
  if(ceramic.test(name))return {...row,status:'도자기 후보 · 모델·배치 소유 확인'};
  return {...row,status:'소품 · 상호작용 용도 개별 확인'};
 }
 return {...row,status:'별도 용도 · 수목·소품 후보와 구분'};
}
const rows=full.map(row=>{
 const entry=classify(row), prefab=unity.get(row.path);
 if(!prefab)return entry;
 return {...entry,loaded:prefab.loaded,colliders:prefab.colliders,lodGroups:prefab.lods,
  gathering:prefab.gathering,reactive:prefab.reactive,breakable:prefab.breakable};
});
const categories=Object.fromEntries([...new Set(rows.map(row=>row.cat))].map(cat=>[cat,rows.filter(row=>row.cat===cat).length]));
const statuses=Object.fromEntries([...new Set(rows.map(row=>row.status))].map(status=>[status,rows.filter(row=>row.status===status).length]));
const groups={};for(const r of rows.filter(x=>x.geometry&&x.status==='절단 저작 후보'))(groups[r.geometry]??=[]).push(r.path);
const idyllicMeshGroups=new Map();
for(const row of rows.filter(row=>row.path.includes('/Idyllic Fantasy Nature/')&&(row.geometry||row.name==='Fir_02'))){
 const signature=unity.get(row.path).meshes.join('|');
 if(!idyllicMeshGroups.has(signature))idyllicMeshGroups.set(signature,[]);
 idyllicMeshGroups.get(signature).push(row.path);
}
fs.writeFileSync(folder+'inventory.json',JSON.stringify({scope:'docs/asset-catalog/manifest.json and skipped.txt',total:rows.length,installed:installed.length,categories,statuses,unityLoaded:[...unity.values()].filter(row=>row.loaded).length,idyllicSourceFamilies:groups,idyllicMeshGroups:[...idyllicMeshGroups.values()],skipped,rows},null,2)+'\n');
const csvValue=v=>'"'+String(v??'').replaceAll('"','""')+'"';
const fields=['cat','name','path','status','geometry','parts','wrapper','loaded','colliders','lodGroups','gathering','reactive','breakable','h','w','tris'];
fs.writeFileSync(folder+'inventory.csv','\uFEFF'+fields.join(',')+'\r\n'+rows.map(row=>fields.map(key=>csvValue(row[key])).join(',')).join('\r\n')+'\r\n');
console.log(JSON.stringify({counts:categories,installed:installed.length,statuses,idyllicShapeFamilies:idyllicMeshGroups.size,skipped:skipped.length},null,2));
