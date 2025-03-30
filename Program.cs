using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace CertificateExpirationWatcher
{
    class Program
    {
        private const string ConfigFileName = "config.json";
        private const int delayInHours = 12;
        private const int timeoutInSeconds = 3;

        static async Task Main(string[] args)
        {
            if (!File.Exists(ConfigFileName))
            {
                Console.WriteLine("Configuration file not found.");
                return;
            }

            var configData = JsonSerializer.Deserialize<ConfigData>(await File.ReadAllTextAsync(ConfigFileName), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (configData == null || configData.Watchers == null || configData.Watchers.Length == 0)
            {
                Console.WriteLine("No configurations found.");
                return;
            }

            while (true)
            {
                foreach (var config in configData.Watchers)
                {
                    await CheckCertificateExpiration(config, configData.EmailSettings);
                }

                // Save the updated configuration back to the file
                await File.WriteAllTextAsync(ConfigFileName, JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true }));

                // Wait before the next check
                await Task.Delay(TimeSpan.FromHours(delayInHours));
            }
        }

        static async Task CheckCertificateExpiration(WatcherConfig config, EmailSettings emailSettings)
        {
            string domainRoot = new Uri(config.url).GetLeftPart(UriPartial.Authority);

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                X509Certificate2 serverCertificate = null;

                handler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    serverCertificate = new X509Certificate2(certificate);
                    return true; // Accept all certificates
                };

                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeoutInSeconds) })
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(config.url);
                        if (response.IsSuccessStatusCode)
                        {
                            if (serverCertificate == null)
                            {
                                config.latest = $"No server certificate found for {domainRoot}";
                                Console.WriteLine(config.latest);
                                return;
                            }

                            DateTime expirationDate = serverCertificate.NotAfter;
                            config.expiration = expirationDate.ToString("o");
                            Console.WriteLine($"Certificate expiration date for {domainRoot} : {expirationDate}");

                            foreach (int daysBefore in config.notification)
                            {
                                DateTime notificationDate = expirationDate.AddDays(-daysBefore);
                                if (DateTime.UtcNow >= notificationDate && config.latest != $"Notification sent {daysBefore} days before expiration for {domainRoot}")
                                {
                                    config.latest = $"Notification sent {daysBefore} days before expiration for {domainRoot}";
                                    Console.WriteLine(config.latest);
                                    SendEmailNotification(emailSettings, domainRoot, expirationDate, daysBefore);
                                }
                            }
                        }
                        else
                        {
                            config.latest = $"Failed to retrieve the web page {domainRoot}. Status code: {response.StatusCode}";
                            Console.WriteLine(config.latest);
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        config.latest = $"HTTP request error: {httpEx.Message}";
                        Console.WriteLine(config.latest);
                    }
                    catch (TaskCanceledException)
                    {
                        config.latest = $"Request timed out for {domainRoot}";
                        Console.WriteLine(config.latest);
                    }
                    catch (Exception ex)
                    {
                        config.latest = $"An error occurred: {ex.Message}";
                        Console.WriteLine(config.latest);
                    }
                }
            }
        }

        static void SendEmailNotification(EmailSettings emailSettings, string url, DateTime expirationDate, int daysBefore)
        {
            try
            {
                using (var client = new SmtpClient(emailSettings.SmtpServer, emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(emailSettings.SmtpUser, emailSettings.SmtpPassword),
                    EnableSsl = true
                })
                {
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(emailSettings.FromEmail),
                        Subject = $"Certificate Expiration Alert for {url}",
                        Body = $"The certificate for {url} is set to expire on {expirationDate}. This is a notification {daysBefore} days before expiration.",
                        IsBodyHtml = false,
                    };
                    mailMessage.To.Add(emailSettings.ToEmail);

                    client.Send(mailMessage);
                    Console.WriteLine($"Email notification sent for {url}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email notification: {ex.Message}" + ((ex.InnerException != null) ? $" ({ex.InnerException.Message})" : ""));
            }
        }
    }

    public class ConfigData
    {
        public EmailSettings EmailSettings { get; set; }
        public WatcherConfig[] Watchers { get; set; }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPassword { get; set; }
        public string FromEmail { get; set; }
        public string ToEmail { get; set; }
    }

    public class WatcherConfig
    {
        public string url { get; set; }
        public string expiration { get; set; }
        public List<int> notification { get; set; }
        public string latest { get; set; }
    }
}