using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

namespace VpnKillSwitch;

public enum SignalState
{
    Healthy,
    Down,
    Unknown
}

public readonly record struct SignalReading(string Name, SignalState State, string Detail)
{
    public static SignalReading Healthy(string name, string detail) => new(name, SignalState.Healthy, detail);

    public static SignalReading Down(string name, string detail) => new(name, SignalState.Down, detail);

    public static SignalReading Unknown(string name, string detail) => new(name, SignalState.Unknown, detail);
}

public enum GuardVerdict
{
    Safe,
    Leaking,
    Unknown,
    NotConfigured
}

internal static class TunnelSignals
{
    private const int IpUnicastIf = 31;

    public static readonly int[] CommonProxyPorts =
    [
        10808, 10809, 1080, 1081, 7890, 7891, 7897, 2080, 2081, 8889, 1087, 8080, 20170, 20171, 20808, 12333, 10810, 7893
    ];

    public static readonly string[] CommonProxyProcesses =
    [
        "v2ray", "xray", "sing-box", "singbox", "clash", "clash-meta", "mihomo", "clash-verge", "clash-verge-service",
        "nekoray", "nekobox", "hiddify", "hiddify-cli", "hiddify-core", "v2rayN", "shadowsocks", "ss-local",
        "trojan", "naive", "gost", "brook", "hysteria", "tuic", "wireguard", "amneziawg", "openvpn", "openconnect",
        "warp", "warp-svc", "tun2socks", "proxifier", "netch", "sstap"
    ];

    public static readonly string[] CommonAdapterPatterns =
    [
        "wintun", "wireguard", "amnezia", "tap-windows", "tap0901", "openvpn", "openconnect", "sing-box", "singbox",
        "clash", "mihomo", "hiddify", "nekoray", "nekobox", "warp", "cloudflare", "tun2socks", "proton", "nordlynx",
        "tailscale", "zerotier", "softether", "anyconnect", "globalprotect", "forticlient", "outline", "shadowsocks"
    ];

    private static readonly string[] TunnelNoise =
    [
        "teredo", "isatap", "6to4", "pseudo-interface", "loopback", "bluetooth", "hyper-v", "vethernet", "vmware", "virtualbox"
    ];

    public static readonly string[] DefaultEgressServices =
    [
        "https://ipinfo.io/json",
        "http://ip-api.com/json/?fields=status,countryCode,as,query",
        "https://api.ipify.org"
    ];

    public static async Task<SignalReading> ProxyPortAsync(string host, int port, int timeoutMs, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Math.Max(100, timeoutMs));

