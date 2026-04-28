using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MikuSB.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MikuSB.Proxy;

public sealed class ProxyCertificateAuthority
{
    private const string Password = "MikuSB.Proxy.LocalCA";
    private readonly ConcurrentDictionary<string, X509Certificate2> _serverCertificates = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ProxyCertificateAuthority> _logger;
    private readonly ProxyOptions _options;
    private readonly X509Certificate2 _rootCertificate;

    public ProxyCertificateAuthority(IOptions<ProxyOptions> options, ILogger<ProxyCertificateAuthority> logger)
    {
        _options = options.Value;
        _logger = logger;
        _rootCertificate = LoadOrCreateRootCertificate();

        if (_options.InstallRootCertificate)
            InstallRootCertificate();
        else
            _logger.LogWarning(
                "MikuSB proxy root certificate is not installed automatically. Import {CertificatePath} into CurrentUser Root to enable HTTPS interception.",
                RootCerPath);
    }

    public string RootCerPath => Path.Join(CertificateDirectory, "MikuSB.Proxy.Root.cer");
    public string RootCerPemPath => Path.Join(CertificateDirectory, "MikuSB.Proxy.Root.pem");

    private static string CertificateDirectory => Path.Combine(AppContext.BaseDirectory, "proxy-certs");

    public X509Certificate2 GetServerCertificate(string host)
    {
        host = host.Trim().TrimEnd('.').ToLowerInvariant();
        return _serverCertificates.GetOrAdd(host, CreateServerCertificate);
    }

    private X509Certificate2 LoadOrCreateRootCertificate()
    {
        Directory.CreateDirectory(CertificateDirectory);
        var pfxPath = Path.Combine(CertificateDirectory, "MikuSB.Proxy.Root.pfx");

        if (File.Exists(pfxPath))
        {
            var existing = X509CertificateLoader.LoadPkcs12(
                File.ReadAllBytes(pfxPath),
                Password,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet);

            if (!File.Exists(RootCerPath))
                File.WriteAllBytes(RootCerPath, existing.Export(X509ContentType.Cert));

            if (!File.Exists(RootCerPemPath))
            {
                string pemString = existing.ExportCertificatePem();
                File.WriteAllText(RootCerPemPath, pemString);
            }

            return existing;
        }

        using var rsa = RSA.Create(4096);
        var request = new CertificateRequest(
            "CN=MikuSB Proxy Root CA",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var root = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
        var exportable = X509CertificateLoader.LoadPkcs12(
            root.Export(X509ContentType.Pfx, Password),
            Password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet);

        File.WriteAllBytes(pfxPath, exportable.Export(X509ContentType.Pfx, Password));
        File.WriteAllBytes(RootCerPath, exportable.Export(X509ContentType.Cert));
        _logger.LogInformation("Created MikuSB proxy root certificate at {CertificatePath}", RootCerPath);
        return exportable;
    }

    private X509Certificate2 CreateServerCertificate(string host)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={host}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var san = new SubjectAlternativeNameBuilder();
        if (IPAddress.TryParse(host, out var address))
            san.AddIpAddress(address);
        else
            san.AddDnsName(host);

        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false));

        var serial = RandomNumberGenerator.GetBytes(16);
        using var certificate = request.Create(
            _rootCertificate,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(2),
            serial);

        return X509CertificateLoader.LoadPkcs12(
            certificate.CopyWithPrivateKey(rsa).Export(X509ContentType.Pfx),
            null,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet);
    }

    private void InstallRootCertificate()
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        var existing = store.Certificates.Find(X509FindType.FindByThumbprint, _rootCertificate.Thumbprint, false);
        if (existing.Count > 0)
            return;

        store.Add(_rootCertificate);
        _logger.LogWarning("Installed MikuSB proxy root certificate into CurrentUser Root store. Thumbprint={Thumbprint}", _rootCertificate.Thumbprint);
    }
}
