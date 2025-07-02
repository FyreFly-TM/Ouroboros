using System;
using System.Globalization;
using Ouro.Core;

namespace Ouro.StdLib.System
{
    /// <summary>
    /// Represents an instant in time, typically expressed as a date and time of day
    /// </summary>
    public struct DateTime : IComparable<DateTime>, IEquatable<DateTime>
    {
        private readonly long ticks;
        
        /// <summary>
        /// Initialize from System.DateTime
        /// </summary>
        internal DateTime(global::System.DateTime value)
        {
            this.ticks = value.Ticks;
        }
        
        /// <summary>
        /// Initialize with year, month, day
        /// </summary>
        public DateTime(int year, int month, int day)
        {
            ticks = new global::System.DateTime(year, month, day).Ticks;
        }
        
        /// <summary>
        /// Initialize with year, month, day, hour, minute, second
        /// </summary>
        public DateTime(int year, int month, int day, int hour, int minute, int second)
        {
            ticks = new global::System.DateTime(year, month, day, hour, minute, second).Ticks;
        }
        
        /// <summary>
        /// Initialize with year, month, day, hour, minute, second, millisecond
        /// </summary>
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            ticks = new global::System.DateTime(year, month, day, hour, minute, second, millisecond).Ticks;
        }
        
        /// <summary>
        /// Initialize with ticks
        /// </summary>
        public DateTime(long ticks)
        {
            this.ticks = ticks;
        }
        
        #region Properties
        
        private global::System.DateTime Value => new global::System.DateTime(ticks);
        
        /// <summary>
        /// Gets the year component
        /// </summary>
        public int Year => Value.Year;
        
        /// <summary>
        /// Gets the month component (1-12)
        /// </summary>
        public int Month => Value.Month;
        
        /// <summary>
        /// Gets the day component (1-31)
        /// </summary>
        public int Day => Value.Day;
        
        /// <summary>
        /// Gets the hour component (0-23)
        /// </summary>
        public int Hour => Value.Hour;
        
        /// <summary>
        /// Gets the minute component (0-59)
        /// </summary>
        public int Minute => Value.Minute;
        
        /// <summary>
        /// Gets the second component (0-59)
        /// </summary>
        public int Second => Value.Second;
        
        /// <summary>
        /// Gets the millisecond component (0-999)
        /// </summary>
        public int Millisecond => Value.Millisecond;
        
        /// <summary>
        /// Gets the day of week
        /// </summary>
        public DayOfWeek DayOfWeek => (DayOfWeek)Value.DayOfWeek;
        
        /// <summary>
        /// Gets the day of year (1-366)
        /// </summary>
        public int DayOfYear => Value.DayOfYear;
        
        /// <summary>
        /// Gets the ticks
        /// </summary>
        public long Ticks => ticks;
        
        /// <summary>
        /// Gets the time of day
        /// </summary>
        public TimeSpan TimeOfDay => new TimeSpan(Value.TimeOfDay);
        
        /// <summary>
        /// Gets the date component
        /// </summary>
        public DateTime Date => new DateTime(Value.Date);
        
        /// <summary>
        /// Gets the current date and time
        /// </summary>
        public static DateTime Now => new DateTime(global::System.DateTime.Now);
        
        /// <summary>
        /// Gets the current UTC date and time
        /// </summary>
        public static DateTime UtcNow => new DateTime(global::System.DateTime.UtcNow);
        
        /// <summary>
        /// Gets today's date
        /// </summary>
        public static DateTime Today => new DateTime(global::System.DateTime.Today);
        
        /// <summary>
        /// Gets the minimum DateTime value
        /// </summary>
        public static DateTime MinValue => new DateTime(global::System.DateTime.MinValue);
        
        /// <summary>
        /// Gets the maximum DateTime value
        /// </summary>
        public static DateTime MaxValue => new DateTime(global::System.DateTime.MaxValue);
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Add timespan to this datetime
        /// </summary>
        public DateTime Add(TimeSpan value)
        {
            return new DateTime(this.Value.Add(value.ToSystemTimeSpan()));
        }
        
        /// <summary>
        /// Add years to this datetime
        /// </summary>
        public DateTime AddYears(int years)
        {
            return new DateTime(Value.AddYears(years));
        }
        
        /// <summary>
        /// Add months to this datetime
        /// </summary>
        public DateTime AddMonths(int months)
        {
            return new DateTime(Value.AddMonths(months));
        }
        
        /// <summary>
        /// Add days to this datetime
        /// </summary>
        public DateTime AddDays(double days)
        {
            return new DateTime(Value.AddDays(days));
        }
        
