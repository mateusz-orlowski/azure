using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Exchange.WebServices.Data;

namespace ExchangeManipulator
{
    public class ExchangeHandler
    {
        public static List<Availability> GetCalendarInfo(string username)
        {
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2010_SP2);
            service.Credentials = new WebCredentials("mateusz.orlowski@atos.net", Settings.Password);
            service.TraceEnabled = true;
            service.TraceFlags = TraceFlags.All;
            service.AutodiscoverUrl("mateusz.orlowski@atos.net", RedirectionUrlValidationCallback);



            DateTime startDate = DateTime.Now;
            DateTime endDate = startDate.AddDays(2);

            List<AttendeeInfo> attendees = new List<AttendeeInfo>();


            attendees.Add(new AttendeeInfo()
            {
                SmtpAddress = username,
                AttendeeType = MeetingAttendeeType.Required
            });

            // Specify availability options.
            AvailabilityOptions myOptions = new AvailabilityOptions();
            myOptions.MeetingDuration = 30;
            myOptions.RequestedFreeBusyView = FreeBusyViewType.FreeBusy;
            var list = new List<Availability>();

            try
            {
                // Return a set of free/busy times.
                GetUserAvailabilityResults freeBusyResults = service.GetUserAvailability(attendees,
                                                                                     new TimeWindow(DateTime.Now, DateTime.Now.AddDays(1)),
                                                                                         AvailabilityData.FreeBusy,
                                                                                         myOptions);
                // Display available meeting times.
                Console.WriteLine("Availability for {0}", attendees[0].SmtpAddress);
                Console.WriteLine();
                
                if (freeBusyResults.AttendeesAvailability.Count > 0)
                {
                    foreach (CalendarEvent calendarItem in freeBusyResults.AttendeesAvailability[0].CalendarEvents)
                    {
                        list.Add(new Availability()
                        {
                            StartDate = calendarItem.StartTime,
                            EndDate = calendarItem.EndTime,
                            Status = calendarItem.FreeBusyStatus.ToString()
                        });
                    }
                }

                return list.OrderBy(o => o.StartDate).ToList();
            }
            catch (Exception ex)
            {
                return list;
            }

            /*
            // Initialize the calendar folder object with only the folder ID. 
            //CalendarFolder calendar = CalendarFolder.Bind(service, WellKnownFolderName.Calendar, new PropertySet());
            var folderIdFromCalendar = new FolderId(WellKnownFolderName.Calendar, username);
            
            CalendarFolder calendar = CalendarFolder.Bind(service, folderIdFromCalendar, new PropertySet());

            // Set the start and end time and number of appointments to retrieve.
            CalendarView ccView = new CalendarView(startDate, endDate, NUM_APPTS);

            // Limit the properties returned to the appointment's subject, start time, and end time.
            ccView.PropertySet = new PropertySet(AppointmentSchema.Subject, AppointmentSchema.Start, AppointmentSchema.End);

            // Retrieve a collection of appointments by using the calendar view.
            FindItemsResults<Appointment> appointments2 = calendar.FindAppointments(ccView);

            Console.WriteLine("\nThe first " + NUM_APPTS + " appointments on your calendar from " + startDate.Date.ToShortDateString() +
                              " to " + endDate.Date.ToShortDateString() + " are: \n");

            foreach (Appointment a in appointments2)
            {
                Console.Write("Subject: " + a.Subject.ToString() + " ");
                Console.Write("Start: " + a.Start.ToString() + " ");
                Console.Write("End: " + a.End.ToString());
                Console.WriteLine();
            }
            */

        }

        private static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            bool result = false;
            Uri redirectionUri = new Uri(redirectionUrl);
            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                result = true;
            }
            return result;
        }
    }
}
