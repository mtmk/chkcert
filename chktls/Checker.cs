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
    private readonly HttpClientHandler _httpClientHandler;
    private readonly object _sync = new();
    private readonly List<Certs> _certs = new();

    public Checker()
    {
        _httpClientHandler = new HttpClientHandler
        {
            //UseDefaultCredentials = true,
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                lock (_sync)
                {
                    _certs.Add(new Certs(chain, errors));

                    // Console.WriteLine("______________________________");
                    if (errors != SslPolicyErrors.None)
                    {
                        Console.WriteLine($"Errors: {errors}");
                    }

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
        string? location;
        {
            Console.WriteLine($"GET {url}");
            var httpClient = new HttpClient(_httpClientHandler);
            using var r = await httpClient.GetAsync(new Uri(url));
            Console.WriteLine($"{(int)r.StatusCode} ({r.StatusCode})");
            using var content = r.Content;
            foreach (var header in r.Headers)
            foreach (var value in header.Value)
            {
                Console.WriteLine($"{header.Key}: {value}");
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

        Console.WriteLine();
        
        lock (_sync)
        {
            if (_certs.Count == 0)
            {
                Console.WriteLine("No certificate chain found");
            }
            for (var index = 0; index < _certs.Count; index++)
            {
                Console.WriteLine(new string('_', Console.BufferWidth));
                Console.Write($"Certificate chain");
                if (_certs.Count > 1)
                {
                    Console.Write($" ({index + 1})");
                }
                Console.WriteLine(":");
                
                var certs = _certs[index];
                //Console.WriteLine(certs);
                foreach (var certificate in certs.CertificateChain)
                {
                    var pem = new PemExtractor().ExportToPEM(certificate);
                    Console.WriteLine($"{certificate}\n----");
                    //Console.WriteLine($"PEM:\n{pem}\n----");
                }
            }
        }


        if (location != null)
        {
            Console.WriteLine(new string('=', Console.BufferWidth));
            Console.WriteLine($"Redirecting to {location}");
            Console.WriteLine();
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
        var p = new Process{StartInfo = new ProcessStartInfo
        {
            FileName = "openssl",
            Arguments = "openssl x509 -text -noout",
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }};
        p.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
                sb.AppendLine(args.Data);
        };
        p.OutputDataReceived+= (_, args) =>
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
    private readonly string _errors;
    private readonly List<string> _certs = new();
    public List<X509Certificate> CertificateChain { get; } = new();
    private const string OidAltSubject = "2.5.29.17";

    public Certs(X509Chain? chain, SslPolicyErrors errors)
    {
        _errors = errors != SslPolicyErrors.None ? errors.ToString() : "";

        if (chain == null) return;
        
        foreach (var element in chain.ChainElements)
        {
            X509Certificate? certificate = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(element.Certificate.Export(X509ContentType.Cert));
            CertificateChain.Add(certificate);
            _certs.Add(FormatCert(element.Certificate));
        }
    }
    
    private string FormatCert(X509Certificate2 certificate)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Subject: {certificate.Subject}");
        sb.AppendLine($" Issuer: {certificate.Issuer}");
        // sb.AppendLine($"SerialNumber: {certificate.SerialNumber}");
        sb.AppendLine($" NotBefore: {certificate.NotBefore}");
        sb.AppendLine($" NotAfter: {certificate.NotAfter}");
        // sb.AppendLine($"Thumbprint: {certificate.Thumbprint}");
        var alt = Alt(certificate);
        if (alt != null)
        {
            sb.AppendLine($" AltSubjectName: {alt}");
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
            //Console.ResetColor();
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
        if (_errors != "")
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