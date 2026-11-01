import fs from 'node:fs';
import assert from 'node:assert/strict';

// 배치는 월드 좌표로 보존한다. 원본 시나리오와 지역 로딩 변환은 변경하지 않는다.
const rows=[];
const base='Assets/03.Prefabs/Environment/';
const east='WorldCities/StonegateEast/';
function add(group,prefab,x,z,yaw=0,scale=1,y=null,align='bottom') {
  rows.push({group,prefab:base+prefab+'.prefab',position:[x,y??0,z],yaw,
    scale:align==='road'?[1,.15,1]:Array.isArray(scale)?scale:[scale,scale,scale],ground:y===null,align:align==='road'?'bottom':align});
}
function floor(group,x,z,y,scale=1) {add(group,'Dungeon/Dng_Floor_4x4',x,z,0,[1.011*scale,1,1.011*scale],y,'top');}
function grid(group,x0,x1,z0,z1,y) {for(let x=x0;x<=x1;x+=4)for(let z=z0;z<=z1;z+=4)floor(group,x,z,y);}
const castle='돌등성';
add(castle+'/본성',east+'Castle_Keep',580,26,180,1,64);
add(castle+'/성문',east+'Castle_Gate',580,216,180);
for(let z=40;z<=200;z+=8)for(const x of [512,648])add(castle+'/성벽',east+'Castle_Wall_Straight',x,z,90);
for(let x=520;x<=640;x+=8){add(castle+'/성벽',east+'Castle_Wall_Straight',x,20);if(Math.abs(x-580)>12)add(castle+'/성벽',east+'Castle_Wall_Straight',x,216);}
for(const x of [512,648])for(const z of [20,216])add(castle+'/망루',east+'Castle_Tower_Corner',x,z,180);
const houses=['House_Tile_A','House_Shop_B','House_Row_E','House_Smithy','House_Inn_C','House_Tile_D','House_Warehouse'];
for(let i=0;i<7;i++)for(const side of [-1,1])add(castle+'/성내 주거',east+houses[(i+(side===1?2:0))%houses.length],580+side*43,66+i*20,side===1?90:270,.7);
for(let z=68;z<=204;z+=8)add(castle+'/중앙 가로',east+'Road_Castle_Straight',580,z,0,1,null,'road');
for(let x=552;x<=608;x+=8)for(let z=116;z<=140;z+=8)add(castle+'/시장 광장',east+'Road_Plaza',x,z,0,1,null,'road');
for(const x of [564,596])for(const z of [116,132,176])add(castle+'/시장 광장',east+'Prop_Market_Stall_East',x,z,x<580?90:270,.85);
add(castle+'/시장 광장',east+'Deco_Well',580,98,0,.85);
for(let z=60;z<210;z+=24)for(const x of [572,588])add(castle+'/가로 소품',east+'Prop_Stone_Lantern',x,z,0,.7);
for(const x of [520,640])for(const z of [52,188])add(castle+'/가로 소품',east+'Deco_War_Banner',x,z);

const village='PoorVillage';
const poor=['PoorHouse_Chogajip','PoorHouse_Hut','PoorHouse_LHouse','PoorHouse_Jumak','PoorHouse_Barn','PoorHouse_Granary','PoorHouse_Dugout'];
const sites=[[938,444,90],[970,438,180],[985,470,270],[985,492,270],[975,512,180],[951,480,90],[940,504,90]];
sites.forEach(([x,z,yaw],i)=>add(village+'/가뭄 마을 주거','PoorVillage/'+poor[i],x,z,yaw));
for(let i=0;i<7;i++){let x=1010+(i%3)*22,z=355+Math.floor(i/3)*24;add(village+'/남쪽 마을 주거','PoorVillage/'+poor[i],x,z,i%2?180:0);}
for(const [x,z] of [[960,458],[1030,390]])add(village+'/공동 우물','PoorVillage/Prop_Well',x,z);
for(const [x,z] of [[952,452],[994,485],[1014,366],[1058,382]]){
  add(village+'/생활 소품','PoorVillage/Prop_JarPlatform',x,z);
  add(village+'/생활 소품','PoorVillage/Prop_Pyeongsang',x+3,z+2);
  add(village+'/생활 소품','PoorVillage/Prop_Firewood',x-3,z);
}
for(const [x,z] of [[974,518],[1048,350]]){
  add(village+'/농가','PoorVillage/Prop_ChickenCoop',x,z);
  add(village+'/농가','PoorVillage/Prop_Haystack',x+4,z);
  add(village+'/농가','PoorVillage/Prop_DryingRack',x-4,z);
}
for(let z=448;z<=504;z+=4)add(village+'/외곽 울타리','PoorVillage/Prop_Fence_Wattle',930,z,90);
for(let x=1006;x<=1058;x+=4)add(village+'/외곽 울타리','PoorVillage/Prop_Fence_Wattle',x,346);
add(village+'/마을 입구','PoorVillage/Prop_Gate',976,425);
add(village+'/마을 입구','PoorVillage/Prop_Jangseung',970,424);
add(village+'/마을 입구','PoorVillage/Prop_Seonangdang',937,479);

