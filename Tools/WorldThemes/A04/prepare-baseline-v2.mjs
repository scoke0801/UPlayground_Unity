import fs from 'node:fs';
const plan=JSON.parse(fs.readFileSync('Tools/WorldThemes/A04/baseline-capture-plan.json','utf8'));
plan.version='A04-baseline-v2-west-A01Look';plan.output='output/WorldThemeProduction/RedTreeForest/captures/BaselineV2';plan.poses=plan.poses.map(p=>p.key==='boundary_west'?{...p,xz:[1130,545],target:[1156,533]}:p);plan.captureKeys=['boundary_west'];
fs.writeFileSync('Tools/WorldThemes/A04/baseline-v2-capture-plan.json',JSON.stringify(plan,null,2));
let code=fs.readFileSync('Tools/WorldThemes/A04/capture-baseline.csx','utf8').replaceAll('baseline-capture-plan.json','baseline-v2-capture-plan.json');
code=code.replace('foreach(var pose in poses){','foreach(var pose in poses.Where(p=>p["key"].ToString()=="boundary_west")){');
fs.writeFileSync('Tools/WorldThemes/A04/capture-baseline-v2.csx',code);
