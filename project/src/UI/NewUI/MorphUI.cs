using System.Collections.Generic;
using System.Linq;
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


	class MorphItem
	{
		public DAZMorph morph;
		public bool selected;

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


	class MorphListView
	{
		private readonly UI.ListView<MorphItem> list_;
		private MorphFilter filter_;

		public MorphListView()
		{
			list_ = new UI.ListView<MorphItem>();
			list_.SelectionChanged += OnSelection;
		}

		public UI.ListView<MorphItem> List
		{
			get { return list_; }
		}

		public MorphFilter Filter
		{
			get { return filter_; }
			set { filter_ = value; }
		}

		private void OnSelection(MorphItem mi)
		{
		}
	}


	class MorphCategoryListView
	{
		public delegate void CategoryCallback(string name);
		public event CategoryCallback CategorySelected;

		public class Category
		{
			public string name;
			public bool hasPoses;
			public bool hasMorphs;

			public Category(string name, bool hasPoses, bool hasMorphs)
			{
				this.name = name;
				this.hasPoses = hasPoses;
				this.hasMorphs = hasMorphs;
			}

			public override string ToString()
			{
				if (name == "")
					return Strings.Get("(All)");
				else
					return name;
			}
		}

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

		private bool ShouldShow(MorphFilter filter, Category c)
		{
			if (!filter.search.IsMatch(c.name))
				return false;

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

			// all
			cats_.Add(new Category("", true, true));

			if (atom_ == null)
				return;

			var d = new Dictionary<string, Category>();

			foreach (var morph in Utilities.GetAtomMorphs(atom_))
			{
				Category cat;

				var name = morph.region;
				if (name == "")
					name = Strings.Get("(No category)");

				if (d.TryGetValue(morph.region, out cat))
				{
					cat.hasMorphs = cat.hasMorphs || !morph.isPoseControl;
					cat.hasPoses = cat.hasPoses || morph.isPoseControl;
				}
				else
				{
					cat = new Category(
						name,
						morph.isPoseControl, !morph.isPoseControl);

					d.Add(morph.region, cat);
				}
			}

			cats_ = d.Values.ToList();
		}

		private void OnSelection(Category cat)
		{
			if (ignore_)
				return;

			CategorySelected?.Invoke(cat.name);
		}
	}


	class AddMorphsTab : UI.Panel
	{
		private readonly UI.Stack mainStack_, morphsStack_;
		private readonly UI.ComboBox<string> show_;
		private readonly MorphCategoryListView categories_;
		private readonly MorphListView morphs_, allMorphs_;
		private readonly SearchTextBox search_;
		private readonly UI.Button toggle_;

		private Atom atom_ = null;
		private readonly HashSet<DAZMorph> selection_ = new HashSet<DAZMorph>();
		private readonly List<MorphItem> items_ = new List<MorphItem>();
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public AddMorphsTab()
		{
			categories_ = new MorphCategoryListView();
			categories_.CategorySelected += OnCategorySelected;

			var cats = new UI.Panel(new UI.BorderLayout());
			cats.Add(new UI.Label(S("Categories")), UI.BorderLayout.Top);
			cats.Add(categories_.List, UI.BorderLayout.Center);


			morphs_ = new MorphListView();
			//morphs_.SelectionChanged += OnMorphSelected;

			allMorphs_ = new MorphListView();
			//allMorphs_.SelectionChanged += OnAllMorphSelected;

			morphsStack_ = new UI.Stack();
			morphsStack_.AddToStack(morphs_.List);
			morphsStack_.AddToStack(allMorphs_.List);

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
				//morphs_.Clear();
			}
		}

		private void UpdateToggleButton()
		{
			var m = ActiveMorphList?.Selected;

			if (m != null && m.selected)
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

			int flags = 0;

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
			if (atom_ == null)
			{
				mainStack_.Select(0);
				return;
			}

			mainStack_.Select(1);
			categories_.Update(atom_, CreateFilter());
		}

		private void UpdateMorphs(int catIndex)
		{
			/*
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
			}*/
		}

		private void OnCategorySelected(string name)
		{
			if (ignore_)
				return;

			//UpdateMorphs(name);
		}

		private void OnSearchChanged(string s)
		{
			//UpdateCategories();
			//UpdateMorphs(categories_.SelectedIndex);
		}

		private UI.ListView<MorphItem> ActiveMorphList
		{
			get
			{
				//if (morphsStack_.Selected == 0)
				//	return morphs_;
				//else
				//	return allMorphs_;
				return null;
			}
		}

		private void OnToggleMorph()
		{
			//var m = ActiveMorphList.Selected;
			//m.selected = !m.selected;
			//ActiveMorphList.UpdateItemText(ActiveMorphList.SelectedIndex);
			//UpdateToggleButton();
		}

		private void OnShowChanged(string s)
		{
			UpdateCategories();
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
