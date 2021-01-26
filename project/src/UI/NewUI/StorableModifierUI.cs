using System.Collections.Generic;

namespace Synergy.NewUI
{
	struct StorableFilter
	{
		private string type_;
		private bool pluginsOnly_;

		public StorableFilter(string type, bool pluginsOnly)
		{
			type_ = type ?? "";
			pluginsOnly_ = pluginsOnly;
		}

		public List<string> GetStorables(Atom atom)
		{
			if (atom == null)
				return new List<string>();

			if (type_ == "")
				return atom.GetStorableIDs();

			var sp = new StorableParameterFactory().Create(type_);
			if (sp == null)
			{
				Synergy.LogError($"unknown type {type_}");
				return atom.GetStorableIDs();
			}

			return new List<string>(sp.GetStorableNames(atom, pluginsOnly_));
		}

		public List<string> GetParameters(JSONStorable storable)
		{
			if (storable == null)
				return new List<string>();

			if (type_ == "")
				return storable.GetAllParamAndActionNames();

			var sp = new StorableParameterFactory().Create(type_);
			if (sp == null)
			{
				Synergy.LogError($"unknown type {type_}");
				return storable.GetAllParamAndActionNames();
			}

			return new List<string>(sp.GetParameterNames(storable));
		}
	}


	class StorableList : UI.Widget
	{
		public delegate void StorableCallback(string id);
		public event StorableCallback StorableChanged;

		private Atom atom_ = null;
		private string id_ = "";
		private readonly UI.ComboBox<string> list_ = new UI.ComboBox<string>();

		public StorableList()
		{
			list_.Filterable = true;
			list_.SelectionChanged += (id) => StorableChanged?.Invoke(id);

			Layout = new UI.BorderLayout();
			Add(list_, UI.BorderLayout.Center);
		}

		public Atom Atom
		{
			get { return atom_; }
		}

		public void Set(Atom atom, string storableID, StorableFilter filter)
		{
			atom_ = atom;
			id_ = storableID;

			Update(filter);
		}

		private void Update(StorableFilter filter)
		{
			if (atom_ == null)
			{
				list_.Clear();
				return;
			}

			var items = filter.GetStorables(atom_);

			Utilities.NatSort(items);
			items.Insert(0, "");

			list_.SetItems(items, id_);
		}
	}


	class ParameterList : UI.Widget
	{
		public delegate void ParameterCallback(string id);
		public event ParameterCallback ParameterChanged;

		private Atom atom_ = null;
		private JSONStorable storable_ = null;
		private string id_ = "";
		private readonly UI.ComboBox<string> list_ = new UI.ComboBox<string>();

		public ParameterList()
		{
			list_.Filterable = true;
			list_.SelectionChanged += (id) => ParameterChanged?.Invoke(id);

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

		private void Update(StorableFilter filter)
		{
			if (atom_ == null || storable_ == null)
			{
				list_.Clear();
				return;
			}

			var items = filter.GetParameters(storable_);

			Utilities.NatSort(items);
			items.Insert(0, "");

			list_.SetItems(items, id_);
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
			filter_.Changed += UpdateLists;
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
