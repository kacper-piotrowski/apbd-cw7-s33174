using ClinicAPI.DTOs;

namespace ClinicAPI.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetWithParamsAsync(string? status, string? patientLastName, CancellationToken ct);
}