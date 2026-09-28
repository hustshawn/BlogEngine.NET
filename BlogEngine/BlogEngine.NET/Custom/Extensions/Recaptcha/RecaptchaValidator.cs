// Copyright (c) 2007 Adrian Godong, Ben Maurer
// Adapted for blogengine by Filip Stanek ( http://www.bloodforge.com )
// Migrated to ASP.NET Core - HttpWebRequest → HttpClient

namespace Recaptcha
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Calls the reCAPTCHA server to validate the answer to a reCAPTCHA challenge.
    /// </summary>
    public class RecaptchaValidator
    {
        private const string VerifyUrl = "http://api-verify.recaptcha.net/verify";
        private string remoteIp;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly Regex RegexIPv4Address = new Regex(@"^(?<address>\d+\.\d+\.\d+\.\d+):\d+$");

        public string Challenge { get; set; }
        public string PrivateKey { get; set; }

        private static string StripPortFromIPv4Address(string address)
        {
            var m = RegexIPv4Address.Match(address);
            return m.Success ? m.Groups["address"].Value : address;
        }

        public string RemoteIP
        {
            get => this.remoteIp;
            set
            {
                var ip = IPAddress.Parse(StripPortFromIPv4Address(value));

                if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    throw new ArgumentException($"Expecting an IP address, got {ip}");
                }

                this.remoteIp = ip.ToString();
            }
        }

        public string Response { get; set; }

        public RecaptchaResponse Validate()
        {
            CheckNotNull(this.PrivateKey, "PrivateKey");
            CheckNotNull(this.RemoteIP, "RemoteIp");
            CheckNotNull(this.Challenge, "Challenge");
            CheckNotNull(this.Response, "Response");

            if (this.Challenge == string.Empty || this.Response == string.Empty)
            {
                return RecaptchaResponse.InvalidSolution;
            }

            try
            {
                var formdata = String.Format(
                    "privatekey={0}&remoteip={1}&challenge={2}&response={3}",
                    Uri.EscapeDataString(this.PrivateKey),
                    Uri.EscapeDataString(this.RemoteIP),
                    Uri.EscapeDataString(this.Challenge),
                    Uri.EscapeDataString(this.Response));

                using var request = new HttpRequestMessage(HttpMethod.Post, VerifyUrl);
                request.Content = new StringContent(formdata, Encoding.ASCII, "application/x-www-form-urlencoded");
                request.Headers.TryAddWithoutValidation("User-Agent", "reCAPTCHA/ASP.NET");

                var response = _httpClient.Send(request);
                using var responseStream = response.Content.ReadAsStream();
                using TextReader readStream = new StreamReader(responseStream, Encoding.UTF8);
                var results = readStream.ReadToEnd().Split();

                switch (results[0])
                {
                    case "true":
                        return RecaptchaResponse.Valid;
                    case "false":
                        return new RecaptchaResponse(false, results[1]);
                    default:
                        throw new InvalidProgramException("Unknown status response.");
                }
            }
            catch (HttpRequestException)
            {
                return RecaptchaResponse.RecaptchaNotReachable;
            }
        }

        private static void CheckNotNull(object obj, string name)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
