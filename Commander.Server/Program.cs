using Cqse.Teamscale.Profiler.Commons.Ipc;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions());
// explicitly initialize the Ipc, otherwise it might be created upon 1st request and we miss the start event in the profiler
builder.Services.AddSingleton<ProfilerIpc>(new ProfilerIpc(new IpcConfig()));
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();