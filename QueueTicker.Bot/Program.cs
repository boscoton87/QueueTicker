using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CommandLine;
using QueueTicker.Bot.Models;
using QueueTicker.Core.Models;
using Microsoft.Extensions.Hosting;
using QueueTicker.Core.Services;
using QueueTicker.Bot.Services;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using QueueTicker.Core.Services.Interfaces;

namespace QueueTicker {
	internal class Program {
		static async Task Main( string[] args ) {
			var settings = new AppSettingsEnv();
			await Parser.Default.ParseArguments<StartupOptions>( args ).WithParsedAsync( async opts => {
				if ( opts.PerformMigrations ) {
					await ApplyMigrations( settings );
				}
				if ( opts.StartHost ) {
					await StartService( settings );
				}
			} );
		}

		public static IHostBuilder CreateHostBuilder( string[] args ) {
			var settings = new AppSettingsEnv();
			return Host.CreateDefaultBuilder( args )
				.ConfigureServices( sp => {
					sp.AddDbContext<QueueTickerDbContext>( builder => BuildDbOptions( builder, settings.DbConnectionString ) );
				} );
		}

		private static DbContextOptionsBuilder BuildDbOptions( DbContextOptionsBuilder builder, string connectionString ) {
			return builder.UseMySql( connectionString, ServerVersion.AutoDetect( connectionString ) );
		}

		private static async Task ApplyMigrations( AppSettingsEnv settings ) {
			int attempts = 10;
			for ( int attempt = 0; attempt < attempts; attempt++ ) {
				try {
					using var services = ConfigureServices( settings );
					using var scope = services.CreateScope();
					using var dbContext = scope.ServiceProvider.GetRequiredService<QueueTickerDbContext>();
					dbContext.Database.Migrate();
					break;
				} catch ( Exception ) {
					if ( attempt == ( attempts - 1 ) ) {
						throw;
					}
				}
				await Task.Delay( 2000 );
			}
		}

		private static async Task StartService( AppSettingsEnv settings ) {
			using var services = ConfigureServices( settings );
			await services.GetRequiredService<MessageProcessor>().InitializeAsync();
			await Task.Delay( Timeout.Infinite );
		}

		private static ServiceProvider ConfigureServices( AppSettingsEnv settings ) {
			return new ServiceCollection()
				.AddDbContext<QueueTickerDbContext>( builder => BuildDbOptions( builder, settings.DbConnectionString ) )
				.AddSingleton( sp => new DiscordSocketClient() )
				.AddSingleton( x =>
					new InteractionService(
						x.GetRequiredService<DiscordSocketClient>(),
						new InteractionServiceConfig { LogLevel = LogSeverity.Info, DefaultRunMode = RunMode.Sync }
					)
				)
				.AddSingleton<IQueueSourceDataService>( sp => new QueueSourceDataService() )
				.AddSingleton<IQueueDataPointRepository>( sp => new QueueDataPointRepository( sp ) )
				.AddSingleton<IActiveMessageRepository>( sp => new ActiveMessageRepository( sp ) )
				.AddSingleton( sp =>
					new MessageProcessor(
						sp.GetRequiredService<DiscordSocketClient>(),
						sp.GetRequiredService<InteractionService>(),
						sp.GetRequiredService<IQueueDataPointRepository>(),
						sp.GetRequiredService<IActiveMessageRepository>(),
						sp.GetRequiredService<IQueueSourceDataService>(),
						sp.GetRequiredService<ILogger<MessageProcessor>>(),
						sp,
						settings.DiscordToken
					)
				)
				.AddLogging( lb =>
					lb.AddSimpleConsole( c => c.IncludeScopes = true )
					.SetMinimumLevel( LogLevel.Information )
					.AddFilter( "Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning )
				)
				.BuildServiceProvider();
		}
	}
}