using Discord;
using QueueTicker.Core.Models;

namespace QueueTicker.Bot.Helpers {
	internal static class TickerEmbedHelpers {
		public static Embed BuildTickerEmbed( QueueDataPoint? dataPoint ) {
			var builder = new EmbedBuilder {
				Title = "Queue Ticker"
			};

			if ( dataPoint is not null ) {
				var content = $"\t{dataPoint.Length} people are in queue.";
				builder.AddField( "Benediction: ", content, true );
				builder.Timestamp = dataPoint.DataFetchedAt;
			}
			return builder.Build();
		}
	}
}
