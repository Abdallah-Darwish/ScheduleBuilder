using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace ScheduleBuilder.Core.Parsing
{
    internal class LoggedInPage : RegnewPage
    {
        private static readonly char[] s_digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        public string Username { get; }
        public DateTime LastLoginTime { get; }

        //<div style="font-size:10px">اخر دخول:23/07/2019 12:39:43 ص</div>

        //<div style="font-size:10px">Last Login:23/07/2019 00:39:43</div>
        public LoggedInPage(string pageSource) : base(pageSource)
        {
            var userInfoSpan = _pageDocument.QuerySelector<IHtmlSpanElement>("#lblLoggedUserName");
            Username = userInfoSpan.InnerHtml;
            Username = Username.Remove(Username.IndexOf('<')).Trim();

            string lastLoginDivContent = userInfoSpan.Children.First().TextContent;
            string lastLoginTimeString = lastLoginDivContent;
            lastLoginTimeString = lastLoginTimeString.Remove(0, lastLoginDivContent.IndexOfAny(s_digits));
            int lastNumberIndex = lastLoginTimeString.LastIndexOfAny(s_digits);
            if (lastNumberIndex != lastLoginTimeString.Length - 1)
            {
                lastLoginTimeString = lastLoginTimeString.Remove(lastNumberIndex + 1);
            }
            LastLoginTime = DateTime.ParseExact(lastLoginTimeString, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            //if (lastLoginDivContent.Contains('ص') && LastLoginTime.Hour == 12) { LastLoginTime -= TimeSpan.FromHours(12); }
        }
    }
}
