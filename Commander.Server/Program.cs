using Cqse.Teamscale.Profiler.Commons.Ipc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IpcConfig>();
builder.Services.AddSingleton<ProfilerIpc>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();