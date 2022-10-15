using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueueTicker.Core.Models;

namespace QueueTicker.Core.Services {
	public class ActiveMessageRepository {
		private readonly IServiceProvider _serviceProvider;

		public ActiveMessageRepository( IServiceProvider serviceProvider ) {
			_serviceProvider = serviceProvider;
		}

		public async Task<List<ActiveMessage>> GetActiveMessages() {
			using var scope = _serviceProvider.CreateScope();
			using QueueTickerDbContext dbContext = scope.ServiceProvider.GetRequiredService<QueueTickerDbContext>();
			return await dbContext.ActiveMessages
				.Select( am => new ActiveMessage {
					MessageId = am.MessageId,
					ChannelId = am.ChannelId,
					ServerId = am.ServerId,
					NotifyChannelId = am.NotifyChannelId
				}
				)
				.ToListAsync();
		}

		public async Task CreateActiveMessage( ActiveMessage activeMessage ) {
			using var scope = _serviceProvider.CreateScope();
			using QueueTickerDbContext dbContext = scope.ServiceProvider.GetRequiredService<QueueTickerDbContext>();
			var exists = await dbContext.ActiveMessages.AnyAsync( am =>
				am.MessageId == activeMessage.MessageId
				&& am.ChannelId == activeMessage.ChannelId
				&& am.ServerId == activeMessage.ServerId
			);
			if ( !exists ) {
				dbContext.ActiveMessages.Add(
					new ActiveMessageEntity {
						MessageId = activeMessage.MessageId,
						ChannelId = activeMessage.ChannelId,
						ServerId = activeMessage.ServerId,
						NotifyChannelId = activeMessage.NotifyChannelId
					}
				);
				await dbContext.SaveChangesAsync();
			}
		}

		public async Task<bool> DeleteActiveMessage( ActiveMessage activeMessage ) {
			using var scope = _serviceProvider.CreateScope();
			using QueueTickerDbContext dbContext = scope.ServiceProvider.GetRequiredService<QueueTickerDbContext>();
			var entities = await dbContext.ActiveMessages.Where( am =>
				am.MessageId == activeMessage.MessageId
				&& am.ChannelId == activeMessage.ChannelId
				&& am.ServerId == activeMessage.ServerId
			).ToListAsync();
			if ( entities.Any() ) {
				dbContext.ActiveMessages.RemoveRange( entities );
				await dbContext.SaveChangesAsync();
				return true;
			}
			return false;
		}
	}
}
