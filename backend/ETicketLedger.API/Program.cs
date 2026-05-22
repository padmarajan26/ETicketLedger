using ETicketLedger.API.BackgroundServices;
using ETicketLedger.API.Data;
using ETicketLedger.API.DTOs;
using ETicketLedger.API.Handlers;
using ETicketLedger.API.Services;
using ETicketLedger.API.Validators;
using ETicketLedger.API.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

// ── Bootstrap Serilog early so startup errors are captured ────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console());

    // ── Database ──────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(3)));

    // ── Payment Handlers (Strategy Pattern) ───────────────────────────────────
    builder.Services.AddScoped<IPaymentHandler, CreditCardHandler>();
    builder.Services.AddScoped<IPaymentHandler, QRHandler>();
    builder.Services.AddScoped<IPaymentHandlerFactory, PaymentHandlerFactory>();

    // ── Application Services ──────────────────────────────────────────────────
    builder.Services.AddScoped<ITicketService, TicketService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<ILedgerService, LedgerService>();

    // ── Background Services ───────────────────────────────────────────────────
    builder.Services.AddSingleton<IQRConfirmationQueue, QRConfirmationQueue>();
    builder.Services.AddHostedService<QRConfirmationWorker>();

    // ── FluentValidation ──────────────────────────────────────────────────────
    builder.Services.AddValidatorsFromAssemblyContaining<CheckoutRequestValidator>();

    // ── Controllers ───────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // ── Swagger / OpenAPI ─────────────────────────────────────────────────────
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "ETicketLedger API",
            Version     = "v1",
            Description = "E-Ticketing & Payment Simulation Platform with Double-Entry Ledger"
        });
        // Include XML comments if generated
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    });

    // ── CORS (allow React dev server) ─────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
            policy.WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:5174",
                    "http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Auto-migrate & seed on startup ────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ETicketLedger API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseCors("AllowFrontend");
    app.UseAuthorization();
    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}