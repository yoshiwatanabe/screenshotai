using Storage.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ProcessingChannel>();
builder.Services.AddOptions<FolderMonitorOptions>().Bind(builder.Configuration.GetSection(nameof(FolderMonitorOptions)));
builder.Services.AddLocalStorageServices(builder.Configuration);
builder.Services.AddHostedService<FolderMonitorWorker>();
builder.Services.AddHostedService<ImageProcessorWorker>();

var host = builder.Build();
host.Run();
