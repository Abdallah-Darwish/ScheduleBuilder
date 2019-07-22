using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScheduleBuilder.Core.Parsing
{
    public static class ProposedCoursesParser
    {
        private static readonly RegnewClient _regnewClient = RegnewClient.Create();
        private static readonly Uri ProposedCoursesUri = new Uri($@"https://{RegnewClient.PsutDomainText}/ProposedCoursesPublic.aspx");

        public static int CurrentYear { get; }
        public static USemester CurrentSemester { get; }
        static ProposedCoursesParser()
        {
            string classesPageSource = _regnewClient.GetStringAsync(ProposedCoursesUri).Result;
            var classesPage = new ProposedCoursesPage(classesPageSource);
            CurrentYear = classesPage.SelectedYear;
            CurrentSemester = classesPage.SelectedSemester;
        }
        /// <summary>
        /// Gets classes from the internet and parse them.
        /// </summary>
        /// <param name="semester">Which sememster to get its classes.</param>
        /// <param name="year">Which year to get its classes in <paramref name="semester"/></param>
        public static async Task<IEnumerable<UClass>> GetClasses(USemester? semester, int? year)
        {
            //wait until _regnewClient isn't used by any other task.
            //I am only using one instance of "HttpClient" for performance reasons
            //And I am using it only by one task per time because of cookie "ASP.NET_SessionId"
            await _regnewClient.SyncRoot.WaitAsync();
            try
            {
                //delete the "ASP.NET_SessionId" cookie
                //really there is no reason for deleting it, but fuck it!
                var sessionId = _regnewClient.Cookies.GetCookies(RegnewClient.PsutDomainUri).Cast<Cookie>().FirstOrDefault(cookie => cookie.Name.Equals("asp.net_sessionid", StringComparison.OrdinalIgnoreCase));
                if (sessionId != null)
                {
                    sessionId.Expired = true;
                }
                //list to store pages while they are being parsed
                List<ProposedCoursesPage> pages = new List<ProposedCoursesPage>(50);
                string classesPageSource = await _regnewClient.GetStringAsync(ProposedCoursesUri);
                var classesPage = new ProposedCoursesPage(classesPageSource);

                if (year.HasValue == false)
                {
                    year = classesPage.SelectedYear;
                }
                if (semester.HasValue == false)
                {
                    semester = classesPage.SelectedSemester;
                }

                //The string that's sent in the request to indicate which page to recive.
                string pager = "btnSearch";

                //pager will be null when we reach the last page
                while (pager != null)
                {
                    //DONT USE classesPage.EncodedHiddenFields cause it will include "__PREVIOUSPAGE" and that shit will fuck up the whole request
                    StringContent content = _regnewClient.CreateStringContent($"ScriptManager1=UpdatePanel1%7C{pager}" +
$"&__EVENTTARGET={pager}" +
classesPage.EncodedHiddenFields +
$"&ddlStudyYear={year}" +
$"&ddlStudySemister={(int)semester}" +
"&ddlCollege=-99" +
"&DDLDiv=-99" +
"&tbCourseNo=" +
"&ddlQualifType=-99" +
"&tbCourseName=" +
"&ddlMajors=-99" +
"&ddlCourseType=-99" +
"&ddlLecNo=-99" +
"&ddlOrderBy=-99" +
 "&");

                    // content.Headers.Add("Referer", "https://regnew.psut.edu.jo/ProposedCoursesPublic.aspx");
                    //_regnewClient.DefaultRequestHeaders.Referrer = ProposedCoursesUri;
                    using (var regnewResponseMessage = await _regnewClient.PostAsync(ProposedCoursesUri, content))
                    {
                        regnewResponseMessage.EnsureSuccessStatusCode();
                        using (var regnewResponseMessageContent = regnewResponseMessage.Content as StreamContent)
                        {
                            classesPageSource = await regnewResponseMessageContent.ReadAsStringAsync();
                        }
                        classesPage = new ProposedCoursesPage(classesPageSource);
                    }
                    //let it parse in the background
                    classesPage.ParseClasses();
                    pages.Add(classesPage);
                    //check if there is any page after this and get its pager
                    pager = classesPage.AvailablePagers.FirstOrDefault(p => p.PageNumber > classesPage.PageNumber)?.Pager;
                }
                //list to store the parsed classes in
                List<UClass> classes = new List<UClass>(300);
                //get the parsed classes from there pages and store them in the list
                foreach (var page in pages)
                {
                    if (!page.AreClassesParsed)
                    {
                        await page.ParseClasses();
                    }
                    classes.AddRange(page.Classes);
                }
                return classes;
            }
            finally
            {
                _regnewClient.SyncRoot.Set();
            }
        }
        /// <summary>
        /// Gets the avilable courses from regnew.
        /// </summary>
        /// <param name="semester">Which sememster to get its courses.</param>
        /// <param name="year">Which year to get its courses in <paramref name="semester"/></param>
        public static async Task<IEnumerable<UCourse>> GetCourses(USemester? semester, int? year) => (await GetClasses(semester, year)).Select(c => c.Course).Distinct();
    }
}
