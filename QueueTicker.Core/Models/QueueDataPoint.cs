namespace QueueTicker.Core.Models {
	public class QueueDataPoint {
		public int Length { get; set; }

		public DateTimeOffset QueueCheckedAt { get; set; }

		public DateTimeOffset DataFetchedAt { get; set; }
	}
}
