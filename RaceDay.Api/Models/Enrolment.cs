namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "Confirmed";

    public Category Category { get; set; } = null!;
    public User Participant { get; set; } = null!;
    public Result? Result { get; set; }
}