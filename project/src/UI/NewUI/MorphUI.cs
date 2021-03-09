using UI = SynergyUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SynergyUI;

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
			morphs_.MorphsChanged += OnMorphsChanged;
			addMorphs_.MorphsChanged += OnMorphsChanged;

			tabs_.Select(0);
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
				progression_.Set(modifier_);
				morphs_.Set(modifier_);
				addMorphs_.Set(modifier_.Atom, modifier_.Morphs);
			});
		}

		private void OnAtomSelected(Atom atom)
		{
			if (ignore_ || modifier_ == null)
				return;

			modifier_.Atom = atom;
			morphs_.Set(modifier_);
			addMorphs_.Set(modifier_.Atom, modifier_.Morphs);
		}

		private void OnMorphsChanged(List<DAZMorph> morphs)
		{
			modifier_.SetMorphs(morphs);
			morphs_.SelectedMorphs = modifier_.Morphs;
			addMorphs_.Set(modifier_.Atom, modifier_.Morphs);
		}

		private void OnTabSelected(int index)
		{
			addMorphs_.SetActive(index == tabs_.IndexOfWidget(addMorphs_));
		}
	}


	class MorphProgressionTab : UI.Panel
	{
		private MorphModifier modifier_ = null;

		private readonly FactoryComboBox<
			MorphProgressionFactory, IMorphProgression> type_;

		private FactoryObjectWidget<
			MorphProgressionFactory,
			IMorphProgression,
			MorphProgressionUIFactory> ui_;

		private IgnoreFlag ignore_ = new IgnoreFlag();


		public MorphProgressionTab()
		{
			type_ = new FactoryComboBox<
				MorphProgressionFactory, IMorphProgression>(OnTypeChanged);

			ui_ = new FactoryObjectWidget<
				MorphProgressionFactory,
				IMorphProgression,
				MorphProgressionUIFactory>();

			Layout = new UI.BorderLayout(20);

			var p = new UI.Panel(new UI.HorizontalFlow(20));
			p.Add(new UI.Label(S("Progression type")));
			p.Add(type_);

			Add(p, UI.BorderLayout.Top);
			Add(ui_, UI.BorderLayout.Center);
		}

		public void Set(MorphModifier m)
		{
			modifier_ = m;

			ignore_.Do(() =>
			{
				type_.Select(modifier_?.Progression);
				ui_.Set(modifier_?.Progression);
			});
		}

		private void OnTypeChanged(IMorphProgression mp)
		{
			if (ignore_ || modifier_ == null)
				return;

			modifier_.Progression = mp;
			ui_.Set(mp);
		}
	}


	class MorphProgressionUIFactory : IUIFactory<IMorphProgression>
	{
		public Dictionary<string, Func<IUIFactoryWidget<IMorphProgression>>> GetCreators()
		{
			return new Dictionary<string, Func<IUIFactoryWidget<IMorphProgression>>>()
			{
				{
					NaturalMorphProgression.FactoryTypeName,
					() => { return new NaturalMorphProgressionUI(); }
				},

				{
					ConcurrentMorphProgression.FactoryTypeName,
					() => { return new ConcurrentMorphProgressionUI(); }
				},

				{
					SequentialMorphProgression.FactoryTypeName,
					() => { return new SequentialMorphProgressionUI(); }
				},

				{
					RandomMorphProgression.FactoryTypeName,
					() => { return new RandomMorphProgressionUI(); }
				},
			};
		}
	}


	class NaturalMorphProgressionUI : UI.Panel, IUIFactoryWidget<IMorphProgression>
	{
		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly DurationPanel duration_ = new DurationPanel();
		private readonly DelayWidgets delay_ = new DelayWidgets();

		private NaturalMorphProgression progression_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public NaturalMorphProgressionUI()
		{
			Layout = new UI.BorderLayout(20);

			Add(new UI.Label(S(
				"Morphs will use their own copy of the duration and " +
				"delay set below.")),
				UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);


			tabs_.AddTab(S("Duration"), duration_);
			tabs_.AddTab(S("Delay"), delay_);

			duration_.Changed += OnDurationTypeChanged;
		}

		public void Set(IMorphProgression o)
		{
			progression_ = o as NaturalMorphProgression;

			ignore_.Do(() =>
			{
				duration_.Set(progression_.Duration);
				delay_.Set(progression_.Delay);
			});
		}

		private void OnDurationTypeChanged(IDuration d)
		{
			if (ignore_)
				return;

			progression_.Duration = d;
		}
	}


	class ConcurrentMorphProgressionUI : UI.Panel, IUIFactoryWidget<IMorphProgression>
	{
		public ConcurrentMorphProgressionUI()
		{
			Layout = new UI.BorderLayout();
			Add(new UI.Label(
				S("All morphs will be set concurrently.")),
				UI.BorderLayout.Top);
		}

		public void Set(IMorphProgression o)
		{
			// no-op
		}
	}


	abstract class OrderedMorphProgressionUI :
		UI.Panel, IUIFactoryWidget<IMorphProgression>
	{
		private OrderedMorphProgression progression_ = null;

		private readonly UI.CheckBox hold_;
		private readonly IgnoreFlag ignore_ = new IgnoreFlag();

		public OrderedMorphProgressionUI(string text)
		{
			hold_ = new UI.CheckBox(S("Hold halfway"), OnHoldHawayChanged);

			var p = new UI.Panel(new UI.VerticalFlow(20));
			p.Add(hold_);

			Layout = new UI.BorderLayout(40);
			Add(new UI.Label(text), UI.BorderLayout.Top);
			Add(p, UI.BorderLayout.Center);
		}

		public void Set(IMorphProgression o)
		{
			progression_ = o as OrderedMorphProgression;

			ignore_.Do(() =>
			{
				hold_.Checked = progression_.HoldHalfway;
			});
		}

		private void OnHoldHawayChanged(bool b)
		{
			if (ignore_)
				return;

			progression_.HoldHalfway = b;
		}
	}


	class SequentialMorphProgressionUI : OrderedMorphProgressionUI
	{
		public SequentialMorphProgressionUI()
			: base(S("Morphs will be set sequentially."))
		{
		}
	}


	class RandomMorphProgressionUI : OrderedMorphProgressionUI
	{
		public RandomMorphProgressionUI()
			: base(S("Morphs will be set sequentially in a random order."))
		{
		}
	}


	class MorphPanel : UI.Panel
	{
		public delegate void MorphsCallback(List<DAZMorph> list);
		public event MorphsCallback MorphsChanged;

		private readonly UI.CheckBox enabled_ = new UI.CheckBox(S("Enabled"));

		private readonly MovementUI movement_ = new MovementUI(
			MovementWidgets.SmallMovement);

		private MorphModifier modifier_ = null;
		private SelectedMorph morph_ = null;

		public MorphPanel()
		{
			var top = new UI.Panel(new UI.HorizontalFlow());
			top.Add(enabled_);
			top.Add(new UI.HorizontalStretch());
			top.Add(new UI.Button(S("Remove morph"), OnRemove));

			Layout = new UI.VerticalFlow(40);

			movement_.MinimumPanel.ButtonsPanel.Add(
				new UI.Button(S("Copy to other morphs"), OnCopyMinimum));

			movement_.MaximumPanel.ButtonsPanel.Add(
				new UI.Button(S("Copy to other morphs"), OnCopyMaximum));

			Add(top);
			Add(movement_);

			enabled_.Changed += OnEnabled;
		}

		public void Set(MorphModifier mm, SelectedMorph sm)
		{
			modifier_ = mm;
			morph_ = sm;

			if (morph_ == null)
				return;

			enabled_.Checked = sm.Enabled;
			movement_.Set(sm.Movement);
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

		private void OnRemove()
		{
			if (modifier_ == null || morph_ == null)
				return;

			var list = new List<DAZMorph>();

			foreach (var sm in modifier_.Morphs)
			{
				if (sm != morph_)
					list.Add(sm.Morph);
			}

			MorphsChanged?.Invoke(list);
		}
	}


	class SelectedMorphsTab : UI.Panel
	{
		public delegate void MorphsCallback(List<DAZMorph> list);
		public event MorphsCallback MorphsChanged;

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
			panel_.MorphsChanged += (list) =>
			{
				MorphsChanged?.Invoke(list);
			};

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
		private UI.Timer timer_ = null;

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
		public const int LatestOnly = 0x04;
		public const int FavoritesOnly = 0x08;

		public string searchText;
		public Regex search;
		public int flags;

		public MorphFilter(string searchText, int flags)
		{
			this.searchText = searchText;
			this.flags = flags;

			var pat = Regex.Escape(searchText).Replace("\\*", ".*");
			this.search = new Regex(pat, RegexOptions.IgnoreCase);
		}

		public bool Restricted
		{
			get
			{
				return (searchText != "" || Bits.IsSet(flags, FavoritesOnly));
			}
		}
	}


	class MorphCategoryListView
	{
		public class Category
		{
			public string name;
			public bool hasPoses;
			public bool hasMorphs;
			public bool hasFavorites;
			public List<DAZMorph> morphs;

			public Category(string name)
			{
				this.name = name;
				hasPoses = false;
				hasMorphs = false;
				hasFavorites = false;
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
			if (Bits.IsSet(filter.flags, MorphFilter.FavoritesOnly))
			{
				if (!c.hasFavorites)
					return false;
			}


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
			// all
			if (c.name == "")
				return true;

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
			cats_.Clear();

			if (atom_ == null)
				return;

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
				cat.hasFavorites = cat.hasFavorites || morph.favorite;

				cat.morphs.Add(morph);
			}

			cats_.AddRange(d.Values.ToList());
			Utilities.NatSort(cats_);

			var all = new Category("");
			all.hasMorphs = true;
			all.hasPoses = true;
			all.hasFavorites = true;
			cats_.Insert(0, all);
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
				string s = "";

				if (active)
					s += "\u2713";
				else
					s += "   ";

				if (morph.isInPackage)
					s += "P ";
				else
					s += "   ";

				s += morph.displayName;

				if (morph.isInPackage)
					s += " " + morph.version;

				return s;
			}
		}

		public class DummyMorph : Morph
		{
			private string s_;

			public DummyMorph(string s)
				: base(null)
			{
				s_ = s;
			}

			public override string ToString()
			{
				return s_;
			}
		}

		class MorphList
		{
			private readonly List<Morph> list_ = new List<Morph>();
			private bool mustSort_ = false;

			public Morph Find(DAZMorph m)
			{
				foreach (var s in list_)
				{
					if (s.morph == m)
						return s;
				}

				return null;
			}

			public void Add(Morph m)
			{
				list_.Add(m);
				mustSort_ = true;
			}

			public List<Morph> SortedList
			{
				get
				{
					if (mustSort_)
					{
						Utilities.NatSort(list_);
						mustSort_ = false;
					}

					return list_;
				}
			}
		}


		public delegate void MorphCallback(Morph m);
		public event MorphCallback MorphSelected;
		public event MorphCallback MorphActivated;

		private readonly UI.ListView<Morph> list_;
		private readonly Dictionary<string, MorphList> morphs_ =
			new Dictionary<string, MorphList>();
		private MorphFilter filter_;
		private Atom atom_ = null;

		public MorphListView()
		{
			list_ = new UI.ListView<Morph>();
			list_.SelectionChanged += OnSelection;
			list_.ItemActivated += OnItemActivated;
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
			if (morphs_.Count == 0)
				return null;

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

			MorphList list = null;
			if (!morphs_.TryGetValue(cat, out list))
			{
				Synergy.LogError("can't find category '" + cat + "'");
				return null;
			}

			return list.Find(m);
		}

		private bool ShouldShow(string category, MorphFilter filter, Morph m)
		{
			// don't check names for the 'all' category when there's no filter
			if (category != "" || filter.searchText != "")
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

			if (Bits.IsSet(filter.flags, MorphFilter.FavoritesOnly))
			{
				if (!m.morph.favorite)
					return false;
			}

			if (Bits.IsSet(filter.flags, MorphFilter.LatestOnly))
			{
				if (!m.morph.isLatestVersion)
					return false;
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
				if (filter.Restricted)
				{
					// all
					foreach (var pair in morphs_)
					{
						foreach (var m in pair.Value.SortedList)
						{
							if (!ShouldShow(category, filter, m))
								continue;

							items.Add(m);
						}
					}
				}
				else
				{
					items.Add(new DummyMorph(Strings.Get("(only for search)")));
				}
			}
			else
			{
				MorphList list = null;

				if (morphs_.TryGetValue(category, out list))
				{
					foreach (var m in list.SortedList)
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
			morphs_.Clear();
			if (atom_ == null)
				return;

			foreach (var morph in Utilities.GetAtomMorphs(atom_))
			{
				var cat = MorphCategoryListView.MakeCategoryName(morph);

				MorphList list = null;

				if (!morphs_.TryGetValue(cat, out list))
				{
					list = new MorphList();
					morphs_.Add(cat, list);
				}

				list.Add(new Morph(morph));
			}
		}

		private void OnSelection(Morph m)
		{
			MorphSelected?.Invoke(m);
		}

		private void OnItemActivated(Morph m)
		{
			MorphActivated?.Invoke(m);
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
		private readonly UI.CheckBox onlyLatest_, onlyFavorites_;
		private readonly UI.Button toggle_;

		private Atom atom_ = null;
		private readonly List<DAZMorph> selection_ = new List<DAZMorph>();
		private bool dirty_ = false;
		private bool active_ = false;
		private IgnoreFlag ignore_ = new IgnoreFlag();
		private IgnoreFlag ignoreMorphChanged_ = new IgnoreFlag();

		public AddMorphsTab()
		{
			categories_ = new MorphCategoryListView();
			categories_.CategorySelected += OnCategorySelected;

			var cats = new UI.Panel(new UI.BorderLayout());
			cats.Add(new UI.Label(S("Categories")), UI.BorderLayout.Top);
			cats.Add(categories_.List, UI.BorderLayout.Center);

			morphs_ = new MorphListView();
			morphs_.MorphSelected += OnMorphSelected;
			morphs_.MorphActivated += OnMorphActivated;

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

			onlyLatest_ = new UI.CheckBox("Latest", OnLatestChanged, true);
			top.Add(onlyLatest_);

			onlyFavorites_ = new UI.CheckBox("Favorites", OnFavoritesChanged);
			top.Add(onlyFavorites_);

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

		public void Set(Atom atom, List<SelectedMorph> morphs)
		{
			if (atom_ == atom && ignoreMorphChanged_)
				return;

			atom_ = atom;

			foreach (var s in selection_)
				morphs_.SetActive(s, false);

			selection_.Clear();

			foreach (var sm in morphs)
				selection_.Add(sm.Morph);

			NeedsUpdate();
			SetStack();
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

			UpdateToggleButton();

			dirty_ = false;
		}

		private void UpdateToggleButton()
		{
			var m = morphs_.Selected;

			if (m?.morph != null && m.active)
				toggle_.Text = S("Remove morph");
			else
				toggle_.Text = S("Add morph");

			toggle_.Enabled = (m?.morph != null);
		}

		private MorphFilter CreateFilter()
		{
			int flags = 0;

			switch (show_.SelectedIndex)
			{
				case 1:
					flags |= MorphFilter.ShowMorphs;
					break;

				case 2:
					flags |= MorphFilter.ShowPoses;
					break;

				case 0:
				default:
					flags |= MorphFilter.ShowMorphs | MorphFilter.ShowPoses;
					break;
			}

			if (onlyLatest_.Checked)
				flags |= MorphFilter.LatestOnly;

			if (onlyFavorites_.Checked)
				flags |= MorphFilter.FavoritesOnly;

			return new MorphFilter(search_.Text, flags);
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

		private void OnMorphActivated(MorphListView.Morph m)
		{
			OnToggleMorph();
		}

		private void OnSearchChanged(string s)
		{
			UpdateCategories();
			UpdateMorphs();
		}

		private void OnLatestChanged(bool b)
		{
			UpdateMorphs();
		}

		private void OnFavoritesChanged(bool b)
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

			ignoreMorphChanged_.Do(() =>
			{
				MorphsChanged?.Invoke(new List<DAZMorph>(selection_));
			});
		}

		private void OnShowChanged(string s)
		{
			UpdateCategories();
			UpdateMorphs();
		}
	}
}
