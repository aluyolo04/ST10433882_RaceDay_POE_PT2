namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "Run";
    public DateTime CreatedAt { get; set; }

    public User Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}