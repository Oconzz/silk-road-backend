using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Business.Services
{
    public class RecurrenceService
    {
        /// <summary>
        /// Expand a RRULE into concrete occurrence dates starting from a given date.
        /// All logic stays in the backend — the mobile just sends the RRULE string.
        /// </summary>
        public List<DateTime> GetOccurrences(
            DateTime startDate,
            string rruleString,
            DateTime? recurrenceEndDate = null,
            string? timeZone = null,
            int maxOccurrences = 52)
        {
            if (string.IsNullOrWhiteSpace(rruleString))
                return new List<DateTime> { startDate };

            var calendar = new Calendar();
            var calendarEvent = new CalendarEvent
            {
                DtStart = new CalDateTime(startDate, timeZone ?? "UTC"),
                Duration = TimeSpan.FromHours(1),
            };

            // Parse and add the recurrence rule
            var rrule = new RecurrencePattern(rruleString);
            calendarEvent.RecurrenceRules.Add(rrule);

            calendar.Events.Add(calendarEvent);

            // Define the search window
            var searchEnd = recurrenceEndDate ?? startDate.AddYears(1);
            var occurrences = calendar.GetOccurrences(startDate, searchEnd)
                .Take(maxOccurrences)
                .Select(o => o.Period.StartTime.AsDateTimeOffset.DateTime)
                .ToList();

            return occurrences;
        }

        /// <summary>
        /// Validate that a RRULE string is well-formed.
        /// </summary>
        public bool ValidateRRule(string rruleString)
        {
            if (string.IsNullOrWhiteSpace(rruleString))
                return false;

            try
            {
                var _ = new RecurrencePattern(rruleString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Build an RRULE string from simple parameters.
        /// This is a convenience helper — the mobile can also build its own.
        /// </summary>
        public string BuildRRule(string frequency, int interval = 1, string? byDay = null, int? count = null, DateTime? until = null)
        {
            // frequency: DAILY, WEEKLY, MONTHLY, YEARLY
            var parts = new List<string>
            {
                $"FREQ={frequency.ToUpper()}",
                $"INTERVAL={interval}"
            };

            if (!string.IsNullOrEmpty(byDay))
                parts.Add($"BYDAY={byDay}");

            if (count.HasValue)
                parts.Add($"COUNT={count.Value}");
            else if (until.HasValue)
                parts.Add($"UNTIL={until.Value:yyyyMMddTHHmmssZ}");

            return string.Join(";", parts);
        }
    }
}
