using AdaptiveTeamBuilder.Data;
using AdaptiveTeamBuilderSvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddAdaptiveTeamBuilderData(connectionString);

var azureAd = builder.Configuration.GetSection("AzureAd");
var apiClientId = azureAd["ClientId"]
    ?? throw new InvalidOperationException("AzureAd:ClientId is required.");
var apiAudience = azureAd["Audience"] ?? $"api://{apiClientId}";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        jwtOptions =>
        {
            jwtOptions.TokenValidationParameters.ValidAudiences =
            [
                apiAudience,
                apiClientId,
                $"api://{apiClientId}",
            ];
            jwtOptions.TokenValidationParameters.ValidateIssuer = true;
            jwtOptions.TokenValidationParameters.ValidateAudience = true;
            jwtOptions.TokenValidationParameters.ValidateLifetime = true;
            jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey = true;
            jwtOptions.TokenValidationParameters.NameClaimType = "preferred_username";
            jwtOptions.TokenValidationParameters.RoleClaimType = "roles";

            jwtOptions.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    logger.LogWarning(context.Exception, "JWT authentication failed.");
                    return Task.CompletedTask;
                },
            };
        },
        identityOptions =>
        {
            builder.Configuration.Bind("AzureAd", identityOptions);
        });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AccessAsUser", policy =>
        policy.RequireAuthenticatedUser()
            .RequireScope("access_as_user"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("UiDev", policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("UiDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .AllowAnonymous();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapProfileEndpoints();

app.Run();
