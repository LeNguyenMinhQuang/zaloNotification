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
    var escalationJobKey = new JobKey("EscalationDispatcherJob");

    q.AddJob<ScheduledDashboardFetcher>(opts => opts.WithIdentity(fetcherJobKey));
    q.AddJob<ProductionMessageDispatcherService>(opts => opts.WithIdentity(dispatcherJobKey)); // Đăng ký job mới
    q.AddJob<EscalationDispatcherService>(opts => opts.WithIdentity(escalationJobKey));

    var cronTimes = new[]
    {
        "0 15 10 * * ?",  // 10:15
        "0 5 13 * * ?",   // 13:05
        "0 15 15 * * ?",  // 15:15
        "0 08 17 * * ?",   // 14:52----------------------
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


        string dispatcherCron = AddMinutesToCron(cron, 1);
        q.AddTrigger(t => t
            .ForJob(dispatcherJobKey)
            .WithIdentity($"ProductionMessageDispatcherTrigger-{index}")
            .WithCronSchedule(dispatcherCron, x => x
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            )
        );

        string supervisorCron = AddMinutesToCron(cron, 2); // Trigger escalation supervisor (15 phút sau);
        q.AddTrigger(t => t
            .ForJob(escalationJobKey)
            .WithIdentity($"EscalationSupervisorTrigger-{index}")
            .WithCronSchedule(supervisorCron, x => x
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            )
        );

        // Trigger escalation manager (30 phút sau)
        string managerCron = AddMinutesToCron(cron, 30);
        q.AddTrigger(t => t
            .ForJob(escalationJobKey)
            .WithIdentity($"EscalationManagerTrigger-{index}")
            .WithCronSchedule(managerCron, x => x
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            )
        );
    }

    // q.AddTrigger(t => t
    //     .ForJob(escalationJobKey)
    //     .WithIdentity("EscalationDispatcherTrigger")
    //     .WithCronSchedule("0 0/5 * * * ?", x => x
    //         .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
    //     )
    // );
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
app.UseAuthentication();

// app.UseMiddleware<CurrentUserMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();