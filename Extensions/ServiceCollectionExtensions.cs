using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSCache.Extensions;
using NSCache.Interfaces;
using NSCore.DatabaseContext;
using NSCore.DbContextHelper;
using NSCore.Models;
using UserManager.Data;
using UserManager.Interfaces;

namespace UserManager.Extensions
{
    /// <summary>
    /// Extension methods for configuring UserManager services in the DI container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds UserManager services with its own dedicated DbContext using NSCore DbContextHelper.
        /// This is the recommended approach for services that need their own isolated DbContext.
        /// </summary>
        /// <typeparam name="TRole">The role enumeration type.</typeparam>
        /// <typeparam name="TStatus">The status enumeration type.</typeparam>
        /// <typeparam name="TPermission">The permission enumeration type.</typeparam>
        /// <typeparam name="TTheme">The theme enumeration type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="inputModel">The database configuration.</param>
        /// <param name="applyMigrationsAutomatically">Whether to apply migrations automatically.</param>
        /// <param name="dbOptions">Optional configuration for pooling, logging, and advanced features. If not provided, uses default pool size of 128.</param>
        /// <param name="cacheOption">Optional cache configuration. If provided, configures NSCache caching services.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddUserManager<TRole, TStatus, TPermission, TTheme>(
            this IServiceCollection services,
            IDatabaseConfig inputModel,
            DbSetupOptions? dbOptions = null,
            ICacheOption? cacheOption = null,
            bool applyMigrationsAutomatically = true
            )
            where TRole : struct, Enum
            where TStatus : struct, Enum
            where TPermission : struct, Enum
            where TTheme : struct, Enum
        {
            // Register dedicated AppDbContext for UserManager using NSCore DbContextHelper
            services.AddCustomDbContextFactory<AppDbContext>(inputModel, dbOptions);

            // Register NSCache with "UserManager" cache key for service isolation
            services.AddNSCache(cacheOption, "UserManager");
            // Register ManageUsers service with cache dependencies as Scoped to prevent state corruption after idle time
			// services.AddSingleton<IManageUsers<TRole, TStatus, TPermission, TTheme>>(provider =>
            services.AddSingleton<IManageUsers<TRole, TStatus, TPermission, TTheme>>(provider =>
            {
                var contextFactory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();

                // Get cache services if configured
                ICacheManager? cacheManager = provider.GetKeyedService<ICacheManager>("UserManager");
                ICacheKeyBuilder? keyBuilder = provider.GetKeyedService<ICacheKeyBuilder>("UserManager");

                if (cacheManager is null || keyBuilder is null)
                {
                    throw new InvalidOperationException("Cache services are not properly configured for UserManager.");
                }

                return new ManageUsers<TRole, TStatus, TPermission, TTheme>(inputModel, contextFactory, cacheManager, keyBuilder, applyMigrationsAutomatically);
            });

            // Register a singleton wrapper for IHostedService that manages initialization across scopes
            services.AddSingleton<IHostedService>(provider =>
            {
				var manageUsersService = provider.GetRequiredService<IManageUsers<TRole, TStatus, TPermission, TTheme>>();
                return (ManageUsers<TRole, TStatus, TPermission, TTheme>)manageUsersService;
            });
            return services;
        }
    }
}