using System;

namespace Want.Patterns
{
	/// <summary>
	/// The Observer interface defines the responsibility for a single observer adhering to the
	/// Observer Design Pattern.</summary>
	/// <history>
	/// 2005-06-22 sin Created
	/// </history>
	public interface Observer
	{
		/// <summary>
		/// Receive an update notification from the observed subject.
		/// </summary>
		/// <param name="subject">The subject running the update.</param>
		void Update(Subject subject);
	}
}
