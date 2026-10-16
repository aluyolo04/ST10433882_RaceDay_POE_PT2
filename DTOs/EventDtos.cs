using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime? EventDate { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    // Run, Walk or Cycle
    [Required]
    public string EventType { get; set; } = string.Empty;
}

public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}