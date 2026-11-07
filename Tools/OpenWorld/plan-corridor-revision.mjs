import fs from 'node:fs';
import assert from 'node:assert/strict';
const folder='output/OpenWorld/DungeonWallRevision';
const kit='Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/prefabs/';
const assets={
 wall:kit+'MODULAR/02_COMPS/Wall/PivotMiddle/COMP_Wall_01_M_straight_med.prefab',
 tall:kit+'MODULAR/02_COMPS/Wall/PivotMiddle/COMP_Wall_01_M_straight_large.prefab',
 gate:kit+'MODULAR/02_COMPS/Gateway/COMP_Door_01_large.prefab',
 roof:kit+'MODULAR/01_PARTS/Floor/PivotEdge/P_MOD_Floor_01_E_straight_med.prefab',
 rubble:kit+'PROPS/natural/P_PROP_debris_stones_dungeon_02.prefab',
};
for(const p of Object.values(assets))assert(fs.existsSync(p),p);
const lines=fs.readFileSync(folder+'/walls-before.tsv','utf8').trim().split(/\r?\n/).map(l=>l.split('\t'));
assert.equal(lines.length,180);
const unique=new Map();
for(const [name,...values] of lines){
 const [x,y,z,sx,sy,sz]=values.slice(0,6).map(Number);
 const p=[x,y,z].map(v=>Math.round(v*100)/100);
 const key=p.join(',');
 if(!unique.has(key))unique.set(key,{name,key:sy>6?'tall':'wall',position:[p[0],p[1]-sy/2,p[2]],yaw:sx<sz?90:0,height:sy});
}
assert.equal(unique.size,90);
const placements=[...unique.values()];
for(const [x,z,yaw] of [[650,492,0],[650,520,0],[650,574,0],[662,588,90],[650,548,0],[650,560,0]])
 placements.push({name:'석조 아치',key:'gate',position:[x,100,z],yaw,scale:[1.7,1,1]});
for(let z=548;z<=572;z+=4)for(const x of [648,652])
 placements.push({name:'석조 회랑 천장',key:'roof',position:[x,107,z],yaw:0});
// 잔해는 이동 중심선과 보상 자리에서 떨어진 벽 밑에만 둔다.
for(const [x,z,yaw] of [[640,523,18],[660,541,115],[640,599,35],[660,578,160],[604,540,90]])
 placements.push({name:'기단 석재 잔해',key:'rubble',position:[x,x<620?104:100,z],yaw,scale:[.65,.65,.65],decoration:true});
fs.writeFileSync('Tools/OpenWorld/corridor-revision-plan.json',JSON.stringify({assets,placements},null,2)+'\n');
console.log({wallPairs:lines.length/2,placements:placements.length,assets:Object.keys(assets).length});
