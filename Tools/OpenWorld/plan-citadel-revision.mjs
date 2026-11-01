import fs from 'node:fs';
import assert from 'node:assert/strict';

const east='Assets/03.Prefabs/Environment/WorldCities/StonegateEast/';
const poor='Assets/03.Prefabs/Environment/PoorVillage/';
const farm='Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/';
const rows=[];
function add(group,name,x,z,yaw=0,scale=1,y=62.02,mode='pivot'){
  rows.push({group,path:east+name+'.prefab',position:[x,y,z],yaw,scale:Array.isArray(scale)?scale:[scale,scale,scale],mode});
}
function line(group,name,a,b,y=62.02,width=1){
  const length=Math.hypot(b[0]-a[0],b[1]-a[1]);
  const limit=name==='Road_Castle_Alley'?3.8:6;
  const count=Math.ceil(length/limit);const step=length/count;
  const yaw=Math.atan2(b[0]-a[0],b[1]-a[1])*180/Math.PI;
  for(let i=0;i<count;i++){const t=(i+.5)/count;add(group,name,a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t,yaw,[width,1,1],y);}
  assert(step<=limit+.001,'도로 간격이 모듈 길이를 넘었습니다.');
}
function wall(a,b){
  const length=Math.hypot(b[0]-a[0],b[1]-a[1]);const count=Math.ceil(length/7.8);
  const yaw=Math.atan2(-(b[1]-a[1]),b[0]-a[0])*180/Math.PI;
  for(let i=0;i<count;i++){const t=(i+.5)/count;add('성곽/연속 성벽','Castle_Wall_Straight',a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t,yaw,1,62);}
  assert(length/count<8,'성벽 이음부가 벌어졌습니다.');
}
add('본성/본관','Castle_Keep',580,26,180,1,63.97);
add('성곽/남문','Castle_Gate',580,216,180,1,62);
for(const [x,z,yaw] of [[512,20,0],[648,20,90],[648,216,180],[512,216,270]])add('성곽/모서리 망루','Castle_Tower_Corner',x,z,yaw,1,62);
wall([512,23.8],[512,212.2]);wall([648,23.8],[648,212.2]);
wall([515.8,216],[571.05,216]);wall([588.95,216],[644.2,216]);
// 본성 자체의 석축에 접속하고 건물을 가로지르는 성벽은 만들지 않는다.
wall([515.8,20],[566.25,20]);wall([593.75,20],[644.2,20]);
add('성곽/서쪽 성벽 계단','Castle_Wall_Stair_Access',513.3,174,90,1,62);
add('성곽/동쪽 성벽 계단','Castle_Wall_Stair_Access',646.7,91,270,1,62);

// 본성의 도시 쪽 석축에는 외부 계단과 별도로 4단 연결 계단을 둔다.
for(let i=0;i<4;i++){
 add('본성/도시쪽 진입 계단','Castle_Terrace_Stairs',580,66-i*8,0,[1.6,3/3.4,1],62+i*3);
 if(i===3)rows[rows.length-1].isEntryStair=true;
}
add('본성/상부 연결참','Road_Plaza',580,37,0,[1,1,.55],73.75);
line('도로/성문에서 시장','Road_Straight_A',[580,224],[580,139]);
line('도로/시장과 본성','Road_Straight_A',[580,115],[580,70]);
line('성밖 진입/문전 돌길','Road_Straight_B_Worn',[580,224],[580,238]);
line('성밖 진입/문전 돌길','Road_Straight_B_Worn',[580,238],[625,238]);
line('성밖 진입/문전 돌길','Road_Straight_B_Worn',[625,238],[660,228]);
line('도로/서쪽 장터 골목','Road_Straight_B_Worn',[556,180],[556,108]);
line('도로/서쪽 공방길','Road_Castle_Alley',[556,108],[549,59],62.02,.8);
line('도로/동쪽 생활 골목','Road_Straight_B_Worn',[609,197],[611,167]);
line('도로/동쪽 공방길','Road_Straight_B_Worn',[611,167],[607,85]);
line('도로/여관 앞 연결길','Road_Straight_A',[556,179],[580,179]);
line('도로/동쪽 문전길','Road_Castle_Alley',[580,187],[609,187]);
line('도로/시장 서편 연결','Road_Straight_B_Worn',[556,129],[568,129]);
line('도로/시장 동편 연결','Road_Straight_B_Worn',[592,129],[610,129]);
line('도로/공방 앞 연결','Road_Castle_Alley',[549,83],[580,83]);
line('도로/정원 진입','Road_Straight_B_Worn',[607,85],[615,62]);

