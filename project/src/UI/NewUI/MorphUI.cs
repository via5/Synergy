using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Synergy.NewUI
{
	class MorphModifierPanel : BasicModifierPanel
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

		public MorphModifierPanel()
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
			tabs_.SelectionChanged += OnTabSelected;
			addMorphs_.MorphsChanged += OnMorphsChanged;

			tabs_.Select(1);
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
				morphs_.Set(modifier_);
				addMorphs_.Atom = modifier_.Atom;
				addMorphs_.SelectedMorphs = modifier_.Morphs;
			});
		}

		private void OnAtomSelected(Atom atom)
		{
			if (ignore_)
				return;

			addMorphs_.Atom = atom;
		}

		private void OnMorphsChanged(List<DAZMorph> morphs)
		{
			modifier_.SetMorphs(morphs);
			morphs_.SelectedMorphs = modifier_.Morphs;
		}

		private void OnTabSelected(int index)
		{
			addMorphs_.SetActive(index == 2);
		}
	}


	class MorphProgressionTab : UI.Panel
	{
	}


	class MorphPanel : UI.Panel
	{
		private readonly UI.CheckBox enabled_ = new UI.CheckBox(S("Enabled"));

		private readonly MovementPanel min_ = new MovementPanel(
			S("Minimum"), MovementWidgets.SmallMovement);

		private readonly MovementPanel max_ = new MovementPanel(
			S("Maximum"), MovementWidgets.SmallMovement);

		private MorphModifier modifier_ = null;
		private SelectedMorph morph_ = null;

		public MorphPanel()
		{
			Layout = new UI.VerticalFlow(40);

			Add(enabled_);
			Add(CreateMovementPanel(min_, OnCopyMinimum));
			Add(CreateMovementPanel(max_, OnCopyMaximum));

			enabled_.Changed += OnEnabled;
		}

		private UI.Panel CreateMovementPanel(
			MovementPanel mp, UI.Button.Callback copyCallback)
		{
			var vf = new UI.VerticalFlow(10);
			vf.Expand = false;

			var p = new UI.Panel(vf);
			p.Add(mp);
			p.Add(new UI.Button(S("Copy to other morphs"), copyCallback));

			return p;
		}

		public void Set(MorphModifier mm, SelectedMorph sm)
		{
			modifier_ = mm;
			morph_ = sm;

			if (morph_ == null)
				return;

			enabled_.Checked = sm.Enabled;
			min_.Set(sm.Movement.Minimum);
			max_.Set(sm.Movement.Maximum);
		}

		private void OnEnabled(bool b)
		{
			if (morph_ == null)
				return;

			morph_.Enabled = b;
		}

		private void OnCopyMinimum()
		{
			if (modifier_ == null || morph_ == null)
				return;

			foreach (var sm in modifier_.Morphs)
			{
				if (sm == morph_)
					continue;

				sm.Movement.Minimum = morph_.Movement.Minimum.Clone();
			}
		}

		private void OnCopyMaximum()
		{
			if (modifier_ == null || morph_ == null)
				return;

			foreach (var sm in modifier_.Morphs)
			{
				if (sm == morph_)
					continue;

				sm.Movement.Maximum = morph_.Movement.Maximum.Clone();
			}
		}
	}


	class SelectedMorphsTab : UI.Panel
	{
		private class SelectedMorphItem
		{
			public SelectedMorph sm;

			public SelectedMorphItem(SelectedMorph sm)
			{
				this.sm = sm;
			}

			public override string ToString()
			{
				return sm.DisplayName;
			}
		}

		private readonly UI.ListView<SelectedMorphItem> list_;
		private readonly MorphPanel panel_;
		private MorphModifier modifier_ = null;

		public SelectedMorphsTab()
		{
			list_ = new UI.ListView<SelectedMorphItem>();
			panel_ = new MorphPanel();

			var left = new UI.Panel(new UI.BorderLayout());
			left.Add(list_, UI.BorderLayout.Center);

			Layout = new UI.BorderLayout(10);
			Add(left, UI.BorderLayout.Left);
			Add(panel_, UI.BorderLayout.Center);

			list_.SelectionChanged += OnSelection;

			Update(null);
		}

		public void Set(MorphModifier m)
		{
			modifier_ = m;
			Atom = m.Atom;
			SelectedMorphs = m.Morphs;
		}

		public Atom Atom
		{
			set { }
		}

		public List<SelectedMorph> SelectedMorphs
		{
			set
			{
				var items = new List<SelectedMorphItem>();

				foreach (var sm in value)
					items.Add(new SelectedMorphItem(sm));

				list_.SetItems(items, list_.Selected);
				list_.Select(0);
			}
		}

		private void OnSelection(SelectedMorphItem i)
		{
			Update(i?.sm);
		}

		private void Update(SelectedMorph sm)
		{
			panel_.Visible = (sm != null);
			panel_.Set(modifier_, sm);
		}
	}


	class SearchTextBox : UI.Panel
	{
		private const float SearchDelay = 0.7f;

		public delegate void StringCallback(string s);
		public event StringCallback SearchChanged;

		private readonly UI.TextBox textbox_;
		private Timer timer_ = null;

		public SearchTextBox()
		{
			textbox_ = new UI.TextBox();
			textbox_.Placeholder = "Search";
			textbox_.MinimumSize = new UI.Size(300, DontCare);
			textbox_.Changed += OnTextChanged;

			Layout = new UI.BorderLayout();
			Add(textbox_, UI.BorderLayout.Center);
		}

		public string Text
		{
			get { return textbox_.Text; }
		}

		private void OnTextChanged(string s)
		{
			if (timer_ != null)
			{
				timer_.Destroy();
				timer_ = null;
			}

			timer_ = Synergy.Instance.CreateTimer(SearchDelay, OnTimer);
		}

		private void OnTimer()
		{
			SearchChanged?.Invoke(textbox_.Text);
		}
	}


	struct MorphFilter
	{
		public const int ShowPoses = 0x01;
		public const int ShowMorphs = 0x02;

		public Regex search;
		public int flags;

		public MorphFilter(Regex search, int flags)
		{
			this.search = search;
			this.flags = flags;
		}
	}


	class MorphCategoryListView
	{
		public class Category
		{
			public string name;
			public bool hasPoses;
			public bool hasMorphs;
			public List<DAZMorph> morphs;

			public Category(string name)
			{
				this.name = name;
				hasPoses = false;
				hasMorphs = false;
				morphs = new List<DAZMorph>();
			}

			public override string ToString()
			{
				if (name == "")
					return Strings.Get("(All)");
				else
					return name;
			}
		}

		public delegate void CategoryCallback(Category name);
		public event CategoryCallback CategorySelected;

		private readonly UI.ListView<Category> list_;
		private List<Category> cats_ = new List<Category>();
		private Atom atom_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public MorphCategoryListView()
		{
			list_ = new UI.ListView<Category>();
			list_.SelectionChanged += OnSelection;
		}

		public UI.ListView<Category> List
		{
			get { return list_; }
		}

		public Category Selected
		{
			get { return list_.Selected; }
		}

		private bool FlagsMatch(MorphFilter filter, Category c)
		{
			if (Bits.IsSet(filter.flags, MorphFilter.ShowMorphs))
			{
				if (c.hasMorphs)
					return true;
			}

			if (Bits.IsSet(filter.flags, MorphFilter.ShowPoses))
			{
				if (c.hasPoses)
					return true;
			}

			return false;
		}

		private bool ShouldShow(MorphFilter filter, Category c)
		{
			if (!FlagsMatch(filter, c))
				return false;

			foreach (var m in c.morphs)
			{
				if (filter.search.IsMatch(m.displayName))
					return true;
			}

			if (filter.search.IsMatch(c.name))
				return true;

			return false;
		}

		static public string MakeCategoryName(DAZMorph morph)
		{
			if (morph.region == "")
				return Strings.Get("(No category)");
			else
				return morph.region;
		}

		public void Update(Atom atom, MorphFilter filter)
		{
			Category oldSelection = list_.Selected;

			if (cats_.Count == 0 || atom_ != atom)
			{
				atom_ = atom;
				oldSelection = null;
				GetCategories();
			}

			var items = new List<Category>();

			for (int i = 0; i < cats_.Count; ++i)
			{
				var cat = cats_[i];

				if (!ShouldShow(filter, cat))
					continue;

				items.Add(cat);
			}

			ignore_.Do(() =>
			{
				list_.SetItems(items, oldSelection);
			});
		}

		private void GetCategories()
		{
			Synergy.LogError("MorphCategoryListView: getting cats");

			cats_.Clear();

			if (atom_ == null)
				return;

			var all = new Category("");
			all.hasMorphs = true;
			all.hasPoses = true;
			cats_.Add(all);

			var d = new Dictionary<string, Category>();

			foreach (var morph in Utilities.GetAtomMorphs(atom_))
			{
				Category cat;

				var name = MakeCategoryName(morph);

				if (!d.TryGetValue(name, out cat))
				{
					cat = new Category(name);
					d.Add(name, cat);
				}

				cat.hasMorphs = cat.hasMorphs || !morph.isPoseControl;
				cat.hasPoses = cat.hasPoses || morph.isPoseControl;
				cat.morphs.Add(morph);
			}

			cats_.AddRange(d.Values.ToList());
		}

		private void OnSelection(Category cat)
		{
			if (ignore_)
				return;

			CategorySelected?.Invoke(cat);
		}
	}


	class MorphListView
	{
		public class Morph
		{
			public DAZMorph morph;
			public bool active;

			public Morph(DAZMorph m)
			{
				morph = m;
				active = false;
			}

			public override string ToString()
			{
				if (active)
					return "\u2713" + morph.displayName;
				else
					return "   " + morph.displayName;
			}
		}


		public delegate void MorphCallback(Morph m);
		public event MorphCallback MorphSelected;

		private readonly UI.ListView<Morph> list_;
		private readonly Dictionary<string, List<Morph>> morphs_ =
			new Dictionary<string, List<Morph>>();
		private MorphFilter filter_;
		private Atom atom_ = null;

		public MorphListView()
		{
			list_ = new UI.ListView<Morph>();
			list_.SelectionChanged += OnSelection;
		}

		public UI.ListView<Morph> List
		{
			get { return list_; }
		}

		public Morph Selected
		{
			get { return list_.Selected; }
		}

		public MorphFilter Filter
		{
			get { return filter_; }
			set { filter_ = value; }
		}

		public void SelectedItemChanged()
		{
			list_.UpdateItemText(list_.SelectedIndex);
		}

		public void SetActive(Morph m, bool b)
		{
			m.active = b;
			list_.UpdateItemText(m);
		}

		public Morph SetActive(DAZMorph m, bool b)
		{
			var s = Find(m);
			if (s == null)
			{
				Synergy.LogError(
					"can't set '" + m.displayName + "' " +
					"active=" + b.ToString() + ", not in list");

				return null;
			}

			SetActive(s, b);
			return s;
		}

		public Morph Find(DAZMorph m)
		{
			var cat = MorphCategoryListView.MakeCategoryName(m);

			List<Morph> list = null;
			if (!morphs_.TryGetValue(cat, out list))
			{
				Synergy.LogError("can't find category '" + cat + "'");
				return null;
			}

			foreach (var s in list)
			{
				if (s.morph == m)
					return s;
			}

			return null;
		}

		private bool ShouldShow(string category, MorphFilter filter, Morph m)
		{
			// don't check names for the 'all' category
			if (category != "")
			{
				// if the category name matched, don't check the morph names,
				// just show all of them
				//
				// if the category name didn't match, only show morphs that do
				if (!filter.search.IsMatch(category))
				{
					if (!filter.search.IsMatch(m.morph.displayName))
						return false;
				}
			}

			if (Bits.IsSet(filter.flags, MorphFilter.ShowMorphs))
			{
				if (!m.morph.isPoseControl)
					return true;
			}

			if (Bits.IsSet(filter.flags, MorphFilter.ShowPoses))
			{
				if (m.morph.isPoseControl)
					return true;
			}

			return false;
		}

		public void Clear()
		{
			list_.Clear();
		}

		public void Update(Atom atom, string category, MorphFilter filter)
		{
			if (morphs_.Count == 0 || atom_ != atom)
			{
				atom_ = atom;
				GetMorphs();
			}

			Morph oldSelection = list_.Selected;
			var items = new List<Morph>();

			if (category == null)
			{
				// no selection
			}
			else if (category == "")
			{
				// all
				foreach (var pair in morphs_)
				{
					foreach (var m in pair.Value)
					{
						if (!ShouldShow(category, filter, m))
							continue;

						items.Add(m);
					}
				}
			}
			else
			{
				List<Morph> list = null;

				if (morphs_.TryGetValue(category, out list))
				{
					foreach (var m in list)
					{
						if (!ShouldShow(category, filter, m))
							continue;

						items.Add(m);
					}
				}
				else
				{
					Synergy.LogError(
						"MorphListView: category '" + category + "' " +
						"not found");
				}
			}

			list_.SetItems(items, oldSelection);
		}

		private void GetMorphs()
		{
			Synergy.LogError("MorphListView: getting morphs");

			morphs_.Clear();
			if (atom_ == null)
				return;

			foreach (var morph in Utilities.GetAtomMorphs(atom_))
			{
				var cat = MorphCategoryListView.MakeCategoryName(morph);

				List<Morph> list = null;

				if (!morphs_.TryGetValue(cat, out list))
				{
					list = new List<Morph>();
					morphs_.Add(cat, list);
				}

				list.Add(new Morph(morph));
			}
		}

		private void OnSelection(Morph m)
		{
			MorphSelected?.Invoke(m);
		}
	}


	class AddMorphsTab : UI.Panel
	{
		public delegate void MorphsCallback(List<DAZMorph> list);
		public event MorphsCallback MorphsChanged;

		private readonly UI.Stack stack_;
		private readonly UI.ComboBox<string> show_;
		private readonly MorphCategoryListView categories_;
		private readonly MorphListView morphs_;
		private readonly SearchTextBox search_;
		private readonly UI.Button toggle_;

		private Atom atom_ = null;
		private readonly List<DAZMorph> selection_ = new List<DAZMorph>();
		private bool dirty_ = false;
		private bool active_ = false;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public AddMorphsTab()
		{
			categories_ = new MorphCategoryListView();
			categories_.CategorySelected += OnCategorySelected;

			var cats = new UI.Panel(new UI.BorderLayout());
			cats.Add(new UI.Label(S("Categories")), UI.BorderLayout.Top);
			cats.Add(categories_.List, UI.BorderLayout.Center);

			morphs_ = new MorphListView();
			morphs_.MorphSelected += OnMorphSelected;

			var morphs = new UI.Panel(new UI.BorderLayout());
			var mp = new UI.Panel(new UI.BorderLayout());
			mp.Add(new UI.Label(S("Morphs")), UI.BorderLayout.Center);

			toggle_ = new UI.Button("", OnToggleMorph);
			toggle_.MinimumSize = new UI.Size(250, DontCare);
			mp.Add(toggle_, UI.BorderLayout.Right);

			morphs.Add(mp, UI.BorderLayout.Top);
			morphs.Add(morphs_.List, UI.BorderLayout.Center);


			var ly = new UI.GridLayout(2);
			ly.HorizontalSpacing = 20;
			var lists = new UI.Panel(ly);
			lists.Add(cats);
			lists.Add(morphs);


			var top = new UI.Panel(new UI.HorizontalFlow(20));

			show_ = new UI.ComboBox<string>(new List<string>()
			{
				"Show all", "Show morphs only", "Show poses only"
			}, OnShowChanged);

			top.Add(show_);

			search_ = new SearchTextBox();
			search_.SearchChanged += OnSearchChanged;
			top.Add(search_);

			var mainPanel = new UI.Panel();
			mainPanel.Layout = new UI.BorderLayout(20);
			mainPanel.Add(top, UI.BorderLayout.Top);
			mainPanel.Add(lists, UI.BorderLayout.Center);


			var noAtomPanel = new UI.Panel();
			noAtomPanel.Layout = new UI.VerticalFlow();
			noAtomPanel.Add(new UI.Label(S("No atom selected")));


			stack_ = new UI.Stack();
			stack_.AddToStack(noAtomPanel);
			stack_.AddToStack(mainPanel);

			Layout = new UI.BorderLayout();
			Add(stack_, UI.BorderLayout.Center);

			SetStack();
			UpdateToggleButton();
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
				NeedsUpdate();
				SetStack();
			}
		}

		public List<SelectedMorph> SelectedMorphs
		{
			set
			{
				foreach (var s in selection_)
					morphs_.SetActive(s, false);

				selection_.Clear();

				foreach (var sm in value)
					selection_.Add(sm.Morph);

				NeedsUpdate();
			}
		}

		public void SetActive(bool b)
		{
			active_ = b;

			if (active_ && dirty_)
				DoUpdate();
		}

		private void NeedsUpdate()
		{
			if (!active_)
			{
				dirty_ = true;
				return;
			}

			DoUpdate();
		}

		private void DoUpdate()
		{
			UpdateCategories();
			UpdateMorphs();

			foreach (var s in selection_)
				morphs_.SetActive(s, true);

			dirty_ = false;
		}

		private void UpdateToggleButton()
		{
			var m = morphs_.Selected;

			if (m != null && m.active)
				toggle_.Text = S("Remove morph");
			else
				toggle_.Text = S("Add morph");

			toggle_.Enabled = (m != null);
		}

		private MorphFilter CreateFilter()
		{
			var text = search_.Text;
			var pat = Regex.Escape(text).Replace("\\*", ".*");
			var re = new Regex(pat, RegexOptions.IgnoreCase);

			int flags;

			switch (show_.SelectedIndex)
			{
				case 1:
					flags = MorphFilter.ShowMorphs;
					break;

				case 2:
					flags = MorphFilter.ShowPoses;
					break;

				case 0:
				default:
					flags = MorphFilter.ShowMorphs | MorphFilter.ShowPoses;
					break;
			}

			return new MorphFilter(re, flags);
		}

		private void UpdateCategories()
		{
			categories_.Update(atom_, CreateFilter());
		}

		private void UpdateMorphs()
		{
			morphs_.Update(atom_, categories_.Selected?.name, CreateFilter());
		}

		private void SetStack()
		{
			if (atom_ == null)
			{
				stack_.Select(0);
				return;
			}

			stack_.Select(1);
		}

		private void OnCategorySelected(MorphCategoryListView.Category cat)
		{
			if (ignore_)
				return;

			UpdateMorphs();
		}

		private void OnMorphSelected(MorphListView.Morph m)
		{
			UpdateToggleButton();
		}

		private void OnSearchChanged(string s)
		{
			UpdateCategories();
			UpdateMorphs();
		}

		private void OnToggleMorph()
		{
			var m = morphs_.Selected;
			if (m == null)
				return;

			m.active = !m.active;

			if (m.active)
				selection_.Add(m.morph);
			else
				selection_.Remove(m.morph);

			morphs_.SelectedItemChanged();
			UpdateToggleButton();

			MorphsChanged?.Invoke(new List<DAZMorph>(selection_));
		}

		private void OnShowChanged(string s)
		{
			UpdateCategories();
			UpdateMorphs();
		}
	}
}
