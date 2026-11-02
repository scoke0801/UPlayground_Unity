param([Parameter(Mandatory=$true)][string]$CodePath)
$ErrorActionPreference = 'Stop'
$client = [System.Net.Sockets.TcpClient]::new()
try {
    $client.Connect('127.0.0.1', 6400)
    $stream = $client.GetStream()
    $stream.ReadTimeout = 55000
    $stream.WriteTimeout = 10000
    $greeting = [System.Text.StringBuilder]::new()
    do { $next = $stream.ReadByte(); if ($next -lt 0) { throw 'Unity MCP 연결 종료' }; [void]$greeting.Append([char]$next) } while ($next -ne 10)
    if (!$greeting.ToString().StartsWith('WELCOME UNITY-MCP 1 FRAMING=1')) { throw 'Unity MCP 프로토콜 불일치' }
    $code = Get-Content -LiteralPath $CodePath -Raw -Encoding UTF8
    $payload = @{ type = 'execute_code'; params = @{ action = 'execute'; code = $code; compiler = 'auto'; safety_checks = $true } } | ConvertTo-Json -Depth 5 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
    $header = [System.BitConverter]::GetBytes([uint64]$bytes.Length)
    [Array]::Reverse($header)
    $stream.Write($header, 0, 8)
    $stream.Write($bytes, 0, $bytes.Length)
    function Read-Exact([int]$Count) {
        $buffer = [byte[]]::new($Count)
        $offset = 0
        while ($offset -lt $Count) { $received = $stream.Read($buffer, $offset, $Count - $offset); if ($received -eq 0) { throw 'Unity MCP 응답 중 연결 종료' }; $offset += $received }
        return ,$buffer
    }
    $header = Read-Exact 8
    [Array]::Reverse($header)
    $length = [System.BitConverter]::ToUInt64($header, 0)
    if ($length -gt 16777216) { throw 'Unity MCP 응답 크기 초과' }
    $response = Read-Exact ([int]$length)
    [System.Text.Encoding]::UTF8.GetString($response)
} finally { $client.Dispose() }
