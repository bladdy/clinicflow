namespace DentalBot.Shared.DTOs.Clinics;

public class BusinessScheduleDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public List<ScheduleDayDto> Days { get; set; } = [];
    public List<BreakPeriodDto> Breaks { get; set; } = [];
    public LunchConfigDto? LunchConfig { get; set; }
    public int TotalProductiveMinutes { get; set; }
}

public class ScheduleDayDto
{
    public Guid Id { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public string OpenTime { get; set; } = "09:00";
    public string CloseTime { get; set; } = "17:00";
}

public class BreakPeriodDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = "10:30";
    public string EndTime { get; set; } = "10:45";
    public int DurationMinutes { get; set; }
    public int SortOrder { get; set; }
}

public class LunchConfigDto
{
    public Guid Id { get; set; }
    public int DurationMinutes { get; set; }
    public string StartTime { get; set; } = "13:00";
    public string EndTime { get; set; } = "13:45";
}

public class SaveBusinessScheduleRequest
{
    public List<ScheduleDayEntry> Days { get; set; } = [];
    public List<BreakPeriodEntry> Breaks { get; set; } = [];
    public LunchConfigEntry? LunchConfig { get; set; }
}

public class ScheduleDayEntry
{
    public int DayOfWeek { get; set; }
    public bool IsOpen { get; set; }
    public string OpenTime { get; set; } = "09:00";
    public string CloseTime { get; set; } = "17:00";
}

public class BreakPeriodEntry
{
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = "10:30";
    public string EndTime { get; set; } = "10:45";
    public int SortOrder { get; set; }
}

public class LunchConfigEntry
{
    public int DurationMinutes { get; set; } = 45;
    public string StartTime { get; set; } = "13:00";
}
