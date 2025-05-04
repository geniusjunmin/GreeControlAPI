using Quartz;
using Quartz.Spi;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GreeControlAPI.Services
{
    public class SingletonJobFactory : IJobFactory, IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    public SingletonJobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return _serviceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob;
    }

    public void ReturnJob(IJob job) 
    {
        // No-op since we're using DI container
    }

    public void Dispose()
    {
        // No-op since we're using DI container
    }
}
}
