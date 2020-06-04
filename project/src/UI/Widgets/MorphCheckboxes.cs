using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Synergy
{
	class MorphCheckboxes : CompoundWidget
	{
		private struct Morph
		{
			public DAZMorph morph;
			public Checkbox checkbox;

			public Morph(DAZMorph m)
			{
				morph = m;
				checkbox = null;
			}
		}


		private class MorphCategory
		{
			public string Name { get; private set; }

			private readonly int flags_;
			private readonly MorphCheckboxes mc_;

			private readonly SortedDictionary<string, MorphCategory> children_ =
				new SortedDictionary<string, MorphCategory>();

			private readonly SortedDictionary<string, Morph> morphs_ =
				new SortedDictionary<string, Morph>();

			private readonly Collapsible collapsible_;
			private bool loaded_ = false;

			public MorphCategory(string n, int flags, MorphCheckboxes mc)
			{
				Name = n;
				flags_ = flags;
				mc_ = mc;
				collapsible_ = new Collapsible(Name, OnCollapsibleToggled, flags_);
			}

			public SortedDictionary<string, Morph> Morphs
			{
				get
				{
					return morphs_;
				}
			}


			public void Clear()
			{
				children_.Clear();
				morphs_.Clear();
				collapsible_.Clear();
				loaded_ = false;
			}

			public void DisableAll()
			{
				foreach (var m in morphs_)
				{
					if (m.Value.checkbox != null)
						m.Value.checkbox.Value = false;
				}
			}

			public void AddWidget(IWidget w)
			{
				collapsible_.Add(w);
			}

			public void AddMorphFlat(DAZMorph m)
			{
				DoAddMorph(m, "root");
			}

			public void AddMorph(DAZMorph morph, string path, string parentPath)
			{
				var slash = path.IndexOf("/");

				if (slash == -1)
				{
					DoAddMorph(morph, parentPath);
				}
				else
				{
					string subcatName = path.Substring(0, slash).Trim();

					if (subcatName == "")
					{
						// collapse slashes and add morphs in this category
						AddMorph(morph, path.Substring(slash + 1), parentPath);
						return;
					}

					string subcatPath = parentPath;

					if (subcatPath != "")
						subcatPath += "/";

					subcatPath += subcatName;

					MorphCategory subcat;
					if (!children_.TryGetValue(subcatPath, out subcat))
					{
						subcat = new MorphCategory(subcatPath, flags_, mc_);
						children_.Add(subcatPath, subcat);
					}

					subcat.AddMorph(morph, path.Substring(slash + 1), subcatPath);
				}
			}

			public void AddToUI()
			{
				if (collapsible_.Expanded)
					CreateUIElement();

				collapsible_.AddToUI();
			}

			public void RemoveFromUI()
			{
				collapsible_.RemoveFromUI();
			}

			private void OnCollapsibleToggled(bool b)
			{
				CreateUIElement();
			}

			private void DoAddMorph(DAZMorph morph, string parentPath)
			{
				if (morphs_.ContainsKey(morph.displayName))
				{
					Synergy.LogVerbose(
						"duplicate morph '" + morph.displayName + "' " +
						"in '" + parentPath + "'");
				}
				else
				{
					morphs_.Add(morph.displayName, new Morph(morph));
				}
			}

			private void CreateUIElement()
			{
				if (loaded_)
					return;

				foreach (var sc in children_)
				{
					collapsible_.Add(sc.Value.collapsible_);
				}

				foreach (var m in morphs_)
				{
					var mcopy = m.Value;

					mcopy.checkbox = new Checkbox(
						mcopy.morph.displayName,
						mc_.IsSelected(mcopy.morph),
						toggled => OnToggled(mcopy, toggled),
						flags_);

					collapsible_.Add(mcopy.checkbox);
				}

				loaded_ = true;
			}

			private void OnToggled(Morph m, bool b)
			{
				try
				{
					mc_.Toggle(m, b);
				}
				catch (Exception e)
				{
					Synergy.LogError(e.ToString());
				}
			}
		}


		public delegate void MorphsCallback(DAZMorph m);

		private readonly string name_;
		private readonly MorphsCallback addedCallback_, removedCallback_;
		private Atom atom_ = null;
		private readonly MorphCategory root_;
		private readonly Textbox search_;
		private readonly StringList show_;
		private Timer searchTimer_ = null;
		private bool dirty_ = true;
		private HashSet<DAZMorph> selection_ = new HashSet<DAZMorph>();

		private bool focusSearch_ = false;



		public MorphCheckboxes(
			string name, MorphsCallback addedCallback,
			MorphsCallback removedCallback, int flags = 0)
				: base(flags)
		{
			name_ = name;
			addedCallback_ = addedCallback;
			removedCallback_ = removedCallback;
			root_ = new MorphCategory(name_, flags_, this);
			search_ = new Textbox("Search", "", SearchChanged, flags_);

			show_ = new StringList(
				"Show", "all",
				new List<string>() { "All", "Morphs", "Poses" },
				new List<string>() { "all", "morphs", "poses" },
				ShowChanged, flags_);
		}

		public Atom Atom
		{
			get
			{
				return atom_;
			}

			set
			{
				if (atom_ != value)
				{
					atom_ = value;
					selection_.Clear();

					Synergy.LogVerbose("atom changed, making list dirty");
					dirty_ = true;
				}
			}
		}

		public List<DAZMorph> Morphs
		{
			get
			{
				return selection_.ToList();
			}

			set
			{
				bool changed = false;

				if (selection_.Count != value.Count)
				{
					changed = true;
				}
				else
				{
					foreach (var m in value)
					{
						if (!selection_.Contains(m))
						{
							changed = true;
							break;
						}
					}
				}

				if (changed)
				{
					selection_ = new HashSet<DAZMorph>(value);
					Synergy.LogVerbose("morphs changed, making list dirty");
					dirty_ = true;
				}
			}
		}


		protected override void DoAddToUI()
		{
			if (dirty_)
			{
				Synergy.LogVerbose("list is dirty");
				UpdateList();
				dirty_ = false;
			}

			RemoveFromUI();
			root_.AddToUI();

			if (focusSearch_)
			{
				search_.Focus();
				focusSearch_ = false;
			}
		}

		protected override void DoRemoveFromUI()
		{
			if (searchTimer_ != null)
			{
				searchTimer_.Destroy();
				searchTimer_ = null;
			}

			root_.RemoveFromUI();
		}

		public void ChangeAtomKeepMorphs(Atom a)
		{
			atom_ = a;

			selection_ = new HashSet<DAZMorph>(
				Utilities.FindMorphsInNewAtom(a, Morphs));

			dirty_ = true;
		}

		private void Toggle(Morph m, bool b)
		{
			m.morph.morphValue = 0;

			if (b)
			{
				selection_.Add(m.morph);
				addedCallback_?.Invoke(m.morph);
			}
			else
			{
				selection_.Remove(m.morph);
				removedCallback_?.Invoke(m.morph);
			}
		}

		public void DisableAll()
		{
			root_.DisableAll();

			var list = selection_.ToList();
			selection_.Clear();

			foreach (var m in list)
				removedCallback_?.Invoke(m);
		}

		public bool IsSelected(DAZMorph m)
		{
			return selection_.Contains(m);
		}

		private void SearchChanged(string s)
		{
			if (searchTimer_ == null)
				searchTimer_ = sc_.CreateTimer(0.5f, () => DoSearch());
			else
				searchTimer_.Restart();

			focusSearch_ = true;
		}

		private void ShowChanged(string tag)
		{
			SearchChanged("");
		}

		private void DoSearch()
		{
			searchTimer_ = null;
			dirty_ = true;
			sc_.UI.NeedsReset("morph search changed");
		}

		private void UpdateList()
		{
			Synergy.LogVerbose("updating list for atom " + Atom?.ToString());
			root_.Clear();

			if (Atom == null)
			{
				root_.AddWidget(new Header("No atom selected", flags_));
				return;
			}

			root_.AddWidget(new Label("Search, * is a wildcard", flags_));
			root_.AddWidget(search_);
			root_.AddWidget(show_);

			var showText = show_.Value;
			bool showMorphs = (showText == "all" || showText == "morphs");
			bool showPoses = (showText == "all" || showText == "poses");

			if (search_.Value == "")
			{
				foreach (var morph in Utilities.GetAtomMorphs(Atom))
				{
					if (!showMorphs && !morph.isPoseControl)
						continue;

					if (!showPoses && morph.isPoseControl)
						continue;

					var path = morph.region;

					if (path != "")
						path += "/";

					path += morph.displayName;

					root_.AddMorph(morph, path, "");
				}
			}
			else
			{
				var searchText = search_.Value;
				var pattern = Regex.Escape(searchText).Replace("\\*", ".*");
				var re = new Regex(pattern, RegexOptions.IgnoreCase);

				foreach (var morph in Utilities.GetAtomMorphs(Atom))
				{
					if (!showMorphs && !morph.isPoseControl)
						continue;

					if (!showPoses && morph.isPoseControl)
						continue;

					var path = morph.region;

					if (path != "")
						path += "/";

					path += morph.displayName;

					if (re.IsMatch(path))
						root_.AddMorphFlat(morph);
				}
			}
		}
	}
}