        /// <summary>
        /// Add hours to this datetime
        /// </summary>
        public DateTime AddHours(double hours)
        {
            return new DateTime(Value.AddHours(hours));
        }
        
        /// <summary>
        /// Add minutes to this datetime
        /// </summary>
        public DateTime AddMinutes(double minutes)
        {
            return new DateTime(Value.AddMinutes(minutes));
        }
        
        /// <summary>
        /// Add seconds to this datetime
        /// </summary>
        public DateTime AddSeconds(double seconds)
        {
            return new DateTime(Value.AddSeconds(seconds));
        }
        
        /// <summary>
        /// Add milliseconds to this datetime
        /// </summary>
        public DateTime AddMilliseconds(double milliseconds)
        {
            return new DateTime(Value.AddMilliseconds(milliseconds));
        }
        
        /// <summary>
        /// Add ticks to this datetime
        /// </summary>
        public DateTime AddTicks(long ticks)
        {
            return new DateTime(Value.AddTicks(ticks));
        }
        
        /// <summary>
        /// Subtract another datetime
        /// </summary>
        public TimeSpan Subtract(DateTime other)
        {
            return new TimeSpan(Value.Subtract(other.Value));
        }
        
        /// <summary>
        /// Subtract a timespan
        /// </summary>
        public DateTime Subtract(TimeSpan value)
        {
            return new DateTime(this.Value.Subtract(value.ToSystemTimeSpan()));
        }
        
        /// <summary>
        /// Convert to string with default format
        /// </summary>
        public override string ToString()
        {
            return Value.ToString();
        }
        
        /// <summary>
        /// Convert to string with format
        /// </summary>
        public string ToString(string format)
        {
            return Value.ToString(format);
        }
        
        /// <summary>
        /// Convert to string with format and culture
        /// </summary>
        public string ToString(string format, IFormatProvider provider)
        {
            return Value.ToString(format, provider);
        }
        
        /// <summary>
        /// Convert to long date string
        /// </summary>
        public string ToLongDateString()
        {
            return Value.ToLongDateString();
        }
        
        /// <summary>
        /// Convert to long time string
        /// </summary>
        public string ToLongTimeString()
        {
            return Value.ToLongTimeString();
        }
        
        /// <summary>
        /// Convert to short date string
        /// </summary>
        public string ToShortDateString()
        {
            return Value.ToShortDateString();
        }
        
        /// <summary>
        /// Convert to short time string
        /// </summary>
        public string ToShortTimeString()
        {
            return Value.ToShortTimeString();
        }
        
        /// <summary>
        /// Convert to universal time
        /// </summary>
        public DateTime ToUniversalTime()
        {
            return new DateTime(Value.ToUniversalTime());
        }
        
        /// <summary>
        /// Convert to local time
        /// </summary>
        public DateTime ToLocalTime()
        {
            return new DateTime(Value.ToLocalTime());
        }
        
        /// <summary>
        /// Get ISO 8601 string representation
        /// </summary>
        public string ToIso8601String()
        {
            return Value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
        }
        
        /// <summary>
        /// Get Unix timestamp (seconds since 1970-01-01)
        /// </summary>
        public long ToUnixTimestamp()
        {
            var epoch = new global::System.DateTime(1970, 1, 1, 0, 0, 0, global::System.DateTimeKind.Utc);
            return (long)(Value.ToUniversalTime() - epoch).TotalSeconds;
        }
        
        /// <summary>
        /// Create from Unix timestamp
        /// </summary>
        public static DateTime FromUnixTimestamp(long timestamp)
        {
            var epoch = new global::System.DateTime(1970, 1, 1, 0, 0, 0, global::System.DateTimeKind.Utc);
            return new DateTime(epoch.AddSeconds(timestamp));
        }
        
        #endregion
        
        #region Static Methods
        
        /// <summary>
        /// Parse date time from string
        /// </summary>
        public static DateTime Parse(string s)
        {
            return new DateTime(global::System.DateTime.Parse(s));
        }
        
        /// <summary>
        /// Parse date time from string with format
        /// </summary>
        public static DateTime ParseExact(string s, string format)
        {
            return new DateTime(global::System.DateTime.ParseExact(s, format, global::System.Globalization.CultureInfo.InvariantCulture));
        }
        
        /// <summary>
        /// Try parse date time from string
        /// </summary>
        public static bool TryParse(string s, out DateTime result)
        {
            if (global::System.DateTime.TryParse(s, out var dt))
            {
                result = new DateTime(dt);
                return true;
            }
            
            result = default;
            return false;
        }
        
