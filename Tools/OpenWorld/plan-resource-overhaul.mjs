import fs from 'node:fs';
import path from 'node:path';
const input=JSON.parse(fs.readFileSync('output/OpenWorld/ResourceOverhaul/inventory.json','utf8'));
const base='Assets/03.Prefabs/Environment/';
const farm='Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/';
const kit=(n)=>base+n+'.prefab';
const types={};
const unknown=[];
for(const r of input.units){
 const n=(r.name+' '+(!r.prefab.includes('/OpenWorld/Dungeons/')?r.prefab:'')+' '+r.mesh).toLowerCase();
 const dungeon=r.path.startsWith('AuthoredDungeons/');
 let target='',mode='fit',action='replace',category='';
 const set=(c,p,m='fit')=>{category=c;target=p;mode=m;};
 if(!r.active){action='delete';category='비활성 원본 제거';}
 else if(/^(lakewater|coastalwater|reedwater|brineinlet)$/i.test(r.name)){action='water';category='수면 재질';}
 else if(/godray|walltrim|wallcover|arc.?cover|capital|column.*base|spiderweb|chain_|chain.dungeon|marketpennant|marketline|awningpost|cloth_market|cloth_workshop|respawn/.test(n)){action='delete';category='중복 장식·트림 정리';}
 else if(/bush|shrub/.test(n))set('관목',farm+'Foliage/Trees/Bushes/PF_Farm_GenTree_Bush_04.prefab','natural');
 else if(/stump/.test(n))set('그루터기',kit('PoorVillage/Terrain_Stump'),'natural');
 else if(/log|firewood|trunk|roots/.test(n))set('통나무·장작',kit('PoorVillage/Prop_Firewood'),'natural');
 else if(/tree|pine|bamboo|conifer|osmanthus/.test(n))set('수목',farm+'Foliage/Trees/PF_Farm_GenTree_'+(/hero|red|osmanthus/.test(n)?'C_FullTree_01_Red':'D_FullTree_01')+'.prefab','tree');
 else if(/grass|plant|flower|iris|leav|reeds|cattail|mushroom|ivy|basil|berry|sprout|vegetation/.test(n))set('지피·초목',farm+'Foliage/Grass/Grass_02/'+(/tall|reed|iris|silvergrass/.test(n)?'PF_Farm_TallGrass_02':'PF_Farm_SmallGrass_03')+'.prefab','natural');
 else if(/cliff|mountain/.test(n))set('암벽',kit('PoorVillage/Terrain_Boulder_A'),'fit');
 else if(/rock|boulder|stone.*cluster|flatstone|stonebed|gardenstone|debris|dirtpile|irrigationbank|orevein/.test(n))set('바위',kit('PoorVillage/Terrain_Boulder_C'),'fit');
 else if(/cavetunnel/.test(n))set('동굴',kit('Dungeon/Dng_Arch'));
 else if(/stairs|steps/.test(n)&&!/railing|wall_/.test(n))set('계단',kit(dungeon?'Dungeon/Dng_Stairs':'PoorVillage/Terrain_StoneSteps'));
 else if(/gateway|arch|torii/.test(n))set('출입문',kit(dungeon?'Dungeon/Dng_Arch':'WorldCities/StonegateEast/Castle_Gate'));
 else if(/column|pillar|buttress/.test(n))set('기둥',kit('Dungeon/Dng_Pillar'));
 else if(/floor|tile_|arena_01|entrancestonedeck|quayside|westbreakwater|saltpan|lakeSouthBank/i.test(n))set('바닥',kit('Dungeon/Dng_Floor_4x4'));
 else if(/wall|ruins|rampart|parapet|breakwaterend/.test(n))set('벽체',kit(dungeon?'Dungeon/Dng_Wall_Plain':'PoorVillage/Prop_StoneWall'));
 else if(/railing|fence|pierend/.test(n))set('울타리',kit('PoorVillage/Prop_Fence_Wattle'));
 else if(/house|bakery|windmill|home/.test(n))set('건물',kit(r.path.includes('Stonegate')?'WorldCities/StonegateEast/House_Tile_D':r.path.includes('Harbor')?'WorldCities/HarborHouse':'PoorVillage/PoorHouse_Chogajip'),'building');
 else if(/shrine|temple_main/.test(n))set('사당',kit('WorldCities/StonegateEast/House_Shrine'),'building');
 else if(/bridge|woodenpath|wood_|wood_/.test(n))set('목재 바닥',kit('WorldCities/HarborPier'));
 else if(/lantern|lamp|light/.test(n))set('조명 소품',kit('WorldCities/StonegateEast/Prop_Stone_Lantern'),'prop');
 else if(/brazier|fire_|chandelier|candle/.test(n))set('화로',kit('Dungeon/Dng_Brazier'),'prop');
 else if(/statue|fox|spirit/.test(n))set('조각상',kit('Dungeon/Dng_Statue_Guardian'),'prop');
 else if(/banner|flag|pennant/.test(n))set('깃발',kit('WorldCities/StonegateEast/Deco_War_Banner'),'prop');
 else if(/well|fountain/.test(n))set('우물',kit('PoorVillage/Prop_Well'),'prop');
 else if(/boat/.test(n))set('배',kit('WorldCities/CoastalBoat'),'building');
 else if(/market|stall|canopy|awning/.test(n))set('시장',kit('WorldCities/StonegateEast/Prop_Market_Stall_East'),'building');
 else if(/rack|netdry|dryingframe/.test(n))set('건조대',kit('PoorVillage/Prop_DryingRack'),'prop');
 else if(/carriage|cart|crate|box|sack|basket|barrel|barrow|chest/.test(n))set('화물',kit(/barrel/.test(n)?'WorldCities/CargoBarrel':/chest/.test(n)?'Dungeon/Dng_Chest_Reinforced':'WorldCities/CargoCrate'),'prop');
 else if(/cage/.test(n))set('철창',kit('Dungeon/Dng_Cage_Hanging'),'prop');
 else if(/altar/.test(n))set('제단',kit('Dungeon/Dng_Altar'),'prop');
 else if(/book|shelf/.test(n))set('서가',kit('Dungeon/Dng_Bookshelf'),'prop');
 else if(/chair|bench|seat|stool/.test(n))set('평상',kit('PoorVillage/Prop_Pyeongsang'),'prop');
 else if(/table/.test(n))set('탁자',kit('Dungeon/Dng_Table_Set'),'prop');
 else if(/jug|jar|pot|bowl|flour|bottle|bucket|bread|food|garlic|onion/.test(n))set('생활 용기',kit('PoorVillage/Prop_JarPlatform'),'prop');
 else if(/bone|skull|skeleton/.test(n))set('유골',kit('Dungeon/Dng_Bone_Pile'),'prop');
 else if(/door/.test(n))set('문',kit('Dungeon/Dng_Door'));
 else if(/sword|weapon/.test(n))set('무기 걸이',kit('Dungeon/Dng_Weapon_Rack'),'prop');
 else if(/rope|rug|beam|support|post/.test(n)){action='delete';category='독립 조각 정리';}
 else unknown.push(r);
 if(/Mia_Chair|Jun_ReturnChair|Hazel_Bottle|Mia_Rope|Yeonmok_WorkRope|Yeonmok_FinishedRope/.test(r.path)){
  action='replace';mode='prop';category='생활 상태 참조 소품';target=kit(/Chair/.test(r.path)?'PoorVillage/Prop_Pyeongsang':/Bottle/.test(r.path)?'PoorVillage/Prop_JarPlatform':'PoorVillage/Prop_Jige');
 }
 if(category==='수목'&&!/hero|red|osmanthus/.test(n))target=farm+'Foliage/Trees/PF_Farm_GenTree_'+['A','B','D'][Math.abs(r.instanceId)%3]+'_FullTree_01.prefab';
 if(category==='관목')target=farm+'Foliage/Trees/Bushes/PF_Farm_GenTree_Bush_0'+[4,5,7][Math.abs(r.instanceId)%3]+'.prefab';
 if(category==='지피·초목'&&/flower|iris/.test(n))target=farm+'Foliage/Berries/PF_Farm_Strawberry_Flowers.prefab';
 Object.assign(r,{target,mode,action,category});
 if(target&&!fs.existsSync(target))throw Error('Missing '+target);
 types[category]=(types[category]||0)+1;
}
fs.writeFileSync('output/OpenWorld/ResourceOverhaul/unmapped.json',JSON.stringify(unknown,null,2));
fs.writeFileSync('Tools/OpenWorld/resource-overhaul-plan.json',JSON.stringify(input));
console.log(JSON.stringify({counts:types,unmapped:unknown.map(r=>({name:r.name,path:r.path,prefab:r.prefab,mesh:r.mesh}))},null,2));

