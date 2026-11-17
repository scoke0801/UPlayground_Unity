// 설치된 MCP 서버를 통해 열린 Editor 하나에만 요청한다.
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const child = spawn('C:/Users/scoke/.local/bin/uvx.exe', ['--offline', '--from', 'mcpforunityserver==10.3.0', 'mcp-for-unity', '--transport', 'stdio', '--default-instance', 'UPlayground'], { windowsHide: true, env: {...process.env, UNITY_MCP_DISABLE_TELEMETRY:'1'} });
let buffer = '', nextId = 1;
const pending = new Map();
child.stdout.on('data', data => { buffer += data; let end; while ((end = buffer.indexOf('\n')) >= 0) { const line = buffer.slice(0, end); buffer = buffer.slice(end + 1); try { const result = JSON.parse(line); const callback = pending.get(result.id); if(callback) { pending.delete(result.id); callback(result); } } catch {} } });
child.stderr.on('data', data => process.stderr.write(data));
function request(method, params) { return new Promise(resolve => { const id = nextId++; pending.set(id, resolve); child.stdin.write(JSON.stringify({jsonrpc:'2.0',id,method,params})+'\n'); }); }
(async () => {
  try {
    await request('initialize', {protocolVersion:'2024-11-05',capabilities:{},clientInfo:{name:'worldbuilder-local',version:'1'}});
    child.stdin.write(JSON.stringify({jsonrpc:'2.0',method:'notifications/initialized'})+'\n');
    const input = JSON.parse(fs.readFileSync(process.argv[2], 'utf8').replace(/^\uFEFF/, ''));
    const result = await request(input.method, input.params || {});
    const json = JSON.stringify(result, null, 2);
    if(process.argv[3]) fs.writeFileSync(process.argv[3], json);
    process.stdout.write((process.argv[3] ? '응답 저장: '+process.argv[3] : json)+'\n');
  } finally { child.kill(); }
})();
setTimeout(() => { child.kill(); process.exit(2); }, 180000).unref();
