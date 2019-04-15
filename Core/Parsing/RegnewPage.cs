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
            //page* means its in the page source
            rgx_pageEventValidation = new Regex("id=\"__EVENTVALIDATION\"\\svalue=\"([^\"]+)", GlobalRegexOptions.Value),
            rgx_pageViewStateGenerator = new Regex("id=\"__VIEWSTATEGENERATOR\"\\svalue=\"([^\"]+)", GlobalRegexOptions.Value),
            rgx_pageEventTraget = new Regex("id=\"__EVENTTARGET\"\\svalue=\"([^\"]+)", GlobalRegexOptions.Value),
            rgx_pageEventArgument = new Regex("id=\"__EVENTARGUMENT\"\\svalue=\"([^\"]+)", GlobalRegexOptions.Value),
            rgx_pageLastFocus = new Regex("id=\"__LASTFOCUS\"\\svalue=\"([^\"]+)", GlobalRegexOptions.Value),
            rgx_pageViewState = new Regex("id=\"__VIEWSTATE\"\\svalue=\"([^\"]+)", GlobalRegexOptions.Value),
            rgx_pagePreviousPage = new Regex("id=\"__PREVIOUSPAGE\"\\svalue=\"([^\"]+)", GlobalRegexOptions.Value),
            //response* means its recived via ajax
            rgx_responseEventValidation = new Regex("hiddenField\\|__EVENTVALIDATION\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responseViewState = new Regex("hiddenField\\|__VIEWSTATE\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responseEventTarget = new Regex("hiddenField\\|__EVENTTARGET\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responseEventArgument = new Regex("hiddenField\\|__EVENTARGUMENT\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responseLastFocus = new Regex("hiddenField\\|__LASTFOCUS\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responseViewStateGenerator = new Regex("hiddenField\\|__VIEWSTATEGENERATOR\\|([^\\|]+)", GlobalRegexOptions.Value),
            rgx_responsePreviousPage = new Regex("hiddenField\\|__PREVIOUSPAGE\\|([^\\|]+)", GlobalRegexOptions.Value);


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
            + $"&__PREVIOUSPAGE={EncodedPreviousPage}"
            + "&__ASYNCPOST=true";

        private void ParsePageTokens()
        {
            //_pageSource must be trimmed for this to work
            if (_pageSource.EndsWith("</html>"))
            {
                ViewState = rgx_pageViewState.Match(_pageSource).Groups[1].Value;
                ViewStateGenerator = rgx_pageViewStateGenerator.Match(_pageSource).Groups[1].Value;
                EventValidation = rgx_pageEventValidation.Match(_pageSource).Groups[1].Value;
                EventArgument = rgx_pageEventArgument.Match(_pageSource).Groups[1].Value;
                EventTarget = rgx_pageEventTraget.Match(_pageSource).Groups[1].Value;
                LastFocus = rgx_pageLastFocus.Match(_pageSource).Groups[1].Value;
                PreviousPage = rgx_pagePreviousPage.Match(_pageSource).Groups[1].Value;
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
        public RegnewPage(string pageSource)
        {
            if (string.IsNullOrWhiteSpace(pageSource))
            {
                throw new ArgumentException($"Argument {pageSource} can't be a null or a white space.", nameof(pageSource));
            }
            _pageSource = pageSource.Trim();
            ParsePageTokens();
        }

    }
}
