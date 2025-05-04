using GreeControlAPI.Models;
using GreeControlAPI.Jobs;
using Quartz;
using System.Net.Http.Json;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GreeControlAPI.Services
{
    public class AirConditionerService
{
    private readonly ISchedulerFactory _schedulerFactory;

    public AirConditionerService(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AirConditionerService(ISchedulerFactory schedulerFactory, 
        ILogger<AirConditionerService> logger, 
        HttpClient httpClient,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor) : this(schedulerFactory, logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request != null 
            ? $"{request.Scheme}://{request.Host}"
            : "http://localhost:5073";
        
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    private string? _cachedMainMac;
    private string? _cachedPrivateKey;
    private string? _cachedIp;

    private async Task ScanDevicesAsync()
    {
        try
        {
            _logger.LogInformation("Scanning for devices...");
            
            var response = await _httpClient.GetAsync("/Scan");
            response.EnsureSuccessStatusCode();
            
            var scanResult = await response.Content.ReadFromJsonAsync<DeviceScanResponse>();
            
            if (scanResult != null)
            {
                _cachedMainMac = scanResult.DevMac;
                _cachedPrivateKey = scanResult.PrivateKey;
                _cachedIp = scanResult.DevIp;
                
                _logger.LogInformation($"Successfully scanned devices. Main MAC: {_cachedMainMac}, IP: {_cachedIp}");
            }
            else
            {
                throw new InvalidOperationException("Invalid scan response format");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan devices");
            throw;
        }
    }

    private class DeviceScanResponse
    {
        public string DevMac { get; set; }
        public string DevIp { get; set; }
        public string PrivateKey { get; set; }
        public List<DeviceSubInfo> DevSubInfoList { get; set; }
    }

    private class DeviceSubInfo
    {
        public string Name { get; set; }
        public string Mac { get; set; }
    }

    private async Task sendCmd(string macAddress, string command, int value)
    {
        try 
        {
            _logger.LogInformation($"Sending command to {macAddress}: {command}={value}");

            try
            {
                // Get cached values or from config
                var mainmac = _cachedMainMac ?? _configuration["DeviceInfo:MainMAC"];
                var key = _cachedPrivateKey ?? _configuration["DeviceInfo:PrivateKey"];
                var ip = _cachedIp ?? _configuration["DeviceInfo:IP"];
                
                // If still missing, scan for devices
                if (string.IsNullOrEmpty(mainmac) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(ip))
                {
                    try
                    {
                        await ScanDevicesAsync();
                        
                        mainmac = _cachedMainMac;
                        key = _cachedPrivateKey;
                        ip = _cachedIp;
                        
                        if (string.IsNullOrEmpty(mainmac) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(ip))
                        {
                            throw new InvalidOperationException("Failed to get device configuration");
                        }
                    }
                    catch (Exception scanEx)
                    {
                        _logger.LogError(scanEx, "Failed to scan devices");
                        throw;
                    }
                }

                try
                {
                    var url = $"/SendCMD?mainmac={mainmac}&mac={macAddress}&key={key}&ip={ip}&CMDstr={command}&CMDvalue={value}";
                    
                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    
                    _logger.LogInformation($"Successfully sent command to {macAddress}");
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError(httpEx, $"HTTP request failed when sending command to {macAddress}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process command for {macAddress}");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send command to {macAddress}");
            throw;
        }
    }

    public async Task TurnOn(string macAddress)
    {
        await sendCmd(macAddress, "Pow", 1);
    }

    public async Task TurnOff(string macAddress)
    {
        await sendCmd(macAddress, "Pow", 0);
    }

    private readonly ILogger<AirConditionerService> _logger;

    public AirConditionerService(ISchedulerFactory schedulerFactory, ILogger<AirConditionerService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

        public async Task<TimerTask> ScheduleTask(TimerTask task)
    {
        try
        {
            _logger.LogInformation($"Attempting to schedule task for {task.MacAddress} at {task.Time} with action {task.Action}");
            
            var scheduler = await _schedulerFactory.GetScheduler();
            
            var jobKey = new JobKey($"{task.MacAddress}_{task.Time:o}");
            _logger.LogDebug($"Generated JobKey: {jobKey}");
            
            var job = JobBuilder.Create<AirConditionerJob>()
                .WithIdentity(jobKey)
                .UsingJobData("MacAddress", task.MacAddress)
                .UsingJobData("Action", task.Action)
                .UsingJobData("Time", task.Time.ToString("o"))
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartAt(task.Time)
                .Build();

            _logger.LogDebug($"Scheduling job with trigger at {task.Time}");
            await scheduler.ScheduleJob(job, trigger);
            
            _logger.LogInformation($"Successfully scheduled task for {task.MacAddress} at {task.Time}");
            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to schedule task for {task.MacAddress} at {task.Time}");
            throw;
        }
    }

    public async Task<List<TimerTask>> GetScheduledTasks()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobGroups = await scheduler.GetJobGroupNames();
        var tasks = new List<TimerTask>();

        foreach (var group in jobGroups)
        {
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group));
            foreach (var jobKey in jobKeys)
            {
                var jobDetail = await scheduler.GetJobDetail(jobKey);
                if (jobDetail != null)
                {
                    var jobData = jobDetail.JobDataMap;
                    var macAddress = jobData["MacAddress"]?.ToString() ?? string.Empty;
                    var action = jobData["Action"]?.ToString() ?? string.Empty;
                    var timeValue = jobData["Time"];
                    
                    var timeStr = timeValue?.ToString() ?? string.Empty;
                    DateTimeOffset time;
                    
                    if (!string.IsNullOrEmpty(macAddress) && 
                        !string.IsNullOrEmpty(action) &&
                        DateTimeOffset.TryParse(timeStr, out time))
                    {
                        tasks.Add(new TimerTask
                        {
                            MacAddress = macAddress,
                            Action = action,
                            Time = time
                        });
                    }
                }
            }
        }
        return tasks;
    }

    public async Task CancelTask(string macAddress, DateTimeOffset time)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey($"{macAddress}_{time:o}");
        await scheduler.DeleteJob(jobKey);
    }
}
}
