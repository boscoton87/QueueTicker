using Microsoft.EntityFrameworkCore;

namespace QueueTicker.Core.Models {
	public class QueueTickerDbContext : DbContext {
		public QueueTickerDbContext( DbContextOptions options ) : base( options ) { }

		protected override void OnConfiguring( DbContextOptionsBuilder optionsBuilder ) {
			optionsBuilder.EnableSensitiveDataLogging( false );
		}

		public DbSet<ActiveMessageEntity> ActiveMessages => Set<ActiveMessageEntity>();

		public DbSet<QueueDataPointEntity> QueueDataPoints => Set<QueueDataPointEntity>();
	}
}
