using Quartz;
using System.Threading.Tasks;
using GreeControlAPI.Models;
using GreeControlAPI.Services;
using Microsoft.Extensions.Logging;

namespace GreeControlAPI.Jobs
{
    public class AirConditionerJob : IJob
    {
        private readonly AirConditionerService _acService;
        private readonly ILogger<AirConditionerJob> _logger;

        public AirConditionerJob(
            AirConditionerService acService,
            ILogger<AirConditionerJob> logger)
        {
            _acService = acService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Starting AirConditionerJob execution");
            
            try
            {
                var macAddress = context.MergedJobDataMap.TryGetValue("MacAddress", out var macObj) ? macObj?.ToString() : null;
                var action = context.MergedJobDataMap.TryGetValue("Action", out var actionObj) ? actionObj?.ToString() : null;
                var time = context.MergedJobDataMap.TryGetValue("Time", out var timeObj) ? timeObj?.ToString() : null;
                
                _logger.LogDebug($"Job parameters - MAC: {macAddress}, Action: {action}, Time: {time}");

                if (string.IsNullOrEmpty(macAddress) || string.IsNullOrEmpty(action))
                {
                    _logger.LogError("Missing required job parameters");
                    return;
                }

                _logger.LogInformation($"Executing {action} for {macAddress}");
                
                switch (action)
                {
                    case "TurnOn":
                        _logger.LogDebug($"Turning on device {macAddress}");
                        await _acService.TurnOn(macAddress);
                        break;
                    case "TurnOff":
                        _logger.LogDebug($"Turning off device {macAddress}");
                        await _acService.TurnOff(macAddress);
                        break;
                    default:
                        _logger.LogWarning($"Unknown action: {action}");
                        break;
                }

                _logger.LogInformation($"Successfully executed {action} for {macAddress}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing AirConditionerJob");
            }
            finally
            {
                _logger.LogInformation("Completed AirConditionerJob execution");
            }
            return;
        }
    }
}
