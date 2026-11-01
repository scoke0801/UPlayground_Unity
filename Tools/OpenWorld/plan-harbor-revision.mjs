import fs from 'node:fs';
import assert from 'node:assert/strict';
const rows=[];
const base='Assets/03.Prefabs/Environment/';
function add(group,path,x,z,yaw=0,scale=1,y=null,align='bottom',decorative=false){
  rows.push({group,path:base+path+'.prefab',position:[x,y??0,z],yaw,scale:Array.isArray(scale)?scale:[scale,scale,scale],mode:y===null?'ground':'fixed',align,decorative});
}
const east=(g,n,x,z,r=0,s=1,y=null,a='bottom',d=false)=>add(g,'WorldCities/StonegateEast/'+n,x,z,r,s,y,a,d);
const lake=(g,n,x,z,r=0,s=1)=>add(g,'LakeVillage/LakeHouse_'+n,x,z,r,s);
const kit=(g,n,x,z,r=0,s=1,y=50)=>add(g,'HarborRevision/Harbor_'+n,x,z,r,s,y,'origin');
// 항구 진입로를 열고 주거를 작은 생활 단위로 엇갈려 배치한다.
lake('주거/서쪽 어부 골목','Fisher',1899,-247,73,1.25);
lake('주거/서쪽 어부 골목','CottageL',1882,-255,168,1.25);
lake('주거/서쪽 어부 골목','Small',1894,-275,96,1.2);
lake('주거/서쪽 어부 골목','Stilt',1878,-292,84,1.25);
lake('주거/서쪽 어부 골목','Fisher',1891,-302,174,1.3);
lake('주거/동쪽 상점 골목','Shop',1939,-243,264,1.2);
lake('주거/동쪽 상점 골목','Large',1956,-253,182,1.35);
lake('주거/동쪽 상점 골목','CottageL',1948,-277,266,1.3);
lake('주거/동쪽 상점 골목','Small',1964,-292,252,1.2);
east('항구 생업/여관과 창고','House_Inn_C',1948,-312,177,.95);
east('항구 생업/여관과 창고','House_Warehouse',1880,-319,180,1.2);
lake('항구 생업/선박 공방','Boathouse',1866,-307,105,1.35);
east('항구 생업/선박 공방','House_Smithy',1864,-280,85,.85);
// 8m 단위 연속 포장으로 보행로의 틈을 없앤다.
for(let z=-228;z>=-316;z-=8)east('길/중앙 돌길','Road_Straight_B_Worn',1920,z,90,[1,.12,1.24],50.07,'top');
for(let x=1904;x<=1936;x+=8)for(let z=-284;z>=-292;z-=8)east('길/어시장 광장',x===1920&&z===-284?'Road_Plaza':'Road_Straight_B_Worn',x,z,0,[1,.12,1.24],50.07,'top');
for(let x=1880;x<=1912;x+=8)east('길/서쪽 샛길','Road_Straight_B_Worn',x,-264,0,[1,.12,.6],50.06,'top');
for(let x=1928;x<=1960;x+=8)east('길/동쪽 샛길','Road_Straight_B_Worn',x,-264,0,[1,.12,.6],50.06,'top');
for(let x=1872;x<=1984;x+=8)kit('해안/석축 안벽','StoneQuay',x,-326);
for(let z=-334;z>=-350;z-=8)for(let x=1968;x<=1984;x+=8)kit('해안/등대 방파제','StoneQuay',x,z);
for(const [x,last] of [[1884,-350],[1905,-350],[1932,-358]])for(let z=-334;z>=last;z-=8)kit('해안/목재 잔교','TimberDeck',x,z);
for(const [x,z,r] of [[1877,-345,4],[1912,-346,-8],[1925,-354,11],[1940,-342,176]])kit('해안/정박 어선','FishingSkiff',x,z,r,1,46.1);
add('해안/등대','LakeVillage/LakeHouse_Tower',1971,-351,180,[1.35,1.6,1.35],50);
east('해안/쉼터','Prop_Pavilion',1976,-314,0,1.1);
east('항구 생업/어시장','Prop_Market_Stall_East',1907,-281,95,1.1);
east('항구 생업/어시장','Prop_Market_Stall_East',1933,-282,265,1.1);
east('항구 생업/어시장','Prop_Market_Stall_East',1905,-291,85,1);
east('항구 생업/어시장','Prop_Market_Stall_East',1934,-293,275,1);
east('항구 생업/공동 우물','Deco_Well',1904,-253,0,.7);
for(const [x,z] of [[1901,-243],[1938,-250],[1894,-287],[1958,-287],[1885,-309],[1958,-308]]){
 east('생활/처마 아래 항아리','Deco_Jar_Terrace',x,z,15,.55);
 add('생활/어구 건조대','PoorVillage/Prop_DryingRack',x-3,z+3,83,.9);
 add('생활/평상','PoorVillage/Prop_Pyeongsang',x+3,z,0,.8);
}
for(const [x,z] of [[1878,-324],[1884,-322],[1946,-322],[1960,-323],[1862,-307],[1940,-306]]){
 add('생활/하역 물품','PoorVillage/Prop_Jige',x,z,25,1.1);
 add('생활/하역 물품','PoorVillage/Prop_JarPlatform',x+2,z,10,.8);
 add('생활/하역 물품','PoorVillage/Prop_Firewood',x-2,z+2,85,.8);
}
for(const [x,z] of [[1915,-233],[1925,-251],[1915,-273],[1926,-305],[1900,-322],[1943,-322],[1964,-325],[1985,-342],[1965,-354]])east('조명/석등','Prop_Stone_Lantern',x,z,0,.85);
for(const [x,z] of [[1884,-353],[1905,-353],[1932,-361],[1948,-329],[1960,-329]])kit('해안/계선주','Mooring',x,z,0,1,50);
const tree='Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/PF_Farm_GenTree_D_FullTree_01.prefab';
for(const [x,z,s] of [[1870,-242,.8],[1880,-231,.85],[1957,-232,.7],[1971,-265,.85],[1977,-296,.7],[1856,-296,.75],[1891,-284,.65],[1940,-269,.7]])rows.push({group:'식생/그늘 나무',path:tree,position:[x,0,z],yaw:x%90,scale:[s,s,s],mode:'ground',align:'bottom',decorative:true});
const bush='Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/Bushes/PF_Farm_GenTree_Bush_04.prefab';
for(const [x,z] of [[1870,-240],[1881,-232],[1958,-233],[1973,-265],[1979,-296],[1858,-296],[1890,-283],[1941,-269],[1882,-247],[1955,-245],[1875,-302],[1967,-303]]){
 for(let i=0;i<2;i++)rows.push({group:'식생/골목 관목',path:bush,position:[x+i*1.5,0,z+i],yaw:x%180,scale:[.7,.7,.7],mode:'ground',align:'bottom',decorative:true});
}
for(const [x,z,r] of [[1874,-248,0],[1878,-248,0],[1882,-248,0],[1953,-245,0],[1957,-245,0],[1961,-245,0],[1969,-282,90],[1969,-286,90],[1969,-290,90],[1885,-309,0],[1889,-309,0]])add('생활/작은 마당 울타리','PoorVillage/Prop_Fence_Wattle',x,z,r,.9);
for(let i=0;i<28;i++){
 const x=1860+i*4.8,z=-332-Math.sin(i*1.7)*1.6;
 if([1884,1905,1932].some(p=>Math.abs(p-x)<5)||x>1962)continue;
 add('해안/물가 바위','PoorVillage/Terrain_Boulder_'+['A','B','C'][i%3],x,z,i*47,[1.2,.9,1.1],46.5);
}
for(const row of rows){assert(fs.existsSync(row.path),row.path);assert(row.scale.every(v=>v>0));assert(!/WorldCities\/(Harbor|CoastalBoat)|Dungeon/.test(row.path));}
fs.writeFileSync('Tools/OpenWorld/harbor-revision-plan.json',JSON.stringify(rows,null,2)+'\n');
console.log(`${rows.length}개 배치, ${new Set(rows.map(r=>r.path)).size}종 리소스`);