        /// <summary>
        /// Try parse date time from string with format
        /// </summary>
        public static bool TryParseExact(string s, string format, out DateTime result)
        {
            if (global::System.DateTime.TryParseExact(s, format, global::System.Globalization.CultureInfo.InvariantCulture, global::System.Globalization.DateTimeStyles.None, out var dt))
            {
                result = new DateTime(dt);
                return true;
            }
            
            result = default;
            return false;
        }
        
        /// <summary>
        /// Get number of days in month
        /// </summary>
        public static int DaysInMonth(int year, int month)
        {
            return global::System.DateTime.DaysInMonth(year, month);
        }
        
        /// <summary>
        /// Check if year is leap year
        /// </summary>
        public static bool IsLeapYear(int year)
        {
            return global::System.DateTime.IsLeapYear(year);
        }
        
        /// <summary>
        /// Compare two dates
        /// </summary>
        public static int Compare(DateTime t1, DateTime t2)
        {
            return global::System.DateTime.Compare(t1.Value, t2.Value);
        }
        
        #endregion
        
        #region Operators
        
        public static DateTime operator +(DateTime d, TimeSpan t)
        {
            return d.Add(t);
        }
        
        public static DateTime operator -(DateTime d, TimeSpan t)
        {
            return d.Subtract(t);
        }
        
        public static TimeSpan operator -(DateTime d1, DateTime d2)
        {
            return d1.Subtract(d2);
        }
        
        public static bool operator ==(DateTime d1, DateTime d2)
        {
            return d1.ticks == d2.ticks;
        }
        
        public static bool operator !=(DateTime d1, DateTime d2)
        {
            return d1.ticks != d2.ticks;
        }
        
        public static bool operator <(DateTime d1, DateTime d2)
        {
            return d1.ticks < d2.ticks;
        }
        
        public static bool operator >(DateTime d1, DateTime d2)
        {
            return d1.ticks > d2.ticks;
        }
        
        public static bool operator <=(DateTime d1, DateTime d2)
        {
            return d1.ticks <= d2.ticks;
        }
        
        public static bool operator >=(DateTime d1, DateTime d2)
        {
            return d1.ticks >= d2.ticks;
        }
        
        #endregion
        
        #region IComparable
        
        public int CompareTo(DateTime other)
        {
            return ticks.CompareTo(other.ticks);
        }
        
        #endregion
        
        #region IEquatable
        
