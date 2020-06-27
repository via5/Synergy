using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Synergy.NewUI
{
	class MorphPanel : BasicModifierPanel
	{
		public override string Title
		{
			get { return S("Morph"); }
		}


		private readonly AtomComboBox atom_ = new AtomComboBox(
			Utilities.AtomHasMorphs);

		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly MorphProgressionTab progression_ =
			new MorphProgressionTab();
		private readonly SelectedMorphsTab morphs_ = new SelectedMorphsTab();
		private readonly AddMorphsTab addMorphs_ = new AddMorphsTab();

		private MorphModifier modifier_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public MorphPanel()
		{
			var p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Atom")));
			p.Add(atom_);

			Layout = new UI.BorderLayout(20);
			Add(p, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);

			tabs_.AddTab(S("Progression"), progression_);
			tabs_.AddTab(S("Selected morphs"), morphs_);
			tabs_.AddTab(S("Add morphs"), addMorphs_);

			atom_.AtomSelectionChanged += OnAtomSelected;

			tabs_.Select(2);
		}

		public override bool Accepts(IModifier m)
		{
			return m is MorphModifier;
		}

		public override void Set(IModifier m)
		{
			modifier_ = m as MorphModifier;

			ignore_.Do(() =>
			{
				atom_.Select(modifier_.Atom);
				addMorphs_.Atom = modifier_.Atom;
			});
		}

		private void OnAtomSelected(Atom atom)
		{
			if (ignore_)
				return;

			addMorphs_.Atom = atom;
		}
	}


	class MorphProgressionTab : UI.Panel
	{
	}


	class SelectedMorphsTab : UI.Panel
	{
		public SelectedMorphsTab()
		{
			var gl = new UI.GridLayout(1);
			gl.VerticalSpacing = 10;
			gl.UniformHeight = false;
			gl.VerticalStretch = new List<bool>() { false, false, true, false };

			var search = new UI.TextBox();
			search.Placeholder = "Search";

			var left = new UI.Panel(gl);
			left.Add(new UI.Label(S("Selected morphs")));
			left.Add(new UI.ListView<string>());
			left.Add(search);


			var right = new UI.Panel(new UI.VerticalFlow());
			right.Add(new UI.CheckBox(S("Enabled")));

			Layout = new UI.BorderLayout();
			Add(left, UI.BorderLayout.Left);
			Add(right, UI.BorderLayout.Center);
		}
	}


	class AddMorphsTab : UI.Panel
	{
		private class MorphItem
		{
			public DAZMorph morph;
			public bool selected;
			public int allIndex = -1;

			public MorphItem(DAZMorph m, bool sel)
			{
				morph = m;
				selected = sel;
			}

			public override string ToString()
			{
				if (selected)
					return "\u2713" + morph.displayName;
				else
					return "   " + morph.displayName;
			}
		}

		private const float SearchDelay = 0.7f;

		private readonly UI.Stack mainStack_, morphsStack_;
		private readonly UI.ListView<string> categories_;
		private readonly UI.ListView<MorphItem> morphs_, allMorphs_;
		private readonly UI.TextBox search_;
		private readonly UI.Button toggle_;
		private Timer searchTimer_ = null;

		private Atom atom_ = null;
		private readonly HashSet<DAZMorph> selection_ = new HashSet<DAZMorph>();
		private readonly List<MorphItem> items_ = new List<MorphItem>();
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public AddMorphsTab()
		{
			categories_ = new UI.ListView<string>();
			categories_.SelectionIndexChanged += OnCategorySelected;

			var cats = new UI.Panel(new UI.BorderLayout());
			cats.Add(new UI.Label(S("Categories")), UI.BorderLayout.Top);
			cats.Add(categories_, UI.BorderLayout.Center);


			morphs_ = new UI.ListView<MorphItem>();
			morphs_.SelectionChanged += OnMorphSelected;

			allMorphs_ = new UI.ListView<MorphItem>();
			allMorphs_.SelectionChanged += OnAllMorphSelected;

			morphsStack_ = new UI.Stack();
			morphsStack_.AddToStack(morphs_);
			morphsStack_.AddToStack(allMorphs_);

			var morphs = new UI.Panel(new UI.BorderLayout());
			var mp = new UI.Panel(new UI.BorderLayout());
			mp.Add(new UI.Label(S("Morphs")), UI.BorderLayout.Center);

			toggle_ = new UI.Button("", OnToggleMorph);
			toggle_.MinimumSize = new UI.Size(250, DontCare);
			mp.Add(toggle_, UI.BorderLayout.Right);

			morphs.Add(mp, UI.BorderLayout.Top);
			morphs.Add(morphsStack_, UI.BorderLayout.Center);


			var ly = new UI.GridLayout(2);
			ly.HorizontalSpacing = 20;
			var lists = new UI.Panel(ly);
			lists.Add(cats);
			lists.Add(morphs);


			var top = new UI.Panel(new UI.HorizontalFlow(20));

			var show = new UI.ComboBox<string>();
			show.Items = new List<string>()
			{
				"Show all", "Show morphs only", "Show poses only"
			};

			top.Add(show);

			search_ = new UI.TextBox();
			search_.Placeholder = "Search";
			search_.MinimumSize = new UI.Size(300, DontCare);
			search_.Changed += OnSearchChanged;
			top.Add(search_);

			var mainPanel = new UI.Panel();
			mainPanel.Layout = new UI.BorderLayout(20);
			mainPanel.Add(top, UI.BorderLayout.Top);
			mainPanel.Add(lists, UI.BorderLayout.Center);


			var noAtomPanel = new UI.Panel();
			noAtomPanel.Layout = new UI.VerticalFlow();
			noAtomPanel.Add(new UI.Label(S("No atom selected")));


			mainStack_ = new UI.Stack();
			mainStack_.AddToStack(noAtomPanel);
			mainStack_.AddToStack(mainPanel);


			Layout = new UI.BorderLayout();
			Add(mainStack_, UI.BorderLayout.Center);

			UpdateToggleButton();
			UpdateCategories();


			foreach (var morph in Utilities.GetAtomMorphs(atom_))
				items_.Add(new MorphItem(morph, false));
		}

		public Atom Atom
		{
			get
			{
				return atom_;
			}

			set
			{
				atom_ = value;

				UpdateCategories();
				morphs_.Clear();
			}
		}

		private void UpdateToggleButton()
		{
			var m = ActiveMorphList.Selected;

			if (m != null && m.selected)
				toggle_.Text = S("Remove morph");
			else
				toggle_.Text = S("Add morph");

			toggle_.Enabled = (m != null);
		}

		private void UpdateCategories()
		{
			if (atom_ == null)
			{
				mainStack_.Select(0);
				return;
			}

			mainStack_.Select(1);

			bool showMorphs = true;
			bool showPoses = true;


			var oldSel = categories_.Selected;
			categories_.Clear();


			var categoryNames = new HashSet<string>();
			var searchText = search_.Text;
			var searchPattern = Regex.Escape(searchText).Replace("\\*", ".*");
			var searchRe = new Regex(searchPattern, RegexOptions.IgnoreCase);

			categoryNames.Add(S("(All)"));

			int selIndex = -1;
			int i = 1;

			foreach (var mi in items_)
			{
				var morph = mi.morph;

				if (!showMorphs && !morph.isPoseControl)
					continue;

				if (!showPoses && morph.isPoseControl)
					continue;

				var path = morph.region;
				if (path == "")
					path = S("(No category)");

				if (!categoryNames.Contains(path))
				{
					if (searchRe.IsMatch(path))
					{
						if (path == oldSel)
							selIndex = i;

						categoryNames.Add(path);
						++i;
					}
				}
			}

			ignore_.Do(() =>
			{
				categories_.Items = categoryNames.ToList();
				categories_.Select(selIndex);
			});
		}

		private void UpdateMorphs(int catIndex)
		{
			bool showMorphs = true;
			bool showPoses = true;

			var searchText = search_.Text;
			var searchPattern = Regex.Escape(searchText).Replace("\\*", ".*");
			var searchRe = new Regex(searchPattern, RegexOptions.IgnoreCase);

			if (catIndex == 0)
			{
				// all

				if (allMorphs_.Count == 0)
				{
					var items = new List<MorphItem>();
					int i = 0;

					foreach (var mi in items_)
					{
						var morph = mi.morph;

						if (!showMorphs && !morph.isPoseControl)
							continue;

						if (!showPoses && morph.isPoseControl)
							continue;

						if (searchText.Length > 0)
						{
							if (!searchRe.IsMatch(morph.region + " " + morph.displayName))
								continue;
						}

						mi.allIndex = i;
						items.Add(mi);
					}

					allMorphs_.Items = items;
				}

				morphsStack_.Select(1);
			}
			else
			{
				morphsStack_.Select(0);
				morphs_.Clear();

				if (catIndex > 0)
				{
					var items = new List<MorphItem>();
					var catName = categories_.At(catIndex);

					foreach (var mi in items_)
					{
						var morph = mi.morph;

						if (!showMorphs && !morph.isPoseControl)
							continue;

						if (!showPoses && morph.isPoseControl)
							continue;

						var path = morph.region;
						if (path == "")
							path = S("(No category)");

						if (path == catName)
							items.Add(mi);
					}

					morphs_.Items = items;
				}
			}
		}

		private void OnCategorySelected(int catIndex)
		{
			if (ignore_)
				return;

			UpdateMorphs(catIndex);
		}

		private void OnSearchChanged(string s)
		{
			Synergy.LogError("search changed");

			if (searchTimer_ != null)
			{
				searchTimer_.Destroy();
				searchTimer_ = null;
			}

			searchTimer_ = Synergy.Instance.CreateTimer(
				SearchDelay, OnSearchTimer);
		}

		private void OnSearchTimer()
		{
			Synergy.LogError("searching '" + search_.Text + "'");

			UpdateCategories();
			UpdateMorphs(categories_.SelectedIndex);
		}

		private UI.ListView<MorphItem> ActiveMorphList
		{
			get
			{
				if (morphsStack_.Selected == 0)
					return morphs_;
				else
					return allMorphs_;
			}
		}

		private void OnToggleMorph()
		{
			var m = ActiveMorphList.Selected;
			m.selected = !m.selected;
			ActiveMorphList.UpdateItemText(ActiveMorphList.SelectedIndex);
			UpdateToggleButton();
		}

		private void OnMorphSelected(MorphItem m)
		{
			UpdateToggleButton();
		}

		private void OnAllMorphSelected(MorphItem m)
		{
			UpdateToggleButton();
		}
	}
}
