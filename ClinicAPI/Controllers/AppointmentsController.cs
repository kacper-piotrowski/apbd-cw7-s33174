using ClinicAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController(IAppointmentService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetWithParams([FromQuery] string? status,
            [FromQuery] string? patientLastName, CancellationToken ct)
        {
            return Ok(await service.GetWithParamsAsync(status, patientLastName, ct));
        }
    }
}
