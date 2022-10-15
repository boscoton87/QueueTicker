using QueueTicker.Core.Enums;
using QueueTicker.Core.Models;

namespace QueueTicker.Core.Services {
	public class DataFetchProcessor {
		public delegate Task UpdateTickers( QueueDataPoint dataPoint, bool isStartOfQueue );

		private readonly QueueDataPointRepository _queueDataPointRepository;
		private readonly QueueSourceDataService _queueSourceDataService;
		private readonly ConsoleLoggingService _consoleLoggingService;
		private readonly UpdateTickers? _updateTickers;

		public DataFetchProcessor(
			QueueDataPointRepository queueDataPointRepository,
			QueueSourceDataService queueSourceDataService,
			ConsoleLoggingService consoleLoggingService,
			UpdateTickers? updateTickers
		) {
			_queueDataPointRepository = queueDataPointRepository;
			_queueSourceDataService = queueSourceDataService;
			_consoleLoggingService = consoleLoggingService;
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
			await _consoleLoggingService.LogMessage( LogLevel.Info, "Data fetch job: started", context: "DataFetchProcessor" );
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
				await _consoleLoggingService.LogMessage( LogLevel.Info, $"Data points created: {dataPointsToInsert.Count}", context: "DataFetchProcessor" );
				if ( _updateTickers is not null ) {
					await _consoleLoggingService.LogMessage( LogLevel.Info, $"Updating queue tickers", context: "DataFetchProcessor" );
					var lastTwoDataPoints = await _queueDataPointRepository.GetDataPoints( 2 );
					var isStartOfQueue = lastTwoDataPoints[ 1 ].Length == 0 && lastTwoDataPoints[ 0 ].Length > 0;
					await _updateTickers( lastTwoDataPoints[ 0 ], isStartOfQueue );
				}
			}
			await _consoleLoggingService.LogMessage( LogLevel.Info, "Data fetch job: completed", context: "DataFetchProcessor" );
		}
	}
}
