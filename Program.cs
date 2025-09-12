using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SigmaNotificationBackend.Data;
using SigmaNotificationBackend.Models;
using SigmaNotificationBackend.Services;
using System.Text;
using Quartz;
using SigmaNotificationBackend.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<CheckAccessTokenService>();


builder.Services.AddOpenApi();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DashboardService>();


// Thêm dòng này để đăng ký AppDbContext cho job Quartz
builder.Services.AddScoped<AppDbContext>();

builder.Services.AddQuartz(q =>
{
    var fetcherJobKey = new JobKey("ScheduledDashboardFetcherJob");
    var dispatcherJobKey = new JobKey("ProductionMessageDispatcherJob"); // JobKey mới

    q.AddJob<ScheduledDashboardFetcher>(opts => opts.WithIdentity(fetcherJobKey));
    q.AddJob<ProductionMessageDispatcherService>(opts => opts.WithIdentity(dispatcherJobKey)); // Đăng ký job mới

    var cronTimes = new[]
    {
        "0 15 10 * * ?",  // 10:15
        "0 5 13 * * ?",   // 13:05
        "0 15 15 * * ?",  // 15:15
        "0 10 18 * * ?",  // 18:10 
        "0 30 22 * * ?"   // 22:30
    };

    foreach (var (cron, index) in cronTimes.Select((val, idx) => (val, idx)))
    {

        q.AddTrigger(t => t
            .ForJob(fetcherJobKey)
            .WithIdentity($"ScheduledDashboardFetcherTrigger-{index}")
            .WithCronSchedule(cron, x => x
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            )
        );


        string dispatcherCron = "5 " + cron.Substring(2); // Cách sửa lỗi
        q.AddTrigger(t => t
            .ForJob(dispatcherJobKey)
            .WithIdentity($"ProductionMessageDispatcherTrigger-{index}")
            .WithCronSchedule(dispatcherCron, x => x
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            )
        );
    }
});


builder.Services.AddQuartzHostedService();


builder.Logging.AddConsole();

builder.Services.Configure<QuartzOptions>(options =>
{
    options.SchedulerName = "DashboardScheduler";
});

// Đọc cấu hình ZaloOAuth từ phần "ZaloOAuth" trong appsettings.json
builder.Services.Configure<ZaloOAuthSettings>(builder.Configuration.GetSection("ZaloOAuth"));


// Đăng ký HttpClient để sử dụng trong các controller/service
builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddControllers();



// 1. Đọc config JWT từ appsettings.json
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);



// 2. Cấu hình authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});


// --- ĐĂNG KÝ TOKEN STORAGE SERVICE ---
builder.Services.AddScoped<ITokenStorageService, TokenStorageService>();
// ------------------------------------


// Đăng ký ZaloFollower
builder.Services.AddScoped<IFollowerStorageService, FollowerStorageService>();

builder.Services.AddScoped<IZaloSendService, ZaloSendService>();




// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", corsBuilder =>
    {
        corsBuilder.WithOrigins("http://localhost:8081", "http://10.10.99.10:8103")
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();
    });
});

// 3.1 Add Authorization & Connection Database 
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



// 3.2 Connection Database svn_pentaho
builder.Services.AddDbContext<DashboardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DashboardConnection")));

builder.Services.AddEndpointsApiExplorer();



var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}



// app.UseHttpsRedirection();

app.UseRouting();

app.UseStaticFiles();

app.UseCors("AllowSpecificOrigins");
app.UseAuthentication();

// app.UseMiddleware<CurrentUserMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();