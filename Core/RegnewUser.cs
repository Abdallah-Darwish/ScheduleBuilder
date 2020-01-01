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
        
        private static readonly Uri RegisteredCoursesUri = new Uri($@"https://{RegnewClient.PsutDomainText}/StudentServices/StudentRegistration.aspx");

        private RegnewUser() { }

        public string Name { get; private set; }
        public DateTime LastLoginTime { get; private set; }
        public async Task<bool> CanRegisterClasses()
        {
            var registrationPage = new StudentRegistrationPage(await Client.GetStringAsync(RegisteredCoursesUri));
            var resultBoxColor = registrationPage.ResultBoxColor;
            return resultBoxColor != ResultBoxColor.Unknown && resultBoxColor != ResultBoxColor.Red;
        }

        [IgnoreDataMember]
        public RegnewClient Client { get; } = RegnewClient.Create();

        private readonly List<UClass> _registeredClasses = new List<UClass>();
        public IReadOnlyList<UClass> RegisteredClasses => _registeredClasses;

        public IReadOnlyList<UClass> AvilableClasses { get; private set; }


        /// <summary>
        /// Creats a <see cref="RegnewUser"/> object and logs him/her in.
        /// </summary>
        /// <param name="userName">The name of the <see cref="RegnewUser"/>(usually his id).</param>
        /// <param name="password">password of the <see cref="RegnewUser"/></param>
        /// <param name="parseAvilableClasses">Whether you want to parse the classes that the user can register or not.</param>
        /// <returns><see cref="RegnewUser"/> if the user is logged-in successfuly or <see cref="null"/> otherwise.</returns>
        public static async Task<RegnewUser> Login(string userName, string password, bool parseAvilableClasses)
        {
            var user = new RegnewUser();
            var regnewPage = new RegnewPage(await user.Client.GetStringAsync(RegnewClient.PsutDomainUri));

            using (StringContent loginRequestContent = user.Client.CreateStringContent(
                "__EVENTTARGET=btnLogin" +
                "&__EVENTARGUMENT=" +
                regnewPage.EncodedHiddenFields +
                $"&tbUsername={WebUtility.UrlEncode(userName)}" +
                $"&tbPsw={WebUtility.UrlEncode(password)}" +
                $"&CbRememberMe=on"))
            {
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

            //check if the regestration page is working or not!
            if (firstRegistrationPageSource.Length > 600)
            {
                var registrationPage = new StudentRegistrationPage(firstRegistrationPageSource);
                user.Name = registrationPage.Username;
                user.LastLoginTime = registrationPage.LastLoginTime;

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
                
                    nextPageRequestContent = user.Client.CreateStringContent(
                        registrationPage.GetInputFileds("ctl00%24ContentPlaceHolder1%24UpdatePanel1%7Cctl00%24ContentPlaceHolder1%24gvRegistrationCoursesSchedule"
                        , "ctl00%24ContentPlaceHolder1%24gvRegistrationCoursesSchedule"
                        , $"Page%24{registrationPage.PageNumber + 1}"));

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
                user.Name = new LoggedInPage(indexPageSource).Username;
            }
            return user;
        }


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
                using (StringContent findCourseRequestContent = Client.CreateStringContent(
                    registrationPage.GetInputFileds("ctl00%24ContentPlaceHolder1%24UpdatePanel1%7Cctl00%24ContentPlaceHolder1%24btnSearch"
                    , "ctl00%24ContentPlaceHolder1%24btnSearch", txtCourseNum: cls.Course.Id.ToString())))
                {
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
                if (registrationPage.AvilableClasses.Any(c => c.Id == cls.Id) == false || registrationPage.AvilableClasses.First().Capacity == registrationPage.AvilableClasses.First().NumberOfRegisteredStudents)
                {
                    processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.ClassIsFullOrDoesntExist, cls));
                    continue;
                }

                await registrationPage.ParseClassesRegistrationEventTargets();

                //register the class
                using (StringContent registerCourseRequestContent = Client.CreateStringContent(
                    registrationPage.GetInputFileds("ctl00%24ContentPlaceHolder1%24UpdatePanel1%7Cctl00%24ContentPlaceHolder1%24gvRegistrationCoursesSchedule%24ctl02%24lbtnAddCourse"
                    , WebUtility.UrlEncode(registrationPage.AvilableClassesRegistrationEventTargets[cls.Id]), txtCourseNum: cls.Course.Id.ToString())))
                {
                    using (var registerCourseResponse = await Client.PostAsync(RegisteredCoursesUri, registerCourseRequestContent))
                    {
                        registerCourseResponse.EnsureSuccessStatusCode();
                        using (var registerCourseResponseContent = registerCourseResponse.Content)
                        {
                            registrationPage = new StudentRegistrationPage(await registerCourseResponseContent.ReadAsStringAsync());
                            if (registrationPage.ResultBoxColor == ResultBoxColor.Green)
                            {
                                _registeredClasses.Add(cls);
                                processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.Succeeded, cls));
                            }
                            else if (registrationPage.ResultBoxColor == ResultBoxColor.Yellow)
                            {
                                processObserver.OnNext(new UClassRegisterationNotification(UClassRegisterationNotificationType.RequiredConfirmation, cls));

                                //This class was registered before and the site is asking if you want to take it again
                                using (var confirmationRequestContent = Client.CreateStringContent(
                                    registrationPage.GetInputFileds("ctl00%24ContentPlaceHolder1%24UpdatePanel1%7Cctl00%24ContentPlaceHolder1%24lbtnYes"
                                    , "ctl00%24ContentPlaceHolder1%24lbtnYes")))
                                {
                                    using (var confirmationResponse = await Client.PostAsync(RegisteredCoursesUri, confirmationRequestContent))
                                    {
                                        confirmationResponse.EnsureSuccessStatusCode();
                                        using (var confirmationResponseContent = confirmationResponse.Content)
                                        {
                                            registrationPage = new StudentRegistrationPage(await confirmationResponseContent.ReadAsStringAsync());
                                            if (registrationPage.ResultBoxColor == ResultBoxColor.Green)
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
