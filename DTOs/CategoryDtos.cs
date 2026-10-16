using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryRequest
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    [Range(0, 99999.99)]
    public decimal EntryFee { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
    public int EnrolmentCount { get; set; }
}