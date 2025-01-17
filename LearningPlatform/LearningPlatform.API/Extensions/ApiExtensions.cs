﻿using LearningPlatform.API.Endpoints;
using LearningPlatform.Core.Enums;
using LearningPlatform.Core.Interfaces.Services;
using LearninPlatform.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LearningPlatform.API.Extensions;

public static class ApiExtensions
{
    public static void AddMappedEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCoursesEndpoints();
        app.MapLessonsEndpoints();
        app.MapUsersEndpoints();
    }

    public static void AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions!.SecretKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["secretCookie"];

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);

                policy.Requirements.Add(new PermissionRequirement([Permission.CreateCourse]));
            });
        });
    }

    public static IEndpointConventionBuilder RequirePermissions<TBuilder>(
        this TBuilder builder, params Permission[] permissions)
            where TBuilder : IEndpointConventionBuilder
    {
        return builder
            .RequireAuthorization(pb =>
                pb.AddRequirements(new PermissionRequirement(permissions)));
    }
}
