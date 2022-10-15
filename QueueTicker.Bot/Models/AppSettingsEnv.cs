using QueueTicker.Bot.Helpers;

namespace QueueTicker.Bot.Models {
	public class AppSettingsEnv {
		public string DbConnectionString => EnvVarHelpers.GetEnvVar( "QueueTicker_DB_Conn", string.Empty, true );
		public string DiscordToken => EnvVarHelpers.GetEnvVar( "QueueTicker_Token_Discord", string.Empty, true );
	}
}
