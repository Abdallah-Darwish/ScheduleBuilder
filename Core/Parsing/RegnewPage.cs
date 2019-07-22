using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
namespace ScheduleBuilder.Core.Parsing
{
    class RegnewPage
    {
        private static readonly Regex
            //response* means its recived via ajax
            rgx_responseEventValidation = new Regex("hiddenField\\|__EVENTVALIDATION\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responseViewState = new Regex("hiddenField\\|__VIEWSTATE\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responseEventTarget = new Regex("hiddenField\\|__EVENTTARGET\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responseEventArgument = new Regex("hiddenField\\|__EVENTARGUMENT\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responseLastFocus = new Regex("hiddenField\\|__LASTFOCUS\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responseViewStateGenerator = new Regex("hiddenField\\|__VIEWSTATEGENERATOR\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responsePreviousPage = new Regex("hiddenField\\|__PREVIOUSPAGE\\|([^\\|]+)", GlobalRegexOptions.Value);


        protected bool IsAjaxPage { get; }

        public string ViewState { get; private set; }
        public string EventValidation { get; private set; }
        public string EventTarget { get; private set; }
        public string EventArgument { get; private set; }
        public string LastFocus { get; private set; }
        public string ViewStateGenerator { get; private set; }
        public string PreviousPage { get; set; }

        public string EncodedViewState => WebUtility.UrlEncode(ViewState);
        public string EncodedEventValidation => WebUtility.UrlEncode(EventValidation);
        public string EncodedEventTarget => WebUtility.UrlEncode(EventTarget);
        public string EncodedEventArgument => WebUtility.UrlEncode(EventArgument);
        public string EncodedLastFocus => WebUtility.UrlEncode(LastFocus);
        public string EncodedViewStateGenerator => WebUtility.UrlEncode(ViewStateGenerator);
        public string EncodedPreviousPage => WebUtility.UrlEncode(PreviousPage);

        public string EncodedHiddenFields =>
            $"&__LASTFOCUS={EncodedLastFocus}"
            + $"&__VIEWSTATE={EncodedViewState}"
            + $"&__VIEWSTATEGENERATOR={EncodedViewStateGenerator}"
            + $"&__EVENTVALIDATION={EncodedEventValidation}"
            //TODO: In-case of errors try removing the comment on next line
            //+ $"&__PREVIOUSPAGE={EncodedPreviousPage}"
            + "&__ASYNCPOST=true";

        private void ParsePageTokens()
        {
            //_pageSource must be trimmed for this to work
            if (IsAjaxPage == false)
            {
                ViewState = _pageDocument.QuerySelector<IHtmlInputElement>("input#__VIEWSTATE").Value;
                ViewStateGenerator = _pageDocument.QuerySelector<IHtmlInputElement>("input#__VIEWSTATEGENERATOR").Value;
                EventValidation = _pageDocument.QuerySelector<IHtmlInputElement>("input#__EVENTVALIDATION").Value;
                EventArgument = _pageDocument.QuerySelector<IHtmlInputElement>("input#__EVENTARGUMENT").Value;
                EventTarget = _pageDocument.QuerySelector<IHtmlInputElement>("input#__EVENTTARGET").Value;
                LastFocus = _pageDocument.QuerySelector<IHtmlInputElement>("input#__LASTFOCUS").Value;
                PreviousPage = _pageDocument.QuerySelector<IHtmlInputElement>("input#__PREVIOUSPAGE")?.Value;
            }
            else
            {
                ViewState = rgx_responseViewState.Match(_pageSource).Groups[1].Value;
                ViewStateGenerator = rgx_responseViewStateGenerator.Match(_pageSource).Groups[1].Value;
                EventValidation = rgx_responseEventValidation.Match(_pageSource).Groups[1].Value;
                EventArgument = rgx_responseEventArgument.Match(_pageSource).Groups[1].Value;
                EventTarget = rgx_responseEventTarget.Match(_pageSource).Groups[1].Value;
                LastFocus = rgx_responseLastFocus.Match(_pageSource).Groups[1].Value;
                var previousPageMatch = rgx_responsePreviousPage.Match(_pageSource);
                if (previousPageMatch.Success)
                {
                    PreviousPage = previousPageMatch.Groups[1].Value;
                }
            }
        }

        protected string _pageSource = null;
        protected IHtmlDocument _pageDocument = null;
        public RegnewPage(string pageSource)
        {
            if (string.IsNullOrWhiteSpace(pageSource))
            {
                throw new ArgumentException($"Argument {pageSource} can't be a null or a white space.", nameof(pageSource));
            }

            _pageSource = pageSource.Trim();
            IsAjaxPage = !_pageSource.EndsWith("</html>");

            string htmlPageSource;
            if (IsAjaxPage)
            {
                int openDiv = pageSource.IndexOf("<div"), closeDiv = pageSource.LastIndexOf("</div>");
                htmlPageSource = _pageSource.Substring(openDiv, (closeDiv + 5) - openDiv + 1);
            }
            else { htmlPageSource = _pageSource; }

            var parser = new HtmlParser();
            _pageDocument = parser.ParseDocument(htmlPageSource);

            ParsePageTokens();
            _pageSource = htmlPageSource;
        }

    }
}
