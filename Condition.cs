using System;
using System.Data;
using Want.Logging;

namespace Want.RecordEditor
{
	/// <summary>
	/// Summary description for Condition.
	/// (('Name of field 1' == 'T1') or ('Name of other field' != 'R44')) and ('Header' == 'A03')
	/// </summary>
	public class Condition
	{

		private string _originalCondition;

		private DataTable _data;

		public Condition(string s, DataTable rows)
		{
			_originalCondition = s;
			_data = rows;
		}

		public bool IsConditionTrue()
		{
			bool b = false;
			try
			{
				 b = (_data.Select(_originalCondition).Length > 0); 
			}
			catch (System.Data.EvaluateException ex)
			{
				Logger.GetSingleton().Log(Logger.MessageType.DEBUG, ex);
			}
			return b;
		}
	}
}
