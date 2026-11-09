import fs from 'node:fs';
let s=fs.readFileSync('Tools/WorldThemes/ShadeCreekR3/lod-forced.csx','utf8');
s=s.replace('lod-final-forced','lod-angle-review');
s=s.replace('foreach(float distance in new[]{40f,430f,640f}){camera.transform.SetPositionAndRotation(center+new Vector3(0,0,-distance),Quaternion.identity);','foreach(float angle in new[]{0f,45f,90f,135f,180f,225f,270f,315f}){float distance=80;var position=center+Quaternion.Euler(0,angle,0)*new Vector3(0,0,-distance);camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(center-position));');
s=s.replace('names[i]+"-"+distance+".png"','names[i]+"-"+angle+".png"');
s=s.replace('names[i]+" distance="+distance','names[i]+" angle="+angle+" distance="+distance');
fs.writeFileSync('Tools/WorldThemes/ShadeCreekR3/lod-angle-review.csx',s);
