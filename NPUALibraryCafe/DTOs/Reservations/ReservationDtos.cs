namespace NPUALibraryCafe.DTOs.Reservations;

public class CreateReservationDto
{
    public int TableId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Notes { get; set; }
}

public class TableAvailabilityDto
{
    public int Id { get; set; }
    public string TableNumber { get; set; } = "";
    public int Capacity { get; set; }
    public bool Available { get; set; }
}

public class ReservationResponseDto
{
    public int Id { get; set; }
    public int TableId { get; set; }
    public string TableName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ReservationDetailDto : ReservationResponseDto
{
    public string UserEmail { get; set; } = "";
    public string UserName { get; set; } = "";
}