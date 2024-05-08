var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();
//���þ�̬�ļ�
app.UseStaticFiles();
//��һ���ܹؼ�
DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
//������ҳ����ϣ���û���`localhost`���ʵ�����`wwwroot`�µ�Index.html�ļ�
defaultFilesOptions.DefaultFileNames.Add("Home.html");
app.UseDefaultFiles(defaultFilesOptions);


app.UseCors("AllowAllOrigins");

app.UseAuthorization();

app.MapControllers();

app.Run();