const harbor='항구';
for(let i=0;i<5;i++)for(const side of [-1,1])add(harbor+'/주거 거리','WorldCities/HarborHouse',1920+side*27,-238-i*18,side===1?90:270,.8);
for(const x of [1890,1966])add(harbor+'/창고 지구','WorldCities/HarborWarehouse',x,-316,180,.9);
for(let z=-240;z>=-320;z-=8)add(harbor+'/중앙 가로',east+'Road_Straight_A',1920,z,0,1,null,'road');
for(let x=1876;x<=1972;x+=4)for(let z=-326;z>=-334;z-=4)floor(harbor+'/부두',x,z,50);
for(const x of [1890,1920,1950]){
  add(harbor+'/잔교','WorldCities/HarborPier',x,-344,0,1,50,'top');
  add(harbor+'/어선','WorldCities/CoastalBoat',x+8,-353,180,1,46);
}
add(harbor+'/등대','WorldCities/HarborLighthouse',1972,-356,0,1,50);
grid(harbor+'/등대 기단',1968,1976,-360,-352,50);
grid(harbor+'/등대 연결로',1968,1972,-348,-336,50);
for(let z=-352;z<=-336;z+=4)floor(harbor+'/동쪽 작업 부두',1980,z,50);
floor(harbor+'/동쪽 작업 부두',1976,-336,50);
add(harbor+'/낚시 잔교','WorldCities/HarborPier',1905,-344,0,1,50,'top');
for(const x of [1878,1962])for(let z=-308;z>=-324;z-=4){add(harbor+'/화물','WorldCities/CargoCrate',x,z);add(harbor+'/화물','WorldCities/CargoBarrel',x+2,z);}
for(const x of [1905,1935])for(const z of [-250,-290])add(harbor+'/장터',east+'Prop_Market_Stall_East',x,z,x<1920?90:270,.8);
for(const x of [1874,1966])for(const z of [-280,-294])add(harbor+'/어촌 소품','PoorVillage/Prop_DryingRack',x,z);

const hall='던전/묻힌 등불전';
grid(hall+'/전실과 대청',318,342,518,586,104);
for(let z=518;z<=586;z+=4)for(const x of [324,336])for(const y of [104,108.5])add(hall+'/외벽','Dungeon/Dng_Wall_Plain',x,z,90,1,y);
for(let x=326;x<=334;x+=4)for(const y of [104,108.5])add(hall+'/외벽','Dungeon/Dng_Wall_Plain',x,590,0,1,y);
for(let x=318;x<=342;x+=4)if(Math.abs(x-330)>4)for(const y of [104,108.5])add(hall+'/석문','Dungeon/Dng_Wall_Plain',x,514,0,1,y);
add(hall+'/석문','Dungeon/Dng_Arch',330,514,0,1.5,104);
for(let z=542;z<=578;z+=12)for(const x of [326,334])add(hall+'/대청 기둥','Dungeon/Dng_Pillar',x,z,0,.7,104);
grid(hall+'/지하 천장',326,334,518,586,113);
add(hall+'/유물 제단','Dungeon/Dng_Altar',330,582,180,1,104);
for(const z of [522,538,554,570,586])for(const x of [318,342])add(hall+'/등불','Dungeon/Dng_Brazier',x,z,0,1,104);

const temple='던전/사슬 제단';
grid(temple+'/석교',588,592,826,846,140);
grid(temple+'/제단 광장',576,608,850,874,140);
for(const x of [576,608])for(const z of [850,874])add(temple+'/경계 기둥','Dungeon/Dng_Pillar',x,z,0,1.5,140);
add(temple+'/입구','Dungeon/Dng_Arch',590,828,0,1.5,140);
add(temple+'/제단','Dungeon/Dng_Altar',600,866,180,1.5,140);
grid(temple+'/상부 회랑',588,592,858,866,144.5);
for(let i=0;i<3;i++)add(temple+'/오름 계단','Dungeon/Dng_Stairs',590,850+i*2.7,180,[1.5,1,1],140+i*1.5);
for(const x of [580,604])for(const z of [850,870])add(temple+'/제단 화로','Dungeon/Dng_Brazier',x,z,0,1,140);
add(temple+'/감시탑','Dungeon/Dng_Watchtower',612,862,0,1,140);

const ring='던전/가시울 옛터';
grid(ring+'/서쪽 진입로',1850,1870,910,914,110);
grid(ring+'/옛터 광장',1874,1934,878,934,110);
for(let z=878;z<=934;z+=4)for(const x of [1870,1938])if(!(x===1870&&z>=906&&z<=918))add(ring+'/폐허 성벽','Dungeon/Dng_Wall_Ruined',x,z,90,1,110);
for(let x=1874;x<=1934;x+=4)for(const z of [874,938])add(ring+'/폐허 성벽','Dungeon/Dng_Wall_Ruined',x,z,0,1,110);
add(ring+'/서문','Dungeon/Dng_Arch',1868,912,90,1.5,110);
add(ring+'/예배당','Dungeon/Dng_Chapel',1900,882,180,1,110);
add(ring+'/옛 병영','Dungeon/Dng_Barracks',1882,928,0,1,110);
add(ring+'/감옥','Dungeon/Dng_Prison',1926,928,0,1,110);
for(const [x,z] of [[1880,888],[1930,888],[1880,924],[1930,916]])add(ring+'/경계 화로','Dungeon/Dng_Brazier',x,z,0,1,110);
add(ring+'/수호상','Dungeon/Dng_Statue_Guardian',1900,896,180,1.5,110);

for(const row of rows){assert(fs.existsSync(row.prefab),row.prefab);assert(row.scale.every(n=>n>0));assert(row.position.every(Number.isFinite));}
fs.writeFileSync('Tools/OpenWorld/full-replacement-plan.json',JSON.stringify(rows,null,2)+'\n');
console.log(JSON.stringify({instances:rows.length,groups:[...new Set(rows.map(r=>r.group))].length}));
