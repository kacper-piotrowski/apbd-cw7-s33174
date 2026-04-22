using ClinicAPI.DTOs;

namespace ClinicAPI.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetWithParamsAsync(string? status, string? patientLastName, CancellationToken ct = default);
    Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken ct = default);
}