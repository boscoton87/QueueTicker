using System.ComponentModel.DataAnnotations;

namespace QueueTicker.Core.Models {
	public class QueueDataPointEntity {
		[Key]
		public long Id { get; set; }

		public int Length { get; set; }

		public DateTimeOffset QueueCheckedAt { get; set; }

		public DateTimeOffset DataFetchedAt { get; set; }
	}
}
