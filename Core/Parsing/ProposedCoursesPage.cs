using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace ScheduleBuilder.Core.Parsing
{
    internal class ProposedCoursesPage : RegnewPage, IEquatable<ProposedCoursesPage>
    {
        /// <summary>
        /// Classes in the page.
        /// WARNING YOU MUST CALL <see cref="ParseClasses"/> before using <see cref="Classes"/> or else it will return <see cref="null"/>.
        /// </summary>
        public IReadOnlyList<UClass> Classes { get; private set; } = null;

        public USemester SelectedSemester { get; private set; }
        public int SelectedYear { get; private set; }

        public IReadOnlyList<ProposedCoursesPager> AvailablePagers { get; private set; }
        public int PageNumber { get; private set; } = 0;

        public ProposedCoursesPage(string pageSource) : base(pageSource)
        {
            ParsePage();
        }

        public bool AreClassesParsed { get; private set; } = false;

        readonly object _parsingClassesLock = new object();
        Task _parsingClassesTask;

        private void ParsePage()
        {
            var pagers = _pageDocument.QuerySelectorAll<IHtmlAnchorElement>("a[id*=rptPager_lnkPage]").Where(p => p.Text.Contains('-') == false).ToArray();
            PageNumber = int.Parse(pagers.FirstOrDefault(p => p.ClassName?.Equals("aspNetDisabled", StringComparison.OrdinalIgnoreCase) ?? false)?.Text ?? "-1");

            AvailablePagers = pagers.Select(p =>
            {
                if (string.IsNullOrWhiteSpace(p.Href)) { return new ProposedCoursesPager(int.Parse(p.Text), string.Empty); }
                int firstDQuoteIndex = p.Href.IndexOf("\"");
                int secondDQuoteIndex = p.Href.IndexOf("\"", firstDQuoteIndex + 1);
                return new ProposedCoursesPager(int.Parse(p.Text), p.Href.Substring(firstDQuoteIndex + 1, secondDQuoteIndex - firstDQuoteIndex - 1));
            }).ToArray();
            SelectedSemester = (USemester)int.Parse(_pageDocument.QuerySelector<IHtmlSelectElement>(@"select[name=ddlStudySemister]").SelectedOptions.First().Value); ;
            SelectedYear = int.Parse(_pageDocument.QuerySelector<IHtmlSelectElement>(@"select[name=ddlStudyYear]").SelectedOptions.First().Value);
        }


        private void InternalParseClasses()
        {
            Classes = UClassParser.ParseClassesTable(_pageDocument.QuerySelector<IHtmlTableElement>("table[id*=ProposedCoursesSchedule]"), SelectedYear, SelectedSemester);
            _pageSource = null;
            AreClassesParsed = true;

        }
        public Task ParseClasses()
        {
            lock (_parsingClassesLock)
            {
                if (_parsingClassesTask == null)
                {
                    _parsingClassesTask = Task.Run(() => InternalParseClasses());
                }
                return _parsingClassesTask;
            }
        }
        /// <remarks>Not 100% right because SelectedYear and SelectedSemester could be different from the year and semester of the classes in the page.</remarks>
        public override int GetHashCode() => HashCodeHelper.CombineHashCodes(SelectedYear, (int)SelectedSemester, PageNumber);

        public override bool Equals(object obj)
        {
            if (obj is ProposedCoursesPage page) { return Equals(page); }
            return false;
        }

        public bool Equals(ProposedCoursesPage other)
        {
            if (other is null) { return false; }
            return GetHashCode() == other.GetHashCode();
        }
    }
}