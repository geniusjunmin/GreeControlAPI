using Microsoft.AspNetCore.Mvc;
using GreeControlAPI.Models;
using GreeControlAPI.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GreeControlAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimerController : ControllerBase
    {
        private readonly AirConditionerService _acService;
        private readonly ILogger<TimerController> _logger;

        public TimerController(
            AirConditionerService acService,
            ILogger<TimerController> logger)
        {
            _acService = acService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimerTask>>> GetTimers()
        {
            try
            {
                var tasks = await _acService.GetScheduledTasks();
                var result = tasks.Select(t => new {
                    mac = t.MacAddress,
                    action = t.Action,
                    time = t.Time
                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scheduled tasks");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddTimer([FromBody] TimerTask task)
        {
            try
            {
                if (task == null || string.IsNullOrEmpty(task.MacAddress))
                {
                    return BadRequest("Invalid timer task");
                }

                var scheduledTask = await _acService.ScheduleTask(task);
                _logger.LogInformation($"Added timer task for {task.MacAddress} at {task.Time}");
                return Ok(new {
                    mac = scheduledTask.MacAddress,
                    action = scheduledTask.Action,
                    time = scheduledTask.Time
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding timer task");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{macAddress}/{time}")]
        public async Task<IActionResult> DeleteTimer(string macAddress, string time)
        {
            try
            {
                if (string.IsNullOrEmpty(macAddress) || string.IsNullOrEmpty(time))
                {
                    return BadRequest("Invalid parameters");
                }

                if (DateTimeOffset.TryParse(time, out var taskTime))
                {
                    await _acService.CancelTask(macAddress, taskTime);
                    _logger.LogInformation($"Deleted timer task for {macAddress} at {time}");
                    return Ok();
                }
                return BadRequest("Invalid time format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting timer task");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