        try
        {
            await client.ConnectAsync(host, port, timeout.Token).ConfigureAwait(false);
            return SignalReading.Healthy("proxy port", $"{host}:{port} accepts connections");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SignalReading.Down("proxy port", $"{host}:{port} did not answer within {timeoutMs} ms");
        }
        catch (SocketException ex)
        {
            return SignalReading.Down("proxy port", $"{host}:{port} refused the connection ({ex.SocketErrorCode})");
        }
        catch (Exception ex)
        {
            return SignalReading.Down("proxy port", $"{host}:{port} failed: {ex.Message}");
        }
    }

    public static SignalReading ProxyProcess(string[] names)
    {
        if (names.Length == 0)
            return SignalReading.Unknown("proxy process", "no proxy process is being watched");

        var running = new List<string>();
        foreach (string name in names)
        {
            string trimmed = name.Trim();
            if (trimmed.Length == 0)
                continue;

            try
            {
                string bare = Path.GetFileNameWithoutExtension(trimmed);
                if (Process.GetProcessesByName(bare).Length > 0)
                    running.Add(bare);
            }
            catch (Exception)
            {
            }
        }

        return running.Count > 0
            ? SignalReading.Healthy("proxy process", $"running: {string.Join(", ", running)}")
            : SignalReading.Down("proxy process", $"none of {names.Length} watched process(es) are running");
    }

    public static SignalReading TunnelAdapters(string[] patterns)
    {
        if (patterns.Length == 0)
            return SignalReading.Unknown("tunnel adapter", "no adapter pattern is configured");

        var up = new List<string>();
        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (!IsTunnelLike(nic, patterns))
                continue;

            if (nic.OperationalStatus == OperationalStatus.Up)
                up.Add(nic.Name);
        }

        return up.Count > 0
            ? SignalReading.Healthy("tunnel adapter", $"up: {string.Join(", ", up)}")
            : SignalReading.Down("tunnel adapter", "no matching tunnel adapter is up");
    }

    public static async Task<SignalReading> EgressIdentityAsync(
        string proxyHost,
        int proxyPort,
        string proxyScheme,
        string[] services,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        if (services.Length == 0)
            return SignalReading.Unknown("egress identity", "no egress service is configured");

        DirectPath? direct = FindDirectPath();
        if (direct is null)
            return SignalReading.Unknown("egress identity", "no physical adapter with a gateway was found");

        EgressInfo? tunnel = await QueryEgressAsync(null, 0, null, services, timeoutMs, direct, cancellationToken, forceDirect: false).ConfigureAwait(false);
        EgressInfo? directEgress = await QueryEgressAsync(null, 0, null, services, timeoutMs, direct, cancellationToken, forceDirect: true).ConfigureAwait(false);

        if (proxyPort > 0 && proxyHost is not null)
        {
            EgressInfo? viaProxy = await QueryEgressAsync(proxyHost, proxyPort, proxyScheme, services, timeoutMs, direct, cancellationToken, forceDirect: false).ConfigureAwait(false);
            if (viaProxy is not null)
                tunnel = viaProxy;
        }

        if (tunnel is null)
            return SignalReading.Down("egress identity", "the egress service could not be reached through the tunnel");

        if (directEgress is null)
            return SignalReading.Healthy("egress identity", $"tunnel exits at {tunnel} and no direct IPv4 path is available");

        if (EgressInfo.SameAddress(tunnel, directEgress))
            return SignalReading.Down("egress identity", $"both paths exit at {tunnel.Ip}, so nothing is being tunnelled");

        if (EgressInfo.SameNetwork(tunnel, directEgress))
            return SignalReading.Down("egress identity", $"tunnel and direct traffic both leave through {tunnel.NetworkLabel}, so nothing is being tunnelled");

        return SignalReading.Healthy("egress identity", $"tunnel {tunnel} direct {directEgress} via {direct.Name}");
    }

    public static async Task<SignalReading> HealthEndpointAsync(string url, int timeoutMs, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            return SignalReading.Unknown("health endpoint", "no health endpoint is configured");

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https"))
            return SignalReading.Unknown("health endpoint", "the configured health endpoint is not a valid http(s) URL");

        using var handler = new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromMilliseconds(Math.Max(200, timeoutMs)) };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(Math.Max(200, timeoutMs)) };

        try
        {
            using HttpResponseMessage response = await client
                .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            return (int)response.StatusCode < 500
                ? SignalReading.Healthy("health endpoint", $"HTTP {(int)response.StatusCode}")
                : SignalReading.Down("health endpoint", $"HTTP {(int)response.StatusCode}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SignalReading.Down("health endpoint", $"no answer within {timeoutMs} ms");
        }
        catch (Exception ex)
        {
            return SignalReading.Down("health endpoint", ex.Message);
        }
    }

    public static async Task<int?> DetectProxyPortAsync(string host, int timeoutMs, CancellationToken cancellationToken)
    {
        foreach (int port in CommonProxyPorts)
        {
            SignalReading reading = await ProxyPortAsync(host, port, timeoutMs, cancellationToken).ConfigureAwait(false);
            if (reading.State == SignalState.Healthy)
                return port;
        }

        return null;
    }

    public static string[] DetectProxyProcesses()
    {
        var found = new List<string>();
        foreach (string name in CommonProxyProcesses)
        {
            try
            {
                if (Process.GetProcessesByName(name).Length > 0)
                    found.Add(name);
            }
            catch (Exception)
            {
            }
        }

        return [.. found];
    }

    public static string[] DetectTunnelAdapters()
    {
        var found = new List<string>();
        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            if (IsTunnelLike(nic, CommonAdapterPatterns))
                found.Add(nic.Name);
        }

        return [.. found];
    }

    public static string DescribePhysicalPath()
    {
        DirectPath? direct = FindDirectPath();
        return direct is null
            ? "no physical adapter with a gateway"
            : $"{direct.Name} ({direct.Address})";
    }

    private static bool IsTunnelLike(NetworkInterface nic, string[] patterns)
    {
        string haystack = $"{nic.Name} {nic.Description}";
        if (TunnelNoise.Any(noise => haystack.Contains(noise, StringComparison.OrdinalIgnoreCase)))
            return false;

        return patterns.Any(pattern =>
            pattern.Length > 0 && haystack.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static DirectPath? FindDirectPath()
    {
        DirectPath? best = null;

        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            if (nic.NetworkInterfaceType is not (NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211))
                continue;

            if (IsTunnelLike(nic, CommonAdapterPatterns))
                continue;

            IPInterfaceProperties properties = nic.GetIPProperties();
            if (properties.GatewayAddresses.Count == 0)
                continue;

            UnicastIPAddressInformation? unicast = properties.UnicastAddresses
                .FirstOrDefault(address =>
                    address.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address.Address) &&
                    !address.Address.ToString().StartsWith("169.254.", StringComparison.Ordinal));

            if (unicast is null)
                continue;

            int index = properties.GetIPv4Properties()?.Index ?? 0;
            var candidate = new DirectPath(unicast.Address, index, nic.Name);

            if (best is null || candidate.Index < best.Index)
                best = candidate;
        }

        return best;
    }

    private static async Task<EgressInfo?> QueryEgressAsync(
        string? proxyHost,
        int proxyPort,
        string? proxyScheme,
        string[] services,
        int timeoutMs,
        DirectPath direct,
        CancellationToken cancellationToken,
        bool forceDirect = false)
    {
        using HttpClient client = CreateClient(proxyHost, proxyPort, proxyScheme, timeoutMs, forceDirect ? direct : null);

        foreach (string service in services)
        {
            if (!Uri.TryCreate(service, UriKind.Absolute, out Uri? uri))
                continue;

            try
            {
                string body = await client.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
                EgressInfo? parsed = EgressInfo.Parse(body);
                if (parsed is not null)
                    return parsed;
            }
            catch (Exception)
            {
            }
        }

        return null;
    }

    private static HttpClient CreateClient(string? proxyHost, int proxyPort, string? proxyScheme, int timeoutMs, DirectPath? bindDirect)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromMilliseconds(Math.Max(300, timeoutMs)),
            PooledConnectionLifetime = TimeSpan.FromSeconds(20),
            UseProxy = proxyPort > 0,
            ConnectCallback = async (context, token) =>
            {
                IPAddress[] resolved = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, token).ConfigureAwait(false);
                IPAddress? target = resolved.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);

                if (target is null)
                    throw new SocketException((int)SocketError.HostNotFound);

                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    ForceInterface(socket, bindDirect);
                    await socket.ConnectAsync(new IPEndPoint(target, context.DnsEndPoint.Port), token).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        if (proxyPort > 0 && proxyHost is not null)
        {
            string scheme = string.IsNullOrWhiteSpace(proxyScheme) ? "socks5" : proxyScheme.Trim();
            handler.Proxy = new WebProxy($"{scheme}://{proxyHost}:{proxyPort}");
        }

        return new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(Math.Max(500, timeoutMs)) };
    }

    private static void ForceInterface(Socket socket, DirectPath? direct)
    {
        if (direct is null)
            return;

        try
        {
            socket.Bind(new IPEndPoint(direct.Address, 0));
        }
        catch (Exception)
        {
        }

        try
        {
            socket.SetSocketOption(
                SocketOptionLevel.IP,
                (SocketOptionName)IpUnicastIf,
                IPAddress.HostToNetworkOrder(direct.Index));
        }
        catch (Exception)
        {
        }
    }

    private sealed record DirectPath(IPAddress Address, int Index, string Name);

    internal sealed record EgressInfo(string Ip, string? CountryCode, string? Network)
    {
        public string NetworkLabel => Network ?? CountryCode ?? "an unidentified network";

        public static bool SameAddress(EgressInfo a, EgressInfo b) =>
            string.Equals(a.Ip, b.Ip, StringComparison.OrdinalIgnoreCase);

        public static bool SameNetwork(EgressInfo a, EgressInfo b)
        {
            if (string.IsNullOrWhiteSpace(a.Network) || string.IsNullOrWhiteSpace(b.Network))
                return false;

            return string.Equals(a.Network, b.Network, StringComparison.OrdinalIgnoreCase);
        }

        public static EgressInfo? Parse(string body)
        {
            string trimmed = body.Trim();
            if (trimmed.Length == 0)
                return null;

            if (trimmed[0] != '{')
            {
                return IPAddress.TryParse(trimmed, out _) ? new EgressInfo(trimmed, null, null) : null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(trimmed);
                JsonElement root = document.RootElement;

                string? ip = Read(root, "ip") ?? Read(root, "query") ?? Read(root, "IPv4");
                if (string.IsNullOrWhiteSpace(ip))
                    return null;

                string? country = Read(root, "country_code") ?? Read(root, "countryCode");
                string? network = Read(root, "org") ?? Read(root, "as") ?? Read(root, "asn");

                return new EgressInfo(ip.Trim(), Normalize(country), NormalizeNetwork(network));
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? Read(JsonElement root, string name) =>
            root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

        private static string? NormalizeNetwork(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            int space = value.IndexOf(' ');
            string head = space > 0 ? value[..space] : value;
            return head.Trim().ToUpperInvariant();
        }
    }
}
