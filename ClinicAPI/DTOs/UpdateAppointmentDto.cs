using System.ComponentModel.DataAnnotations;

namespace ClinicAPI.DTOs;

public class UpdateAppointmentDto
{
    [Required]
    public int PatientId { get; set; }
    [Required]
    public int DoctorId { get; set; }
    [Required]
    public DateTime AppointmentDate { get; set; }
    [Required, AllowedValues("Scheduled", "Completed", "Cancelled")]
    public string Status { get; set; } = string.Empty;
    [Required, MaxLength(250)]
    public string Reason { get; set; } = string.Empty;
    public string? InternalNotes { get; set; } = string.Empty;
}