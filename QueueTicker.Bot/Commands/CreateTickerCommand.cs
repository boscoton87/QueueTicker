using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using QueueTicker.Bot.Helpers;
using QueueTicker.Core.Enums;
using QueueTicker.Core.Models;
using QueueTicker.Core.Services;

namespace QueueTicker.Bot.Commands {
	public class CreateTickerCommand : InteractionModuleBase<SocketInteractionContext> {
		private readonly ActiveMessageRepository _activeMessageRepository;
		private readonly QueueDataPointRepository _queueDataPointRepository;
		private readonly ConsoleLoggingService _logger;

		public CreateTickerCommand(
			ActiveMessageRepository activeMessageRepository,
			QueueDataPointRepository queueDataPointRepository,
			ConsoleLoggingService logger
		) {
			_activeMessageRepository = activeMessageRepository;
			_queueDataPointRepository = queueDataPointRepository;
			_logger = logger;
		}

		[SlashCommand( "create-queue-ticker", "Create a queue ticker to track current queue length" )]
		public async Task CreateQueueTicker( SocketChannel? notifyChannel = null ) {
			await RespondAsync( "Creating queue ticker...", ephemeral: true );
			var dataPoints = await _queueDataPointRepository.GetDataPoints( 1 );
			var publishedMessage = await ReplyAsync( embed: TickerEmbedHelpers.BuildTickerEmbed( dataPoints.SingleOrDefault() ) );
			if ( publishedMessage.Channel is IGuildChannel guildChannel ) {
				var activeMessage = new ActiveMessage {
					MessageId = publishedMessage.Id,
					ChannelId = guildChannel.Id,
					ServerId = guildChannel.Guild.Id,
					NotifyChannelId = notifyChannel?.Id
				};
				await _activeMessageRepository.CreateActiveMessage( activeMessage );
				await _logger.LogMessage( LogLevel.Info, $"Created queue ticker: serverId -> {activeMessage.ServerId}, messageId -> {activeMessage.MessageId}" );
			}
		}
	}
}
