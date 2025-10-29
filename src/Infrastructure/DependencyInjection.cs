using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MicroSaas.Application.Common.Interfaces;
using MicroSaas.Infrastructure.Identity;
using MicroSaas.Infrastructure.Persistence;
using MicroSaas.Infrastructure.Repositories;

namespace MicroSaas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration.GetValue<string>("Database:Provider");

        if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var sqliteConnectionString = configuration.GetConnectionString("Default")
                ?? "Data Source=microsaas.db";

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(sqliteConnectionString);
            });
        }
        else
        {
            var sqlServerConnectionString = configuration.GetConnectionString("Default")
                ?? "Server=(localdb)\\mssqllocaldb;Database=MicroSaasIgreja;Trusted_Connection=True;MultipleActiveResultSets=true";

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(sqlServerConnectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure();
                });
            });
        }

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(jwt => !string.IsNullOrWhiteSpace(jwt.SigningKey), "Jwt:SigningKey configuration is required")
            .ValidateOnStart();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
        {
            throw new InvalidOperationException("Configure Jwt:SigningKey in appsettings.json or environment variables");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        services.AddScoped<IAuthTokenService, TokenService>();
        services.AddScoped<IMemberRepository, MemberRepository>();

        return services;
    }
}
