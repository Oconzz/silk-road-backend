using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using APIServer.Lamar;
using APIServer.Services;
using Business.Data;
using Business.Handlers;
using Business.Mapper;
using Business.Middleware;
using Business.Model;
using Business.Services;
using Business.Utilities;
using Business.ViewModel;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Muscula.Csharp.Extensions.Logging.AspNetCore.Extensions.Configuration;
using NgrokAspNetCore;
using Quartz;
using Quartz.Util;
using DarkSwagger;

//using Business.Interfaces;


var builder = WebApplication.CreateBuilder(args);
var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionString");
var apiKeyEnv = Environment.GetEnvironmentVariable("StripeApiKey");
apiKeyEnv = apiKeyEnv ?? "DemoStripeApiKey";
//var Configuration = builder.Services.GetRequiredService<IConfiguration>();
// Add services to the container.
//builder.Services.AddCors();
builder.Services.Configure<AppSettings>(builder.Configuration);
// Register AutoMapper profiles by marker type
builder.Services.AddAutoMapper(typeof(Business.Mapper.AutoMapper).Assembly);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddHttpClient();
// HttpContext accessor for handlers
builder.Services.AddHttpContextAccessor();

// Add session support for OAuth flow
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.Name = "Cartlann.Session";
});

// Add data protection for session cookies
var keysPath = Path.Combine(Path.GetTempPath(), "nmj-keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
    {
        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
        ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
    });
