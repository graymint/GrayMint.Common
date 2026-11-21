namespace GrayMint.Common.Blazor.Components;

public class TimePeriod(TimePeriodMode mode = TimePeriodMode.Nothing)
{
    public TimePeriodMode Mode { get; } = mode;
    public DateTime? EndTime
    {
        get
        {
            return Mode switch
            {
                TimePeriodMode.Yesterday => DateTime.Today, // the end of yesterday
                _ => null
            };
        }
    }

    public DateTime? BeginTime
    {
        get
        {
            return Mode switch
            {
                TimePeriodMode.Nothing => null,
                TimePeriodMode.LastHour => DateTime.Now.AddHours(-1),
                TimePeriodMode.Today => DateTime.Today,
                TimePeriodMode.Yesterday => DateTime.Today.AddDays(-1),
                TimePeriodMode.Last24Hours => DateTime.Now.AddHours(-24),
                TimePeriodMode.Last48Hours => DateTime.Now.AddHours(-48),
                TimePeriodMode.Last72Hours => DateTime.Now.AddHours(-72),
                TimePeriodMode.Last7Days => DateTime.Now.AddDays(-7),
                TimePeriodMode.Last31Days => DateTime.Now.AddDays(-31),
                TimePeriodMode.CurrentMonth => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                _ => throw new ArgumentException()
            };
        }
    }

    public override string ToString()
    {
        return Mode switch
        {
            TimePeriodMode.Nothing => CommonResource.PeriodNothing,
            TimePeriodMode.LastHour => CommonResource.PeriodLastHour,
            TimePeriodMode.Today => CommonResource.PeriodToday,
            TimePeriodMode.Yesterday => CommonResource.PeriodYesterday,
            TimePeriodMode.Last24Hours => CommonResource.PeriodLast24Hours,
            TimePeriodMode.Last48Hours => CommonResource.PeriodLast48Hours,
            TimePeriodMode.Last72Hours => CommonResource.PeriodLast72Hours,
            TimePeriodMode.Last7Days => CommonResource.PeriodLast7Days,
            TimePeriodMode.Last31Days => CommonResource.PeriodLast31Days,
            TimePeriodMode.CurrentMonth => CommonResource.PeriodCurrentMounth,
            _ => throw new ArgumentException()
        };
    }
}