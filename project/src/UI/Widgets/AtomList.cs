using System;
using System.Collections.Generic;

namespace Synergy
{
	class AtomList : StringList, IDisposable
	{
		public delegate void AtomCallback(Atom atom);
		public delegate bool AtomPredicate(Atom atom);

		private readonly AtomCallback callback_;
		private readonly AtomPredicate pred_;

		public AtomList(
			string name, string def,
			AtomCallback callback, AtomPredicate pred = null, int flags = 0)
				: base(flags)
		{
			callback_ = callback;
			pred_ = pred;

			CreateStorable(name, def, new List<string>(), null, Changed);

			OnOpen += UpdateList;

			SuperController.singleton.onAtomUIDRenameHandlers +=
				OnAtomUIDChanged;
		}

		public void Dispose()
		{
			SuperController.singleton.onAtomUIDRenameHandlers -=
				OnAtomUIDChanged;
		}

		protected override void DoAddToUI()
		{
			base.DoAddToUI();

			if (element_)
				UpdateList();
		}

		private void UpdateList()
		{
			var names = new List<string>();

			if (sc_.GetAtomById("Player") != null)
				names.Add("Player");

			foreach (var a in sc_.GetSceneAtoms())
			{
				if (pred_ != null)
				{
					if (!pred_(a))
						continue;
				}

				names.Add(a.name);
			}

			Utilities.NatSort(names);

			// on top
			if (Bits.IsSet(flags_, AllowNone))
				names.Insert(0, "None");

			Choices = names;
		}

		private void Changed(string s)
		{
			if (callback_ == null)
				return;

			if (s == "None")
			{
				callback_(null);
				return;
			}

			var atom = sc_.GetAtomById(s);
			if (atom == null)
				Synergy.LogError("atom '" + s + "' not found");

			callback_(atom);
		}

		private void OnAtomUIDChanged(string oldUID, string newUID)
		{
			if (Value == oldUID)
				Value = newUID;
		}
	}
}
