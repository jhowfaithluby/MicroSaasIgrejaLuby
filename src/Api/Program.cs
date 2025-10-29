using System.Linq;
using System.Net.Mime;
using System.Threading.RateLimiting;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MicroSaas.Application;
using MicroSaas.Application.Common.Interfaces;
using MicroSaas.Application.Common.Models;
using MicroSaas.Application.Members.Commands.CreateMember;
using MicroSaas.Application.Members.Dtos;
using MicroSaas.Application.Members.Queries.GetMembers;
using MicroSaas.Infrastructure;
using MicroSaas.Infrastructure.Identity;
using MicroSaas.Infrastructure.Persistence;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromSeconds(10);
        limiterOptions.PermitLimit = 50;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 20;
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MicroSaas.Api"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddConsoleExporter())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MicroSaaS Igreja API",
        Version = "v1",
        Description = "API para gestão de membros da igreja"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT no formato: Bearer {token}"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Default")!, name: "database");

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Frontend:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

await EnsureDatabaseMigratedAndSeededAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Content-Security-Policy", "default-src 'self'; connect-src 'self' http://localhost:5173; style-src 'self' 'unsafe-inline'; img-src 'self' data:; script-src 'self'");

    await next();
});

app.UseCors("frontend");

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsJsonAsync(new ValidationProblemDetails(validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray())));
                return;
            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Erro interno do servidor",
                    Detail = exception?.Message
                });
                return;
        }
    });
});

app.UseAuthentication();
app.UseAuthorization();

var auth = app.MapGroup("/api/v1/auth");
auth.MapPost("register", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IAuthTokenService tokenService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.FullName))
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Dados inválidos",
            Detail = "Informe nome, e-mail e senha válidos."
        });
    }

    if (await userManager.FindByEmailAsync(request.Email) is not null)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Usuário existente",
            Detail = "Já existe um usuário cadastrado com este e-mail."
        });
    }

    if (!await roleManager.RoleExistsAsync(DefaultRoles.Member))
    {
        await roleManager.CreateAsync(new IdentityRole<Guid>(DefaultRoles.Member));
    }

    var user = new ApplicationUser
    {
        Email = request.Email,
        UserName = request.Email,
        FullName = request.FullName
    };

    var identityResult = await userManager.CreateAsync(user, request.Password);
    if (!identityResult.Succeeded)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Falha ao cadastrar usuário",
            Detail = string.Join(";", identityResult.Errors.Select(error => error.Description))
        });
    }

    await userManager.AddToRoleAsync(user, DefaultRoles.Member);

    var roles = await userManager.GetRolesAsync(user);
    var tokenPair = await tokenService.GenerateTokensAsync(user.Id, user.Email ?? string.Empty, roles, cancellationToken);

    return Results.Created($"/api/v1/auth/users/{user.Id}", new TokenResponse(tokenPair.AccessToken, tokenPair.RefreshToken, tokenPair.ExpiresAtUtc));
})
    .WithName("RegisterUser")
    .WithOpenApi()
    .Produces<TokenResponse>(StatusCodes.Status201Created)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

auth.MapPost("login", async (
    LoginRequest request,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuthTokenService tokenService,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
    if (!signInResult.Succeeded)
    {
        return Results.Unauthorized();
    }

    var roles = await userManager.GetRolesAsync(user);
    var tokenPair = await tokenService.GenerateTokensAsync(user.Id, user.Email ?? string.Empty, roles, cancellationToken);

    return Results.Ok(new TokenResponse(tokenPair.AccessToken, tokenPair.RefreshToken, tokenPair.ExpiresAtUtc));
})
    .WithName("Login")
    .WithOpenApi()
    .Produces<TokenResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized);

auth.MapPost("refresh", async (
    RefreshTokenRequest request,
    IAuthTokenService tokenService,
    CancellationToken cancellationToken) =>
{
    var tokenPair = await tokenService.RefreshAsync(request.RefreshToken, cancellationToken);
    if (tokenPair is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new TokenResponse(tokenPair.AccessToken, tokenPair.RefreshToken, tokenPair.ExpiresAtUtc));
})
    .WithName("RefreshToken")
    .WithOpenApi()
    .Produces<TokenResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized);

auth.MapPost("logout", async (
    RefreshTokenRequest request,
    IAuthTokenService tokenService,
    CancellationToken cancellationToken) =>
{
    await tokenService.RevokeAsync(request.RefreshToken, cancellationToken);
    return Results.NoContent();
})
    .WithName("Logout")
    .WithOpenApi()
    .Produces(StatusCodes.Status204NoContent);

var members = app.MapGroup("/api/v1/members")
    .RequireAuthorization()
    .RequireRateLimiting("fixed");
members.MapGet("", async (IMediator mediator, CancellationToken token) =>
{
    var result = await mediator.Send(new GetMembersQuery(), token);
    return Results.Ok(result);
})
    .WithName("GetMembers")
    .WithOpenApi()
    .Produces<MemberDto[]>(StatusCodes.Status200OK);

members.MapPost("", async (CreateMemberCommand command, IMediator mediator, CancellationToken token) =>
{
    var result = await mediator.Send(command, token);
    return Results.Created($"/api/v1/members/{result.Id}", result);
})
    .WithName("CreateMember")
    .WithOpenApi()
    .Produces<MemberDto>(StatusCodes.Status201Created)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

app.MapHealthChecks("/health");

await app.RunAsync();

static async Task EnsureDatabaseMigratedAndSeededAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        await dbContext.Database.MigrateAsync();
    }
    else
    {
        await dbContext.Database.EnsureCreatedAsync();
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

    if (!await roleManager.RoleExistsAsync(DefaultRoles.Member))
    {
        await roleManager.CreateAsync(new IdentityRole<Guid>(DefaultRoles.Member));
    }

    var seedAdmin = configuration.GetSection("Seed:Admin").Get<SeedUserOptions>();
    if (seedAdmin is not null && !string.IsNullOrWhiteSpace(seedAdmin.Email) && !string.IsNullOrWhiteSpace(seedAdmin.Password))
    {
        foreach (var role in seedAdmin.Roles ?? Array.Empty<string>())
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var existingAdmin = await userManager.FindByEmailAsync(seedAdmin.Email);
        if (existingAdmin is null)
        {
            var adminUser = new ApplicationUser
            {
                Email = seedAdmin.Email,
                UserName = seedAdmin.Email,
                FullName = seedAdmin.FullName
            };

            var createResult = await userManager.CreateAsync(adminUser, seedAdmin.Password);
            if (!createResult.Succeeded)
            {
                logger.LogWarning("Falha ao criar usuário administrador seed {Email}: {Errors}", seedAdmin.Email, string.Join(",", createResult.Errors.Select(error => error.Description)));
            }
            else if (seedAdmin.Roles?.Length > 0)
            {
                await userManager.AddToRolesAsync(adminUser, seedAdmin.Roles);
            }
        }
    }
}

public sealed record RegisterRequest(string FullName, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);

internal sealed class SeedUserOptions
{
    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string[] Roles { get; set; } = Array.Empty<string>();
}

internal static class DefaultRoles
{
    public const string Member = "Member";

    public const string Administrator = "Administrator";
}

public partial class Program
{
}