// 장터는 세 길이 만나는 작은 마당이며, 포장 무늬는 중앙에만 쓴다.
for(const x of [572,580,588])for(const z of [119,127,135])add('시장/작은 중앙 광장','Road_Plaza',x,z);
for(const [x,z,yaw] of [[563,118,90],[562,132,90],[595,119,270],[597,134,265],[559,151,95],[594,155,270]])add('시장/점포와 노점','Prop_Market_Stall_East',x,z,yaw,.95,62.02,'ground');
add('시장/우물 쉼터','Deco_Well',566,100,12,.85,62.02,'ground');
add('시장/우물 쉼터','Prop_Pavilion',561,91,8,.8,62.02,'ground');

const plots=[
 ['문전 여관가','House_Inn_C',545,192,90,.95],
 ['문전 여관가','House_Shop_B',563,201,178,.92],
 ['문전 여관가','House_Tile_A',536,175,92,1],
 ['문전 여관가','House_Warehouse',530,200,180,.9],
 ['문전 여관가','House_Row_E',541,155,180,.93],
 ['문전 여관가','House_Tile_A',529,187,90,.9],
 ['문전 여관가','House_Lookout',545,166,4,.8],
 ['서쪽 장터','House_Shop_B',548,139,88,.95],
 ['서쪽 장터','House_Row_E',533,126,90,.92],
 ['서쪽 장터','House_Tile_D',533,110,88,.9],
 ['서쪽 장터','House_Tile_A',546,115,183,.9],
 ['서쪽 장터','House_Tile_A',530,144,90,.88],
 ['서쪽 공방','House_Smithy',532,81,90,1],
 ['서쪽 공방','House_Warehouse',529,63,89,.9],
 ['서쪽 공방','House_Tile_A',558,49,180,.85],
 ['서쪽 공방','House_Row_E',530,96,91,.9],
 ['동문 주거','House_Tile_D',602,201,182,.95],
 ['동문 주거','House_Tile_A',621,202,175,.98],
 ['동문 주거','House_Shop_B',626,182,268,.9],
 ['동문 주거','House_Row_E',626,160,270,.9],
 ['동문 주거','House_Tile_A',597,166,90,.9],
 ['동문 주거','House_Tile_A',596,176,90,.9],
 ['동문 주거','House_Lookout',635,194,12,.7],
 ['동문 주거','House_Warehouse',635,174,270,.9],
 ['동쪽 공방','House_Smithy',625,138,267,1],
 ['동쪽 공방','House_Warehouse',634,120,180,.92],
 ['동쪽 공방','House_Tile_D',621,109,270,.94],
 ['동쪽 공방','House_Shop_B',624,88,266,.92],
 ['동쪽 공방','House_Tile_A',633,72,270,.9],
 ['동쪽 공방','House_Row_E',619,73,180,.88],
 ['동쪽 공방','House_Warehouse',635,99,270,.9],
 ['본성 정원','House_Shrine',535,40,180,.9],
];
for(const [district,name,x,z,yaw,scale] of plots)add('생활 구획/'+district,name,x,z,yaw,scale,62.02,'ground');
add('정원/정자','Prop_Pavilion',613,51,24,1,62.02,'ground');
add('정원/담장과 석물','Deco_Stone_Cairn',605,42,20,1,62.02,'ground');
add('정원/담장과 석물','Deco_Jangseung_Pair',538,51,180,.85,62.02,'ground');
for(const [x,z,yaw] of [[601,40,0],[607,40,0],[613,40,0],[619,40,0],[625,40,0],[629,44,90],[629,50,90],[529,38,90],[529,44,90]]){
 add('정원/담장과 석물','Deco_Jar_Terrace',x,z,yaw,.45,62.02,'ground');
 rows[rows.length-1].path=poor+'Prop_StoneWall.prefab';rows[rows.length-1].scale=[1,.75,1];
}

