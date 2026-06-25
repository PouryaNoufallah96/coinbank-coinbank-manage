using Autofac;
using Autofac.Extensions.DependencyInjection;
using CoinBank.Manage.Utilities.Configurations;
using CoinBank.Manage.Utilities.Middlewares;
using Utilities.Configuration;
using Utilities.Exceptions.Common;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCustomControllers();

builder.Services.AddCustomApiVersioning();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwagger();

builder.Services.AddHttpClient();

builder.Services.AddMemoryCache();

builder.Services.AddCodeAssistantSettings(builder.Configuration);

builder.Services.AddSettings(builder.Configuration);



builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(autofacConfigure =>
{
    autofacConfigure.AddServices();
    autofacConfigure.AddControllerServices();
});

builder.WebHost.UseSentry(o =>
{
    o.Dsn = "https://30c849fb66da66c45623128c8bc84f76@o4510492345368576.ingest.de.sentry.io/4511528763326544";
    o.Environment = builder.Environment.EnvironmentName;
    o.TracesSampleRate = 0.1;
    o.AttachStacktrace = true;
    o.SendDefaultPii = false;
    o.Debug = builder.Environment.IsDevelopment();
    o.IncludeActivityData = true;

    o.SetBeforeSend((evt, _) =>
    {
        if (evt.Exception is BaseException be && (int)be.HttpStatusCode < 500)
            return null;

        if (IsTransientNetworkException(evt.Exception))
            return null;

        return evt;
    });
});

static bool IsTransientNetworkException(Exception ex)
{
    for (var e = ex; e != null; e = e.InnerException)
    {
        if (e is OperationCanceledException
              or System.Net.WebSockets.WebSocketException
              or System.Net.Sockets.SocketException
              or System.IO.IOException
              or TimeoutException
              or System.Net.Http.HttpRequestException)
            return true;

    }

    return false;
}


var app = builder.Build();

app.UseHsts(app.Environment);

app.UseDeveloperExceptionPage(app.Environment);

app.UseSwaggerAndUI();

app.UseRequestLogger();

app.UseCustomExceptionHandler();

app.UseJWTBlackList();

app.UseProductionCors();

app.UseFirewall();

app.UseSignature();

app.UseJwt();

app.UseRouting();

app.UseCustomRateLimiting();

app.UseAuthorization();

app.UseEndpoints();

app.Run();
