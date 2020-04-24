using System;
using System.Windows.Forms;
using System.Net.Mail;
using System.Resources;
using System.Text.RegularExpressions;
using System.IO;

namespace Want.Logging
{
	/// <summary>
	/// The Logger class provides a central logging singleton, writing log records both to a logfile 
	/// and displaying warnings and error messages using a Forms.MessageBox.
	/// The Logger will automatically add a timestamp and indication of the MessageType to the log
	/// record.
	/// </summary>
	/// <history>
	/// 2005-06-22 sin Created
	/// </history>
	public class Logger
	{
		/// <summary>
		/// The message types WARN, FAIL and ERROR will be force a log record into the Windows
		/// Event Log. All other message types only log records to the logfile.
		/// </summary>
		public enum MessageType
		{
			/// <summary>Failure audit message</summary>
			FAIL = 1,
			/// <summary>Error message</summary>
			ERROR = 2,
			/// <summary>Warning message</summary>
			WARN = 3,
			/// <summary>Informational message</summary>
			INFO = 4,
			/// <summary>Debug message</summary>
			DEBUG = 5
		}

		
		private static Logger logger;
		private static Logger.MessageType globalLogLevel;

        private TextBox _txtBox;

		/// <summary>
		/// Return the singleton instance of the logging component.
		/// </summary>
		/// <returns>The Logger singleton.</returns>
		public static Logger GetSingleton() 
		{
			Object lockThis = new Object();

			lock(lockThis)
			{
				if (logger == null)
				{
					logger = new Logger();
				}
			}
			return logger;
		}

		/// <summary>
		/// Set the log level of the Logger. All log records with a MessageType
		/// smaller or equal the log level will be logged. Logging to the Windows
		/// Event Log is not affected by the log level.
		/// </summary>
		/// <param name="level">One of the possible logging levels:
		/// <code>FAIL | ERROR | WARN | INFO | DEBUG</code></param>
		public static void SetLogLevel(String level) 
		{
			if (level.ToUpper().Equals("INFO"))
			{
				globalLogLevel = MessageType.INFO;
			}
			else if (level.ToUpper().Equals("WARN"))
			{
				globalLogLevel = MessageType.WARN;
			}
			else if (level.ToUpper().Equals("ERROR"))
			{
				globalLogLevel = MessageType.ERROR;
			}
			else if (level.ToUpper().Equals("FAIL"))
			{
				globalLogLevel = MessageType.FAIL;
			}
			else
			{
				globalLogLevel = MessageType.DEBUG;
			}
		}

		private Logger()
		{
			globalLogLevel = MessageType.DEBUG;
            
		}

        public TextBox TextBox
        {
            get { return _txtBox; }
            set
            {
                this._txtBox = value;
            }
        }

		/// <summary>
		/// Log a message based on a string.
		/// </summary>
		/// <param name="level">One of <see cref="T:Umbrella.Logging.Logger.MessageType"/></param>
		/// <param name="s">The message to be logged.</param>
		public void Log(MessageType level, String s)
		{
			this.Log(level, new Exception(s));
		}

		/// <summary>
		/// Log a message based on an exception. If the exception provides a source, it is included
		/// in the log record. If the exception provides a stack trace, it is included in the log record.
		/// </summary>
		/// <param name="level">One of <see cref="T:Umbrella.Logging.Logger.MessageType"/></param>
		/// <param name="ex">The exception to be logged.</param>
		public void Log(MessageType level, Exception ex)
		{
			bool displayMessage = false;
			MessageBoxIcon icon = MessageBoxIcon.Information;
			String msg = "";
			DateTime dt = DateTime.Now;
			msg += dt.ToString("yyyy-MM-dd HH:mm:ss.ffff");


			switch (level) 
			{
				case MessageType.FAIL : 
					msg += " [FAIL]  "; 
					icon = MessageBoxIcon.Error;
					displayMessage = true;
					break;
                case MessageType.ERROR:
                    msg += " [ERROR] ";
                    icon = MessageBoxIcon.Error;
                    displayMessage = true;
                    break;
				case MessageType.WARN : 
					msg += " [WARN]  "; 
					icon = MessageBoxIcon.Warning;
					displayMessage = false; 
					break;
                case MessageType.INFO:
                    msg += " [INFO]  ";
                    break;
				case MessageType.DEBUG : 
					msg += " [DEBUG] "; 
					break;
			}


			msg += "\t";
			if ((ex.Source != null) && (ex.Source.Length > 0))
			{
				msg += "[" + ex.Source + "] ";
				msg += ex.GetType().FullName + ": ";
			}

			msg += ex.Message;

            if (level <= globalLogLevel)
			{
                if (this._txtBox != null)
                {
                    this._txtBox.AppendText(getFullExceptionMessage(ex) + Environment.NewLine);
                }
			}

			if (displayMessage) 
			{
                MessageBox.Show(getFullExceptionMessage(ex), "Record Editor", MessageBoxButtons.OK, icon);
			}
		}

        private string getFullExceptionMessage(Exception e)
        {
            string s = e.Message;
            while (e.InnerException != null)
            {
                e = e.InnerException;
                s += " (" + e.Message + ")";
            }

            return s;
        }
	}
}
