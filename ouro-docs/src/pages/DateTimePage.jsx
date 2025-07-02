import React from 'react';
import CodeBlock from '../components/CodeBlock';
import Callout from '../components/Callout';

const DateTimePage = () => {
  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-4xl font-bold mb-4">DateTime Operations</h1>
        <p className="text-lg text-gray-600 dark:text-gray-400">
          Comprehensive date and time handling with natural language support.
        </p>
      </div>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Overview</h2>
        <p>
          Ouroboros provides an intuitive and powerful DateTime API that combines traditional 
          programming approaches with natural language understanding. The language handles timezones, 
          formatting, and calculations with ease.
        </p>
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Creating DateTime Objects</h2>
        
        <div className="space-y-4">
          <h3 className="text-2xl font-semibold">Current Date and Time</h3>
          
          <CodeBlock 
            code={`// Get current date and time
now := DateTime.Now
print($"Current time: {now}")

// Get today's date (time set to midnight)
today := DateTime.Today
print($"Today: {today}")

// Get UTC time
utcNow := DateTime.UtcNow
print($"UTC time: {utcNow}")`}
            title="Current DateTime"
          />
        </div>

        <div className="space-y-4">
          <h3 className="text-2xl font-semibold">Creating Specific Dates</h3>
          
          <CodeBlock 
            code={`// Create specific date
birthday := new DateTime(1990, 7, 15)
print($"Birthday: {birthday}")

// Create date with time
appointment := new DateTime(2024, 3, 20, 14, 30, 0)
print($"Appointment: {appointment}")

// Parse from string
meeting := DateTime.Parse("2024-03-15 10:30:00")
event := DateTime.Parse("March 15, 2024 at 2:30 PM")

// Natural language parsing
deadline := DateTime.Parse("next Friday at 5pm")
reminder := DateTime.Parse("in 3 days")`}
            title="Creating DateTime Objects"
          />
        </div>
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">DateTime Properties</h2>
        
        <CodeBlock 
          code={`dt := DateTime.Now

// Date components
year := dt.Year          // 2024
month := dt.Month        // 3 (March)
day := dt.Day           // 15
dayOfWeek := dt.DayOfWeek    // Friday
dayOfYear := dt.DayOfYear    // 74

// Time components
hour := dt.Hour         // 14 (2 PM)
minute := dt.Minute     // 30
second := dt.Second     // 45
millisecond := dt.Millisecond // 123

// Useful properties
isLeapYear := dt.IsLeapYear
quarter := dt.Quarter   // 1-4
weekOfYear := dt.WeekOfYear

print($"Date: {dt.Year}-{dt.Month:D2}-{dt.Day:D2}")
print($"Time: {dt.Hour:D2}:{dt.Minute:D2}:{dt.Second:D2}")
print($"Day of week: {dt.DayOfWeek}")
print($"Is leap year: {isLeapYear}")`}
          title="DateTime Properties"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Date Arithmetic</h2>
        
        <div className="space-y-4">
          <h3 className="text-2xl font-semibold">Adding and Subtracting Time</h3>
          
          <CodeBlock 
            code={`now := DateTime.Now

// Add time spans
tomorrow := now.AddDays(1)
nextWeek := now.AddDays(7)
nextMonth := now.AddMonths(1)
nextYear := now.AddYears(1)

// Add specific time units
later := now.AddHours(3).AddMinutes(30)
meeting := now.AddBusinessDays(5)  // Skip weekends

// Subtract time
yesterday := now.AddDays(-1)
lastMonth := now.AddMonths(-1)

// Using TimeSpan
duration := TimeSpan.FromHours(2.5)
futureTime := now + duration

// Natural language arithmetic
deadline := now + "3 days 4 hours"
past := now - "1 week"`}
            title="Date Arithmetic"
          />
        </div>

        <div className="space-y-4">
          <h3 className="text-2xl font-semibold">Date Differences</h3>
          
          <CodeBlock 
            code={`start := DateTime.Parse("2024-01-01")
end := DateTime.Parse("2024-03-15")

// Calculate difference
diff := end - start
print($"Days: {diff.TotalDays}")
print($"Hours: {diff.TotalHours}")
print($"Weeks: {diff.TotalDays / 7}")

// Business days calculation
businessDays := DateTime.GetBusinessDays(start, end)
print($"Business days: {businessDays}")

// Age calculation
birthDate := DateTime.Parse("1990-07-15")
age := DateTime.GetAge(birthDate)
print($"Age: {age.Years} years, {age.Months} months, {age.Days} days")`}
            title="Date Differences"
          />
        </div>
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Formatting</h2>
        
        <CodeBlock 
          code={`dt := DateTime.Now

// Standard formats
print(dt.ToString("yyyy-MM-dd"))           // 2024-03-15
print(dt.ToString("dd/MM/yyyy"))           // 15/03/2024
print(dt.ToString("MMM dd, yyyy"))         // Mar 15, 2024
print(dt.ToString("dddd, MMMM dd, yyyy"))  // Friday, March 15, 2024

// Time formats
print(dt.ToString("HH:mm:ss"))             // 14:30:45
print(dt.ToString("h:mm tt"))              // 2:30 PM
print(dt.ToString("HH:mm:ss.fff"))         // 14:30:45.123

// Full date/time
print(dt.ToString("F"))  // Friday, March 15, 2024 2:30:45 PM
print(dt.ToString("G"))  // 3/15/2024 2:30:45 PM
print(dt.ToString("R"))  // Fri, 15 Mar 2024 14:30:45 GMT

// Custom formats
print(dt.ToString("yyyy年MM月dd日"))       // 2024年03月15日

// Relative formatting
print(dt.ToRelativeString())  // "2 hours ago", "in 3 days", etc.
print(dt.ToFriendlyString())  // "Today at 2:30 PM", "Yesterday", etc.`}
          title="DateTime Formatting"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Timezone Handling</h2>
        
        <CodeBlock 
          code={`// Get timezone info
local := DateTime.Now
localZone := TimeZone.Local
print($"Local timezone: {localZone.DisplayName}")
print($"UTC offset: {localZone.GetUtcOffset(local)}")

// Convert between timezones
utc := local.ToUniversalTime()
eastern := TimeZone.Convert(utc, "Eastern Standard Time")
pacific := TimeZone.Convert(utc, "Pacific Standard Time")
tokyo := TimeZone.Convert(utc, "Tokyo Standard Time")

print($"Local: {local}")
print($"UTC: {utc}")
print($"Eastern: {eastern}")
print($"Tokyo: {tokyo}")

// Create DateTime in specific timezone
meeting := new DateTimeOffset(2024, 3, 15, 10, 0, 0, 
                              TimeZone.GetTimeZone("Europe/London"))

// Handle daylight saving time
isDST := TimeZone.Local.IsDaylightSavingTime(local)
print($"Is DST active: {isDST}")`}
          title="Timezone Operations"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Natural Language DateTime</h2>
        
        <Callout variant="tip" title="Natural Language Support">
          Ouroboros understands many natural language date/time expressions.
        </Callout>
        
        <CodeBlock 
          code={`// Natural language parsing
tomorrow := DateTime.Parse("tomorrow at 3pm")
nextWeek := DateTime.Parse("next Monday")
deadline := DateTime.Parse("in 2 weeks")
meeting := DateTime.Parse("March 15th at 2:30")

// Relative expressions
soon := DateTime.Parse("in 30 minutes")
recently := DateTime.Parse("2 hours ago")
endOfMonth := DateTime.Parse("last day of this month")
startOfYear := DateTime.Parse("beginning of next year")

// Complex expressions
vacation := DateTime.Parse("first Monday of July 2024")
payday := DateTime.Parse("last Friday of the month")
quarterly := DateTime.Parse("end of Q2 2024")

// Natural language conditions
if DateTime.Now is after "5:00 PM" {
    print("Working hours are over")
}

if meeting is within next week {
    sendReminder()
}`}
          title="Natural Language DateTime"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Working with Time Periods</h2>
        
        <CodeBlock 
          code={`// TimeSpan creation
duration := TimeSpan.FromHours(2.5)
workDay := TimeSpan.FromHours(8)
break := TimeSpan.FromMinutes(15)

// TimeSpan from difference
start := DateTime.Parse("09:00")
end := DateTime.Parse("17:30")
worked := end - start

print($"Worked: {worked.Hours}h {worked.Minutes}m")

// Period calculations
period := new DatePeriod {
    Start = DateTime.Parse("2024-01-01"),
    End = DateTime.Parse("2024-03-31")
}

print($"Quarter duration: {period.Days} days")
print($"Business days: {period.BusinessDays}")
print($"Weekends: {period.Weekends}")

// Recurring dates
recurring := new Recurrence {
    Pattern = RecurrencePattern.Weekly,
    Interval = 2,  // Every 2 weeks
    DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday],
    StartDate = DateTime.Today
}

nextOccurrences := recurring.GetOccurrences(10)
for date in nextOccurrences {
    print($"Next occurrence: {date}")
}`}
          title="Time Periods and Recurrence"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Calendar Operations</h2>
        
        <CodeBlock 
          code={`// Get calendar information
year := 2024
month := 3

daysInMonth := DateTime.DaysInMonth(year, month)
firstDay := new DateTime(year, month, 1)
lastDay := new DateTime(year, month, daysInMonth)

print($"Days in {firstDay:MMMM yyyy}: {daysInMonth}")
print($"First day: {firstDay:dddd}")
print($"Last day: {lastDay:dddd}")

// Week calculations
weekNumber := DateTime.Now.GetIso8601WeekOfYear()
firstDayOfWeek := DateTime.Now.StartOfWeek()
lastDayOfWeek := DateTime.Now.EndOfWeek()

// Holiday checking (with custom holiday provider)
holidays := HolidayProvider.GetHolidays(year, "US")
isHoliday := holidays.Contains(DateTime.Today)

if isHoliday {
    holiday := holidays.GetHoliday(DateTime.Today)
    print($"Today is {holiday.Name}")
}

// Business day checking
if DateTime.Today.IsBusinessDay() {
    print("Today is a business day")
}`}
          title="Calendar Operations"
        />
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Performance and Best Practices</h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Callout variant="tip" title="Use UTC for storage">
            Always store dates in UTC and convert to local time for display.
          </Callout>
          
          <Callout variant="tip" title="Immutable operations">
            DateTime objects are immutable; operations return new instances.
          </Callout>
          
          <Callout variant="warning" title="Timezone awareness">
            Be explicit about timezones when dealing with global applications.
          </Callout>
          
          <Callout variant="tip" title="Format consistently">
            Use ISO 8601 format (yyyy-MM-dd) for data exchange.
          </Callout>
        </div>

        <div className="mt-6">
          <h3 className="text-xl font-semibold mb-4">Common Patterns</h3>
          
          <CodeBlock 
            code={`// Start/end of period helpers
today := DateTime.Today
startOfWeek := today.StartOfWeek()
endOfWeek := today.EndOfWeek()
startOfMonth := today.StartOfMonth()
endOfMonth := today.EndOfMonth()
startOfYear := today.StartOfYear()
endOfYear := today.EndOfYear()

// Date range validation
func isDateInRange(date: DateTime, start: DateTime, end: DateTime) -> bool {
    return date >= start and date <= end
}

// Age-appropriate content
func canViewContent(birthDate: DateTime, minAge: int) -> bool {
    age := DateTime.GetAge(birthDate)
    return age.Years >= minAge
}

// Booking system example
func getAvailableSlots(date: DateTime, duration: TimeSpan) -> List<DateTime> {
    slots := []
    current := date.Date.AddHours(9)  // Start at 9 AM
    endTime := date.Date.AddHours(17) // End at 5 PM
    
    while current + duration <= endTime {
        if isSlotAvailable(current, duration) {
            slots.Add(current)
        }
        current = current.AddMinutes(30)  // 30-minute intervals
    }
    
    return slots
}`}
            title="Common DateTime Patterns"
          />
        </div>
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Advanced Features</h2>
        
        <CodeBlock 
          code={`// High-precision timing
stopwatch := Stopwatch.StartNew()
performOperation()
stopwatch.Stop()
print($"Operation took: {stopwatch.ElapsedMilliseconds}ms")

// Date/time parsing with culture
germanDate := DateTime.Parse("15.03.2024", CultureInfo.German)
usDate := DateTime.Parse("03/15/2024", CultureInfo.US)

// Custom calendar systems
persianCalendar := new PersianCalendar()
persianDate := persianCalendar.GetYear(DateTime.Now)

// Unix timestamp conversion
timestamp := DateTime.Now.ToUnixTimestamp()
dateFromTimestamp := DateTime.FromUnixTimestamp(timestamp)

// Astronomical calculations
sunrise := Astronomy.GetSunrise(latitude, longitude, DateTime.Today)
sunset := Astronomy.GetSunset(latitude, longitude, DateTime.Today)
moonPhase := Astronomy.GetMoonPhase(DateTime.Today)

print($"Sunrise: {sunrise:HH:mm}")
print($"Sunset: {sunset:HH:mm}")
print($"Moon phase: {moonPhase}")`}
          title="Advanced DateTime Features"
        />
      </section>
    </div>
  );
};

export default DateTimePage; 