using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using GreeControlAPI.Services;
using GreeControlAPI.Jobs;
using Quartz.Impl.AdoJobStore;

var builder = WebApplication.CreateBuilder(args);

// Add Quartz services
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
    
    q.UsePersistentStore(store =>
    {
store.UseSQLite(c => 
{
    c.ConnectionString = "Data Source=./quartz.db";
    c.TablePrefix = "QRTZ_";
});
        store.UseJsonSerializer();
        
        store.SetProperty("quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz");
        store.SetProperty("quartz.serializer.type", "json");
        store.SetProperty("quartz.dataSource.default.connectionString", "Data Source=quartz.db");
        store.SetProperty("quartz.dataSource.default.provider", "SQLite-Microsoft");
store.SetProperty("quartz.jobStore.driverDelegateType", "Quartz.Impl.AdoJobStore.SQLiteDelegate, Quartz");
store.SetProperty("quartz.jobStore.performSchemaValidation", "false");
    });
});

// Add Quartz hosted service
builder.Services.AddQuartzHostedService(options => {
    options.WaitForJobsToComplete = true;
});

// Add services to the container.
builder.Services.AddLogging();
builder.Services.AddSingleton<IJobFactory, SingletonJobFactory>();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AirConditionerService>(sp => 
    new AirConditionerService(
        sp.GetRequiredService<ISchedulerFactory>(),
        sp.GetRequiredService<ILogger<AirConditionerService>>(),
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHttpContextAccessor>()
    )
);
builder.Services.AddSingleton<AirConditionerJob>();

// Add Quartz hosted service
builder.Services.AddHostedService<QuartzHostedService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();
//启用静态文件
app.UseStaticFiles();
//这一步很关键
DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
//设置首页，我希望用户打开`localhost`访问到的是`wwwroot`下的Index.html文件
defaultFilesOptions.DefaultFileNames.Add("Home.html");
app.UseDefaultFiles(defaultFilesOptions);

app.UseCors("AllowAllOrigins");

app.UseAuthorization();

app.MapControllers();

app.Run();
