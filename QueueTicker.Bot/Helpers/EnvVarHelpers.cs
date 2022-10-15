namespace QueueTicker.Bot.Helpers {
	public static class EnvVarHelpers {
		public static string GetEnvVar( string envVarName, string defaultValue, bool assert = false ) {
			var envVar = Environment.GetEnvironmentVariable( envVarName );
			if ( envVar == null && assert ) {
				throw new NotSupportedException( $"Environment Variable ( {envVarName} ) must be defined." );
			}
			return envVar ?? defaultValue;
		}
	}
}
