using System;
using System.IO;
using System.Data;
using System.Text;
using System.Collections;

using Want.RecordEditor;
using Want.Logging;
using Want.Patterns;

namespace Want.RecordEditor.Model
{

	/// <summary>
	/// Summary description for Formatter.
	/// </summary>
	public class Formatter 
	{
        private static Logger logger = Logger.GetSingleton();
        private static Encoding encoder = System.Text.Encoding.UTF8;

		private string _filename;
		private FormattedDataDataset ds;

		private Datafile data;
		private Layoutfile layout;

		private string _error;
        private Boolean hasErrors = false;
		private string _leftover;
        private string _eolDelimiter = "\r\n";
        private char[] buffer;
        private int idx;

		private bool _isUpdating;
		
		/// <summary>
		/// 
		/// </summary>
		class RecordIdentifier
		{
			public int[] PositionFromStart = null;
			public int[] Length = null;
			public string[] IdentifyingValue = null;
            public string RecordName = "";
            public bool HideInFormattedView = false;

			public RecordIdentifier(int[] PositionFromStart, int[] Length, string RecordName, string[] Values, bool Hide)
			{
                this.PositionFromStart = PositionFromStart;
				this.Length = Length;
				this.RecordName = RecordName;
				this.IdentifyingValue = Values;
                this.HideInFormattedView = Hide;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public Formatter()
		{
			data = new Datafile();
			layout = new Layoutfile();
			_isUpdating = false;

			ds = new FormattedDataDataset ();
			ds.Record.RowChanged += new DataRowChangeEventHandler(DataChanged);
			ds.Field.RowChanged += new DataRowChangeEventHandler(DataChanged);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filename"></param>
		public void Load(string filename)
		{
			_filename = filename;
			_isUpdating = true;
			ds.ReadXml(_filename);
			_isUpdating = false;
		}

        /// <summary>
        /// Use the current layout.DataSet to format data and populate the FormattedDataDataset (ds).
        /// As a single record can span multiple lines, the read approach is based on a memory
        /// stream (instead of simple readlines)
        /// </summary>
		public void FormatDatafile()
		{

            if (logger.TextBox != null)
            {
                logger.TextBox.Clear();
            }

            ValidateLayout();

			if ((data.Source != null) && (data.Source.Length > 0) &&
				(layout.DataSet.Record.Count > 0)) 
			{
				_isUpdating = true;

                RecordIdentifier[] recordTable = GetRecordIdentifiers();

                hasErrors = false;
				_error = "";
				_leftover = "";
				ds.Clear();

				// 2. put entire data source into a memory stream
                this.buffer = data.Source.ToCharArray();
                this.idx = 0;
                int recordIndex = 0;

				try
				{
                    while (this.idx < this.buffer.Length)
					{
                        RecordIdentifier theRecId = GetCurrentRecord(recordTable);

						if (theRecId != null)
						{
                            FormatRecord(recordIndex, theRecId);
                            recordIndex++;
						}
						else
						{
                            // no matching record identifier found, add to error message and
                            // jump to next line
                            string unparsedSection = "";
                            while (this.idx < this.buffer.Length)
                            {
                                unparsedSection += this.buffer[idx];
                                idx++;
                                if (unparsedSection.EndsWith(_eolDelimiter))
                                {
                                    break;
                                }
                            }

                            String errorMessage = "Parse error: failed to find a matching record around [";
                            if (unparsedSection.Length > 10)
                            {
                                errorMessage += unparsedSection.Substring(0, 10);
                                errorMessage += "...";
                            }
                            else
                            {
                                errorMessage += unparsedSection;
                            }

                            errorMessage += "], proceeding to next line ...";
                            _error += Environment.NewLine + errorMessage;
                            hasErrors = true;
						}


					} // while position is < StringReader buffer

				}
				catch (Exception ex)
				{
					logger.Log(Logger.MessageType.DEBUG, ex);

					_leftover = GetString(0, this.buffer.Length - idx);

					_error += "\n" + ex.Message;
                    _error += "\nUnparsed: ";

                    if (_leftover.Length > 50)
                    {
                        _error += _leftover.Substring(0, 50);
                        _error += "...";
                    }
                    else
                    {
                        _error += _leftover;
                    }
                    hasErrors = true;
					return;
				}
				finally
				{
					_isUpdating = false;
				}
			} // if data and layout present
		}

        private void FormatRecord(int recordIndex, RecordIdentifier theRecId)
        {
            LayoutDataset.FieldRow[] fields;
            FormattedDataDataset.RecordRow fmtRec = ds.Record.NewRecordRow();
            fmtRec.Name = theRecId.RecordName;
            fmtRec.Index = recordIndex;
            fmtRec.Hidden = theRecId.HideInFormattedView;
            ds.Record.AddRecordRow(fmtRec);

            // b. process this record and move position accordingly
            fields = (LayoutDataset.FieldRow[])layout.DataSet.Field.Select("RecordName = '" + theRecId.RecordName + "'", "Index");

            logger.Log(Logger.MessageType.DEBUG, "Parsing " + fields.Length + " fields ...");

            int fieldIndex = 0;
            for (; fieldIndex < fields.Length; fieldIndex++)
            {
                bool indexMoved = false;
                LayoutDataset.FieldRow fieldRow = fields[fieldIndex];
                LayoutDataset.FieldRow eor = GetNextEorField(theRecId, fieldRow, recordIndex);

                string dataValue = null;

                if (fieldRow.Length < 0)
                {
                    dataValue = GetVariableLengthValue(fields, fieldIndex, fieldRow);
                    indexMoved = true;
                }
                else
                {
                    dataValue = GetString(0, fieldRow.Length);
                }

                if (dataValue != null)
                {
                    bool processField = CheckValueForProcessing(recordIndex, theRecId, fmtRec, fieldRow, eor, dataValue);

                    if (processField)
                    {
                        FormattedDataDataset.FieldRow fmtField = ds.Field.NewFieldRow();
                        fmtField.Name = fieldRow.Name;
                        fmtField.RecordName = fmtRec.Name;
                        fmtField.RecordIndex = recordIndex;
                        fmtField.Description = fieldRow.IsDescriptionNull() ? "" : fieldRow.Description;
                        fmtField.Value = dataValue;
                        fmtField.Index = fieldRow.Index;
                        fmtField.Length = fieldRow.Length;

                        ds.Field.AddFieldRow(fmtField);

                        if (!indexMoved)
                        {
                            this.idx += dataValue.Length;
                        }
                    }

                }
                else if (!indexMoved && !fieldRow.IsLengthNull())
                {
                    // if data field was not set, still move index
                    this.idx += fieldRow.Length;
                }
            }
        }

        /// <summary>
        /// Check if current data value should be processed (mainly for conditional fields). For values
        /// which do not match a condition, the StringReader is set back to the previous position. If
        /// an optional fields does not define a condition or predefined value and the datavalue matches
        /// next EOR, that EOR is written.
        /// </summary>
        /// <param name="recordIndex"></param>
        /// <param name="theRecId"></param>
        /// <param name="fmtRec"></param>
        /// <param name="fieldRow"></param>
        /// <param name="eor"></param>
        /// <param name="dataValue"></param>
        /// <returns></returns>
        private bool CheckValueForProcessing(int recordIndex, RecordIdentifier theRecId, FormattedDataDataset.RecordRow fmtRec, LayoutDataset.FieldRow fieldRow, LayoutDataset.FieldRow eor, string dataValue)
        {
            if ((!fieldRow.IsIsOptionalNull()) && (fieldRow.IsOptional))
            {
                // optional values are identified either by predefined values
                // or by conditions


                if ((!fieldRow.IsConditionNull()) && (fieldRow.Condition.Length > 0))
                {
                    string condExpression = "((RecordName = '" + theRecId.RecordName + "') and (RecordIndex = '" + recordIndex + "'))";
                    condExpression = condExpression + " and (" + fieldRow.Condition + ")";

                    Condition cond = new Condition(condExpression, ds.Field);

                    if (!cond.IsConditionTrue())
                    {
                        // go back
                        return false;
                    }
                    return true;
                }

                if ((!fieldRow.IsPredefinedValueNull()) &&
                        (fieldRow.PredefinedValue.Length > 0))
                {
                    // field has predefined value, check and roll back if no match
                    if (!layout.Escape(fieldRow.PredefinedValue).Equals(dataValue))
                    {
                        return false;
                    }

                    return true;
                }

                if (eor != null)
                {
                    // optional field has no condition and no predefined value,
                    // compare current data against next EOR, maybe record is
                    // already finished
                    string seor = layout.Escape(eor.PredefinedValue);
                    if (dataValue.Substring(0, seor.Length).Equals(seor))
                    {

                        FormattedDataDataset.FieldRow fmtField = ds.Field.NewFieldRow();
                        fmtField.Name = eor.Name;
                        fmtField.RecordName = fmtRec.Name;
                        fmtField.RecordIndex = recordIndex;
                        fmtField.Description = eor.IsDescriptionNull() ? "" : fieldRow.Description;
                        fmtField.Value = seor;
                        fmtField.Index = eor.Index;
                        fmtField.Length = eor.Length;

                        ds.Field.AddFieldRow(fmtField);

                        this.idx += seor.Length;
                        // datavalue already written, do not process again
                        return false;
                    }
                    return true;

                }
                
                throw new Exception("Parse error: optional field " + fieldRow.Name + " must have condition or predefined value.");
            }

            return true;
        }

        

        /// <summary>
        /// Return a list of all defined record identifiers
        /// </summary>
        /// <returns></returns>
        private RecordIdentifier[] GetRecordIdentifiers()
        {
            int recIdPos = 0;
            int index = 0;
            RecordIdentifier[] recordTable = new RecordIdentifier[layout.DataSet.Record.Count];
            LayoutDataset.FieldRow[] fields;

            foreach (LayoutDataset.RecordRow recRow in layout.DataSet.Record.Rows)
            {
                recIdPos = 0;

                // get fields for record name, sorted by 'Index'
                fields = (LayoutDataset.FieldRow[])layout.DataSet.Field.Select("RecordName = '" + recRow.Name + "'", "Index");

                ArrayList posList = new ArrayList();
                ArrayList valueList = new ArrayList();
                ArrayList lengthList = new ArrayList();

                foreach (LayoutDataset.FieldRow fieldRow in fields)
                {
                    if (fieldRow.IsRecordIdentifier == true)
                    {
                        if ((fieldRow.IsPredefinedValueNull()) || (fieldRow.IsLengthNull()))
                        {
                            throw new Exception("Parse error: Predefined value and length must be set for " + fieldRow.Name + " in " + recRow.Name);
                        }
                        else
                        {
                            string escPredefinedValue = layout.Escape(fieldRow.PredefinedValue);
                            posList.Add(recIdPos);
                            valueList.Add(escPredefinedValue);
                            lengthList.Add(fieldRow.Length);
                        }
                    }
                    recIdPos += fieldRow.Length;
                }

                if (posList.Count > 0)
                {


                    recordTable[index] = new RecordIdentifier(
                        (int[])posList.ToArray(typeof(int)),
                        (int[])lengthList.ToArray(typeof(int)),
                        recRow.Name,
                        (string[])valueList.ToArray(typeof(string)),
                        recRow.IsHideInFormatViewNull() ? false : recRow.HideInFormatView);
                    index++;
                    logger.Log(Logger.MessageType.DEBUG, "Loaded record identifier for " + recRow.Name);

                    posList.Clear();
                    valueList.Clear();
                    lengthList.Clear();


                }

                else
                {
                    logger.Log(Logger.MessageType.WARN, "Failed to locate an identifier field for record " + recRow.Name);
                }

            }

            return recordTable;
        }

        /// <summary>
        /// Find matching record based on (1-n) Field.IsRecordIdentifier
        /// </summary>
        /// <returns></returns>
        private RecordIdentifier GetCurrentRecord(RecordIdentifier[] recordTable) 
        {
            foreach (RecordIdentifier recId in recordTable)
            {
                if (recId != null)
                {
                    bool match = true;
                    for (int i = 0; i < recId.PositionFromStart.Length; i++) 
                    {

                        int pos = recId.PositionFromStart[i];
                        int len = recId.Length[i];
                        string[] expectedValue = recId.IdentifyingValue[i].Split(new char[] {'|'});

                        // if a record has been defined but without fields, recId will be null
                        string sourceData = GetString(pos, len);
                        if (sourceData != null) {
                            // expectedValue could be an array of options, but one has to match
                            bool oneOptionMatches = false;
                            foreach (string option in expectedValue)
                            {
                                if (option.Equals(sourceData))
                                {
                                    oneOptionMatches = true;
                                    break;
                                }
                            }

                            if (oneOptionMatches)
                            {
                                logger.Log(Logger.MessageType.DEBUG, "Found match for " + sourceData + " at position " + pos);
                            }
                            else
                            {
                                match = false;
                            }
                        }
                    }

                    if (match)
                    {
                        logger.Log(Logger.MessageType.DEBUG, "Found record identifier " + recId.RecordName);
                        return recId;
                    }
                }
            }

            int preview = buffer.Length > 20 ? 20 : buffer.Length;
            logger.Log(Logger.MessageType.WARN, "Failed to identify record type, buffer was " + GetString(0, preview));

            return null;
        }

        /// <summary>
        /// Get next assigned EOR. This is needed if the record ends with a series
        /// of optional fields, and the next field could be the EOR.
        /// </summary>
        /// <param name="theRecId"></param>
        /// <param name="fieldRow"></param>
        /// <param name="index">Record index</param>
        /// <returns></returns>
        private LayoutDataset.FieldRow GetNextEorField(RecordIdentifier theRecId, LayoutDataset.FieldRow fieldRow, int index)
        {
            LayoutDataset.FieldRow[] eors = (LayoutDataset.FieldRow[])layout.DataSet.Field.Select("(RecordName = '" + theRecId.RecordName +
                "') and (IsEOR = true) and (Index > " + fieldRow.Index + ")", "Index");

            LayoutDataset.FieldRow eor = null;

            foreach (LayoutDataset.FieldRow r in eors)
            {
                if ((!r.IsConditionNull()) && (r.Condition.Length > 0))
                {
                    string condExpression = "((RecordName = '" + theRecId.RecordName + "') and (RecordIndex = '" + index + "'))";
                    condExpression = condExpression + " and (" + r.Condition + ")";
                    if (ds.Field.Select(condExpression).Length > 0)
                    {
                        eor = r;
                        break;
                    }
                }
                else
                {
                    eor = r;
                }

            }

            return eor;
        }

        /// <summary>
        /// Read from memory stream until predefined value of NEXT field definition is reached
        /// </summary>
        /// <param name="mem"></param>
        /// <param name="pos"></param>
        /// <param name="fields"></param>
        /// <param name="fieldIndex"></param>
        /// <param name="fieldRow"></param>
        /// <returns></returns>
        private string GetVariableLengthValue(LayoutDataset.FieldRow[] fields, int fieldIndex, LayoutDataset.FieldRow fieldRow)
        {
            // field has variable length, next field MUST have a predefined value
            LayoutDataset.FieldRow nextRow = fields[fieldIndex + 1];
            if (nextRow.IsPredefinedValueNull() || (nextRow.PredefinedValue.Length == 0))
            {
                throw new Exception("Parse error: variable length field " + fieldRow.Name + " MUST be followed by a field with predefined value.");
            }

            string dataValue = "";
            string terminator = layout.Escape(nextRow.PredefinedValue);
            while (idx < buffer.Length)
            {
                dataValue += buffer[idx];
                if (dataValue.EndsWith(terminator))
                {
                    break;
                }
                idx++;
            }

            // move file pointer back by length of predefined value, 
            idx -= terminator.Length;
            // +1 to position at beginning of next field
            idx++;
            dataValue = dataValue.Remove(dataValue.Length - terminator.Length);

            if (dataValue.Length == 0)
            {
                dataValue = null;
            }

            return dataValue;
        }

		
		public void UpdateSourceFromDataset()
		{
			if (ds.Record.Count > 0)
			{
				string s = "";

				foreach(FormattedDataDataset.RecordRow recRow in ds.Record.Rows)
				{

					foreach(FormattedDataDataset.FieldRow fieldRow in ds.Field.Select("(RecordName = '" + 
						recRow.Name + "') and (RecordIndex = " + recRow.Index + ")"))
					{
                        if (fieldRow.Length >= 0)
                        {
                            s += fieldRow.Value.PadLeft(fieldRow.Length).Substring(0, fieldRow.Length);
                        }
                        else
                        {
                            // variable length!
                            s += fieldRow.Value;
                        }
					}

				}

				data.Source = s + _leftover;
			}
		}

        /// <summary>
        /// Check each record for an identifier and EOR field. Check lengths against pre-defined values.
        /// </summary>
        /// <returns>True if validation is OK, false if there are errors</returns>
        protected Boolean ValidateLayout()
        {
            int autofix = -1;
            Boolean hasErrors = false;

            foreach (LayoutDataset.RecordRow recRow in layout.DataSet.Record.Rows)
            {
                Boolean hasEor = false;
                Boolean hasIdentity = false;
                
                // these fields are not sorted!
                LayoutDataset.FieldRow[] fields = (LayoutDataset.FieldRow[])recRow.GetChildRows("RecordField");

                foreach (LayoutDataset.FieldRow fieldRow in fields)
                {
                    if (fieldRow.IsEOR)
                    {
                        hasEor = true;
                    }
                    if (fieldRow.IsRecordIdentifier)
                    {
                        hasIdentity = true;
                    }
                    if ((!fieldRow.IsPredefinedValueNull()) && (fieldRow.PredefinedValue.Length > 0)) 
                    {
                        string value = layout.Escape(fieldRow.PredefinedValue);

                        if ((value.Length != fieldRow.Length) && (value.IndexOf('|') < 0))
                        {
                            if (autofix < 0)
                            {
                                System.Windows.Forms.DialogResult answer = System.Windows.Forms.MessageBox.Show(
                                    "Some fields have a mismatch between field-length and length of the predefined value." +
                                    Environment.NewLine + "Should the field-length automatically be updated to the of the "+ 
                                    "predefined value?", 
                                    "Record Editor", 
                                    System.Windows.Forms.MessageBoxButtons.YesNo, 
                                    System.Windows.Forms.MessageBoxIcon.Question);
                                autofix = (answer == System.Windows.Forms.DialogResult.Yes ? 1 : 0);
                            }

                            if (autofix > 0)
                            {
                                fieldRow.Length = value.Length;
                            }
                            else
                            {
                                logger.Log(Logger.MessageType.WARN, "[VALIDATION] - Field " + recRow.Name + "." + fieldRow.Name + " has mismatch between field length and length of predefined value.");
                                hasErrors = true;
                            }
                        }
                    }
                }

                if (!hasEor)
                {
                    logger.Log(Logger.MessageType.WARN, "[VALIDATION] - Record '" + recRow.Name + "' has no end-of-record field.");
                    hasErrors = true;
                }
                if (!hasIdentity)
                {
                    logger.Log(Logger.MessageType.WARN, "[VALIDATION] - Record '" + recRow.Name + "' has no identity field.");
                    hasErrors = true;
                }

            }

            if (hasErrors)
            {
                logger.Log(Logger.MessageType.ERROR, "The current layout has errors. See \nthe 'formatting log' tab for details.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Read a string from buffer
        /// </summary>
        /// <param name="offset">Offset from current idx</param>
        /// <param name="length"></param>
        /// <returns></returns>
        private String GetString(int offset, int length)
        {
            if (buffer.Length >= (idx + offset + length))
            {
                char[] result = new char[length];
                Array.Copy(buffer, idx + offset, result, 0, length);
                return new String(result);
            }

            return null;
        }

		public void SaveData()
		{
			data.Save();
		}

		public void SaveLayout()
		{
			layout.Save();
		}

		#region Properties

		public Subject LayoutSubject
		{
			get { return layout; }
		}

		public Subject DataSubject
		{
			get { return data; }
		}
		
		/// <summary>
		/// 
		/// </summary>
		public string DataFile
		{
			get { return data.FileName; }
			set 
			{
				data.Load(value);
				FormatDatafile();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public string LayoutFile
		{
			get { return layout.FileName; }
			set 
			{
				layout.BeginUpdate();
				layout.Load(value);
				layout.EndUpdate();
				FormatDatafile();
			}
		}

        public string EolDelimiter
        {
            get { return _eolDelimiter; }
            set { _eolDelimiter = value; }
        }

		public string OverrideLayoutFile 
		{
			set {layout.FileName = value;}
		}

		public string OverrideDataFile 
		{
			set {data.FileName = value;}
		}

		/// <summary>
		/// 
		/// </summary>
		public string Source
		{
			get { return data.Source; }
			set { 
				data.Source = value; 
				FormatDatafile();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public FormattedDataDataset DataSet
		{
			get { return ds; }
		}

		/// <summary>
		/// 
		/// </summary>
		public LayoutDataset LayoutDataSet
		{
			get { return layout.DataSet; }
		}

		public string Error
		{
			get { return _error;}
		}

        public Boolean HasErrors
        {
            get { return hasErrors; }
        }


		#endregion

		private void DataChanged(object sender, DataRowChangeEventArgs e)
		{
			if (!_isUpdating)
			{
				data.SetState(State.DIRTY);
			}
		}

        /// <summary>
        /// Get encoding
        /// </summary>
        public static Encoding DefaultEncoding {
            get { return encoder; }
            set { encoder = value; }
        }
	}
}
