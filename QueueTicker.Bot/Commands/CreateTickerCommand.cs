using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using QueueTicker.Bot.Helpers;
using QueueTicker.Core.Models;
using QueueTicker.Core.Services.Interfaces;

namespace QueueTicker.Bot.Commands {
	public class CreateTickerCommand : InteractionModuleBase<SocketInteractionContext> {
		private readonly IActiveMessageRepository _activeMessageRepository;
		private readonly IQueueDataPointRepository _queueDataPointRepository;
		private readonly ILogger<CreateTickerCommand> _logger;

		public CreateTickerCommand(
			IActiveMessageRepository activeMessageRepository,
			IQueueDataPointRepository queueDataPointRepository,
			ILogger<CreateTickerCommand> logger
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
				var currentServerTickers = await _activeMessageRepository.GetActiveMessages( guildChannel.Guild.Id );
				var activeMessage = new ActiveMessage {
					MessageId = publishedMessage.Id,
					ChannelId = guildChannel.Id,
					ServerId = guildChannel.Guild.Id,
					NotifyChannelId = notifyChannel?.Id
				};
				await _activeMessageRepository.CreateActiveMessage( activeMessage );
				_logger.LogInformation( $"Created queue ticker: serverId -> {activeMessage.ServerId}, messageId -> {activeMessage.MessageId}" );
			}
		}
	}
}