for(const [x,z,yaw] of [[548,185,80],[554,197,180],[543,134,90],[536,74,80],[629,132,270],[630,116,180],[625,83,270]]){
 add('생활 소품/장독과 작업장','Deco_Jar_Terrace',x,z,yaw,.75,62.02,'ground');
 add('생활 소품/작업 물자','House_Warehouse',x+2.3,z+2.1,yaw,.8,62.02,'ground');
 rows[rows.length-1].path=poor+'Prop_Firewood.prefab';
}
for(const [x,z] of [[552,143],[550,144],[532,67],[534,67],[631,139],[633,140],[533,197],[534,196]]){
 add('생활 소품/화물','House_Warehouse',x,z,(x*13+z)%50,.8,62.02,'ground');rows[rows.length-1].path='Assets/03.Prefabs/Environment/WorldCities/CargoCrate.prefab';
}
for(const [x,z,yaw] of [[557,97,8],[561,87,100],[613,56,24],[618,53,110],[602,194,0]]){
 add('생활 소품/쉼터','Prop_Pavilion',x,z,yaw,.8,62.02,'ground');rows[rows.length-1].path='Assets/03.Prefabs/Environment/WorldCities/TownBench.prefab';
}
for(const [x,z] of [[574,205],[586,205],[574,162],[586,149],[570,111],[589,108],[575,75],[585,75],[552,174],[613,177],[551,121],[611,115],[607,59],[538,52]])add('가로 연출/석등','Prop_Stone_Lantern',x,z,0,.85,62.02,'ground');
for(const [x,z,yaw] of [[570,210,0],[590,210,0],[520,206,90],[640,206,270],[566,66,90],[594,66,270]])add('가로 연출/깃발','Deco_War_Banner',x,z,yaw,.8,62.02,'ground');

const trees=[
 [521,183,8,0],[521,146,8.5,1],[523,117,9,0],[522,92,7,1],[525,47,8,0],
 [537,49,7.5,2],[548,43,9,0],[554,66,7,1],[568,54,7.5,0],
 [599,52,9,2],[621,48,8,0],[627,57,10,1],[636,44,9,0],[636,65,8.5,0],
 [641,107,8,1],[638,152,9,0],[641,184,10,1],[638,207,8,0],
 [601,157,8,0],[600,144,7.5,1],[552,163,8,0],[546,99,8,1],
 [486,53,17,0],[493,61,13,1],[482,69,16,0],
 [493,106,15,1],[484,116,17,0],[493,130,14,0],
 [487,169,16,0],[495,181,12,1],[484,190,18,0],
 [522,239,13,0],[532,246,17,1],[544,242,14,0],
 [613,249,12,0],[626,253,16,0],[641,247,13,1],
 [670,185,16,0],[680,174,18,1],[671,158,13,0],
 [669,92,16,1],[682,80,17,0],[675,64,14,0],
];
for(const [x,z,height,type] of trees){
 const prefix=type===2?'C_FullTree_01_Red':type===1?'A_FullTree_01':'D_FullTree_01';
 add('정원과 식생/마을 수목',prefix,x,z,(x*17+z)%360,1,62.02,'height');
 const row=rows[rows.length-1];row.path=farm+'Trees/PF_Farm_GenTree_'+prefix+'.prefab';row.height=height;
}
for(const [cx,cz] of [[525,180],[524,143],[523,115],[536,45],[550,43],[601,46],[622,46],[633,60],[638,101],[637,153],[635,192],[599,173],[552,164]]){
 for(let i=0;i<3;i++){
  add('정원과 식생/낮은 관목','Prop_Pavilion',cx+(i-1)*1.7,cz+(i%2)*1.2,i*109,1,62.02,'height');
  const row=rows[rows.length-1];row.path=farm+'Trees/Bushes/PF_Farm_GenTree_Bush_04.prefab';row.height=1.1+(i%2)*.35;row.decorative=true;
 }
}
for(const row of rows){assert(fs.existsSync(row.path),row.path);assert(row.position.every(Number.isFinite));assert(row.scale.every(n=>n>0));}
fs.writeFileSync('Tools/OpenWorld/citadel-revision-plan.json',JSON.stringify(rows,null,2)+'\n');
console.log(JSON.stringify({instances:rows.length,houses:plots.length,groups:[...new Set(rows.map(r=>r.group))].length}));
