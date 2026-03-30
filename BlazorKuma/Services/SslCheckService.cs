using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

public class SslCheckService
{
    public async Task<(bool IsValid, string Issuer, DateTime ExpiryDate, string Error)> GetSslDetailsAsync(string domain)
    {
        try
        {
            using var client = new TcpClient();
            // Timeout after 5 seconds if the server doesn't respond
            var connectTask = client.ConnectAsync(domain, 443);
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                return (false, "", DateTime.MinValue, "Connection Timeout");

            using var sslStream = new SslStream(client.GetStream(), false, (sender, cert, chain, errors) => true);
            await sslStream.AuthenticateAsClientAsync(domain);

            var certificate = new X509Certificate2(sslStream.RemoteCertificate!);

            return (
                true,
                certificate.Issuer,
                certificate.NotAfter, // Expiration Date
                ""
            );
        }
        catch (Exception ex)
        {
            return (false, "", DateTime.MinValue, ex.Message);
        }
    }
}