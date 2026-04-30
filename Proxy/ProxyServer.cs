using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using MikuSB.Configuration;
using MikuSB.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MikuSB.Proxy;

public sealed class ProxyServer(
    IOptions<ProxyOptions> options,
    ProxyCertificateAuthority certificateAuthority,
    HttpClient httpClient,
    Logger logger) : BackgroundService
{
    private static readonly string[] TargetDomains =
    [
        "amazingseasuncdn.com",
        "amazingseasun.com",
        "seasungames.com",
        "snowbreak-game.com",
        "xoyo.games",
        "yo.games",
        "qcloud.com",
        "xgsdk.xoyo.games",
        "xqdata.xoyo.games",
        "tencentcs.com"
    ];

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Proxy-Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade"
    };

    private readonly ProxyOptions _options = options.Value;
    private TcpListener? _listener;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.Info("MikuSB proxy is disabled");
            return;
        }

        for (var i = 0; i < 20; i++)
        {
            try
            {
                var address = IPAddress.Parse(_options.BindAddress);
                _listener = new TcpListener(address, _options.Port);
                _listener.Start();
                logger.Info($"MikuSB proxy listening on {_listener.LocalEndpoint}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (SocketException ex)
            {
                logger.Info($"MikuSB proxy listening error {ex}");
                _options.Port += Random.Shared.Next(1, 500); // try another port
                await Task.Delay(100, stoppingToken);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _listener?.Stop();
        return base.StopAsync(cancellationToken);
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            // TODO: pass client.Client.LocalEndPoint to SDK server
            // let SDK server return IP/address for client to connect with
            // api: /api/serverlist, /query_version, /query
            var cliAddr = client.Client.RemoteEndPoint;
            logger.Info($"Proxy New client: {cliAddr}");
            try
            {
                await HandleClientCoreAsync(client, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (IOException)
            {
            }
            catch (SocketException)
            {
            }
            catch (AuthenticationException ex)
            {
                logger.Warn($"Proxy TLS authentication failed: {ex}");
            }
            catch (Exception ex)
            {
                logger.Warn($"Proxy client failed {ex}");
            }
            logger.Info($"Proxy client close: {cliAddr}");
        }
    }

    private async Task HandleClientCoreAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using var stream = client.GetStream();
        var request = await ProxyHttpRequest.ReadAsync(stream, cancellationToken);
        if (request is null)
            return;

        if (request.Method.Equals("CONNECT", StringComparison.OrdinalIgnoreCase))
        {
            var (host, port) = SplitHostPort(request.Target, 443);
            if (ShouldRedirect(host))
            {
                await WriteAsciiAsync(stream, "HTTP/1.1 200 Connection Established\r\nProxy-Agent: MikuSB.Proxy\r\n\r\n", cancellationToken);
                using var tlsStream = new SslStream(stream, false);
                await tlsStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificateAuthority.GetServerCertificate(host),
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                }, cancellationToken);

                await HandleRedirectedHttpLoopAsync(tlsStream, host, cancellationToken);
                return;
            }

            await TunnelAsync(stream, host, port, cancellationToken);
            return;
        }

        await HandlePlainHttpLoopAsync(stream, request, cancellationToken);
    }

    private async Task HandlePlainHttpLoopAsync(Stream clientStream, ProxyHttpRequest request, CancellationToken cancellationToken)
    {
        while (true)
        {
            var host = request.Host;
            if (string.IsNullOrWhiteSpace(host))
            {
                await WriteSimpleResponseAsync(clientStream, HttpStatusCode.BadRequest, "Missing Host header", cancellationToken);
                return;
            }

            if (ShouldRedirect(SplitHostPort(host, 80).Host))
                await ForwardToServerAsync(clientStream, request, cancellationToken);
            else
                await ForwardToOriginAsync(clientStream, request, cancellationToken);

            if (request.ShouldClose)
                return;

            var nextRequest = await ProxyHttpRequest.ReadAsync(clientStream, cancellationToken);
            if (nextRequest is null)
                return;

            request = nextRequest;
        }
    }

    private async Task HandleRedirectedHttpLoopAsync(Stream tlsStream, string originalHost, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var request = await ProxyHttpRequest.ReadAsync(tlsStream, cancellationToken);
            if (request is null)
                return;

            request.HostOverride = originalHost;
            await ForwardToServerAsync(tlsStream, request, cancellationToken);

            if (request.ShouldClose)
                return;
        }
    }

    private async Task ForwardToServerAsync(Stream clientStream, ProxyHttpRequest request, CancellationToken cancellationToken)
    {
        var pathAndQuery = request.GetPathAndQuery();
        var uri = new Uri($"http://{_options.ServerHttpAddress}:{_options.ServerHttpPort}{pathAndQuery}");
        logger.Info($"Redirect: {request.Method} {request.HostOverride ?? request.Host}{pathAndQuery} -> {uri}");
        await SendHttpRequestAsync(clientStream, request, uri, true, cancellationToken);
    }

    private async Task ForwardToOriginAsync(Stream clientStream, ProxyHttpRequest request, CancellationToken cancellationToken)
    {
        var uri = request.GetAbsoluteUri();
        if (uri is null)
        {
            await WriteSimpleResponseAsync(clientStream, HttpStatusCode.BadRequest, "Only absolute-form proxy requests are supported for non-target HTTP", cancellationToken);
            return;
        }

        if (IsSelfReference(uri))
        {
            logger.Info($"Self-reference blocked: {request.Method} {uri}");
            await WriteSimpleResponseAsync(clientStream, HttpStatusCode.LoopDetected, "Proxy self-reference detected", cancellationToken);
            return;
        }

        await SendHttpRequestAsync(clientStream, request, uri, false, cancellationToken);
    }

    private bool IsSelfReference(Uri uri)
    {
        if (uri.Port != _options.Port)
            return false;

        return uri.Host is "127.0.0.1" or "localhost" or "::1"
            || uri.Host.Equals(_options.BindAddress, StringComparison.OrdinalIgnoreCase);
    }

    private async Task SendHttpRequestAsync(Stream clientStream, ProxyHttpRequest request, Uri uri, bool addCors, CancellationToken cancellationToken)
    {
        using var outgoing = new HttpRequestMessage(new HttpMethod(request.Method), uri);
        if (request.Body.Length > 0)
            outgoing.Content = new ByteArrayContent(request.Body);

        foreach (var (name, value) in request.Headers)
        {
            if (HopByHopHeaders.Contains(name) || name.Equals("Host", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!outgoing.Headers.TryAddWithoutValidation(name, value))
            {
                outgoing.Content ??= new ByteArrayContent(request.Body);
                outgoing.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        using var response = await httpClient.SendAsync(outgoing, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.Append("HTTP/1.1 ")
            .Append((int)response.StatusCode)
            .Append(' ')
            .Append(response.ReasonPhrase ?? response.StatusCode.ToString())
            .Append("\r\n");

        foreach (var header in response.Headers)
            AppendHeader(builder, header.Key, header.Value);

        foreach (var header in response.Content.Headers)
        {
            if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                AppendHeader(builder, header.Key, header.Value);
        }

        if (addCors)
            builder.Append("Access-Control-Allow-Origin: *\r\n");

        builder.Append("Content-Length: ").Append(body.Length).Append("\r\n");
        builder.Append("Connection: keep-alive\r\n\r\n");

        await WriteAsciiAsync(clientStream, builder.ToString(), cancellationToken);
        if (body.Length > 0)
            await clientStream.WriteAsync(body, cancellationToken);
    }

    private async Task TunnelAsync(Stream clientStream, string host, int port, CancellationToken cancellationToken)
    {
        using var upstream = new TcpClient();
        await upstream.ConnectAsync(host, port, cancellationToken);
        await WriteAsciiAsync(clientStream, "HTTP/1.1 200 Connection Established\r\nProxy-Agent: MikuSB.Proxy\r\n\r\n", cancellationToken);

        await using var upstreamStream = upstream.GetStream();
        var clientToServer = clientStream.CopyToAsync(upstreamStream, cancellationToken);
        var serverToClient = upstreamStream.CopyToAsync(clientStream, cancellationToken);
        await Task.WhenAny(clientToServer, serverToClient);
    }

    private bool ShouldRedirect(string host)
    {
        host = host.Trim().TrimEnd('.').ToLowerInvariant();
        foreach (var target in TargetDomains)
        {
            var normalized = target.Trim().TrimEnd('.').ToLowerInvariant();
            if (host == normalized || host.EndsWith($".{normalized}", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void AppendHeader(StringBuilder builder, string name, IEnumerable<string> values)
    {
        if (HopByHopHeaders.Contains(name))
            return;

        foreach (var value in values)
            builder.Append(name).Append(": ").Append(value).Append("\r\n");
    }

    private static async Task WriteSimpleResponseAsync(Stream stream, HttpStatusCode statusCode, string message, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(message);
        await WriteAsciiAsync(
            stream,
            $"HTTP/1.1 {(int)statusCode} {statusCode}\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n",
            cancellationToken);
        await stream.WriteAsync(body, cancellationToken);
    }

    private static Task WriteAsciiAsync(Stream stream, string value, CancellationToken cancellationToken) =>
        stream.WriteAsync(Encoding.ASCII.GetBytes(value), cancellationToken).AsTask();

    private static (string Host, int Port) SplitHostPort(string hostPort, int defaultPort)
    {
        if (hostPort.StartsWith('['))
        {
            var end = hostPort.IndexOf(']');
            if (end > 0 && hostPort.Length > end + 2 && hostPort[end + 1] == ':' && int.TryParse(hostPort[(end + 2)..], out var ipv6Port))
                return (hostPort[1..end], ipv6Port);

            return (hostPort.Trim('[', ']'), defaultPort);
        }

        var colon = hostPort.LastIndexOf(':');
        if (colon > 0 && int.TryParse(hostPort[(colon + 1)..], out var port))
            return (hostPort[..colon], port);

        return (hostPort, defaultPort);
    }

    private sealed class ProxyHttpRequest
    {
        public required string Method { get; init; }
        public required string Target { get; init; }
        public required string Version { get; init; }
        public required List<KeyValuePair<string, string>> Headers { get; init; }
        public required byte[] Body { get; init; }
        public string? HostOverride { get; set; }

        public string? Host => HostOverride ?? Headers.FirstOrDefault(x => x.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)).Value;

        public bool ShouldClose =>
            Headers.Any(x => x.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)
                && x.Value.Contains("close", StringComparison.OrdinalIgnoreCase));

        public Uri? GetAbsoluteUri() => Uri.TryCreate(Target, UriKind.Absolute, out var uri) ? uri : null;

        public string GetPathAndQuery()
        {
            // "/query?version=a.b.c&platform=PC"
            // => Uri.TryCreate() return true && uri.Scheme == "file"
            // => will return "/query%3Fversion=a.b.c&platform=PC" cause 404
            if (Uri.TryCreate(Target, UriKind.Absolute, out var uri) && uri.IsAbsoluteUri && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return uri.PathAndQuery;

            if (string.IsNullOrEmpty(Target))
                return "/";

            return Target[0] == '/' ? Target : $"/{Target}";
        }

        public static async Task<ProxyHttpRequest?> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var rented = ArrayPool<byte>.Shared.Rent(64 * 1024);
            try
            {
                var length = 0;
                while (true)
                {
                    var read = await stream.ReadAsync(rented.AsMemory(length, 1), cancellationToken);
                    if (read == 0)
                        return null;

                    length += read;
                    if (length >= 4
                        && rented[length - 4] == '\r'
                        && rented[length - 3] == '\n'
                        && rented[length - 2] == '\r'
                        && rented[length - 1] == '\n')
                        break;

                    if (length == rented.Length)
                        throw new InvalidDataException("HTTP proxy request header is too large");
                }

                var headerText = Encoding.ASCII.GetString(rented, 0, length);
                var lines = headerText.Split("\r\n", StringSplitOptions.None);
                var requestLine = lines[0].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (requestLine.Length != 3)
                    throw new InvalidDataException("Invalid HTTP proxy request line");

                var headers = new List<KeyValuePair<string, string>>();
                var contentLength = 0;
                for (var i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrEmpty(line))
                        break;

                    var colon = line.IndexOf(':');
                    if (colon <= 0)
                        continue;

                    var name = line[..colon].Trim();
                    var value = line[(colon + 1)..].Trim();
                    headers.Add(new KeyValuePair<string, string>(name, value));
                    if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var parsedLength))
                        contentLength = parsedLength;
                }

                var body = new byte[contentLength];
                var offset = 0;
                while (offset < body.Length)
                {
                    var read = await stream.ReadAsync(body.AsMemory(offset), cancellationToken);
                    if (read == 0)
                        throw new EndOfStreamException("HTTP proxy request body ended early");

                    offset += read;
                }

                return new ProxyHttpRequest
                {
                    Method = requestLine[0],
                    Target = requestLine[1],
                    Version = requestLine[2],
                    Headers = headers,
                    Body = body
                };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
