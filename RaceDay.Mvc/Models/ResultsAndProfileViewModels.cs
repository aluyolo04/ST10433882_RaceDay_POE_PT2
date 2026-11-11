
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public class RaceResultViewModel
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "";
}

public class CaptureResultViewModel
{
    [Required]
    public int EnrolmentId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public string? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";
}

public class UserProfileViewModel
{
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = "";

    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
