using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Data;
using OnlineAuction.Data.Seeders;
using OnlineAuction.Entities;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

#region MVC + Localization

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
var mvcBuilder = builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(OnlineAuction.SharedResource));
    });

if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("vi-VN"),
        new CultureInfo("ja-JP"),
        new CultureInfo("ko-KR")
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;

    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider
    {
        CookieName = CookieRequestCultureProvider.DefaultCookieName
    });
});

#endregion

#region Session

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

#endregion

#region Database

var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "MySql";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AuctionHouseDbContext>(options =>
{
    if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        var serverVersion = ServerVersion.Parse("8.0.36-mysql");
        options.UseMySql(connectionString, serverVersion, mySql =>
        {
            mySql.MigrationsHistoryTable("__ef_migrations_history");
            mySql.EnableRetryOnFailure();
        });
    }
});

#endregion

#region Identity (FIXED ARCHITECTURE)

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.User.RequireUniqueEmail = true;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AuctionHouseDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".AuctionHouse.User";
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/Login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);

    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        var returnUrl = context.Request.Path + context.Request.QueryString;
        var loginUrl = $"/Auth/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
        context.Response.Redirect(loginUrl);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        context.Response.Redirect("/Auth/Login");
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthentication()
    .AddCookie(AuthSchemes.Admin, options =>
    {
        options.Cookie.Name = ".AuctionHouse.Admin";
        options.Cookie.Path = "/Admin";
        options.LoginPath = "/Admin/Account/Login";
        options.LogoutPath = "/Admin/Account/Logout";
        options.AccessDeniedPath = "/Admin/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

#endregion

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

#region Cloudinary + Services

builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.Configure<PayPalSettings>(
    builder.Configuration.GetSection(PayPalSettings.SectionName));
builder.Services.Configure<FirebaseSettings>(
    builder.Configuration.GetSection(FirebaseSettings.SectionName));

builder.Services.AddHttpClient<IPayPalService, PayPalService>();

builder.Services.AddScoped<IAvatarStorageService, CloudinaryAvatarStorageService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuctionService, AuctionService>();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<IAuctionRegistrationService, AuctionRegistrationService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderCreationService, OrderCreationService>();
builder.Services.AddScoped<IOrderPaymentService, OrderPaymentService>();
builder.Services.AddHostedService<AuctionFinalizationWorker>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISellService, SellService>();
builder.Services.AddScoped<ISellerAuctionService, SellerAuctionService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFcmService, FirebaseMessagingService>();
builder.Services.AddScoped<IRegistrationDepositService, RegistrationDepositService>();
builder.Services.AddScoped<IRegistrationDepositRefundService, RegistrationDepositRefundService>();
#endregion

var firebaseSettings = builder.Configuration
    .GetSection(FirebaseSettings.SectionName)
    .Get<FirebaseSettings>() ?? new FirebaseSettings();

var app = builder.Build();

FirebaseMessagingService.Initialize(
    firebaseSettings,
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Firebase"));

#region DB Init + Seeders

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuctionHouseDbContext>();

    if (db.Database.ProviderName?.Contains("Sqlite") == true)
    {
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var refreshTestAuctions = configuration.GetValue("SeedData:RefreshTestAuctionsOnStartup", false)
        || (app.Environment.IsDevelopment()
            && configuration.GetValue("SeedData:RefreshTestAuctionsInDevelopment", true));

    await UserSeeder.SeedAsync(db, userManager);
    await AdminSeeder.SeedAsync(db, userManager, roleManager);
    await AuctionCatalogSeeder.SeedAsync(db, refreshTestAuctions);
}

using (var scope = app.Services.CreateScope())
{
    var orderCreationService = scope.ServiceProvider.GetRequiredService<IOrderCreationService>();
    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
    await orderCreationService.FinalizeExpiredAuctionsAsync();
    await orderService.CancelAllExpiredPendingOrdersAsync();
}

#endregion

#region Pipeline

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRequestLocalization(
    app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

#endregion

#region Routes

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

#endregion

app.Run();
