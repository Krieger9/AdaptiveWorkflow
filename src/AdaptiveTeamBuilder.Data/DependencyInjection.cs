using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdaptiveTeamBuilder.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddAdaptiveTeamBuilderData(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AdaptiveTeamBuilderDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
