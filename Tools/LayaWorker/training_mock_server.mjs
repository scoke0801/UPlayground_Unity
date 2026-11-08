// 통신/Unity 통합 검사 전용. 실제 Laya 추론이 아니며 production endpoint로 사용하지 않는다.
import http from 'node:http';
const port = Number(process.argv[2] || 18765);
const metadata = {backend:'transport-mock', model:'aac6fef/laya-multilingual-mlx',
  revision:'ba40c87fcb357f1643d04d71323af9cdc3b9e591',
  config_sha256:'e98b441f42d45aa39c026cb3e7f2a0e0212406a76375370fbe70bbdefe6831d3',
  worker_version:'MOCK-NOT-LAYA'};
http.createServer((req,res) => {
  const reply=(status,body)=> {res.writeHead(status, {'Content-Type':'application/json'}); res.end(JSON.stringify(body));};
  if(req.url==='/health') return reply(200,{ready:true,protocol_version:1,metadata});
  if(req.url!=='/v1/decide') return reply(404,{error:'not_found'});
  let body=''; req.on('data',chunk=> {body+=chunk;if(body.length>65536)req.destroy();});
  req.on('end',()=> {
    try {
      const input=JSON.parse(body), fault=input.observation?.mock_fault;
      if(fault==='503') return reply(503,{error:'busy'});
      if(fault==='malformed') {res.writeHead(200);return res.end('{broken');}
      if(fault==='oversized') {res.writeHead(200);return res.end('x'.repeat(70000));}
      const ids=input.candidates.map(c=>c.id);
      const selected=ids.includes('Attack')?'Attack':ids.includes('Chase')?'Chase':ids[0];
      const response={protocol_version:1,request_id:input.request_id,session_id:input.session_id,state_seq:input.state_seq,
        selected_action_id:selected,probabilities:Object.fromEntries(ids.map(id=>[id,id===selected?1:0])),
        selected_probability:1,decision_source:'laya',inference_ms:0,metadata,
        usage:{truncated:false,state_tokens_dropped:0},mock:true};
      setTimeout(()=>reply(200,response),fault==='timeout'?1500:80);
    }catch {reply(400,{error:'invalid_json'});}
  });
}).listen(port,'127.0.0.1',()=>console.log(`MOCK ONLY http://127.0.0.1:${port}`));
