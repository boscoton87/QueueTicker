using System.Text.Json;
using Flurl.Http;
using QueueTicker.Core.Models;

namespace QueueTicker.Core.Services {
	public class QueueSourceDataService {
		public async Task<List<QueueDataPoint>> GetQueueDataPoints() {
			var pageContent = await "https://multidollar.company".GetStringAsync();
			var startKeyword = "\"data\": JSON.parse('";
			var dataStart = pageContent.IndexOf( startKeyword );
			pageContent = pageContent[ ( dataStart + startKeyword.Length ).. ];
			var dataEnd = pageContent.IndexOf( "')," );
			pageContent = pageContent[ ..dataEnd ];
			var data = JsonSerializer.Deserialize<List<DataPoint>>( pageContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true } );
			if ( data is null ) {
				return new List<QueueDataPoint>();
			}
			var fetchedAt = DateTimeOffset.UtcNow;
			return data.Select( d => new QueueDataPoint { Length = d.Y, QueueCheckedAt = DateTimeOffset.FromUnixTimeMilliseconds( d.X ), DataFetchedAt = fetchedAt } ).ToList();
		}

		private class DataPoint {
			public long X { get; set; }
			public int Y { get; set; }
		}
	}
}
