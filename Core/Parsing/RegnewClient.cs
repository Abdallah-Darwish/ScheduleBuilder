using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ScheduleBuilder.Core.Parsing
{
    public class RegnewClient : HttpClient
    {
        public const string PsutDomainText = @"regnew.psut.edu.jo";
        public static readonly Uri PsutDomainUri = new Uri($@"https://{PsutDomainText}/");
        public static readonly Uri IndexPageUri = new Uri($@"https://{PsutDomainText}/IndexPage.aspx");
        public AsyncAutoResetEvent SyncRoot { get; } = new AsyncAutoResetEvent();
        public CookieContainer Cookies { get; }
        public HttpClientHandler Handler { get; }
        private RegnewClient(HttpClientHandler handler, CookieContainer cookiesConatiner) : base(handler)
        {
            Cookies = cookiesConatiner;
            DefaultRequestHeaders.Clear();

            //All of the headers were copied from fiddler and firefox!
            BaseAddress = new Uri($@"https://{PsutDomainText}/");
            DefaultRequestHeaders.UserAgent.ParseAdd(@"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:61.0) Gecko/20100101 Firefox/61.0");
            DefaultRequestHeaders.AcceptLanguage.ParseAdd(@"en-US,en;q=0.5");
            DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
            DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            DefaultRequestHeaders.Add("X-MicrosoftAjax", "Delta=true");
            DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue() { NoCache = true };
            DefaultRequestHeaders.Connection.Add("keep-alive");
            DefaultRequestHeaders.Accept.ParseAdd("*/*");
            DefaultRequestHeaders.Referrer = PsutDomainUri;
        }

        public static RegnewClient Create(params Cookie[] additionalCookies)
        {
            var cookies = new CookieContainer();
            cookies.Add(new CookieCollection() {
                //I Choose arabic language because some courses have the same Id and english name but they differ in arabic name like "clac 2 for it" and "clac 2 for eng"
                new Cookie("lang", "ar-JO", "/", PsutDomainText) { Expires = DateTime.UtcNow + TimeSpan.FromDays(1) },
                new Cookie("AspxAutoDetectCookieSupport", "1", "/", PsutDomainText),
                //session_id will be added by the server upon the first request
                //new Cookie("ASP.NET_SessionId", sessionID, "/", PsutDomain) { HttpOnly = true }
            });
            foreach (var additionalCookie in additionalCookies)
            {
                cookies.Add(additionalCookie);
            }
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                CookieContainer = cookies
            };
            return new RegnewClient(handler, cookies);
        }

        public StringContent CreateStringContent(string content)
        {
            var result = new StringContent(content, Encoding.UTF8);
            result.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = "utf-8" };
            return result;
        }
    }
}
