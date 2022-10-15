using System.Runtime.CompilerServices;
using System.Text;
using QueueTicker.Core.Enums;

namespace QueueTicker.Core.Services {
	public class ConsoleLoggingService {

		private readonly LogLevel _minimumLevel;

		private readonly ConsoleColor _defaultColor;

		private readonly int _indentSize;

		private readonly SemaphoreSlim _loggingLock;

		public ConsoleLoggingService( LogLevel minimumLevel ) {
			_minimumLevel = minimumLevel;
			_defaultColor = Console.ForegroundColor;
			_indentSize = 6;
			_loggingLock = new SemaphoreSlim( 1, 1 );
		}

		public async Task LogMessage( LogLevel logLevel, string message, Exception? exception = null, [CallerMemberName] string context = "" ) {
			if ( logLevel > _minimumLevel ) {
				return;
			}
			try {
				await _loggingLock.WaitAsync();
				PerformLogging( logLevel, message, exception, context );
			} finally {
				_loggingLock.Release();
			}
		}

		private void PerformLogging( LogLevel logLevel, string message, Exception? exception, string context ) {
			Console.ForegroundColor = GetColor( logLevel );
			Console.Write( $"{logLevel}" );
			Console.ForegroundColor = _defaultColor;
			Console.WriteLine( $": {context} - {DateTime.UtcNow}" );
			var indent = GetIndent( 1 );

			if ( !string.IsNullOrWhiteSpace( message ) ) {
				var formattedMessage = message.Replace( "\r\n", $"\r\n{indent}" );
				Console.WriteLine( $"{indent}Message: {formattedMessage}" );
			}

			if ( exception != null ) {
				var formattedException = exception.ToString().Replace( "\r\n", $"\r\n{indent}" );
				Console.WriteLine( $"{indent}Exception: {formattedException}" );
			}
		}

		private string GetIndent( int numberOfIndents ) {
			var indentBuilder = new StringBuilder( _indentSize * numberOfIndents );
			for ( var index = 0; index < _indentSize * numberOfIndents; index++ ) {
				indentBuilder.Append( ' ' );
			}
			return indentBuilder.ToString();
		}

		private ConsoleColor GetColor( LogLevel logLevel ) {
			switch ( logLevel ) {
				case LogLevel.Debug:
					return ConsoleColor.DarkGray;
				case LogLevel.Info:
					return ConsoleColor.DarkGreen;
				case LogLevel.Warning:
					return ConsoleColor.DarkYellow;
				case LogLevel.Error:
					return ConsoleColor.DarkRed;
				case LogLevel.Critical:
					return ConsoleColor.DarkMagenta;
				default:
					return ConsoleColor.DarkCyan;
			}
		}
	}
}
