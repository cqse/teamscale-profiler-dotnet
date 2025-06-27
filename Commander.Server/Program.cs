using Cqse.Teamscale.Profiler.Commons.Ipc;
using Cqse.Teamscale.Profiler.Commander.Server;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions());
// explicitly initialize the Ipc, otherwise it might be created upon 1st request and we miss the start event in the profiler
builder.Services.AddSingleton(new ProfilerIpc(new IpcConfig("tcp://0.0.0.0:7145")));
builder.Services.AddSingleton(new ProfilerTestControllerState());
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();