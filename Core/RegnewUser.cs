using ScheduleBuilder.Core.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScheduleBuilder.Core
{
    public class RegnewUser : IDisposable
    {
        private enum ResultBoxColor
        {
            Green, Red, Yellow, Unknown
        }
        private static readonly Uri RegisteredCoursesUri = new Uri($@"https://{RegnewClient.PsutDomainText}/StudentServices/StudentRegistration.aspx");

        private static readonly Regex
            rgx_userName = new Regex("\\<span\\s*id=\"lblLoggedUserName\"\\>([^\\<]+)", GlobalRegexOptions.Value),
            rgx_resultBoxColor = new Regex("ResultBox\"\\s*style=\"color:([^\\;]+)", GlobalRegexOptions.Value);

        private RegnewUser() { }

        public string Name { get; private set; }
        public async Task<bool> CanRegisterClasses()
        {
            var registrationPageSource = await Client.GetStringAsync(RegisteredCoursesUri);
            var resultBoxColor = ParseResultBoxColor(registrationPageSource);
            return resultBoxColor != ResultBoxColor.Unknown && resultBoxColor != ResultBoxColor.Red;
        }

        [IgnoreDataMember]
        public RegnewClient Client { get; } = RegnewClient.Create();

        private readonly List<UClass> _registeredClasses = new List<UClass>();
        public IReadOnlyList<UClass> RegisteredClasses => _registeredClasses;

        public IReadOnlyList<UClass> AvilableClasses { get; private set; }

        private static ResultBoxColor ParseResultBoxColor(string src)
        {
            string resultBoxColor = rgx_resultBoxColor.Match(src).Groups[1].Value.ToLowerInvariant().Trim();
            switch (resultBoxColor)
            {
                case "green": return ResultBoxColor.Green;
                case "#cccc00": return ResultBoxColor.Yellow;
                case "#c00": return ResultBoxColor.Red;
                default: return ResultBoxColor.Unknown;
            }
        }

        /// <summary>
        /// Creats a <see cref="RegnewUser"/> object and logs him in.
        /// </summary>
        /// <param name="userName">The name of the <see cref="RegnewUser"/>(usually his id).</param>
        /// <param name="password">password of the <see cref="RegnewUser"/></param>
        /// <param name="parseAvilableClasses">Whether you want to parse the classes that the user can register or not.</param>
        /// <returns></returns>
        public static async Task<RegnewUser> Login(string userName, string password, bool parseAvilableClasses)
        {
            var user = new RegnewUser();
            var regnewPage = new RegnewPage(await user.Client.GetStringAsync(RegnewClient.PsutDomainUri));

            using (StringContent loginRequestContent = new StringContent(
                "__EVENTTARGET=btnLogin" +
                "&__EVENTARGUMENT=" +
                $"&__LASTFOCUS={regnewPage.EncodedLastFocus}" +
                $"&__VIEWSTATE={regnewPage.EncodedViewState}" +
                $"&__EVENTVALIDATION={regnewPage.EncodedEventValidation}" +
                $"&__PREVIOUSPAGE={regnewPage.EncodedPreviousPage}" +
                $"&__VIEWSTATEGENERATOR={regnewPage.EncodedViewStateGenerator}" +
                $"&tbUsername={WebUtility.UrlEncode(userName)}" +
                $"&tbPsw={WebUtility.UrlEncode(password)}" +
                $"&CbRememberMe=on", Encoding.UTF8))
            {
                loginRequestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = "utf-8" };
                using (var loginResponse = await user.Client.PostAsync($"https://{RegnewClient.PsutDomainText}/Login.aspx", loginRequestContent))
                {
                    loginResponse.EnsureSuccessStatusCode();
                    //In case the response length is bigger than 1000 this means that the login credintials are invalid
                    if (loginResponse.Content.Headers.ContentLength > 1000)
                    {

                        user.Dispose();
                        return null;
                    }
                }
            }
            
            //We must Request this link to get authentication cookies
            await user.Client.GetAsync($@"https://{RegnewClient.PsutDomainText}/WaitingInfo/WaitingList.aspx");
            string firstRegistrationPageSource = await user.Client.GetStringAsync(RegisteredCoursesUri);
            if (firstRegistrationPageSource.Length > 600)
            {
                user.Name = rgx_userName.Match(firstRegistrationPageSource).Groups[1].Value;
                var registrationPage = new StudentRegistrationPage(firstRegistrationPageSource);
                await registrationPage.ParseRegisteredClasses();
                user._registeredClasses.AddRange(registrationPage.RegisteredClasses);

                if (parseAvilableClasses == false) { return user; }

                List<UClass> avilableClasses = new List<UClass>(150);
                StringContent nextPageRequestContent;
                while (true)
                {
                    await registrationPage.ParseAvilableClasses();
                    avilableClasses.AddRange(registrationPage.AvilableClasses);
                    if (registrationPage.PageNumber == registrationPage.LastPageNumber) { break; }
                    nextPageRequestContent = new StringContent(
    "ctl00%24toolkitScriptMaster=ctl00%24ContentPlaceHolder1%24UpdatePanel1%7Cctl00%24ContentPlaceHolder1%24gvRegistrationCoursesSchedule"
    + "&toolkitScriptMaster_HiddenField="
    + registrationPage.HiddenFieldsText
    + "&ctl00%24ContentPlaceHolder1%24ddlCourseType=-99"
    + "&ctl00%24ContentPlaceHolder1%24ddlCourseLevel=-99"
    + "&ctl00%24ContentPlaceHolder1%24ddlCourseName=-99"
    + "&ctl00%24ContentPlaceHolder1%24TxtCourseNo="
    + "&ctl00%24ContentPlaceHolder1%24ddlDay=-99"
    + "&ctl00%24ContentPlaceHolder1%24ddlTime=-99"
    + "&ctl00%24ContentPlaceHolder1%24ddlShowRemainOnly=2"
    + "&ctl00%24ContentPlaceHolder1%24ddlHideClosedSections=2"
    + "&ctl00%24lbllang=ar-JO"
    + "&__EVENTTARGET=ctl00%24ContentPlaceHolder1%24gvRegistrationCoursesSchedule"
    + $"&__EVENTARGUMENT=Page%24{registrationPage.PageNumber + 1}"
    + $"&__LASTFOCUS={registrationPage.EncodedLastFocus}"
    + $"&__VIEWSTATE={registrationPage.EncodedViewState}"
    + $"&__VIEWSTATEGENERATOR={registrationPage.EncodedViewStateGenerator}"
    + $"&__EVENTVALIDATION={registrationPage.EncodedEventValidation}"
    + "&__ASYNCPOST=true"
    , Encoding.UTF8);
                    nextPageRequestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = "utf-8" };
                    using (var nextPageResponse = await user.Client.PostAsync(RegisteredCoursesUri, nextPageRequestContent))
                    {
                        nextPageResponse.EnsureSuccessStatusCode();
                        registrationPage = new StudentRegistrationPage(await nextPageResponse.Content.ReadAsStringAsync());
                    }
                    nextPageRequestContent.Dispose();
                }
                user.AvilableClasses = avilableClasses;
            }
            else
            {
                var cookies = user.Client.Cookies.GetCookies(RegnewClient.PsutDomainUri).OfType<Cookie>().ToArray();
                user.AvilableClasses = new List<UClass>();
                string indexPageSource = "";
                using (var indexPageResponse = await user.Client.GetAsync(RegnewClient.IndexPageUri))
                {
                    indexPageSource = await indexPageResponse.Content.ReadAsStringAsync();
                }
                user.Name = rgx_userName.Match(indexPageSource).Groups[1].Value;
            }
            return user;
        }



        /// <returns>The registered classes.</returns>
        public async Task RegisterClasses(IObserver<UClassRegisterationNotification> processObserver, IEnumerable<UClass> classes)
        {
            StudentRegistrationPage registrationPage = new StudentRegistrationPage(await Client.GetStringAsync(RegisteredCoursesUri));
            var alreadyRegisteredClasses = RegisteredClasses.Intersect(classes);
            foreach (var cls in alreadyRegisteredClasses)
            {
                processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.Succeeded, cls));
            }

            foreach (var cls in classes.Except(RegisteredClasses))
            {
                processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.Processing, cls));
                //Find the course
                using (StringContent findCourseRequestContent = new StringContent(
"ctl00%24toolkitScriptMaster=ctl00%24ContentPlaceHolder1%24UpdatePanel1%7Cctl00%24ContentPlaceHolder1%24btnSearch"
+ "&toolkitScriptMaster_HiddenField="
//+ registrationPage.HiddenFieldsText
+ "&ctl00%24ContentPlaceHolder1%24ddlCourseType=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlCourseLevel=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlCourseName=-99"
+ $"&ctl00%24ContentPlaceHolder1%24TxtCourseNo={cls.Course.Id}"
+ "&ctl00%24ContentPlaceHolder1%24ddlDay=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlTime=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlShowRemainOnly=2"
//set to true
+ "&ctl00%24ContentPlaceHolder1%24ddlHideClosedSections=1"
+ "&ctl00%24lbllang=ar-JO"
+ "&__EVENTTARGET=ctl00%24ContentPlaceHolder1%24btnSearch"
+ $"&__EVENTARGUMENT="
+ $"&__LASTFOCUS={registrationPage.EncodedLastFocus}"
+ $"&__VIEWSTATE={registrationPage.EncodedViewState}"
+ $"&__VIEWSTATEGENERATOR={registrationPage.EncodedViewStateGenerator}"
+ $"&__EVENTVALIDATION={registrationPage.EncodedEventValidation}"
+ "&__ASYNCPOST=true"
, Encoding.UTF8))
                {
                    findCourseRequestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = "utf-8" };
                    using (var findCourseResponse = await Client.PostAsync(RegisteredCoursesUri, findCourseRequestContent))
                    {
                        findCourseResponse.EnsureSuccessStatusCode();
                        using (var findCourseResponseContent = findCourseResponse.Content)
                        {
                            registrationPage = new StudentRegistrationPage(await findCourseResponseContent.ReadAsStringAsync());
                        }
                    }
                }

                await registrationPage.ParseAvilableClasses();

                //check if the requested class have space
                if (registrationPage.AvilableClasses.Any(c => c.Id == cls.Id) == false)
                {
                    processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.ClassIsFullOrDoesntExist, cls));
                    continue;
                }

                await registrationPage.ParseClassesRegistrationEventTargets();

                //register the class
                using (StringContent registerCourseRequestContent = new StringContent(
"ctl00%24toolkitScriptMaster=ctl00%24ContentPlaceHolder1%24UpdatePanel1%7Cctl00%24ContentPlaceHolder1%24gvRegistrationCoursesSchedule%24ctl02%24lbtnAddCourse"
+ "&toolkitScriptMaster_HiddenField="
//+ registrationPage.HiddenFieldsText
+ "&ctl00%24ContentPlaceHolder1%24ddlCourseType=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlCourseLevel=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlCourseName=-99"
+ $"&ctl00%24ContentPlaceHolder1%24TxtCourseNo={cls.Course.Id}"
+ "&ctl00%24ContentPlaceHolder1%24ddlDay=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlTime=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlShowRemainOnly=2"
+ "&ctl00%24ContentPlaceHolder1%24ddlHideClosedSections=1"
+ "&ctl00%24lbllang=ar-JO"
+ $"&__EVENTTARGET={WebUtility.UrlEncode(registrationPage.AvilableClassesRegistrationEventTargets[cls.Id])}"
+ $"&__EVENTARGUMENT="
+ $"&__LASTFOCUS={registrationPage.EncodedLastFocus}"
+ $"&__VIEWSTATE={registrationPage.EncodedViewState}"
+ $"&__VIEWSTATEGENERATOR={registrationPage.EncodedViewStateGenerator}"
+ $"&__EVENTVALIDATION={registrationPage.EncodedEventValidation}"
+ "&__ASYNCPOST=true"
    , Encoding.UTF8))
                {
                    registerCourseRequestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = "utf-8" };
                    using (var registerCourseResponse = await Client.PostAsync(RegisteredCoursesUri, registerCourseRequestContent))
                    {
                        registerCourseResponse.EnsureSuccessStatusCode();
                        using (var registerCourseResponseContent = registerCourseResponse.Content)
                        {
                            string contentText = await registerCourseResponseContent.ReadAsStringAsync();
                            var resultBoxColor = ParseResultBoxColor(contentText);
                            registrationPage = new StudentRegistrationPage(contentText);
                            contentText = null;
                            if (resultBoxColor == ResultBoxColor.Green)
                            {
                                _registeredClasses.Add(cls);
                                processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.Succeeded, cls));
                            }
                            else if (resultBoxColor == ResultBoxColor.Yellow)
                            {
                                processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.RequiredConfirmation, cls));

                                //This class was registered before and the site is asking if you want to take it again
                                using (var confirmationRequestContent = new StringContent(
"ctl00%24toolkitScriptMaster=ctl00%24ContentPlaceHolder1%24UpdatePanel1%7Cctl00%24ContentPlaceHolder1%24lbtnYes"
+ "&toolkitScriptMaster_HiddenField="
//+ registrationPage.HiddenFieldsText
+ "&ctl00%24ContentPlaceHolder1%24ddlCourseType=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlCourseLevel=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlCourseName=-99"
+ $"&ctl00%24ContentPlaceHolder1%24TxtCourseNo="
+ "&ctl00%24ContentPlaceHolder1%24ddlDay=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlTime=-99"
+ "&ctl00%24ContentPlaceHolder1%24ddlShowRemainOnly=2"
+ "&ctl00%24ContentPlaceHolder1%24ddlHideClosedSections=1"
+ "&ctl00%24lbllang=ar-JO"
+ $"&__EVENTTARGET=ctl00%24ContentPlaceHolder1%24lbtnYes"
+ $"&__EVENTARGUMENT="
+ $"&__LASTFOCUS={registrationPage.EncodedLastFocus}"
+ $"&__VIEWSTATE={registrationPage.EncodedViewState}"
+ $"&__VIEWSTATEGENERATOR={registrationPage.EncodedViewStateGenerator}"
+ $"&__EVENTVALIDATION={registrationPage.EncodedEventValidation}"
+ "&__ASYNCPOST=true", Encoding.UTF8))
                                {
                                    confirmationRequestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = "utf-8" };
                                    using (var confirmationResponse = await Client.PostAsync(RegisteredCoursesUri, confirmationRequestContent))
                                    {
                                        confirmationResponse.EnsureSuccessStatusCode();
                                        using (var confirmationResponseContent = confirmationResponse.Content)
                                        {
                                            contentText = await confirmationResponseContent.ReadAsStringAsync();
                                            resultBoxColor = ParseResultBoxColor(contentText);
                                            registrationPage = new StudentRegistrationPage(contentText);
                                            contentText = null;
                                            if (resultBoxColor == ResultBoxColor.Green)
                                            {
                                                _registeredClasses.Add(cls);
                                                processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.SucceededAfterConfirmation, cls));
                                            }
                                            else
                                            {
                                                processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.Failed, cls));
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.Failed, cls));
                            }

                        }
                    }
                }
            }
            processObserver.OnCompleted();

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Client.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
