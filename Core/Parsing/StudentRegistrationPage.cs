using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace ScheduleBuilder.Core.Parsing
{
    enum ResultBoxColor
    {
        Green, Red, Yellow, Unknown
    }
    class StudentRegistrationPage : LoggedInPage
    {
        public StudentRegistrationPage(string pageSource) : base(pageSource)
        {
            ParsePage();
        }

        public ResultBoxColor ResultBoxColor { get; private set; }

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

        public string HiddenFieldsText { get; private set; }
        private void ParsePage()
        {
            var resultBoxDiv = _pageDocument.QuerySelector<IHtmlDivElement>(".ResultBox");
            if (resultBoxDiv != null)
            {
                string resultBoxStyle = resultBoxDiv.GetAttribute("style");
                resultBoxStyle = resultBoxStyle.Substring(resultBoxStyle.IndexOf(':') + 1);
                resultBoxStyle = resultBoxStyle.Remove(resultBoxStyle.IndexOf(';'));
                //Now resultBoxStyle conatins the color of the box
                resultBoxStyle = resultBoxStyle.Trim();

                switch (resultBoxStyle)
                {
                    case "green":
                        ResultBoxColor = ResultBoxColor.Green;
                        break;
                    case "#cccc00":
                        ResultBoxColor = ResultBoxColor.Yellow;
                        break;
                    case "#c00":
                        ResultBoxColor = ResultBoxColor.Red;
                        break;
                    default:
                        ResultBoxColor = ResultBoxColor.Unknown;
                        break;
                }
            }
            else { ResultBoxColor = ResultBoxColor.Unknown; }

            var hdnFinHours = _pageDocument.QuerySelector<IHtmlInputElement>("input[type=hidden][id*=dnFinHours]");
            var hdnPreRequsite = _pageDocument.QuerySelector<IHtmlInputElement>("input[type=hidden][id*=dnPreRequsite]");
            //The request with or without it is working
            if (hdnFinHours != null && hdnPreRequsite != null)
            {
                HiddenFieldsText = $"{WebUtility.UrlEncode(hdnFinHours.Name)}={WebUtility.UrlEncode(hdnFinHours.ValidationMessage)}&{WebUtility.UrlEncode(hdnPreRequsite.Name)}={WebUtility.UrlEncode(hdnPreRequsite.ValidationMessage)}";
            }
            else { HiddenFieldsText = string.Empty; }

            var pagesRow = _pageDocument.QuerySelector<IHtmlTableRowElement>("tr.paging");
            if (pagesRow != null && pagesRow.Cells.Length > 0)
            {
                PageNumber = int.Parse(pagesRow.QuerySelector<IHtmlSpanElement>("td>span").TextContent);
                LastPageNumber = int.Parse(pagesRow.Cells.Last().TextContent);
            }
            else
            {
                LastPageNumber = PageNumber = -1;
            }
        }
        private void ClearPageSource()
        {
            if (AreAvilableClassesParsed && AreRegisteredClassesParsed && AreAvilableClassesRegistrationEventTargetsParsed)
            {
                _pageSource = null;
            }
        }
        private void InternalParseAvilableClasses()
        {
            AvilableClasses = UClassParser.ParseClassesTable(_pageDocument.QuerySelector<IHtmlTableElement>("table[id*=RegistrationCoursesSchedule]"));
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
            RegisteredClasses = UClassParser.ParseClassesTable(_pageDocument.QuerySelector<IHtmlTableElement>("table[id*=RegisteredCourses]"));
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

            AvilableClassesRegistrationEventTargets = UClassParser.ParseRegestrationEventTargets(_pageDocument.QuerySelector<IHtmlTableElement>("table[id*=RegistrationCoursesSchedule]"));
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

        //TODO: Rename me!
        public string GetInputFileds(string toolkitScriptMaster, string eventTarget, string eventArgument = "", string txtCourseNum = "") =>
            $"ctl00%24toolkitScriptMaster={toolkitScriptMaster}"
                + "&toolkitScriptMaster_HiddenField="
                + HiddenFieldsText
                + "&ctl00%24ContentPlaceHolder1%24ddlCourseType=-99"
                + "&ctl00%24ContentPlaceHolder1%24ddlCourseLevel=-99"
                + "&ctl00%24ContentPlaceHolder1%24ddlCourseName=-99"
                + $"&ctl00%24ContentPlaceHolder1%24TxtCourseNo={txtCourseNum}"
                + "&ctl00%24ContentPlaceHolder1%24ddlDay=-99"
                + "&ctl00%24ContentPlaceHolder1%24ddlTime=-99"
                + "&ctl00%24ContentPlaceHolder1%24ddlShowRemainOnly=2"
                + "&ctl00%24ContentPlaceHolder1%24ddlHideClosedSections=2"
                + "&ctl00%24lbllang=ar-JO"
                + $"&__EVENTTARGET={eventTarget}"
                + $"&__EVENTARGUMENT={eventArgument}"
                + EncodedHiddenFields;
    }
}
