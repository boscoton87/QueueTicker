using System.ComponentModel.DataAnnotations;

namespace QueueTicker.Core.Models {
	public class ActiveMessageEntity {
		[Key]
		public Guid Id { get; set; }

		public ulong ServerId { get; set; }

		public ulong ChannelId { get; set; }

		public ulong MessageId { get; set; }

		public ulong? NotifyChannelId { get; set; }
	}
}
