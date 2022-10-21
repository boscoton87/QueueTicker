using QueueTicker.Core.Models;

namespace QueueTicker.Core.Services.Interfaces {
	public interface IQueueSourceDataService {
		Task<List<QueueDataPoint>> GetQueueDataPoints();
	}
}