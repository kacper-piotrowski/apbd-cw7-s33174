using ClinicAPI.DTOs;

namespace ClinicAPI.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetWithParamsAsync(string? status, string? patientLastName, CancellationToken ct = default);
    Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> AddAppointmentAsync(CreateAppointmentDto createAppointmentDto, CancellationToken ct = default);
    Task<int> UpdateAppointmentAsync(int id, UpdateAppointmentDto updateAppointmentDto, CancellationToken ct = default);
}