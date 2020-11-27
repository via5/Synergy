using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	class ForceReceiverList : StringList
	{
		public delegate void RigidbodyCallback(Rigidbody rb);

		public Atom Atom { get; set; } = null;
		private readonly RigidbodyCallback callback_;

		public ForceReceiverList(
			string name, string def, RigidbodyCallback callback, int flags = 0)
				: base(flags | Filterable)
		{
			callback_ = callback;
			CreateStorable(name, def, new List<string>(), null, Changed);
			OnOpen += UpdateList;
		}

		protected override void DoAddToUI()
		{
			base.DoAddToUI();
		}

		private void UpdateList()
		{
			var names = new List<string>();

			if (Atom != null)
			{
				foreach (var fr in Atom.forceReceivers)
				{
					var rb = fr.GetComponent<Rigidbody>();
					if (rb != null)
						names.Add(rb.name);
				}
			}

			Utilities.NatSort(names);
			Choices = names;
		}

		private void Changed(string s)
		{
			if (callback_ == null || Atom == null)
				return;

			foreach (var fr in Atom.forceReceivers)
			{
				if (fr.name == s)
				{
					callback_(fr.GetComponent<Rigidbody>());
					break;
				}
			}
		}
	}


	class RigidBodyList : StringList
	{
		public delegate void RigidbodyCallback(Rigidbody rb);

		public Atom Atom { get; set; } = null;
		private readonly RigidbodyCallback callback_;

		public RigidBodyList(
			string name, string def, RigidbodyCallback callback, int flags = 0)
				: base(flags | Filterable)
		{
			callback_ = callback;
			CreateStorable(name, def, new List<string>(), null, Changed);
			OnOpen += UpdateList;
		}

		private void UpdateList()
		{
			var names = new List<string>();

			if (Atom != null)
			{
				foreach (var fr in Atom.rigidbodies)
				{
					var rb = fr.GetComponent<Rigidbody>();
					if (rb != null)
						names.Add(rb.name);
				}
			}

			Utilities.NatSort(names);
			Choices = names;
		}

		private void Changed(string s)
		{
			if (callback_ == null || Atom == null)
				return;

			foreach (var fr in Atom.rigidbodies)
			{
				if (fr.name == s)
				{
					callback_(fr.GetComponent<Rigidbody>());
					break;
				}
			}
		}
	}


	class FreeControllerList : StringList
	{
		public delegate void FreeControllerCallback(FreeControllerV3 fc);

		public Atom Atom { get; set; } = null;
		private readonly FreeControllerCallback callback_;

		public FreeControllerList(
			string name, string def, FreeControllerCallback callback,
			int flags = 0)
				: base(flags | Filterable)
		{
			callback_ = callback;
			CreateStorable(name, def, new List<string>(), null, Changed);
			OnOpen += UpdateList;
		}

		private void UpdateList()
		{
			var names = new List<string>();

			if (Atom != null)
			{
				foreach (var fr in Atom.freeControllers)
					names.Add(fr.name);
			}

			Utilities.NatSort(names);
			Choices = names;
		}

		private void Changed(string s)
		{
			if (callback_ == null || Atom == null)
				return;

			foreach (var fr in Atom.freeControllers)
			{
				if (fr.name == s)
				{
					callback_(fr);
					break;
				}
			}
		}
	}


	class LinkTargetList : StringList
	{
		public delegate void LinkTargetCallback(Rigidbody rb);

		public Atom Atom { get; set; } = null;
		private readonly LinkTargetCallback callback_;

		public LinkTargetList(
			string name, string def, LinkTargetCallback callback, int flags = 0)
				: base(flags | Filterable)
		{
			callback_ = callback;
			CreateStorable(name, def, new List<string>(), null, Changed);
			OnOpen += UpdateList;
		}

		private void UpdateList()
		{
			var names = new List<string>();

			if (Bits.IsSet(flags_, AllowNone))
				names.Add("None");

			if (Atom != null)
			{
				foreach (var fr in Atom.freeControllers)
					names.Add(fr.name);

				foreach (var fr in Atom.forceReceivers)
				{
					var rb = fr.GetComponent<Rigidbody>();
					if (rb != null)
						names.Add(rb.name);
				}
			}

			// don't sort, keep the same order as what vam shows
			Choices = names;
		}

		private void Changed(string s)
		{
			if (callback_ == null || Atom == null)
				return;

			if (s == "None")
			{
				callback_(null);
				return;
			}

			foreach (var fr in Atom.rigidbodies)
			{
				if (fr.name == s)
				{
					callback_(fr.GetComponent<Rigidbody>());
					break;
				}
			}
		}
	}

	class PositionStateList : StringList
	{
		public struct State
		{
			public string display;
			public int value;

			public State(string d, int i)
			{
				display = d;
				value = i;
			}

			public State(string d, FreeControllerV3.PositionState s)
			{
				display = d;
				value = (int)s;
			}
		}

		public delegate void PositionStateCallback(int i);
		private readonly PositionStateCallback callback_;

		public PositionStateList(
			string name, string def, PositionStateCallback callback,
			int flags = 0)
				: base(flags)
		{
			callback_ = callback;
			CreateStorable(name, def, GetDisplayChoices(), GetChoices(), Changed);
		}

		public List<State> GetStates()
		{
			var list = new List<State>
			{
				new State("On", FreeControllerV3.PositionState.On),
				new State("Comply", FreeControllerV3.PositionState.Comply),
				new State("Off", FreeControllerV3.PositionState.Off),
				new State("ParentLink", FreeControllerV3.PositionState.ParentLink),
				new State("PhysicsLink", FreeControllerV3.PositionState.PhysicsLink),
				new State("Hold", FreeControllerV3.PositionState.Hold),
				new State("Lock", FreeControllerV3.PositionState.Lock)
			};

			if (Bits.IsSet(flags_, AllowNone))
				list.Insert(0, new State("", -1));

			return list;
		}

		public List<string> GetDisplayChoices()
		{
			var states = GetStates();
			var display = new List<string>();

			foreach (var s in states)
				display.Add(s.display);

			return display;
		}

		public List<string> GetChoices()
		{
			var states = GetStates();
			var choices = new List<string>();

			foreach (var s in states)
				choices.Add(s.value.ToString());

			return choices;
		}

		public new int Value
		{
			get
			{
				int i = 0;

				if (!Int32.TryParse(base.Value, out i))
				{
					Synergy.LogError($"bad position state {base.Value}");
					return -1;
				}

				return i;
			}

			set
			{
				base.Value = value.ToString();
			}
		}

		private void Changed(string s)
		{
			if (callback_ == null)
				return;

			callback_(Value);
		}
	}


	class RotationStateList : StringList
	{
		public struct State
		{
			public string display;
			public int value;

			public State(string d, int i)
			{
				display = d;
				value = i;
			}

			public State(string d, FreeControllerV3.RotationState s)
			{
				display = d;
				value = (int)s;
			}
		}

		public delegate void RotationStateCallback(int i);
		private readonly RotationStateCallback callback_;

		public RotationStateList(
			string name, string def, RotationStateCallback callback,
			int flags = 0)
				: base(flags)
		{
			callback_ = callback;
			CreateStorable(name, def, GetDisplayChoices(), GetChoices(), Changed);
		}

		public List<State> GetStates()
		{
			var list = new List<State>
			{
				new State("On", FreeControllerV3.RotationState.On),
				new State("Comply", FreeControllerV3.RotationState.Comply),
				new State("Off", FreeControllerV3.RotationState.Off),
				new State("ParentLink", FreeControllerV3.RotationState.ParentLink),
				new State("PhysicsLink", FreeControllerV3.RotationState.PhysicsLink),
				new State("Hold", FreeControllerV3.RotationState.Hold),
				new State("Lock", FreeControllerV3.RotationState.Lock)
			};

			if (Bits.IsSet(flags_, AllowNone))
				list.Insert(0, new State("", -1));

			return list;
		}

		public List<string> GetDisplayChoices()
		{
			var states = GetStates();
			var display = new List<string>();

			foreach (var s in states)
				display.Add(s.display);

			return display;
		}

		public List<string> GetChoices()
		{
			var states = GetStates();
			var choices = new List<string>();

			foreach (var s in states)
				choices.Add(s.value.ToString());

			return choices;
		}

		public new int Value
		{
			get
			{
				int i = 0;

				if (!Int32.TryParse(base.Value, out i))
				{
					Synergy.LogError($"bad position state {base.Value}");
					return -1;
				}

				return i;
			}

			set
			{
				base.Value = value.ToString();
			}
		}

		private void Changed(string s)
		{
			if (callback_ == null)
				return;

			callback_(Value);
		}
	}
}
