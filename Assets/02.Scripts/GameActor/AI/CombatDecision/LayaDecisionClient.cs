#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UPlayGround.AI.CombatDecision
{
    /// <summary>리다이렉트·응답 크기·시간을 제한한 비동기 HTTP v1 전송.</summary>
    public static class LayaDecisionClient
    {
        public sealed class Reply { public long Status; public string Body, Error; public double Sent, Received; }

        public static async UniTask<Reply> Send(string endpoint, string json, float timeout, CancellationToken cancellation)
        {
            var reply = new Reply { Sent = Time.realtimeSinceStartupAsDouble };
            if (!LayaProtocol.TryEndpoint(endpoint, out var uri)) { reply.Error = "서버 주소 형식 오류"; return reply; }
            using var download = new LimitedDownload();
            using var request = new UnityWebRequest(new Uri(uri, json == null ? "health" : "v1/decide"), json == null ? "GET" : "POST");
            request.downloadHandler = download;
            request.disposeDownloadHandlerOnDispose = false;
            request.redirectLimit = 0;
            if (json != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            var token = Environment.GetEnvironmentVariable("LAYA_WORKER_TOKEN");
            if (!string.IsNullOrEmpty(token)) request.SetRequestHeader("Authorization", "Bearer " + token);
            try
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (cancellation.IsCancellationRequested) { request.Abort(); reply.Error = "요청 취소"; break; }
                    if (Time.realtimeSinceStartupAsDouble - reply.Sent >= timeout)
                    { request.Abort(); reply.Error = "응답 시간 초과"; break; }
                    await UniTask.Yield();
                }
                reply.Status = request.responseCode;
                reply.Body = LayaProtocol.Redact(download.Text);
                reply.Error ??= download.Overflow ? "응답 크기 제한 초과" : request.result == UnityWebRequest.Result.Success ? null : "HTTP/연결 실패 " + request.responseCode;
            }
            catch (Exception error) { reply.Error = "전송 실패: " + LayaProtocol.Redact(error.Message); }
            reply.Received = Time.realtimeSinceStartupAsDouble;
            return reply;
        }

        private sealed class LimitedDownload : DownloadHandlerScript
        {
            private readonly byte[] _body = new byte[LayaProtocol.MaxBodyBytes];
            private int _length;
            public bool Overflow { get; private set; }
            public string Text => Encoding.UTF8.GetString(_body, 0, _length);
            public LimitedDownload() : base(new byte[4096]) { }
            protected override bool ReceiveData(byte[] data, int length)
            {
                if (_length + length > _body.Length) { Overflow = true; return false; }
                Buffer.BlockCopy(data, 0, _body, _length, length);
                _length += length;
                return true;
            }
        }
    }
}
#endif
