using Microsoft.Extensions.Logging;
using QueueTicker.Core.Models;
using QueueTicker.Core.Services.Interfaces;

namespace QueueTicker.Core.Services {
	public class DataFetchProcessor {
		public delegate Task UpdateTickers( QueueDataPoint dataPoint, bool isStartOfQueue );

		private readonly IQueueDataPointRepository _queueDataPointRepository;
		private readonly IQueueSourceDataService _queueSourceDataService;
		private readonly ILogger _logger;
		private readonly UpdateTickers? _updateTickers;

		public DataFetchProcessor(
			IQueueDataPointRepository queueDataPointRepository,
			IQueueSourceDataService queueSourceDataService,
			ILogger logger,
			UpdateTickers? updateTickers
		) {
			_queueDataPointRepository = queueDataPointRepository;
			_queueSourceDataService = queueSourceDataService;
			_logger = logger;
			_updateTickers = updateTickers;
		}

		public async Task StartJob() {
			// Run the job immediately on startup, then schedule it every minute
			await PerformJob();
			var timer = new PeriodicTimer( TimeSpan.FromMinutes( 1 ) );
			while ( await timer.WaitForNextTickAsync() ) {
				await PerformJob();
			}
		}

		private async Task PerformJob() {
			try {
				_logger.LogInformation( "Data fetch job: started" );
				var dataPoints = await _queueDataPointRepository.GetDataPoints( 1 );
				var latestDataPoint = dataPoints.SingleOrDefault();
				var fetchedDataPoints = await _queueSourceDataService.GetQueueDataPoints();
				var orderedFetchedDataPoints = fetchedDataPoints.OrderBy( dp => dp.QueueCheckedAt );
				List<QueueDataPoint> dataPointsToInsert;
				if ( latestDataPoint is null ) {
					dataPointsToInsert = orderedFetchedDataPoints.ToList();
				} else {
					dataPointsToInsert = orderedFetchedDataPoints.Where( dp => dp.QueueCheckedAt > latestDataPoint.QueueCheckedAt ).ToList();
				}
				if ( dataPointsToInsert.Any() ) {
					await _queueDataPointRepository.CreateDataPoints( dataPointsToInsert );
					_logger.LogInformation( $"Data points created: {dataPointsToInsert.Count}" );
					if ( _updateTickers is not null ) {
						_logger.LogInformation( $"Updating queue tickers" );
						var lastTwoDataPoints = await _queueDataPointRepository.GetDataPoints( 2 );
						var isStartOfQueue = lastTwoDataPoints[ 1 ].Length == 0 && lastTwoDataPoints[ 0 ].Length > 0;
						await _updateTickers( lastTwoDataPoints[ 0 ], isStartOfQueue );
					}
				}
				_logger.LogInformation( "Data fetch job: completed" );
			} catch ( Exception ex ) {
				_logger.LogError( ex, "Data fetch job: failed" );
			}
		}
	}
}
