using System.Collections.Generic;

namespace Synergy.NewUI
{
	// note that GetAllParamAndActionNames() seems to return an internal
	// reference, changing it breaks the parameters for the atom until vam is
	// restarted, so this always copies the list, wherever it comes from, just
	// in case
	//
	class StorableFilter
	{
		private string type_;
		private bool pluginsOnly_;

		public StorableFilter(string type, bool pluginsOnly)
		{
			type_ = type ?? "";
			pluginsOnly_ = pluginsOnly;
		}

		public override int GetHashCode()
		{
			return HashHelper.GetHashCode(type_, pluginsOnly_);
		}

		public override bool Equals(object o)
		{
			var f = o as StorableFilter;
			if (f == null)
				return false;

			return Equals(f);
		}

		public bool Equals(StorableFilter f)
		{
			return (type_ == f.type_ && pluginsOnly_ == f.pluginsOnly_);
		}

		public List<string> GetStorables(Atom atom)
		{
			var list = new List<string>();
			if (atom == null)
				return list;

			if (type_ == "")
			{
				list = new List<string>(atom.GetStorableIDs());
			}
			else
			{
				var sp = new StorableParameterFactory().Create(type_);
				if (sp == null)
				{
					Synergy.LogError($"unknown type {type_}");
					list = new List<string>(atom.GetStorableIDs());
				}
				else
				{
					list = new List<string>(sp.GetStorableNames(atom, pluginsOnly_));
				}
			}

			return list;
		}

		public List<string> GetParameters(JSONStorable storable)
		{
			var list = new List<string>();
			if (storable == null)
				return list;

			if (type_ == "")
			{
				list = new List<string>(storable.GetAllParamAndActionNames());
			}
			else
			{
				var sp = new StorableParameterFactory().Create(type_);
				if (sp == null)
				{
					Synergy.LogError($"unknown type {type_}");
					list = new List<string>(storable.GetAllParamAndActionNames());
				}
				else
				{
					list = new List<string>(sp.GetParameterNames(storable));
				}
			}

			return list;
		}
	}


	class StorableList : UI.Widget
	{
		public delegate void StorableCallback(string id);
		public event StorableCallback StorableChanged;

		private Atom atom_ = null;
		private string id_ = "";
		private bool stale_ = true;
		private StorableFilter filter_ = null;

		private readonly IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly UI.ComboBox<string> list_ = new UI.ComboBox<string>();

		public StorableList()
		{
			list_.Filterable = true;
			list_.SelectionChanged += OnSelectionChanged;
			list_.Opened += OnOpened;

			Layout = new UI.BorderLayout();
			Add(list_, UI.BorderLayout.Center);
		}

		public Atom Atom
		{
			get { return atom_; }
		}

		public void Set(Atom atom, string storableID, StorableFilter filter)
		{
			ignore_.Do(() =>
			{
				if (atom_ == atom && filter_ != null && filter_.Equals(filter))
				{
					UpdateIfStale();
					list_.Select(storableID);
				}
				else
				{
					// fake it
					list_.Clear();
					list_.SetItems(new List<string>() { storableID }, storableID);
					stale_ = true;
				}

				atom_ = atom;
				id_ = storableID;
				filter_ = filter;
			});
		}

		private void OnSelectionChanged(string id)
		{
			if (ignore_)
				return;

			StorableChanged?.Invoke(id);
		}

		private void OnOpened()
		{
			UpdateIfStale();
		}

		private void UpdateIfStale()
		{
			if (stale_)
			{
				Update();
				stale_ = false;
			}
		}

		private void Update()
		{
			ignore_.Do(() =>
			{
				if (atom_ == null || filter_ == null)
				{
					list_.Clear();
					return;
				}

				var items = filter_.GetStorables(atom_);

				if (id_ != "" && !items.Contains(id_))
				{
					// current selection is filtered out, add it
					items.Add(id_);
				}

				Utilities.NatSort(items);
				items.Insert(0, "");

				list_.SetItems(items, id_);
			});
		}
	}


	class ParameterList : UI.Widget
	{
		public delegate void ParameterCallback(string id);
		public event ParameterCallback ParameterChanged;

		private Atom atom_ = null;
		private JSONStorable storable_ = null;
		private string id_ = "";

		private readonly IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly UI.ComboBox<string> list_ = new UI.ComboBox<string>();

		public ParameterList()
		{
			list_.Filterable = true;
			list_.SelectionChanged += OnSelectionChanged;

			Layout = new UI.BorderLayout();
			Add(list_, UI.BorderLayout.Center);
		}

		public void Set(
			Atom atom, JSONStorable storable, string parameterID,
			StorableFilter filter)
		{
			atom_ = atom;
			storable_ = storable;
			id_ = parameterID;

			Update(filter);
		}

