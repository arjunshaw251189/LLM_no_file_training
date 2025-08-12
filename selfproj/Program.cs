using Microsoft.AspNetCore.Http.Features;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = false;
    options.SerializerOptions.PropertyNamingPolicy = null;
});
//Rate Limit var
int PermitLimit_ = 20;
int Window_ = 1;
try { PermitLimit_ = int.Parse(Environment.GetEnvironmentVariable("RateLimiter_PermitLimit").ToString()); } catch { }
try { Window_ = int.Parse(Environment.GetEnvironmentVariable("RateLimiter_TimeSpan").ToString()); } catch { }
////Rate Limit
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey: httpContext.Request.Headers.Host.ToString(), partition =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = PermitLimit_,
                AutoReplenishment = true,
                Window = TimeSpan.FromSeconds(Window_)
            });
    });
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("why? STOP, get some life", cancellationToken: token);
    };
});
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();

//Some essential files

app.Use(async (context, next) =>
{
    var httpMaxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();

    context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");
    context.Response.Headers.Add("Feature-Policy", new[] { "accelerometer 'none'; "+
        "camera 'none'; geolocation 'self'; gyroscope 'none'; magnetometer 'none'; payment 'none'; usb 'none'" });
    context.Response.Headers.Add("X-AspNet-Version", "arjunsocialcoding");
    context.Response.Headers.Add("X-Powered-By", "arjunsocialcoding");
    context.Response.Headers.Add("User-Agent", "arjunsocialcoding");
    context.Response.Headers.Add("server", "arjunsocialcoding");

    httpMaxRequestBodySizeFeature.MaxRequestBodySize = long.MaxValue;
    await next();
});
app.UseXXssProtection(options => options.EnabledWithBlockMode());
app.UseXfo(options => options.Deny());
app.UseXContentTypeOptions();
app.UseReferrerPolicy(options => options.NoReferrer());
app.UseHsts(hsts => hsts.MaxAge(365).IncludeSubdomains());
app.UseCsp(opts => opts
 .BlockAllMixedContent()
 .ScriptSources(s => s.Self())
 .FormActions(s => s.Self())
 .FrameAncestors(s => s.Self())
 .ConnectSources(s => s.Self())
 .ObjectSources(s => s.Self())
 .FrameSources(s => s.Self())
 );

if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();

}
app.UseCors(options => options.WithOrigins("*").WithMethods(["POST", "GET"]).AllowAnyHeader());
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

