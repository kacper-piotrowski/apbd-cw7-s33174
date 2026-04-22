using ClinicAPI.DTOs;
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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetByIdAsync([FromRoute]int id, CancellationToken ct)
        {
            var result = await service.GetByIdAsync(id, ct);

            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto, CancellationToken ct)
        {
            if (dto.AppointmentDate < DateTime.Now)
            {
                return BadRequest("Wprowadzony termin jest w przeszłości!");
            }

            try
            {
                var inserted = await service.AddAppointmentAsync(dto, ct);
                if (inserted == 1)
                {
                    return Created();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (InvalidOperationException)
            {
                return Conflict();
            }
            catch(ArgumentException)

            {
                return BadRequest();
            }
        }
    }
}
