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

    public async Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        AppointmentDetailsDto? result = null;

        var sqlCommand = @"SELECT
                p.Email,
                p.PhoneNumber,
                d.LicenseNumber,
                a.InternalNotes,
                a.CreatedAt
            FROM dbo.Appointments a
            JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
            JOIN dbo.Doctors d ON d.IdDoctor = a.IdDoctor
            WHERE a.IdAppointment = @IdAppointment";

        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand(sqlCommand, connection);
        
        command.Parameters.Add(new SqlParameter("@IdAppointment", (object?)id ?? DBNull.Value));
        
        await connection.OpenAsync(ct);

        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            result = new AppointmentDetailsDto
            {
                PatientEmail = await reader.IsDBNullAsync(0, ct) ? null :reader.GetString(0),
                PatientPhoneNumber = await reader.IsDBNullAsync(1, ct) ? null :reader.GetString(1),
                DoctorLicenseNumber = await reader.IsDBNullAsync(2, ct) ? null :reader.GetString(2),
                InternalNotes = await reader.IsDBNullAsync(3, ct) ? null :reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
            };
        }

        return result;
    }

    public async Task<int> AddAppointmentAsync(CreateAppointmentDto createAppointmentDto, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await connection.OpenAsync(ct);
        
        var patientCheck = "SELECT 1 FROM dbo.Patients WHERE IdPatient = @IdPatient AND IsActive = 1;";
        await using var patientCommand = new SqlCommand(patientCheck, connection);
        patientCommand.Parameters.Add(new SqlParameter("@IdPatient", createAppointmentDto.PatientId));
        if (await patientCommand.ExecuteScalarAsync(ct) == null)
        {
            throw new ArgumentException("Błąd! Pacjent nie istnieje lub jest nieaktywny");
        }
        
        var doctorCheck = "SELECT 1 FROM dbo.Doctors WHERE IdDoctor = @IdDoctor AND IsActive = 1;";
        await using var doctorCommand = new SqlCommand(doctorCheck,connection);
        doctorCommand.Parameters.Add(new SqlParameter("@IdDoctor", createAppointmentDto.DoctorId));
        if (await doctorCommand.ExecuteScalarAsync(ct) == null)
        {
            throw new ArgumentException("Błąd! Doktor nie istnieje lub jest nieaktywny");
        }

        var conflictCheck =
            "SELECT 1 FROM dbo.Appointments WHERE IdDoctor = @IdDoctor AND AppointmentDate = @AppointmentDate;";
        await using var conflictCommand = new SqlCommand(conflictCheck,connection);
        conflictCommand.Parameters.Add(new SqlParameter("@IdDoctor", createAppointmentDto.DoctorId));
        conflictCommand.Parameters.Add(new SqlParameter("@AppointmentDate", createAppointmentDto.AppointmentDate));
        if (await conflictCommand.ExecuteScalarAsync(ct) != null)
        {
            throw new InvalidOperationException("Błąd! Konflikt!");
        }

        var insert = @"
        INSERT INTO dbo.Appointments (IdPatient, IdDoctor, AppointmentDate, Reason, Status)
        VALUES (@IdPatient, @IdDoctor, @AppointmentDate, @Reason, 'Scheduled');";
        
        await using var insertCommand = new SqlCommand(insert, connection);
        insertCommand.Parameters.Add(new SqlParameter("@IdPatient", createAppointmentDto.PatientId));
        insertCommand.Parameters.Add(new SqlParameter("@IdDoctor", createAppointmentDto.DoctorId));
        insertCommand.Parameters.Add(new SqlParameter("@AppointmentDate", createAppointmentDto.AppointmentDate));
        insertCommand.Parameters.Add(new SqlParameter("@Reason", createAppointmentDto.Reason));

        var insertResult = await insertCommand.ExecuteNonQueryAsync(ct);

        return insertResult;

    }

    public async Task<int> UpdateAppointmentAsync(int id, UpdateAppointmentDto updateAppointmentDto, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await connection.OpenAsync(ct);

        var getAppointment =
            "SELECT Status, AppointmentDate FROM dbo.Appointments WHERE IdAppointment = @IdAppointment;";
        var getAppointmentCommand = new SqlCommand(getAppointment, connection);
        getAppointmentCommand.Parameters.Add(new SqlParameter("@IdAppointment", id));

        string status =  string.Empty;
        DateTime date = new DateTime();

        await using (var reader = await getAppointmentCommand.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct))
            {
                return 0;
            }
            status = reader.GetString(0);
            date = reader.GetDateTime(1);
        }
        

        if (status == "Completed" && date != updateAppointmentDto.AppointmentDate)
        {
            throw new ArgumentException("Błąd status to completed lub data jest już zakończona");
        }

        var patientCheck = "SELECT 1 FROM dbo.Patients WHERE IdPatient = @IdPatient AND IsActive = 1;";
        await using var patientCheckCommand = new SqlCommand(patientCheck, connection);
        patientCheckCommand.Parameters.Add(new SqlParameter("@IdPatient", updateAppointmentDto.PatientId));
        if (await patientCheckCommand.ExecuteScalarAsync(ct) == null)
        {
            throw new ArgumentException("Pacjent nie istnieje lub nie jest aktywny");
        }
        
        var doctorCheck = "SELECT 1 FROM dbo.Doctors WHERE IdDoctor = @IdDoctor AND IsActive = 1;";
        await using var doctorCheckCommand = new SqlCommand(doctorCheck, connection);
        doctorCheckCommand.Parameters.Add(new SqlParameter("@IdDoctor", updateAppointmentDto.DoctorId));
        if (await doctorCheckCommand.ExecuteScalarAsync(ct) == null)
        {
            throw new ArgumentException("Doktor nie istnieje lub nie jest aktywny");
        }
        
        var conflictCheck =
            "SELECT 1 FROM dbo.Appointments WHERE IdDoctor = @IdDoctor AND AppointmentDate = @AppointmentDate AND IdAppointment != @IdAppointment;";
        await using var conflictCommand = new SqlCommand(conflictCheck,connection);
        conflictCommand.Parameters.Add(new SqlParameter("@IdDoctor", updateAppointmentDto.DoctorId));
        conflictCommand.Parameters.Add(new SqlParameter("@AppointmentDate", updateAppointmentDto.AppointmentDate));
        conflictCommand.Parameters.Add(new SqlParameter("@IdAppointment", id));
        if (await conflictCommand.ExecuteScalarAsync(ct) != null)
        {
            throw new InvalidOperationException("Błąd! Konflikt!");
        }
        
        var update =@"
        UPDATE dbo.Appointments
        SET IdPatient = @IdPatient,
            IdDoctor = @IdDoctor,
            AppointmentDate = @AppointmentDate,
            Status = @Status,
            Reason = @Reason,
            InternalNotes = @InternalNotes
        WHERE IdAppointment = @IdAppointment;";

        await using var updateCommand = new SqlCommand(update, connection);
        updateCommand.Parameters.Add(new SqlParameter("@IdPatient", updateAppointmentDto.PatientId));
        updateCommand.Parameters.Add(new SqlParameter("@IdDoctor", updateAppointmentDto.DoctorId));
        updateCommand.Parameters.Add(new SqlParameter("@AppointmentDate", updateAppointmentDto.AppointmentDate));
        updateCommand.Parameters.Add(new SqlParameter("@Status", updateAppointmentDto.Status));
        updateCommand.Parameters.Add(new SqlParameter("@Reason", updateAppointmentDto.Reason));
        updateCommand.Parameters.Add(new SqlParameter("@InternalNotes", (object?)updateAppointmentDto.InternalNotes ?? DBNull.Value));
        updateCommand.Parameters.Add(new SqlParameter("@IdAppointment", id));
        
        var updateResult = await updateCommand.ExecuteNonQueryAsync(ct);

        return updateResult;
    }
}