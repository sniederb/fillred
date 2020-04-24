using System;

namespace Want.Patterns
{
	/// <summary>General states for any subject.</summary>
	public enum State
	{
		/// <summary>The processing duty of the subject has not yedt commenced.</summary>
		NEW,

		DIRTY,

		/// <summary>The processing has started, but not yet finished.</summary>
		/// <summary>The processing had nothing to do.</summary>
		VALID,

		ERRONEOUS
	}

	/// <summary>
	/// The Subject interface defines the responsibility for a subject adhering to the
	/// Observer Design Pattern.</summary>
	/// <history>
	/// 2005-06-22 sin Created
	/// </history>
	public interface Subject
	{
		/// <summary>
		/// Register an observer with this subject. The observer will receive notification a state
		/// updates after registering.
		/// </summary>
		/// <param name="o">The observer object to be notifed.</param>
		void RegisterObserver(Observer o);

		/// <summary>
		/// Notify all registered observers of a change in state.
		/// </summary>
		/// <remarks>When receiving notification, the observer must request 
		/// the subject's state using the <see cref="M:Umbrella.Patterns.Subject.GetState"/> method.</remarks>
		void NotifyObservers();

		/// <summary>
		/// Get the current state of the subject.
		/// </summary>
		/// <returns>The subject's state as <see cref="T:Umbrella.Patterns.Subject.State"/>. Note that the observer
		/// might call other getter methods on the subject depending on the state.</returns>
		State GetState();

		/// <summary>
		/// Set the current state of the subject.
		/// </summary>
		/// <param name="state">The subject's state as object. Note that the subject and observer are tightly coupled
		/// by the declaration of what the state object contains.</param>
		void SetState(State state);
	}
}
