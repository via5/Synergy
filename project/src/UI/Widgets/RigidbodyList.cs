using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	class RigidbodyList : StringList
	{
		public delegate void RigidbodyCallback(Rigidbody rb);

		public Atom Atom { get; set; } = null;
		private readonly RigidbodyCallback callback_;

		public RigidbodyList(
			string name, string def, RigidbodyCallback callback, int flags = 0)
				: base(flags)
		{
			callback_ = callback;
			CreateStorable(name, def, new List<string>(), null, Changed);
		}

		protected override void DoAddToUI()
		{
			base.DoAddToUI();

			if (element_)
				element_.popup.onOpenPopupHandlers += UpdateList;
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

			names.Sort();
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
}
