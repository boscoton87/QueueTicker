namespace QueueTicker.Core.Models {
	public class ActiveMessage {
		public ulong ServerId { get; set; }

		public ulong ChannelId { get; set; }

		public ulong MessageId { get; set; }

		public ulong? NotifyChannelId { get; set; }
	}
}
