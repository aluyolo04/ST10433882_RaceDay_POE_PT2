
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public class EventViewModel
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = "";

    [Required, StringLength(150)]
    public string EventName { get; set; } = "";

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime? EventDate { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = "";

    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    [Required]
    public string EventType { get; set; } = "Run";

    public DateTime CreatedAt { get; set; }
}

public class CategoryViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required, StringLength(100)]
    public string CategoryName { get; set; } = "";

    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    [Range(0, 99999.99)]
    public decimal EntryFee { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;

    public int EnrolmentCount { get; set; }
}

public class EnrolmentViewModel
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "";
}

public class EventDetailsViewModel
{
    public EventViewModel Event { get; set; } = new();
    public List<CategoryViewModel> Categories { get; set; } = new();
}

public class OrganiserDashboardViewModel
{
    public List<EventViewModel> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int UpcomingEvents => Events.Count(e =>
        e.EventDate >= DateTime.Today);
}
