using ClinicAPI.DTOs;
using Microsoft.Data.SqlClient;

namespace ClinicAPI.Services;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    public async Task<IEnumerable<AppointmentListDto>> GetWithParamsAsync(string? status, string? patientLastName, CancellationToken ct)
    {
        var result = new List<AppointmentListDto>();
        
        var sqlCommand = @"
            SELECT
                a.IdAppointment,
                a.AppointmentDate,
                a.Status,
                a.Reason,
                p.FirstName + N' ' + p.LastName AS PatientFullName,
                p.Email AS PatientEmail
            FROM dbo.Appointments a
            JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
            WHERE (@Status IS NULL OR a.Status = @Status)
              AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
            ORDER BY a.AppointmentDate;";

        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand(sqlCommand, connection);
        
        command.Parameters.Add(new SqlParameter("@Status", (object?)status ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@PatientLastName", (object?)patientLastName ?? DBNull.Value));

        await connection.OpenAsync(ct);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = await reader.IsDBNullAsync(2, ct) ? null : reader.GetString(2),
                Reason = await reader.IsDBNullAsync(3, ct) ? null : reader.GetString(3),
                PatientFullName = await reader.IsDBNullAsync(4, ct) ? null : reader.GetString(4),
                PatientEmail = await reader.IsDBNullAsync(5, ct) ? null : reader.GetString(5),
            });
        }

        return result;
    }
}