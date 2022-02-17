using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace chktls;

public class Checker
{
    private readonly int _verbosity;
    private readonly HttpClientHandler _httpClientHandler;
    private readonly object _sync = new();
    private readonly List<Certs> _certs = new();

    public Checker(int verbosity)
    {
        _verbosity = verbosity;
        _httpClientHandler = new HttpClientHandler
        {
            //UseDefaultCredentials = true,
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                lock (_sync)
                {
                    _certs.Add(new Certs(verbosity, chain, errors));

                    // Console.WriteLine("______________________________");
                    // if (errors != SslPolicyErrors.None)
                    // {
                    //     Console.WriteLine($"Errors: {errors}");
                    // }

                    if (chain != null)
                    {
                    }
                    else
                    {
                        Console.WriteLine($"No chain");
                    }

                    return true;
                }
            }
        };
    }


    public async Task Go(string url)
    {
        Console.WriteLine($"{url}");
        if (_verbosity > 1)
        {
            Console.WriteLine(new string('_', Console.BufferWidth));
            Console.WriteLine("RESPONSE");
            Console.WriteLine();
            Console.WriteLine($"GET {url}");
        }

        string? location;
        {

            var httpClient = new HttpClient(_httpClientHandler);
            using var r = await httpClient.GetAsync(new Uri(url));

            if (_verbosity > 1)
            {
                Console.WriteLine($"{(int)r.StatusCode} ({r.StatusCode}) HTTP/{r.Version}");
                Console.WriteLine();
            }

            using var content = r.Content;

            if (_verbosity > 1)
            {
                foreach (var header in r.Headers)
                foreach (var value in header.Value)
                {
                    Console.WriteLine($"{header.Key}: {value}");
                }
            }

            if (r.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Moved or HttpStatusCode.MovedPermanently)
            {
                location = r.Headers
                    .FirstOrDefault(kv => string.Equals(kv.Key, "Location", StringComparison.OrdinalIgnoreCase))
                    .Value.FirstOrDefault();
                if (location == null)
                {
                    throw new Exception("Can't find 'Location' header for redirect");
                }
            }
            else
            {
                location = null;
            }
        }

        if (_verbosity > 1)
        {
            Console.WriteLine();
        }

        lock (_sync)
        {
            if (_certs.Count == 0)
            {
                Console.WriteLine("No certificate chain found");
                Console.WriteLine();
            }

            for (var index = 0; index < _certs.Count; index++)
            {
                if (_verbosity > 1)
                {
                    Console.WriteLine(new string('_', Console.BufferWidth));
                    Console.WriteLine("X.509 SUMMARY");
                    Console.WriteLine();
                }
                
                //Console.Write($"Certificate chain");
                // if (_certs.Count > 1)
                // {
                //     Console.Write($" ({index + 1})");
                // }
                // Console.WriteLine(":");

                var certs = _certs[index];

                Console.Write(certs);

                if (_verbosity > 2)
                {
                    Console.WriteLine(new string('_', Console.BufferWidth));
                    Console.WriteLine("X.509 DETAILS");
                    Console.WriteLine();
                    foreach (var certificate in certs.CertificateChain)
                    {
                        var pem = new PemExtractor().ExportToPEM(certificate);
                        Console.Write($"{certificate}");
                        Console.WriteLine($"{pem}\n");
                    }
                }
            }

            _certs.Clear();
        }


        if (location != null)
        {
            //Console.WriteLine(new string('>', Console.BufferWidth));
            if (_verbosity > 1)
            {
                Console.WriteLine($">>> REDIRECT {location}");
            }
            //Console.WriteLine(new string('>', Console.BufferWidth));

            await Go(location);
            //Console.WriteLine(await content.ReadAsStringAsync());
        }
    }
}

