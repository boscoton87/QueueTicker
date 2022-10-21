using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueueTicker.Core.Models;
using QueueTicker.Core.Services.Interfaces;

namespace QueueTicker.Core.Services {
	public class QueueDataPointRepository : IQueueDataPointRepository {
		private readonly IServiceProvider _serviceProvider;

		public QueueDataPointRepository( IServiceProvider serviceProvider ) {
			_serviceProvider = serviceProvider;
		}

		public async Task<List<QueueDataPoint>> GetDataPoints( int numberOfDataPoints = 50 ) {
			using var scope = _serviceProvider.CreateScope();
			using QueueTickerDbContext dbContext = scope.ServiceProvider.GetRequiredService<QueueTickerDbContext>();
			return await dbContext.QueueDataPoints
				.OrderByDescending( qdp => qdp.Id )
				.Take( numberOfDataPoints )
				.Select( dp => new QueueDataPoint { Length = dp.Length, QueueCheckedAt = dp.QueueCheckedAt, DataFetchedAt = dp.DataFetchedAt } )
				.ToListAsync();
		}

		public async Task CreateDataPoints( IEnumerable<QueueDataPoint> dataPoints ) {
			using var scope = _serviceProvider.CreateScope();
			using QueueTickerDbContext dbContext = scope.ServiceProvider.GetRequiredService<QueueTickerDbContext>();
			var entities = dataPoints.Select( dp => new QueueDataPointEntity { Length = dp.Length, QueueCheckedAt = dp.QueueCheckedAt, DataFetchedAt = dp.DataFetchedAt } );
			dbContext.QueueDataPoints.AddRange( entities );
			await dbContext.SaveChangesAsync();
		}
	}
}
