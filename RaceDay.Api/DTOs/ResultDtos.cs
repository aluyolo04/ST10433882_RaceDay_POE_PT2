using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class ResultRequest
{
    // Format HH:mm:ss, for example 00:28:45
    public string? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }

    // Finished, DNF, DNS or DSQ
    public string Status { get; set; } = "Finished";
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;
}