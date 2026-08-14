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

builder.Services.AddScoped<IBeliefProfileStore, BeliefProfileStore>();
builder.Services.Configure<AgentFrameworkOptions>(
    builder.Configuration.GetSection(AgentFrameworkOptions.SectionName));

var agentFramework = builder.Configuration
    .GetSection(AgentFrameworkOptions.SectionName)
    .Get<AgentFrameworkOptions>() ?? new AgentFrameworkOptions();

builder.Services.AddSingleton<ICollaborationAgentTranscriptLogger, FileCollaborationAgentTranscriptLogger>();
builder.Services.AddSingleton<GlossaryProvider>();
builder.Services.AddSingleton<IInteractionLog, FileInteractionLog>();
builder.Services.AddSingleton<IAgentRunRecorder, FileAgentRunRecorder>();
builder.Services.AddSingleton<ShadowCounterService>();
builder.Services.AddSingleton<SyntheticPersonaProvider>();
builder.Services.AddSingleton<IAdaptationApprovalPolicy, AutoApproveAdaptationPolicy>();
builder.Services.AddSingleton<StubCollaborationAdvisor>();
builder.Services.AddSingleton<StubCollaborationProfileUpdater>();

if (agentFramework.IsConfigured)
{
    builder.Services.AddSingleton<FoundryCollaborationAgents>();
    builder.Services.AddSingleton<ICollaborationAdvisor, AgentCollaborationAdvisor>();
    builder.Services.AddSingleton<ICollaborationProfileUpdater, AgentCollaborationProfileUpdater>();
}
else
{
    builder.Services.AddSingleton<ICollaborationAdvisor>(sp =>
        sp.GetRequiredService<StubCollaborationAdvisor>());
    builder.Services.AddSingleton<ICollaborationProfileUpdater>(sp =>
        sp.GetRequiredService<StubCollaborationProfileUpdater>());
}

builder.Services.AddSingleton<ICollaborationProfileUpdateQueue, CollaborationProfileUpdateQueue>();
// Registered as a resolvable singleton so the dev-only replay endpoint can reuse ProcessAsync.
builder.Services.AddSingleton<CollaborationProfileUpdateBackgroundService>();
builder.Services.AddHostedService(sp =>
    sp.GetRequiredService<CollaborationProfileUpdateBackgroundService>());

var app = builder.Build();

if (!agentFramework.IsConfigured)
{
    app.Logger.LogWarning(
        "AgentFramework:ApiKey is not set. Collaboration advise/profile update will use stubs. "
        + "Set the key with: dotnet user-secrets set \"AgentFramework:ApiKey\" \"<your-api-key>\"");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Skip HTTPS redirection in Development so the SPA on http://localhost:5173
// can call http://localhost:5106 without a CORS-breaking 307 to https://7199
// (common when debugging the API in Visual Studio with the https profile).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("UiDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .AllowAnonymous();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapProfileEndpoints();
app.MapTeamEndpoints();
app.MapContractEndpoints();
app.MapCollaborationEndpoints(includeDevEndpoints: app.Environment.IsDevelopment());

app.Run();
