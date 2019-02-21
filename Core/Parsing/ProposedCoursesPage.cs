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

        private static readonly Regex
            rgx_selectedYear = new Regex("\\<option\\sselected=\"selected\"\\svalue=\"(\\d{4})\"\\>\\d{4}\\s-\\s\\d{4}\\<", GlobalRegexOptions.Value),
            rgx_selectedSemester = new Regex("\\<option\\sselected=\"selected\"\\svalue=\"(\\d)\">(الفصل\\sالأول|الفصل\\sالثاني|الفصل\\sالصيفي)\\<", GlobalRegexOptions.Value),
            rgx_pager = new Regex("\\<a\\s*?id=\"rptPager_lnkPage_\\d+\"[^\\<]*(?<pager>rptPager\\$ctl\\d+\\$lnkPage)[^\\>]*\\>(?<pageNumber>\\d+)\\<\\/a\\>", GlobalRegexOptions.Value),
            rgx_pageNumber = new Regex("\\<a\\s*?id=\"rptPager_lnkPage_\\d+\"\\s+class=\"aspNetDisabled\"[^\\>]*\\>(\\d+)\\<\\/a\\>", GlobalRegexOptions.Value);

        private void ParsePage()
        {
            var pageNumberMatch = rgx_pageNumber.Match(_pageSource);
            PageNumber = pageNumberMatch.Success ? int.Parse(pageNumberMatch.Groups[1].Value) : -1;

            AvailablePagers = rgx_pager.Matches(_pageSource).OfType<Match>().Select(m => new ProposedCoursesPager(int.Parse(m.Groups["pageNumber"].Value), m.Groups["pager"].Value)).ToArray();
            SelectedSemester = (USemester)int.Parse(rgx_selectedSemester.Match(_pageSource).Groups[1].Value);
            SelectedYear = int.Parse(rgx_selectedYear.Match(_pageSource).Groups[1].Value);
        }


        private void InternalParseClasses()
        {
            Classes = UClassParser.ParseClassesTable(_pageSource, SelectedYear, SelectedSemester);
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