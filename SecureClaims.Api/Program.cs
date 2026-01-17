using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureClaims.Api.Services;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

// 🛡️ Prevent claim name mapping for consistent JWT claim names (scp, aud, etc.)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IHelloService, HelloService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 🔐 Azure AD Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Authority = builder.Configuration["AzureAd:Authority"];
        options.Audience = builder.Configuration["AzureAd:Audience"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Authority,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                if (context.Response.HasStarted) return;

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 401,
                    message = "Unauthorized - Valid bearer token required."
                });
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted) return;

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 403,
                    message = "Forbidden - You do not have permission to access this resource."
                });
            }
        };
    });

// 🔐 Scope-based authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == "scp" &&
                c.Value.Split(' ').Contains("claims.access")
            )
        );
    });
});

var app = builder.Build();

// 🔥 Global Exception Handling (Must be first)
app.UseMiddleware<SecureClaims.Api.Middleware.ExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowUI");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
