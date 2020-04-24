using System;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Data;
using System.Collections;
using System.Windows.Forms;
using System.Text;
using System.Text.RegularExpressions;

using Want.Patterns;
using Want.Logging;

namespace Want.RecordEditor.Model
{
	/// <summary>
	/// Summary description for Layoutfile.
	/// </summary>
	class Layoutfile : Subject
	{
        private static Logger logger = Logger.GetSingleton();
        private static Regex unicodeCharacters = new Regex(@"\\u(\d+)");

		private string _filename;
		private LayoutDataset _layoutds;

		private State _state;
		private string _error;

		private bool _isUpdating;

		private ArrayList _observers;

		/// <summary>
		/// 
		/// </summary>
		public Layoutfile()
		{
			_layoutds = new LayoutDataset();

			_layoutds.Record.RowChanged += new DataRowChangeEventHandler( DataChanged );
			_layoutds.Field.RowChanged += new DataRowChangeEventHandler( DataChanged );

			_isUpdating = false;
			_error = "";
			_state = State.VALID;
			_observers = new ArrayList();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filename"></param>
		public void Load(string filename)
		{
			_filename = filename;
			_layoutds.Clear();
			if (_filename.Length > 0) 
			{
                try
                {
                    _layoutds.ReadXml(_filename);
                    _layoutds.AcceptChanges();

                    _state = State.VALID;
                }
                catch (Exception e)
                {
                    logger.Log(Logger.MessageType.WARN, e);
                    _state = State.ERRONEOUS;
                }
			}
			
		}

		/// <summary>
		/// 
		/// </summary>
		public void Save()
		{
			bool doSave = Validate();

			if (this._state != State.VALID)
			{
				string msg = "There are layout validation errors:\n" + _error;
				msg += "\nDo you still want to save?";
				DialogResult dlgRes = MessageBox.Show(msg, "FillRed", 
					System.Windows.Forms.MessageBoxButtons.OKCancel);
				if (dlgRes == DialogResult.OK)
				{
					doSave = true;
				}
			}

			if (doSave)
			{
                try
                {
                    BeginUpdate();
                    _layoutds.AcceptChanges();

                    System.IO.FileStream fsWriteXml = new System.IO.FileStream(_filename, System.IO.FileMode.Create);
                    XmlTextWriter xmlWriter = new XmlTextWriter(fsWriteXml, System.Text.Encoding.Unicode);
                    _layoutds.WriteXml(xmlWriter);

                    fsWriteXml.Close();

                    _state = State.VALID;

                    EndUpdate();

                    // attempt to sort data in XML. Datasets themselves are never sorted, so the WriteXml will produce
                    // a rather 'random' output. Use XSL to re-order, but leave XML intact in case of any errors
                    XPathDocument myXPathDoc = new XPathDocument(_filename);

                    // apparently access to an XSLT resource as stream is tricky, so do the roundtrip vai string here
                    String xsltString = global::Want.RecordEditor.Properties.Resources.sortlayout;
                    XmlReader xslTextReader = XmlReader.Create(new System.IO.StringReader(xsltString));

                    XslCompiledTransform myXslTrans = new XslCompiledTransform();
                    myXslTrans.Load(xslTextReader);

                    xmlWriter = new XmlTextWriter(_filename, null);
                    xmlWriter.Formatting = Formatting.Indented;
                    xmlWriter.Indentation = 1;
                    xmlWriter.IndentChar = '\t';
                    myXslTrans.Transform(myXPathDoc, null, xmlWriter);
                }
                catch (Exception e)
                {
                    logger.Log(Logger.MessageType.ERROR, e);
                }
			}

			NotifyObservers();
		}

		public bool Validate()
		{
			_error = "";

			foreach (LayoutDataset.RecordRow recRow in _layoutds.Record.Rows)
			{
                if (recRow.RowState != DataRowState.Deleted)
                {
                    foreach (LayoutDataset.FieldRow fieldRow in recRow.GetFieldRows() /*_layoutds.Field.Select("RecordName = '" + recRow.Name + "'")*/)
                    {
                        if (fieldRow.IsIsEORNull())
                        {
                            fieldRow.IsEOR = false;
                        }
                        if (fieldRow.IsIsOptionalNull())
                        {
                            fieldRow.IsOptional = false;
                        }
                        if (fieldRow.IsIsRecordIdentifierNull())
                        {
                            fieldRow.IsRecordIdentifier = false;
                        }

                        if (fieldRow.IsNameNull())
                        {
                            _error += recRow.Name + "[" + fieldRow.Index + "] has (null) as name.\n";
                        }
                        else if (fieldRow.IsLengthNull())
                        {
                            _error += recRow.Name + "[" + fieldRow.Name + "] has (null) as length.\n";
                        }
                        else if ((!fieldRow.IsIsEORNull()) && (fieldRow.IsEOR) && fieldRow.IsPredefinedValueNull())
                        {
                            _error += recRow.Name + "[" + fieldRow.Name + "] is EOR but without predefined value.\n";
                        }
                        else if ((!fieldRow.IsIsRecordIdentifierNull()) && (fieldRow.IsRecordIdentifier) && fieldRow.IsPredefinedValueNull())
                        {
                            _error += recRow.Name + "[" + fieldRow.Name + "] is record identifier but without predefined value.\n";
                        }

                    }
                }
			}

			if (_error.Length > 0)
			{
				this._state = State.ERRONEOUS;
			}
			else
			{
                this._state = State.VALID;
			}

            return (this._state == State.VALID);
		}

		public void BeginUpdate()
		{
			_isUpdating = true;
		}

		public void EndUpdate()
		{
			_isUpdating = false;
		}

        /// <summary>
        /// Set state to dirty, update all observers of change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		public void DataChanged( object sender, DataRowChangeEventArgs e )
		{
			if (!_isUpdating)
			{
				_state = State.DIRTY;
				NotifyObservers();
			}
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

        /// <summary>
        /// Escape a pre-defined value. Esacped values may be
        /// \n, \r, \\, \t, \', \", or any Unicode character in the form \uNNNN
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
		public string Escape(string s)
		{
			if (s == null)
			{
				return "";
			}

            // c# special characters: \n, \r, \\, \t, \', \"
			s = s.Replace(@"\n", "\n")
			    .Replace(@"\r", "\r")
                .Replace(@"\t", "\t")
                .Replace(@"\\", "\\")
                .Replace("\\\"", "\"")
			    .Replace(@"\'", "'");

            
            if (unicodeCharacters.IsMatch(s)) {
                string escapeUnicodeString = "";
                Match match = unicodeCharacters.Match(s);

                while (match != Match.Empty) {
                    escapeUnicodeString = escapeUnicodeString + char.ConvertFromUtf32(Int32.Parse(match.Groups[1].ToString()));
                    match = match.NextMatch();
                }

                s = escapeUnicodeString;
            }

			return s;
		}

		public State GetState()
		{
			return _state;
		}

		public void SetState(State s)
		{
			// State is read-only
		}

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		public string FileName
		{
			get { return _filename; }
			set { _filename = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public LayoutDataset DataSet
		{
			get { return _layoutds; }
		}

		#endregion


	}
}