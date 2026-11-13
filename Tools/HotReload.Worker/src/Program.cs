using System;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using UPlayGround.HotReload.Protocol;

namespace UPlayGround.HotReload.Worker
{
    /// <summary>
    /// stdin NDJSON 요청을 순서대로 처리하고 stdout에 응답 한 줄을 쓴다. 로그는 stderr.
    /// 프로토콜에는 임의 셸 명령이나 C# eval이 없으며, 게임 어셈블리의 사용자 코드를 실행하지 않는다.
    /// 요청 취소는 Editor가 자신이 시작한 이 프로세스를 종료하고 늦게 도착한 응답을 버리는 방식으로 처리한다.
    /// </summary>
    public static class Program
    {
        public const string Version = "0.1.0";

        public static int Main()
        {
            Console.InputEncoding = new UTF8Encoding(false);
            var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
            var stdin = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
            var analyzer = new Analyzer();
            string token = null;

            string line;
            while ((line = stdin.ReadLine()) != null)
            {
                if (line.Length == 0) continue;
                WorkerResponse resp;
                WorkerRequest req = null;
                try
                {
                    req = JsonConvert.DeserializeObject<WorkerRequest>(line);
                    if (req == null || req.ProtocolVersion != WorkerProtocol.Version)
                        throw new UnsupportedException(ResultCodes.ProtocolError, "프로토콜 버전 불일치");
                    if (req.Type == WorkerProtocol.Hello) token = req.SessionToken;
                    else if (token == null || req.SessionToken != token)
                        throw new UnsupportedException(ResultCodes.ProtocolError, "세션 토큰 불일치");

                    resp = req.Type switch
                    {
                        WorkerProtocol.Hello => new WorkerResponse { ResultCode = ResultCodes.Ok },
                        WorkerProtocol.PrepareBaseline => analyzer.PrepareBaseline(req, CancellationToken.None),
                        WorkerProtocol.Analyze => analyzer.Analyze(req, CancellationToken.None),
                        WorkerProtocol.Shutdown => new WorkerResponse { ResultCode = ResultCodes.Ok },
                        _ => throw new UnsupportedException(ResultCodes.ProtocolError, "알 수 없는 요청: " + req.Type),
                    };
                }
                catch (UnsupportedException e)
                {
                    resp = new WorkerResponse { ResultCode = e.Code };
                    resp.Diagnostics.Add(new Diagnostic { Code = e.Code, Message = e.Message });
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    resp = new WorkerResponse { ResultCode = ResultCodes.WorkerUnavailable };
                    resp.Diagnostics.Add(new Diagnostic { Code = ResultCodes.WorkerUnavailable, Message = e.GetType().Name + ": " + e.Message });
                }
                resp.RequestId = req?.RequestId;
                resp.WorkerVersion = Version;
                stdout.WriteLine(JsonConvert.SerializeObject(resp, Formatting.None));
                if (req?.Type == WorkerProtocol.Shutdown) break;
            }
            return 0;
        }
    }
}
