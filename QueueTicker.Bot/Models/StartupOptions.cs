using CommandLine;

namespace QueueTicker.Bot.Models {
	public class StartupOptions {
		[Option( 'm', "migrations", Required = false, HelpText = "Perform database migrations." )]
		public bool PerformMigrations { get; set; }

		[Option( 's', "startHost", Required = false, HelpText = "Start the Discord Bot Host." )]
		public bool StartHost { get; set; }
	}
}
