import fs from 'node:fs';
const dir='Tools/WorldThemes/ShadeCreekR3/Integration';fs.mkdirSync(dir,{recursive:true});
const main='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
for(const name of ['refresh-contract-baseline','validate-progress','check-walking-support','check-enemies','check-enemy-paths','check-references','check-editor-final','capture']){
let s=fs.readFileSync(`Tools/WorldThemes/ShadeCreekR3/${name}.csx`,'utf8');
s=s.replaceAll('output/ShadeCreekR3/','output/ShadeCreekR3/Integration/');
if(name!=='refresh-contract-baseline')s=s.replaceAll('Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity',main);
if(name==='check-editor-final'){s=s.replaceAll('Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/','Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/');s=s.replace('p.Contains("/ShadeCreekR1/")||p.Contains("/ShadeCreekR2/")','p.Contains("/ShadeCreekR1/")||p.Contains("/ShadeCreekR2/")||p.Contains("/ShadeCreekR3/")');}
if(name==='capture')s=s.replace('Tools/WorldThemes/ShadeCreekR3/stage.txt','Tools/WorldThemes/ShadeCreekR3/Integration/stage.txt');
fs.writeFileSync(`${dir}/${name}.csx`,s);
}
fs.writeFileSync(`${dir}/stage.txt`,'main');