class PemExtractor
{
    public string OpenSsl(X509Certificate certificate)
    {
        var sb = new StringBuilder();
        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "openssl",
                Arguments = "openssl x509 -text -noout",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        p.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
                sb.AppendLine(args.Data);
        };
        p.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
                sb.AppendLine(args.Data);
        };
        p.Start();
        p.BeginErrorReadLine();
        p.BeginOutputReadLine();
        p.StandardInput.WriteLine(ExportToPEM(certificate));
        p.StandardInput.Close();
        p.WaitForExit();

        return sb.ToString();
    }

    /// <summary>
    /// Export a certificate to a PEM format string
    /// </summary>
    /// <param name="cert">The certificate to export</param>
    /// <returns>A PEM encoded string</returns>
    public string ExportToPEM(X509Certificate cert)
    {
        // https://stackoverflow.com/a/4740292/248393
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("-----BEGIN CERTIFICATE-----");
        builder.AppendLine(Convert.ToBase64String(cert.GetEncoded(), Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine("-----END CERTIFICATE-----");

        return builder.ToString();
    }
}

internal class Certs
{
    private readonly int _verbosity;
    private readonly string _errors;
    private readonly List<string> _certs = new();
    public List<X509Certificate> CertificateChain { get; } = new();
    private const string OidAltSubject = "2.5.29.17";

    public Certs(int verbosity, X509Chain? chain, SslPolicyErrors errors)
    {
        _verbosity = verbosity;
        _errors = errors != SslPolicyErrors.None ? errors.ToString() : "";

        if (chain == null) return;

        foreach (var element in chain.ChainElements)
        {
            X509Certificate? certificate =
                new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(
                    element.Certificate.Export(X509ContentType.Cert));
            CertificateChain.Add(certificate);
            _certs.Add(FormatCert(element.Certificate));
        }
    }

    private string FormatCert(X509Certificate2 certificate)
    {
        var sb = new StringBuilder();
        sb.AppendLine($" Subject: {certificate.Subject}");
        sb.AppendLine($"  Issuer: {certificate.Issuer}");

        if (_verbosity > 1)
        {
            sb.AppendLine($"  NotBefore: {certificate.NotBefore}");
            sb.AppendLine($"  NotAfter: {certificate.NotAfter}");
            sb.AppendLine($"  SerialNumber: {certificate.SerialNumber}");
            sb.AppendLine($"  Thumbprint: {certificate.Thumbprint}");
        }

        var alt = Alt(certificate);
        if (_verbosity > 0 && alt != null)
        {
            var others = alt
                .Split(',')
                .Select(a => a.Replace("DNS:", "").Trim())
                .ToList();
            var w = Console.BufferWidth;
            var t = "  Others:";
            var i = t.Length;
            var a = w - i - 2;
            //Console.WriteLine($">>>>>>> w={w} i={i} a={a}");
            sb.Append(t);
            if (a > 20)
            {
                var c = 0;
                foreach (var other in others)
                {
                    c += other.Length + 1;
                    //Console.WriteLine($">>>>>>> c={c} a={a} {other}");
                    if (c >= a)
                    {
                        //Console.WriteLine($">>>>>>> LF");
                        c = other.Length + 1;
                        sb.AppendLine();
                        sb.Append(new string(' ', i));
                    }
                    sb.Append($" {other}");
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine(string.Join(" ", others));
            }
        }

        foreach (var extension in certificate.Extensions)
        {
            // sb.AppendLine($"  ext ({extension.Oid?.Value}) {extension.Oid?.FriendlyName}");
            // var asn = new AsnEncodedData(extension.Oid, extension.RawData);
            // sb.AppendLine($"    Extension type: {extension.Oid?.FriendlyName}");
            // sb.AppendLine($"    Oid value: {asn.Oid?.Value}");
            // sb.AppendLine($"    Raw data length: {asn.RawData.Length}");
            // sb.AppendLine($"    Data: {asn.Format(true)}");

            // sb.Append("    ASCII: ");
            // foreach (var b in asndata.RawData)
            // {
            //     if (b is > 20 and < 127)
            //     {
            //         sb.Append((char)b);
            //     }
            //     else
            //     {
            //         sb.Append('.');
            //     }
            // }
            // sb.AppendLine();
        }

        return sb.ToString();
    }

    string? Alt(X509Certificate2 certificate)
    {
        foreach (var extension in certificate.Extensions)
        {
            // alt subject: 2.5.29.17
            if (string.Equals(extension.Oid?.Value, OidAltSubject))
            {
                return new AsnEncodedData(extension.Oid, extension.RawData).Format(false);
            }
        }

        return null;
    }


    public override string ToString()
    {
        var sb = new StringBuilder();
        if (_verbosity > 1 && _errors != "")
        {
            sb.AppendLine($"Certificate errors: {_errors}");
        }

        foreach (var cert in _certs)
        {
            sb.AppendLine($"{cert}");
        }

        return sb.ToString();
    }
}

//https://en.code-bude.net/2017/02/21/how-to-download-ssl-tls-certificates-in-csharp/
/*
 * try
{
    X509Certificate2 cert = null;
    var client = new TcpClient("imap.gmail.com", 993);
    var certValidation = new RemoteCertificateValidationCallback(delegate (object snd, X509Certificate certificate,
                X509Chain chainLocal, SslPolicyErrors sslPolicyErrors)
    {
        return true; //Accept every certificate, even if it's invalid
    });
 
    // Create an SSL stream and takeover client's stream
    using (var sslStream = new SslStream(client.GetStream(), true, certValidation))
    {
        sslStream.AuthenticateAsClient("imap.gmail.com");
        var serverCertificate = sslStream.RemoteCertificate;
        cert = new X509Certificate2(serverCertificate);
    }
}
catch (Exception e) {
    //throw some fancy exception ;-)
}
 */