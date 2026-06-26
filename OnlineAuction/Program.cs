using System.Globalization;
using System.Data;
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

#endregion

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Account/Login";
    options.LogoutPath = "/Admin/Account/Logout";
    options.AccessDeniedPath = "/Admin/Account/AccessDenied";

    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);

    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path.StartsWithSegments("/hubs"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        if (context.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect(context.RedirectUri);
        }
        else
        {
            var returnUrl = context.Request.Path + context.Request.QueryString;
            var loginUrl = $"/Auth/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
            context.Response.Redirect(loginUrl);
        }

        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect(context.RedirectUri);
        }
        else
        {
            context.Response.Redirect("/Auth/Login");
        }

        return Task.CompletedTask;
    };
});

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
builder.Services.AddScoped<IAdminAuctionVerificationService, AdminAuctionVerificationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFcmService, FirebaseMessagingService>();
builder.Services.AddScoped<IRegistrationDepositService, RegistrationDepositService>();
builder.Services.AddScoped<IRegistrationDepositRefundService, RegistrationDepositRefundService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IRealtimePublisher, RealtimePublisher>();
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
        await EnsureSqliteProductNumberColumnAsync(db);
        await EnsureSqliteProductTemplateSchemaAsync(db);
        await EnsureSqliteNotificationSchemaAsync(db);
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    await ProductNumberBackfill.BackfillMissingAsync(db);
    await ProductTemplateSync.SyncAsync(db);

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var refreshTestAuctions = configuration.GetValue("SeedData:RefreshTestAuctionsOnStartup", false)
        || (app.Environment.IsDevelopment()
            && configuration.GetValue("SeedData:RefreshTestAuctionsInDevelopment", true));

    await UserSeeder.SeedAsync(db, userManager);
    await AdminSeeder.SeedAsync(db, userManager, roleManager);
    await AuctionCatalogSeeder.SeedAsync(db, refreshTestAuctions);
    await ProductNumberBackfill.BackfillMissingAsync(db);
    await ProductTemplateSync.SyncAsync(db);
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

app.MapHub<OnlineAuction.Hubs.AppHub>("/hubs/app");

#endregion

app.Run();

static async Task EnsureSqliteProductNumberColumnAsync(AuctionHouseDbContext dbContext)
{
    await using var connection = dbContext.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    var hasProductNumber = false;
    await using (var pragmaCommand = connection.CreateCommand())
    {
        pragmaCommand.CommandText = "PRAGMA table_info('products');";
        await using var reader = await pragmaCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader["name"]?.ToString(), "product_number", StringComparison.OrdinalIgnoreCase))
            {
                hasProductNumber = true;
                break;
            }
        }
    }

    if (!hasProductNumber)
    {
        await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE products ADD COLUMN product_number TEXT;");
    }

    await dbContext.Database.ExecuteSqlRawAsync(
        "UPDATE products SET product_number = printf('PRD-%08d', id) WHERE product_number IS NULL OR product_number = '';");

    var hasUniqueIndex = false;
    await using (var indexCheckCommand = connection.CreateCommand())
    {
        indexCheckCommand.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'index' AND name = 'uk_products_product_number' LIMIT 1;";
        var result = await indexCheckCommand.ExecuteScalarAsync();
        hasUniqueIndex = result is not null && result != DBNull.Value;
    }

    if (!hasUniqueIndex)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX uk_products_product_number ON products(product_number);");
    }
}

static async Task EnsureSqliteProductTemplateSchemaAsync(AuctionHouseDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS product_templates (
            id INTEGER NOT NULL CONSTRAINT PK_product_templates PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            short_description TEXT NULL,
            description_html TEXT NULL,
            primary_image TEXT NOT NULL,
            category_id INTEGER NOT NULL,
            slug TEXT NOT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NULL,
            created_by INTEGER NULL,
            updated_by INTEGER NULL,
            deleted_at TEXT NULL,
            deleted_by INTEGER NULL,
            CONSTRAINT fk_product_templates_category FOREIGN KEY (category_id) REFERENCES categories(id)
        );
        """);

    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS ix_product_templates_category_id ON product_templates(category_id);");
    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS ix_product_templates_deleted_at ON product_templates(deleted_at);");
    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS ix_product_templates_name ON product_templates(name);");
    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE UNIQUE INDEX IF NOT EXISTS uk_product_templates_slug ON product_templates(slug);");

    await using var connection = dbContext.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    await using (var pragmaCommand = connection.CreateCommand())
    {
        pragmaCommand.CommandText = "PRAGMA table_info('products');";
        await using var reader = await pragmaCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var columnName = reader["name"]?.ToString();
            if (!string.IsNullOrWhiteSpace(columnName))
            {
                existingColumns.Add(columnName);
            }
        }
    }

    if (!existingColumns.Contains("product_template_id"))
    {
        await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE products ADD COLUMN product_template_id INTEGER NULL;");
    }

    if (!existingColumns.Contains("price"))
    {
        await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE products ADD COLUMN price REAL NULL;");
    }

    if (!existingColumns.Contains("quantity"))
    {
        await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE products ADD COLUMN quantity INTEGER NOT NULL DEFAULT 1;");
    }

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS ix_products_product_template_id ON products(product_template_id);
        """);
}

static async Task EnsureSqliteNotificationSchemaAsync(AuctionHouseDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS notifications (
            id INTEGER NOT NULL CONSTRAINT PK_notifications PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            title TEXT NOT NULL,
            message TEXT NOT NULL,
            type TEXT NOT NULL,
            related_url TEXT NULL,
            is_read INTEGER NOT NULL DEFAULT 0,
            read_at TEXT NULL,
            reference_type TEXT NULL,
            reference_id INTEGER NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NULL,
            created_by INTEGER NULL,
            updated_by INTEGER NULL,
            deleted_at TEXT NULL,
            deleted_by INTEGER NULL
        );
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS user_device_tokens (
            id INTEGER NOT NULL CONSTRAINT PK_user_device_tokens PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            fcm_token TEXT NOT NULL,
            device_info TEXT NULL,
            created_at TEXT NOT NULL,
            last_used_at TEXT NOT NULL
        );
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE UNIQUE INDEX IF NOT EXISTS uk_user_device_tokens_fcm_token
        ON user_device_tokens(fcm_token);
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS ix_user_device_tokens_user_id
        ON user_device_tokens(user_id);
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS ix_notifications_user_read
        ON notifications(user_id, is_read);
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS ix_notifications_user_created
        ON notifications(user_id, created_at);
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS ix_notifications_reference
        ON notifications(reference_type, reference_id, user_id);
        """);
}