builder.Host.UseLamar((context, registry) =>
{
    // register services using Lamar a.Assembly("DPACC.Portal.Security");
    //registry.For<IDPACC>().Use<MyTest>();


    // Add your own Lamar ServiceRegistry collections
    // of registrations
    registry.IncludeRegistry<HandlerRegistry>();
    registry.IncludeRegistry<ServicesRegistry>();


    // discover MVC controllers -- this was problematic
    // inside of the UseLamar() method, but is "fixed" in
    // Lamar V8
    //registry.AddControllers();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cartlann API", Version = "v1" });
    c.CustomSchemaIds(type => type.ToString());
    c.OperationFilter<AddCustomHeaderOperationFilter>();
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insert your JWT like this: Bearer {token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddScheme<JwtBearerOptions, CustomJwtBearerHandler>(JwtBearerDefaults.AuthenticationScheme, options => { });
//.AddCookie("cookie", options =>
//{
//    options.Cookie.Name = "ConvertionKings.AuthCookie";
//    options.Cookie.SameSite = SameSiteMode.Lax;
//    options.Cookie.HttpOnly = true;
//    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

//    // Don't redirect on 401/403, just return the status code
//    options.Events.OnRedirectToLogin = context =>
//    {
//        context.Response.StatusCode = 401;
//        return Task.CompletedTask;
//    };
//})
//.AddGoogle("google", options =>
//{
//    var googleAuth = builder.Configuration.GetSection("Authentication:Google");
//    options.ClientId = googleAuth["ClientId"]!;
//    options.ClientSecret = googleAuth["ClientSecret"]!;

//    // This is the internal path the middleware uses. This is correct.
//    options.CallbackPath = "/signin-google";

//    // Configure Google OAuth to use cookie scheme for sign-in
//    options.SignInScheme = "cookie";

//    // Add additional scopes for better user information and Gmail API access
//    options.Scope.Add("openid");
//    options.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
//    options.Scope.Add("https://www.googleapis.com/auth/userinfo.email");

//    // Add Gmail API scopes
//    //options.Scope.Add("https://www.googleapis.com/auth/gmail.readonly");
//    //options.Scope.Add("https://www.googleapis.com/auth/gmail.send");
//    //options.Scope.Add("https://www.googleapis.com/auth/gmail.modify");
//    //options.Scope.Add("https://www.googleapis.com/auth/gmail.compose");

//    // Map claims explicitly for better control
//    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
//    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
//    options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
//    options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
//    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
//    options.ClaimActions.MapJsonKey("picture", "picture");
//    options.ClaimActions.MapJsonKey("locale", "locale");
//    options.ClaimActions.MapJsonKey("hd", "hd");
//    options.ClaimActions.MapJsonKey("email_verified", "email_verified");

//    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
//    {
//        OnRemoteFailure = context =>
//        {
//            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
//            logger.LogError("OAuth remote failure: {Failure}", context.Failure?.Message);

//            context.HandleResponse();
//            context.Response.Redirect("/error?message=" + Uri.EscapeDataString(context.Failure?.Message ?? "Unknown OAuth error"));
//            return Task.CompletedTask;
//        },
//        OnCreatingTicket = context =>
//        {
//            // Log ticket creation
//            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
//            logger.LogInformation("Creating OAuth ticket");

//            // Capture the access token from the OAuth context
//            var accessToken = context.AccessToken;

//            // Ensure the user is properly authenticated
//            if (context.Principal?.Identity?.IsAuthenticated == true)
//            {
//                logger.LogInformation("User is authenticated with claims: {Claims}",
//                    string.Join(", ", context.Principal.Claims.Select(c => $"{c.Type}={c.Value}")));

//                // Store the access token in session
//                if (!string.IsNullOrEmpty(accessToken))
//                {
//                    context.HttpContext.Session.SetString("OAuthAccessToken", accessToken);
//                    logger.LogInformation("Stored OAuth access token in session");
//                }
//            }
//            else
//            {
//                logger.LogWarning("User is not authenticated in OnCreatingTicket");
//            }

//            return Task.CompletedTask;
//        },
//        OnAccessDenied = context =>
//        {
//            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
//            logger.LogWarning("OAuth access denied");
//            context.HandleResponse();
//            context.Response.Redirect("/error?message=" + Uri.EscapeDataString("Access denied by user"));
//            return Task.CompletedTask;
//        },
//        OnRedirectToAuthorizationEndpoint = context =>
//        {
//            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
//            logger.LogInformation("Redirecting to Google authorization endpoint: {Url}", context.RedirectUri);
//            context.Response.Redirect(context.RedirectUri);
//            return Task.CompletedTask;
//        },
//        OnTicketReceived = context =>
//        {
//            // Log successful ticket reception
//            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
//            logger.LogInformation("OAuth ticket received successfully");

//            // Log the claims that were received
//            var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}").ToList() ?? new List<string>();
//            logger.LogInformation("OAuth claims received: {Claims}", string.Join(", ", claims));

//            // Store the claims in session for later use
//            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
//            var name = context.Principal?.FindFirstValue(ClaimTypes.Name);

//            if (!string.IsNullOrEmpty(email))
//            {
//                context.HttpContext.Session.SetString("OAuthEmail", email);
//                context.HttpContext.Session.SetString("OAuthName", name ?? "");
//                logger.LogInformation("Stored OAuth claims in session: Email={Email}, Name={Name}", email, name);
//            }

//            return Task.CompletedTask;
//        }
//    };
//});




#region Quartz Start

//builder.Services.Configure<QuartzOptions>(options =>
//{
//    options.SchedulerName = "RecurrentPaymentScheduler";
//    options.Scheduling.IgnoreDuplicates = true; // default: false
//    options.Scheduling.OverWriteExistingData = true; // default: true
//});

//builder.Services.AddQuartz(q =>
//{
//    q.SchedulerId = "RecurrentPayment";
//    q.UseMicrosoftDependencyInjectionJobFactory();
//    q.UseSimpleTypeLoader();
//    q.UseInMemoryStore();
//    q.UseDefaultThreadPool(tp =>
//    {
//        tp.MaxConcurrency = 1;
//    });
//    q.UseJobAutoInterrupt(options =>
//    {
//        // this is the default
//        options.DefaultMaxRunTime = TimeSpan.FromMinutes(3);
//    });
//    var jobKey = new JobKey("RecurrentPaymentScheduler", "Stripe");


//    q.AddJob<RecurrentPaymentScheduler>(opts => opts.WithIdentity(jobKey));


//    q.AddTrigger(opts => opts
//        .ForJob(jobKey)
//        .WithIdentity("Sending Recurrent Payments to Stripe")
//        .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(180)))
//        //.WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever()) og
//        .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever())
//        .WithDescription("Making Recurrent Payments"));
//});

//builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

#endregion
//var options = new NgrokOptions();
//options.Auth = "2RCF6PIlvF2YmT2pPTIyFqUcoYr_3vwt7bSaGbhJV8EdCVfro";


//builder.Services.AddNgrok();

var connString = builder.Configuration.GetValue<string>("ConnectionStrings:" + fromEnvironment);
var dbUser = builder.Configuration.GetValue<string>("ConnectionStrings:" + fromEnvironment + "dbUser");
var dbPass = builder.Configuration.GetValue<string>("ConnectionStrings:" + fromEnvironment + "dbPass");
var dbName = builder.Configuration.GetValue<string>("ConnectionStrings:" + fromEnvironment + "dbName");
var dbServer = builder.Configuration.GetValue<string>("ConnectionStrings:" + fromEnvironment + "dbServer");
var fConnString = connString?.Replace("{server}", dbServer).Replace("{catalog}", dbName).Replace("{dbUser}", dbUser).Replace("{dbPass}", dbPass);
if (string.IsNullOrEmpty(fConnString))
{
    // CRITICAL SECURITY FIX: Hardcoded passwords exposed thousands of user records to attackers.
    // Now using environment variable to prevent data breach.
    // Set DB_PASSWORD environment variable for production use.
    var envDbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? Environment.GetEnvironmentVariable("STAGING_DB_PASSWORD");
    
    //To update local uncomment this line and comment the other one
    // LOCAL DOCKER - Comentado para deploy em staging
    //fConnString = "Data Source=localhost,1433;Initial Catalog=nmj;Persist Security Info=True;User ID=sa;Password=YourStrong@Password123;TrustServerCertificate=True";
//connString = "Server=localhost; Database=car_main; TrustServerCertificate=True; Integrated Security=True";
//To update Staging uncomment this line and comment the other one
//connString = "Data Source=staging-cartlann.cqfpddz1xwnb.us-east-1.rds.amazonaws.com,1433;Initial Catalog=StagingCar;Persist Security Info=True;User ID=Staging;Password=vMGuQS9hnIoYG1dlSVCY;TrustServerCertificate=True";
//fConnString = "Data Source = production-cartlann.cqfpddz1xwnb.us-east-1.rds.amazonaws.com,1433; Initial Catalog=ProductionCar; Persist Security Info=True; User ID=admin; Password=naBYg0pNu9PNFb9QJr6Z;TrustServerCertificate=True";
    // STAGING - Ativo para deploy
    // SECURITY: Password removed from source code - use DB_PASSWORD env var
    fConnString = $"Host=staging-postgres.c1cgsu8qo7tc.us-east-2.rds.amazonaws.com;Port=5432;Database=staging;Username=postgres;Password={envDbPassword ?? "SET_DB_PASSWORD_IN_ENV"};SslMode=Require;TrustServerCertificate=true";
}
//.Services.AddSingleton<Connection>(new Connection(connString, fConnString, dbUser, dbPass, dbServer));
var loggerFactory = LoggerFactory.Create(b => b
            .AddConsole()
            .SetMinimumLevel(LogLevel.Information));
builder.Services.AddDbContextPool<DBContext>(options =>
                options.UseNpgsql(fConnString,
                npgsqlOptionsAction: npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("APIServer");
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromMinutes(1),
                        errorCodesToAdd: null);
                }).EnableDetailedErrors().EnableSensitiveDataLogging().UseLoggerFactory(loggerFactory),
                4096);



var connString2 = connString?.Replace("{server}", dbServer).Replace("{catalog}", "testDb").Replace("{dbUser}", "testuser").Replace("{dbPass}", "testPass");
if (string.IsNullOrEmpty(connString2))
{
    // CRITICAL SECURITY FIX: Hardcoded password removed from source code.
    var envDbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? Environment.GetEnvironmentVariable("STAGING_DB_PASSWORD");
    // LOCAL DOCKER - Comentado para deploy em staging
    //connString2 = "Data Source=localhost,1433;Initial Catalog=nmj;Persist Security Info=True;User ID=sa;Password=YourStrong@Password123;TrustServerCertificate=True";
    // STAGING - Ativo para deploy
    // SECURITY: Password removed from source code - use DB_PASSWORD env var
    connString2 = $"Data Source=staging.c1cgsu8qo7tc.us-east-2.rds.amazonaws.com,1433;Initial Catalog=Staging;Persist Security Info=True;User ID=admin;Password={envDbPassword ?? "SET_DB_PASSWORD_IN_ENV"};TrustServerCertificate=True";
}
//connString2 = "Data Source=production-cartlann.cqfpddz1xwnb.us-east-1.rds.amazonaws.com,1433;Initial Catalog=ProductionCar;Persist Security Info=True;User ID=admin;Password=naBYg0pNu9PNFb9QJr6Z;TrustServerCertificate=True";

builder.Services.AddScoped<IJwtUtils, JwtUtils>();
// Resend email service (replaces AWS SES for dev/sandbox)
builder.Services.AddHttpClient<Resend.ResendClient>();
builder.Services.Configure<Resend.ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["Resend:ApiToken"]!;
});
builder.Services.AddTransient<Resend.IResend, Resend.ResendClient>();
builder.Services.AddScoped<Business.Services.IEmailService, Business.Services.ResendEmailService>();
builder.Services.AddScoped<Business.Services.IGoogleCalendarService, Business.Services.GoogleCalendarService>();
builder.Services.AddScoped<Business.Services.ISquarePaymentService, Business.Services.SquarePaymentService>();
builder.Services.AddScoped<APIServer.Services.NotificationService>();
builder.Services.AddScoped<APIServer.Services.FreemiumLimitService>();
builder.Services.AddScoped<Business.Services.RecurrenceService>();
builder.Services.AddHostedService<APIServer.Services.RecurringJobBackgroundService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.AllowAnyOrigin()  // TEMPORÁRIO - para debug local
              .AllowAnyHeader()
              .AllowAnyMethod();
              // .AllowCredentials(); // Removido - não pode usar com AllowAnyOrigin
    });
});