        public bool Equals(DateTime other)
        {
            return ticks.Equals(other.ticks);
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is DateTime dt)
                return Equals(dt);
            return false;
        }
        
        public override int GetHashCode()
        {
            return ticks.GetHashCode();
        }
        
        #endregion
        
        #region Conversions
        
        // Use explicit conversions to avoid conflicts
        public static explicit operator DateTime(global::System.DateTime value)
        {
            return new DateTime(value);
        }
        
        public static explicit operator global::System.DateTime(DateTime value)
        {
            return value.Value;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Days of the week
    /// </summary>
    public enum DayOfWeek
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6
    }
    
    /// <summary>
    /// Represents a time interval
    /// </summary>
    public struct TimeSpan : IComparable<TimeSpan>, IEquatable<TimeSpan>
    {
        private readonly long ticks;
        
        internal TimeSpan(global::System.TimeSpan value)
        {
            this.ticks = value.Ticks;
        }
        
        public TimeSpan(int hours, int minutes, int seconds)
        {
            ticks = new global::System.TimeSpan(hours, minutes, seconds).Ticks;
        }
        
        public TimeSpan(int days, int hours, int minutes, int seconds)
        {
            ticks = new global::System.TimeSpan(days, hours, minutes, seconds).Ticks;
        }
        
        public TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds)
        {
            ticks = new global::System.TimeSpan(days, hours, minutes, seconds, milliseconds).Ticks;
        }
        
        public TimeSpan(long ticks)
        {
            this.ticks = ticks;
        }
        
        #region Properties
        
        private global::System.TimeSpan Value => new global::System.TimeSpan(ticks);
        
        public int Days => Value.Days;
        public int Hours => Value.Hours;
        public int Minutes => Value.Minutes;
        public int Seconds => Value.Seconds;
        public int Milliseconds => Value.Milliseconds;
        public long Ticks => ticks;
        
        public double TotalDays => Value.TotalDays;
        public double TotalHours => Value.TotalHours;
        public double TotalMinutes => Value.TotalMinutes;
        public double TotalSeconds => Value.TotalSeconds;
        public double TotalMilliseconds => Value.TotalMilliseconds;
        
        public static TimeSpan Zero => new TimeSpan(global::System.TimeSpan.Zero);
        public static TimeSpan MinValue => new TimeSpan(global::System.TimeSpan.MinValue);
        public static TimeSpan MaxValue => new TimeSpan(global::System.TimeSpan.MaxValue);
        
        #endregion
        
        #region Methods
        
        public TimeSpan Add(TimeSpan ts)
        {
            return new TimeSpan(Value.Add(ts.Value));
        }
        
        public TimeSpan Subtract(TimeSpan ts)
        {
            return new TimeSpan(Value.Subtract(ts.Value));
        }
        
        public TimeSpan Negate()
        {
            return new TimeSpan(Value.Negate());
        }
        
        public TimeSpan Duration()
        {
            return new TimeSpan(Value.Duration());
        }
        
        public override string ToString()
        {
            return Value.ToString();
        }
        
        public string ToString(string format)
        {
            return Value.ToString(format);
        }
        
        internal global::System.TimeSpan ToSystemTimeSpan()
        {
            return Value;
        }
        
        #endregion
        
        #region Static Methods
        
        public static TimeSpan FromDays(double value)
        {
            return new TimeSpan(global::System.TimeSpan.FromDays(value));
        }
        
        public static TimeSpan FromHours(double value)
        {
            return new TimeSpan(global::System.TimeSpan.FromHours(value));
        }
        
        public static TimeSpan FromMinutes(double value)
        {
            return new TimeSpan(global::System.TimeSpan.FromMinutes(value));
        }
        
        public static TimeSpan FromSeconds(double value)
        {
            return new TimeSpan(global::System.TimeSpan.FromSeconds(value));
        }
        
        public static TimeSpan FromMilliseconds(double value)
        {
            return new TimeSpan(global::System.TimeSpan.FromMilliseconds(value));
        }
        
        public static TimeSpan FromTicks(long value)
        {
            return new TimeSpan(global::System.TimeSpan.FromTicks(value));
        }
        
        public static TimeSpan Parse(string s)
        {
            return new TimeSpan(global::System.TimeSpan.Parse(s));
        }
        
        public static bool TryParse(string s, out TimeSpan result)
        {
            if (global::System.TimeSpan.TryParse(s, out var ts))
            {
                result = new TimeSpan(ts);
                return true;
            }
            
            result = default;
            return false;
        }
        
        #endregion
        
        #region Operators
        
        public static TimeSpan operator +(TimeSpan t1, TimeSpan t2)
        {
            return t1.Add(t2);
        }
        
        public static TimeSpan operator -(TimeSpan t1, TimeSpan t2)
        {
            return t1.Subtract(t2);
        }
        
        public static TimeSpan operator -(TimeSpan t)
        {
            return t.Negate();
        }
        
        public static bool operator ==(TimeSpan t1, TimeSpan t2)
        {
            return t1.ticks == t2.ticks;
        }
        
        public static bool operator !=(TimeSpan t1, TimeSpan t2)
        {
            return t1.ticks != t2.ticks;
        }
        
        public static bool operator <(TimeSpan t1, TimeSpan t2)
        {
            return t1.ticks < t2.ticks;
        }
        
        public static bool operator >(TimeSpan t1, TimeSpan t2)
        {
            return t1.ticks > t2.ticks;
        }
        
        public static bool operator <=(TimeSpan t1, TimeSpan t2)
        {
            return t1.ticks <= t2.ticks;
        }
        
        public static bool operator >=(TimeSpan t1, TimeSpan t2)
        {
            return t1.ticks >= t2.ticks;
        }
        
        #endregion
        
        #region IComparable
        
        public int CompareTo(TimeSpan other)
        {
            return ticks.CompareTo(other.ticks);
        }
        
        #endregion
        
        #region IEquatable
        
        public bool Equals(TimeSpan other)
        {
            return ticks.Equals(other.ticks);
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is TimeSpan ts)
                return Equals(ts);
            return false;
        }
        
        public override int GetHashCode()
        {
            return ticks.GetHashCode();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Date time styles for parsing
    /// </summary>
    [Flags]
    public enum DateTimeStyles
    {
        None = 0,
        AllowLeadingWhite = 1,
        AllowTrailingWhite = 2,
        AllowInnerWhite = 4,
        AllowWhiteSpaces = AllowLeadingWhite | AllowInnerWhite | AllowTrailingWhite,
        NoCurrentDateDefault = 8,
        AdjustToUniversal = 16,
        AssumeLocal = 32,
        AssumeUniversal = 64,
        RoundtripKind = 128
    }
} 