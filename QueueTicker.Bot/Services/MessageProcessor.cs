using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using QueueTicker.Bot.Helpers;
using QueueTicker.Core.Enums;
using QueueTicker.Core.Models;
using QueueTicker.Core.Services;

namespace QueueTicker.Bot.Services {
	public class MessageProcessor {
		private readonly DiscordSocketClient _client;
		private readonly InteractionService _interactionService;
		private readonly QueueDataPointRepository _queueDataPointRepository;
		private readonly ActiveMessageRepository _activeMessageRepository;
		private readonly QueueSourceDataService _queueSourceDataService;
		private readonly ConsoleLoggingService _logger;
		private readonly IServiceProvider _serviceProvider;
		private readonly string _discordToken;

		public MessageProcessor(
			DiscordSocketClient client,
			InteractionService interactionService,
			QueueDataPointRepository queueDataPointRepository,
			ActiveMessageRepository activeMessageRepository,
			QueueSourceDataService queueSourceDataService,
			ConsoleLoggingService logger,
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
				_logger,
				UpdateTickers
			);
			_ = dataFetchProcessor.StartJob();
		}

		private async Task LogMessage( LogMessage logMessage ) {
			await _logger.LogMessage(
				( LogLevel ) logMessage.Severity,
				logMessage.Message,
				logMessage.Exception,
				logMessage.Source
			);
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
							await _logger.LogMessage( LogLevel.Warning, "Unknown message encountered, purged from tracking", e, "MessageProcessor.UpdateTickers" );
							await _activeMessageRepository.DeleteActiveMessage( message );
							activeMessages.Remove( message );
						}
					} catch ( Exception e ) {
						await _logger.LogMessage( LogLevel.Error, "Error occured updating ticker", e, "MessageProcessor.UpdateTickers" );
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
					ServerId = guildChannel.Guild.Id
				};
				var messageDeleted = await _activeMessageRepository.DeleteActiveMessage( activeMessage );
				if ( messageDeleted ) {
					await _logger.LogMessage(
						LogLevel.Info,
						$"Deleting active message: serverId -> {activeMessage.ServerId}, messageId -> {activeMessage.MessageId}"
					);
				}
			}
		}
	}
}
