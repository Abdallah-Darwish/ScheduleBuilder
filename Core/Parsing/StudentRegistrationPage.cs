using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScheduleBuilder.Core.Parsing
{
    class StudentRegistrationPage : RegnewPage
    {
        public StudentRegistrationPage(string pageSource) : base(pageSource)
        {
            ParsePage();
        }

        public IReadOnlyList<UClass> AvilableClasses { get; private set; } = null;
        public IReadOnlyList<UClass> RegisteredClasses { get; private set; } = null;
        public IReadOnlyDictionary<int, string> AvilableClassesRegistrationEventTargets { get; private set; } = null;

        public int PageNumber { get; private set; } = -1;
        public int LastPageNumber { get; private set; } = -1;

        public bool AreAvilableClassesParsed { get; private set; } = false;
        public bool AreRegisteredClassesParsed { get; private set; } = false;
        public bool AreAvilableClassesRegistrationEventTargetsParsed { get; private set; } = false;

        readonly object _parsingRegisteredClassesLock = new object();
        Task _parsingRegisteredClassesTask;

        readonly object _parsingAvilableClassesLock = new object();
        Task _parsingAvilableClassesTask;

        readonly object _parsingClassesRegestrationEventTargetsLock = new object();
        Task _parsingClassesRegistrationEventTargetsTask;

        private static readonly Regex
            rgx_paging = new Regex("\\<tr class=\"paging\"\\>([\\s\\S]*?\\<\\/tr?\\>)", GlobalRegexOptions.Value),
            rgx_lastPageNumber = new Regex("(\\d+)\\<\\/a\\>\\<\\/td\\>[^\\<]*<\\/tr", GlobalRegexOptions.Value),
            rgx_pageNumber = new Regex("\\<span\\>(\\d+)\\<\\/span", GlobalRegexOptions.Value),
            rgx_registeredCoursesTable = new Regex("_gvRegisteredCourses\"((?:[^\\>])*)\\>([\\s\\S]*?)\\<\\/table\\>", GlobalRegexOptions.Value),
            rgx_avilableCoursesTable = new Regex("_gvRegistrationCoursesSchedule\"((?:[^\\>])*)\\>([\\s\\S]*?)\\<\\/table\\>", GlobalRegexOptions.Value),
            rgx_hdnFinHours = new Regex("name=\"(?<name>ctl\\d+\\$ContentPlaceHolder\\d+\\$gvRegisteredCourses\\$ctl\\d+\\$hdnFinHours)\"[^>]+value=\"(?<value>\\d+)\"", GlobalRegexOptions.Value),
            rgx_hdnPreRequsite = new Regex("name=\"(ctl\\d+\\$ContentPlaceHolder\\d+\\$gvRegisteredCourses\\$ctl\\d+\\$HdnPreRequsite)\"", GlobalRegexOptions.Value);
        public string HiddenFieldsText { get; private set; }
        private void ParsePage()
        {
            //The request with or without it is working
            HiddenFieldsText = string.Join("&",
                rgx_hdnFinHours.Matches(_pageSource).OfType<Match>()
                .Select(m => $"{WebUtility.UrlEncode(m.Groups["name"].Value)}={WebUtility.UrlEncode(m.Groups["value"].Value)}")
                .Concat(rgx_hdnPreRequsite.Matches(_pageSource).OfType<Match>().Select(m => $"{WebUtility.UrlEncode(m.Groups[1].Value)}=")));

            string pagingPart = rgx_paging.Match(_pageSource).Groups[1].Value;
            var pageNumberMatch = rgx_pageNumber.Match(pagingPart);
            PageNumber = pageNumberMatch.Success ? int.Parse(pageNumberMatch.Groups[1].Value) : -1;

            var lastPageNumberMatch = rgx_lastPageNumber.Match(pagingPart);
            //In case its one page only the match will fail
            LastPageNumber = lastPageNumberMatch.Success ? int.Parse(lastPageNumberMatch.Groups[1].Value) : PageNumber;
        }
        private void ClearPageSource()
        {
            if(AreAvilableClassesParsed && AreRegisteredClassesParsed && AreAvilableClassesRegistrationEventTargetsParsed)
            {
                _pageSource = null;
            }
        }
        private void InternalParseAvilableClasses()
        {
            AvilableClasses = UClassParser.ParseClassesTable(rgx_avilableCoursesTable.Match(_pageSource).Groups[0].Value);
            AreAvilableClassesParsed = true;
            ClearPageSource();
        }
        public Task ParseAvilableClasses()
        {
            lock (_parsingAvilableClassesLock)
            {
                if (_parsingAvilableClassesTask == null)
                {
                    _parsingAvilableClassesTask = Task.Run(() => InternalParseAvilableClasses());
                }
                return _parsingAvilableClassesTask;
            }
        }

        private void InternalParseRegisteredClasses()
        {
            RegisteredClasses = UClassParser.ParseClassesTable(rgx_registeredCoursesTable.Match(_pageSource).Groups[0].Value);
            AreRegisteredClassesParsed = true;
            ClearPageSource();
        }
        public Task ParseRegisteredClasses()
        {
            lock (_parsingRegisteredClassesLock)
            {
                if (_parsingRegisteredClassesTask == null)
                {
                    _parsingRegisteredClassesTask = Task.Run(() => InternalParseRegisteredClasses());
                }
                return _parsingRegisteredClassesTask;
            }
        }

        private async Task InternalParseClassesRegistrationEventTargets()
        {
            await ParseAvilableClasses();

            AvilableClassesRegistrationEventTargets = UClassParser.ParseRegestrationEventTargets(_pageSource);
            AreAvilableClassesRegistrationEventTargetsParsed = true;
            ClearPageSource();
        }

        public Task ParseClassesRegistrationEventTargets()
        {
            lock (_parsingClassesRegestrationEventTargetsLock)
            {
                if (_parsingClassesRegistrationEventTargetsTask == null)
                {
                    _parsingClassesRegistrationEventTargetsTask = InternalParseClassesRegistrationEventTargets();
                }
                return _parsingClassesRegistrationEventTargetsTask;
            }
        }
    }
}
