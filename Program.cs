using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using SigmaNotificationBackend.Data;
using SigmaNotificationBackend.Jobs;
using SigmaNotificationBackend.Models;
using SigmaNotificationBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ScheduledDashboardFetcher>();
builder.Services.AddScoped<ProductionMessageDispatcherService>();
builder.Services.AddScoped<EscalationDispatcherService>();
builder.Services.AddScoped<DailyApiFetcherJob>();


// Thêm dòng này để đăng ký AppDbContext cho job Quartz
builder.Services.AddScoped<AppDbContext>();


builder.Services.AddQuartz(q =>
{
    var fetcherJobKey = new JobKey("ScheduledDashboardFetcherJob");
    var dispatcherJobKey = new JobKey("ProductionMessageDispatcherJob"); // JobKey mới
    var escalationSupJobKey = new JobKey("EscalationSupervisorJob");
    var escalationMgrJobKey = new JobKey("EscalationManagerJob");
    var manualJobKey = new JobKey("ManualDispatcherJob");

    q.AddJob<ScheduledDashboardFetcher>(opts => opts.WithIdentity(fetcherJobKey));
    q.AddJob<ProductionMessageDispatcherService>(opts => opts.WithIdentity(dispatcherJobKey)); // Đăng ký job mới
    q.AddJob<EscalationDispatcherService>(opts => opts.WithIdentity(escalationSupJobKey));
    q.AddJob<EscalationDispatcherService>(opts => opts.WithIdentity(escalationMgrJobKey));
    q.AddJob<ManualDispatcherJob>(opts =>
        opts.WithIdentity(manualJobKey)
            .StoreDurably() // bắt buộc nếu không có trigger mặc định
    );

    var cronTimes = new[]
    {
        // "0 15 10 * * ?",  // 10:15
        // "0 5 13 * * ?",   // 13:05
        // // "0 53 15 * * ?",
        // "0 15 15 * * ?",  // 15:15
        // "0 10 18 * * ?",  // 18:10 
        // "0 30 22 * * ?"   // 22:30
        "0 0 0 * * ?"

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


        string dispatcherCron = AddMinutesToCron(cron, 1);
        q.AddTrigger(t => t
            .ForJob(dispatcherJobKey)
            .WithIdentity($"ProductionMessageDispatcherTrigger-{index}")
            .WithCronSchedule(dispatcherCron, x => x
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            )
        );

        // supervisor trigger sau 2 phút
        string supervisorCron = AddMinutesToCron(cron, 10);
        q.AddTrigger(t => t
            .ForJob(escalationSupJobKey)
            .WithIdentity($"EscalationSupervisorTrigger-{index}")
            .WithCronSchedule(supervisorCron, x => x
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            )
            .UsingJobData("Role", "supervisor")
        );

        // manager trigger sau 3 phút
        string managerCron = AddMinutesToCron(cron, 20);
        q.AddTrigger(t => t
            .ForJob(escalationMgrJobKey)
            .WithIdentity($"EscalationManagerTrigger-{index}")
            .WithCronSchedule(managerCron, x => x
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            )
            .UsingJobData("Role", "manager")
        );
    }

    var dailyApiJobKey = new JobKey("DailyApiFetcherJob");
    q.AddJob<DailyApiFetcherJob>(opts => opts.WithIdentity(dailyApiJobKey));
    q.AddTrigger(t => t
        .ForJob(dailyApiJobKey)
        .WithIdentity("DailyApiFetcherTrigger")
        .WithCronSchedule("0 0 8 * * ?", x => x
            .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
        )
    );

    static string AddMinutesToCron(string cron, int minutesToAdd)
    {
        var parts = cron.Split(' '); // ["0","15","10","*","*","?"]
        int second = int.Parse(parts[0]);
        int minute = int.Parse(parts[1]);
        int hour = int.Parse(parts[2]);

        minute += minutesToAdd;
        if (minute >= 60)
        {
            hour += minute / 60;
            minute = minute % 60;
        }
        if (hour >= 24) hour = hour % 24;

        parts[0] = second.ToString();
        parts[1] = minute.ToString();
        parts[2] = hour.ToString();
        return string.Join(" ", parts);
    }
});


builder.Services.AddQuartzHostedService();


builder.Logging.AddConsole();

builder.Services.Configure<QuartzOptions>(options =>
{
    options.SchedulerName = "DashboardScheduler";
});


builder.Services.Configure<ZaloOAuthSettings>(builder.Configuration.GetSection("ZaloOAuth"));



builder.Services.AddHttpClient();


builder.Services.AddControllers();




// var jwtSettings = builder.Configuration.GetSection("Jwt");
// var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);



// // 2. Cấu hình authentication
// builder.Services.AddAuthentication(options =>
// {
//     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
// })
// .AddJwtBearer(options =>
// {
//     options.TokenValidationParameters = new TokenValidationParameters
//     {
//         ValidateIssuer = true,
//         ValidateAudience = true,
//         ValidateLifetime = true,
//         ValidateIssuerSigningKey = true,
//         ValidIssuer = jwtSettings["Issuer"],
//         ValidAudience = jwtSettings["Audience"],
//         IssuerSigningKey = new SymmetricSecurityKey(key)
//     };
// });



builder.Services.AddScoped<ITokenStorageService, TokenStorageService>();
// ------------------------------------



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
// app.UseAuthentication();

// app.UseMiddleware<CurrentUserMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();