		public Atom Atom
		{
			get { return atom_; }
		}

		public string ID
		{
			get { return id_; }
		}

		private void OnSelectionChanged(string id)
		{
			if (ignore_)
				return;

			ParameterChanged?.Invoke(id);
		}

		private void Update(StorableFilter filter)
		{
			ignore_.Do(() =>
			{
				if (atom_ == null || storable_ == null)
				{
					list_.Clear();
					return;
				}

				var items = filter.GetParameters(storable_);

				if (id_ != "" && !items.Contains(id_))
				{
					// current selection is filtered out, add it
					items.Add(id_);
				}

				Utilities.NatSort(items);
				items.Insert(0, "");

				list_.SetItems(items, id_);
			});
		}
	}


	class FilterList : UI.Widget
	{
		class Filter
		{
			private string display_;
			private string type_;

			public Filter(string display, string type)
			{
				display_ = display;
				type_ = type;
			}

			public string Type
			{
				get { return type_; }
			}

			public override string ToString()
			{
				return display_;
			}
		}


		public delegate void FilterCallback();
		public event FilterCallback Changed;

		private readonly UI.ComboBox<Filter> list_ = new UI.ComboBox<Filter>();
		private readonly UI.CheckBox plugins_ = new UI.CheckBox(S("Plugins only"));

		public FilterList()
		{
			Layout = new UI.HorizontalFlow(10);
			Add(list_);
			Add(plugins_);

			var displayNames = new StorableParameterFactory().GetAllDisplayNames();
			var typeNames = new StorableParameterFactory().GetAllFactoryTypeNames();

			var items = new List<Filter>();

			items.Add(new Filter("All", ""));
			for (int i = 0; i < typeNames.Count; ++i)
				items.Add(new Filter(displayNames[i], typeNames[i]));

			list_.Items = items;

			list_.SelectionChanged += (s) => Changed?.Invoke();
			plugins_.Changed += (b) => Changed?.Invoke();
		}

		public StorableFilter Get()
		{
			return new StorableFilter(list_.Selected.Type, plugins_.Checked);
		}
	}


	class StorableModifierPanel : BasicModifierPanel
	{
		private StorableModifier modifier_ = null;

		private IgnoreFlag ignore_ = new IgnoreFlag();
		private AtomComboBox atom_ = new AtomComboBox();
		private StorableList storable_ = new StorableList();
		private ParameterList parameter_ = new ParameterList();
		private FilterList filter_ = new FilterList();

		public StorableModifierPanel()
		{
			var panel = new UI.Panel(new UI.GridLayout(2, 10));
			panel.Add(new UI.Label(S("Filter")));
			panel.Add(filter_);
			panel.Add(new UI.Spacer(30));
			panel.Add(new UI.Panel());
			panel.Add(new UI.Label(S("Atom")));
			panel.Add(atom_);
			panel.Add(new UI.Label(S("Storable")));
			panel.Add(storable_);
			panel.Add(new UI.Label(S("Parameter")));
			panel.Add(parameter_);

			Layout = new UI.VerticalFlow();
			Add(panel);

			atom_.AtomSelectionChanged += OnAtomChanged;
			storable_.StorableChanged += OnStorableChanged;
			parameter_.ParameterChanged += OnParameterChanged;
			filter_.Changed += OnFilterChanged;
		}

		public override string Title
		{
			get { return S("Storable"); }
		}

		public override bool Accepts(IModifier m)
		{
			return m is StorableModifier;
		}

		public override void Set(IModifier m)
		{
			modifier_ = m as StorableModifier;
			UpdateLists();
		}

		private void OnAtomChanged(Atom a)
		{
			if (ignore_ || modifier_ == null)
				return;

			modifier_.Atom = a;
			UpdateLists();
		}

		private void OnStorableChanged(string id)
		{
			if (ignore_ || modifier_ == null)
				return;

			if (id == null)
				id = "";

			modifier_.SetStorable(id);
			UpdateLists();
		}

		private void OnParameterChanged(string id)
		{
			if (ignore_ || modifier_ == null)
				return;

			modifier_.SetParameter(id);
			UpdateLists();
		}

		private void OnFilterChanged()
		{
			UpdateLists();
		}

		private void UpdateLists()
		{
			ignore_.Do(() =>
			{
				atom_.Select(modifier_.Atom);

				storable_.Set(
					modifier_.Atom,
					modifier_.Storable?.storeId ?? "",
					filter_.Get());

				parameter_.Set(
					modifier_.Atom,
					modifier_.Storable,
					modifier_.Parameter?.Name ?? "",
					filter_.Get());
			});
		}
	}
}
