using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueueTicker.Bot.Helpers;
using QueueTicker.Core.Models;
using QueueTicker.Core.Services;
using QueueTicker.Core.Services.Interfaces;

namespace QueueTicker.Bot.Services {
	public class MessageProcessor {
		private readonly DiscordSocketClient _client;
		private readonly InteractionService _interactionService;
		private readonly IQueueDataPointRepository _queueDataPointRepository;
		private readonly IActiveMessageRepository _activeMessageRepository;
		private readonly IQueueSourceDataService _queueSourceDataService;
		private readonly ILogger _logger;
		private readonly IServiceProvider _serviceProvider;
		private readonly string _discordToken;

		public MessageProcessor(
			DiscordSocketClient client,
			InteractionService interactionService,
			IQueueDataPointRepository queueDataPointRepository,
			IActiveMessageRepository activeMessageRepository,
			IQueueSourceDataService queueSourceDataService,
			ILogger logger,
			IServiceProvider serviceProvider,
			string discordToken
		) {
			_client = client;
			_interactionService = interactionService;
			_queueDataPointRepository = queueDataPointRepository;
			_activeMessageRepository = activeMessageRepository;
			_queueSourceDataService = queueSourceDataService;
			_logger = logger;
			_serviceProvider = serviceProvider;
			_discordToken = discordToken;
		}

		public async Task InitializeAsync() {
			StartDataFetchJob();
			_interactionService.Log += LogMessage;
			_client.Log += LogMessage;
			_client.MessageDeleted += OnMessageDeleted;
			_client.Ready += ClientReadyHandler;
			await _client.LoginAsync( TokenType.Bot, _discordToken );
			await _client.StartAsync();
		}

		private async Task ClientReadyHandler() {
			await _interactionService.AddModulesAsync(
				typeof( MessageProcessor ).Assembly,
				_serviceProvider
			);
			await _interactionService.RegisterCommandsGloballyAsync();
			_client.InteractionCreated += async interaction => {
				using var scope = _serviceProvider.CreateScope();
				var ctx = new SocketInteractionContext( _client, interaction );
				var result = await _interactionService.ExecuteCommandAsync( ctx, scope.ServiceProvider );
			};
			await _client.SetGameAsync( "To create a queue ticker use /create-queue-ticker" );
		}

		private void StartDataFetchJob() {
			var dataFetchProcessor = new DataFetchProcessor(
				_queueDataPointRepository,
				_queueSourceDataService,
				_serviceProvider.GetRequiredService<ILogger<DataFetchProcessor>>(),
				UpdateTickers
			);
			_ = dataFetchProcessor.StartJob();
		}

		private Task LogMessage( LogMessage logMessage ) {
			_logger.Log(
				ConverSeverityToLogLevel( logMessage.Severity ),
				logMessage.Exception,
				logMessage.Message,
				logMessage.Source
			);
			return Task.CompletedTask;
		}

		private LogLevel ConverSeverityToLogLevel( LogSeverity severity ) {
			return severity switch {
				LogSeverity.Debug => LogLevel.Debug,
				LogSeverity.Verbose => LogLevel.Trace,
				LogSeverity.Info => LogLevel.Information,
				LogSeverity.Warning => LogLevel.Warning,
				LogSeverity.Error => LogLevel.Error,
				LogSeverity.Critical => LogLevel.Critical,
				_ => LogLevel.None
			};
		}

		private async Task UpdateTickers( QueueDataPoint dataPoint, bool isStartOfQueue ) {
			var activeMessages = await _activeMessageRepository.GetActiveMessages();
			foreach ( var message in activeMessages.ToList() ) {
				var channel = await _client.GetChannelAsync( message.ChannelId );
				if ( channel is IMessageChannel messageChannel ) {
					try {
						await messageChannel.ModifyMessageAsync( message.MessageId, mp => mp.Embed = TickerEmbedHelpers.BuildTickerEmbed( dataPoint ) );
					} catch ( HttpException e ) {
						if ( e.DiscordCode == DiscordErrorCode.UnknownMessage ) {
							_logger.LogWarning( "Unknown message encountered, purged from tracking" );
							await _activeMessageRepository.DeleteActiveMessage( message );
							activeMessages.Remove( message );
						}
					} catch ( Exception e ) {
						_logger.LogError( e, "Error occured updating ticker" );
					}
				}
			}
			if ( isStartOfQueue ) {
				foreach ( var channelId in activeMessages.Where( am => am.NotifyChannelId != null ).Select( am => am.NotifyChannelId!.Value ).Distinct() ) {
					var notifyChannel = await _client.GetChannelAsync( channelId );
					if ( notifyChannel is IMessageChannel messageChannel ) {
						await messageChannel.SendMessageAsync( "A queue has started on Benediction." );
					}
				}
			}
		}

		private async Task OnMessageDeleted( Cacheable<IMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedMessageChannel ) {
			var channel = await cachedMessageChannel.GetOrDownloadAsync();
			if ( channel is IGuildChannel guildChannel ) {
				var activeMessage = new ActiveMessage {
					MessageId = cachedMessage.Id,
					ServerId = guildChannel.Guild.Id,
					ChannelId = guildChannel.Id
				};
				var messageDeleted = await _activeMessageRepository.DeleteActiveMessage( activeMessage );
				if ( messageDeleted ) {
					_logger.LogInformation(
						$"Deleting active message: serverId -> {activeMessage.ServerId}, messageId -> {activeMessage.MessageId}"
					);
				}
			}
		}
	}
}
