import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const root='Assets/10.Datas/World/RiverBasinWorld';
const reference=JSON.parse(fs.readFileSync('Assets/docs/design/RegularWorldReferences/Routes/route-layout.json','utf8'));
const heights={A:[104,99,92,84,77,77,77,65,54,35,25,17,22,28,42,52,77,104],B:[104,88,101,116,126,137,140,119,132,123,111,106,104],C:[104,106,111,123,132,119,119,149,178,188,169,145,113,99,85,77,77,77,84,92,99,104]};
const routes=reference.loops.map(r=>({id:r.id,points:r.worldPoints.map((p,i)=>[p.x,heights[r.id][i],p.z])}));
const riverHeights=[159,140,114,101,96,88,72,65,50,33,24,20,12,0,-2];
const river=reference.river.map((p,i)=>[(p[0]-.5)*1536,riverHeights[i],(p[1]-.5)*1536]);
const nodes=Object.fromEntries(Object.entries(reference.nodes).map(([k,n])=>[k,{name:n.name,x:n.world.x,z:n.world.z}]));
const plan={version:1,mapId:'RiverBasinWorld',seed:73921,size:1536,tileSize:512,heightResolution:513,baseY:-32,heightRange:288,routeWidth:6,routeBlend:18,riverWidth:14,riverBankWidth:26,lake:{x:-15.36,z:280,rx:190,rz:150,level:96},routes,river,nodes,
 detail:{name:'외곽 작업장–B1–초원 전망',points:[[130,88,22],[184.32,84,15.36],[261.12,77,0],[307.2,77,-15.36],[368.64,77,0],[438,68,-81]],clearing:[260,77,0],clearingSize:[24,30]},
 branches:[{id:'Coast',points:[[122.88,17,-476.16],[245.76,15,-460.8],[368.64,11,-506.88],[476.16,8,-537.6]]},{id:'Entry',points:[[-399.36,24,-583.68],[-384,37,-491.52],[-368.64,52,-384]]},{id:'Shrine',points:[[368.64,77,0],[399.36,85,-30.72],[522.24,72,-76.8],[506.88,61,-199.68],[430.08,54,-245.76]]}],
 bridges:[{id:'B1',position:[307.2,77,-15.36],yaw:90,length:36,width:5.6},{id:'B2',position:[122.88,17,-476.16],yaw:129,length:36,width:5.6},{id:'B3',position:[-19.6608,119,481.6896],yaw:63.435,length:36,width:5.6}],
 views:[{id:'01_entry',position:[135,0,21],target:[186,0,15]},{id:'02_fork',position:[219,0,9],target:[280,0,-8]},{id:'03_encounter',position:[267,0,-2],target:[325,0,-14]},{id:'04_return',position:[434,0,-78],target:[362,0,-1]}]};
for(const r of routes){assert(r.points.length===heights[r.id].length);assert(r.points.every(p=>p.every(Number.isFinite)));}
plan.detail.planarLength=plan.detail.points.slice(1).reduce((s,p,i)=>s+Math.hypot(p[0]-plan.detail.points[i][0],p[2]-plan.detail.points[i][2]),0);
assert(plan.detail.planarLength>=300&&plan.detail.planarLength<=400);
fs.writeFileSync('Tools/RiverBasinWorld/world-plan.json',JSON.stringify(plan,null,2));
fs.mkdirSync('output/RiverBasinWorld',{recursive:true});
const protectedFiles=['Assets/01.Scenes/GameLogic/Boot.unity','Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity','ProjectSettings/EditorBuildSettings.asset','Assets/10.Datas/Camera/CameraOccluderNameRules.asset','Assets/Settings/PC_Renderer.asset'];
const hashes=protectedFiles.filter(fs.existsSync).map(path=>({path,sha256:crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex')}));
if(!fs.existsSync('output/RiverBasinWorld/baseline-hashes.json')) fs.writeFileSync('output/RiverBasinWorld/baseline-hashes.json',JSON.stringify(hashes,null,2));
console.log({routes:routes.length,detailMeters:plan.detail.planarLength,root});
