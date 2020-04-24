using System;
using System.IO;
using System.Collections;

using Want.Patterns;
using Want.Logging;

namespace Want.RecordEditor.Model
{
		
	/// <summary>
	/// Summary description for Datafile.
	/// </summary>
	class Datafile : Subject
	{
        private static Logger logger = Logger.GetSingleton();
		private string _filename;
		private string _source;

		private State _state;
		private ArrayList _observers;

		/// <summary>
		/// 
		/// </summary>
		public Datafile()
		{
			_state = State.VALID;
			_observers = new ArrayList();
			_source = "";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filename"></param>
		public void Load(string filename)
		{
			_filename = filename;
			if (_filename.Length > 0) 
			{
				StreamReader sr = new StreamReader(_filename, Formatter.DefaultEncoding);
                logger.Log(Logger.MessageType.DEBUG, "Loading file  " + _filename + " with encoding " + sr.CurrentEncoding);
				_source = sr.ReadToEnd();
				sr.Close();
			}
			else 
			{
				_source = "";
			}

            // eg BTA actually has byte-zeros in the file
            _source = _source.Replace("\0", "0");

			SetState(State.VALID);
		}

		/// <summary>
		/// 
		/// </summary>
		public void Save()
		{
			StreamWriter sw = new StreamWriter(_filename);
			sw.Write (_source);
			sw.Close();
			SetState(State.VALID);
		}

		public void NotifyObservers()
		{
			foreach (Observer o in _observers)
			{
				o.Update(this);
			}
		}

		public void RegisterObserver(Observer o)
		{
			_observers.Add(o);
		}

		public State GetState()
		{
			return _state;
		}

		public void SetState(State s)
		{
			_state = s;
			NotifyObservers();
		}

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		public string Source
		{
			get { return _source; }
			set 
			{
				if (!_source.Equals(value))
				{
					_source = value;
					SetState(State.DIRTY);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public string FileName
		{
			get { return _filename; }
			set { _filename = value; }
		}


		#endregion

	}


}
