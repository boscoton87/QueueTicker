using QueueTicker.Core.Models;

namespace QueueTicker.Core.Services.Interfaces {
	public interface IQueueDataPointRepository {
		Task CreateDataPoints( IEnumerable<QueueDataPoint> dataPoints );

		Task<List<QueueDataPoint>> GetDataPoints( int numberOfDataPoints = 50 );
	}
}