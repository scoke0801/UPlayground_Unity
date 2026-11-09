import fs from 'node:fs';
let code=fs.readFileSync('Tools/WorldThemes/A04/capture-comparison.csx','utf8');
code=code.replaceAll('CandidatesV1','CandidatesV2').replaceAll('A04-existing-candidates-v1','A04-existing-candidates-v2-grounded');
code=code.replace('render(key+"_wide",new Vector3(1130,82.6f,566),new Vector3(1130,79.8f,580));render(key+"_close",new Vector3(1130,81.3f,574),new Vector3(1130,79.7f,580));','');
code=code.replace('foreach(var item in new[]{flat,leaves})','leaves.SetActive(true);var leafRenderer=leaves.GetComponentInChildren<Renderer>(true);float lift=79.61f-leafRenderer.bounds.min.y;leaves.transform.position+=Vector3.up*lift;Physics.SyncTransforms();leaves.SetActive(false);foreach(var item in new[]{leaves})');
code=code.replace('string key=item==flat?"ExistingFlatTree":"GroundLeavesScale1";','string key="GroundLeavesScale1_Grounded";');
code=code.replace('name=r.name,min=','name=r.name,enabled=r.enabled,active=r.gameObject.activeInHierarchy,layer=r.gameObject.layer,shaderInfo=r.sharedMaterials.Select(mat=>mat==null?null:new{path=AssetDatabase.GetAssetPath(mat),shader=mat.shader==null?null:mat.shader.name,supported=mat.shader!=null&&mat.shader.isSupported,queue=mat.renderQueue,passCount=mat.passCount}).ToArray(),min=');
code=code.replace('status="검수용 기존자원 비교. 통합/교체 승인 전"','groundLift=lift,status="동일 실척1, minY를 시험바닥+1cm에 맞춘 V2. 통합/교체 승인 전"');
fs.writeFileSync('Tools/WorldThemes/A04/capture-comparison-v2.csx',code);
