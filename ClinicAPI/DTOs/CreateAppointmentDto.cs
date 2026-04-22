using System.ComponentModel.DataAnnotations;

namespace ClinicAPI.DTOs;

public class CreateAppointmentDto
{
    [Required]
    public int PatientId { get; set; }
    [Required]
    public int DoctorId { get; set; }
    [Required]
    public DateTime AppointmentDate { get; set; }
    [Required, MaxLength(250)]
    public string Reason { get; set; } = string.Empty;
}