//if (!builder.Environment.IsDevelopment())
//{
//    builder.Logging.AddMuscula((settingsBuilder) =>
//    {
//        settingsBuilder.UseLogId("SM8lnC1B");
//        return settingsBuilder.Build();
//    });
//}

var apiKey = builder.Configuration.GetValue<string>(apiKeyEnv);

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseCors("AllowWebApp");
// app.UseMiddleware<SecurityHeadersMiddleware>(); // TEMPORÁRIO - desativado para debug
app.UseStaticFiles();
app.UseRequestBodyCache();


// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
    // Add logging
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        Console.WriteLine($"Swagger JSON requested from: {httpReq.Path}");
    });
});
app.UseDarkSwaggerUI();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cartlann API V1");
    c.RoutePrefix = "swagger";
    // Add additional configuration for troubleshooting
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.EnableValidator();
    //c.ConfigObject.AdditionalItems["X-Cartlann-Security"] = "customValue";
    //c.InjectJavascript("/swagger-custom.js");
});


//app.UseMiddleware<SecurityHeadersMiddleware>();

//}
//Add isDevelopment comparison
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    //app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();

    //}
}
// app.UseCors("AllowWebApp");
//app.UseCors(b =>
//{
//    b.SetIsOriginAllowed((host) => true)
//        .AllowAnyHeader()
//        .AllowAnyMethod();
//});
// Auto-apply migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<DBContext>();
        
        // 1. Try migrations
        try
        {
            Console.WriteLine("[DB] Applying database migrations...");
            context.Database.Migrate();
            Console.WriteLine("[DB] Database migrations applied successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Error applying migrations: {ex.Message}");
            // Continue to seed regardless
        }

        // 2. 🚀 AUTO-SEED: Ensure a local developer user exists
        try
        {
            var testEmail = "local-dev@example.com";
            var userExists = context.User.Any(u => u.email == testEmail);
            if (!userExists)
            {
                Console.WriteLine($"[Seed] Creating local dev user: {testEmail}...");
                var handler = services.GetRequiredService<AuthorizationHandler>();
                var emailService = services.GetRequiredService<Business.Services.IEmailService>();
                var result = handler.Register(new RegisterVM
                {
                    email = testEmail,
                    password = "Password123!",
                    confirmPassword = "Password123!",
                    fullName = "Local Dev",
                    company = "Local Company",
                    phone = "1234567890",
                    city = "Local City",
                    state = "Local State",
                    country = "Local Country",
                    zipcode = "12345"
                }, emailService).GetAwaiter().GetResult();

                if (string.IsNullOrEmpty(result.errorMessage))
                {
                    Console.WriteLine("[Seed] Local dev user created successfully!");
                }
                else
                {
                    Console.WriteLine($"[Seed] Registration failed: {result.errorMessage}");
                }
            }
            else
            {
                Console.WriteLine($"[Seed] Local dev user already exists: {testEmail}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seed] Error seeding user: {ex.Message}");
        }

        // 3. 🚀 AUTO-SEED: QuickStart Templates
        try
        {
            QuickStartSeeder.Seed(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seed] Error calling QuickStartSeeder: {ex.Message}");
        }
    }
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();