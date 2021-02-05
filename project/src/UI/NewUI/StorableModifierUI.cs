using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Synergy.NewUI
{
	using StorableParameterUIWidget = FactoryObjectWidget<
			StorableParameterFactory, IStorableParameter, StorableParameterUIFactory>;

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
			list_.AccuratePreferredSize = false;
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

				List<string> items = filter_.GetStorables(atom_);

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
		private StorableFilter filter_ = null;
		private bool stale_ = false;

		private readonly IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly UI.ComboBox<string> list_ = new UI.ComboBox<string>();

		public ParameterList()
		{
			list_.Filterable = true;
			list_.AccuratePreferredSize = false;
			list_.SelectionChanged += OnSelectionChanged;
			list_.Opened += OnOpened;

			Layout = new UI.BorderLayout();
			Add(list_, UI.BorderLayout.Center);
		}

		public void Set(
			Atom atom, JSONStorable storable, string parameterID,
			StorableFilter filter)
		{
			if (atom_ == atom && storable_ == storable && (filter_ != null && filter_.Equals(filter)))
			{
				list_.Select(parameterID);
			}
			else
			{
				// fake it
				list_.Clear();
				list_.SetItems(new List<string>() { parameterID }, parameterID);
				stale_ = true;
			}


			atom_ = atom;
			storable_ = storable;
			id_ = parameterID;
			filter_ = filter;
		}

		public Atom Atom
		{
			get { return atom_; }
		}

		public string ID
		{
			get { return id_; }
		}

		private void OnOpened()
		{
			UpdateIfStale();
		}

		private void OnSelectionChanged(string id)
		{
			if (ignore_)
				return;

			ParameterChanged?.Invoke(id);
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
				if (atom_ == null || storable_ == null || filter_ == null)
				{
					list_.Clear();
					return;
				}

				var items = filter_.GetParameters(storable_);

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

			list_.SetItems(items);

			list_.SelectionChanged += (s) => Changed?.Invoke();
			plugins_.Changed += (b) => Changed?.Invoke();
		}

		public StorableFilter Get()
		{
			return new StorableFilter(list_.Selected.Type, plugins_.Checked);
		}
	}


	class StorableParameterUIFactory : IUIFactory<IStorableParameter>
	{
		public Dictionary<string, Func<IUIFactoryWidget<IStorableParameter>>> GetCreators()
		{
			return new Dictionary<string, Func<IUIFactoryWidget<IStorableParameter>>>()
			{
				{
					FloatStorableParameter.FactoryTypeName,
					() => { return new FloatStorableParameterUI(); }
				},

				{
					ActionStorableParameter.FactoryTypeName,
					() => { return new ActionStorableParameterUI(); }
				},

				{
					BoolStorableParameter.FactoryTypeName,
					() => { return new BoolStorableParameterUI(); }
				},

				{
					ColorStorableParameter.FactoryTypeName,
					() => { return new ColorStorableParameterUI(); }
				},

				{
					UrlStorableParameter.FactoryTypeName,
					() => { return new UrlStorableParameterUI(); }
				},

				{
					StringStorableParameter.FactoryTypeName,
					() => { return new StringStorableParameterUI(); }
				},

				{
					StringChooserStorableParameter.FactoryTypeName,
					() => { return new StringChooserStorableParameterUI(); }
				}
			};
		}
	}


	class FloatStorableParameterUI : UI.Panel, IUIFactoryWidget<IStorableParameter>
	{
		public FloatStorableParameterUI()
		{
			Layout = new UI.VerticalFlow();

			Add(new UI.Label(S(
				"This is a float parameter. Use the sliders in the Range tab " +
				"to control it.")));
		}

		public void Set(IStorableParameter t)
		{
			// no-op
		}
	}


	class ActionStorableParameterUI : UI.Panel, IUIFactoryWidget<IStorableParameter>
	{
		class TriggerType
		{
			private string display_;
			private int value_;

			public TriggerType(string display, int value)
			{
				display_ = display;
				value_ = value;
			}

			public int Value
			{
				get { return value_; }
			}

			public override int GetHashCode()
			{
				return value_;
			}

			public override bool Equals(object o)
			{
				var t = o as TriggerType;
				if (t == null)
					return false;

				return Equals(t);
			}

			public bool Equals(TriggerType t)
			{
				return (value_ == t.value_);
			}

			public override string ToString()
			{
				return display_;
			}
		}

		private ActionStorableParameter param_ = null;

		private readonly IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly UI.TextSlider triggerAt_ = new UI.TextSlider();
		private readonly UI.ComboBox<TriggerType> triggerType_ = new UI.ComboBox<TriggerType>();

		public ActionStorableParameterUI()
		{
			var panel = new UI.Panel(new UI.GridLayout(2, 10));
			panel.Add(new UI.Label(S("Trigger at")));
			panel.Add(triggerAt_);
			panel.Add(new UI.Label(S("Trigger type")));
			panel.Add(triggerType_);

			Layout = new UI.VerticalFlow();
			Add(panel);


			var names = ActionStorableParameter.TriggerTypeNames();
			var values = ActionStorableParameter.TriggerTypes();

			var items = new List<TriggerType>();
			for (int i = 0; i < names.Count; ++i)
				items.Add(new TriggerType(names[i], values[i]));

			triggerType_.SetItems(items);

			triggerAt_.ValueChanged += OnTriggerAtChanged;
			triggerType_.SelectionChanged += OnTriggerTypeChanged;
		}

		public void Set(IStorableParameter t)
		{
			param_ = t as ActionStorableParameter;
			if (param_ == null)
				return;

			ignore_.Do(() =>
			{
				triggerAt_.Set(param_.TriggerMagnitude, 0, 1);
				triggerType_.Select(new TriggerType("", param_.TriggerType));
			});
		}

		private void OnTriggerAtChanged(float f)
		{
			if (ignore_ || param_ == null)
				return;

			param_.TriggerMagnitude = f;
		}

		private void OnTriggerTypeChanged(TriggerType t)
		{
			if (ignore_ || param_ == null)
				return;

			param_.TriggerType = t.Value;
		}
	}


	class BoolStorableParameterUI : UI.Panel, IUIFactoryWidget<IStorableParameter>
	{
		public BoolStorableParameterUI()
		{
			Layout = new UI.VerticalFlow();

			Add(new UI.Label(S(
				"This is a bool parameter. Use the sliders in the Range tab " +
				"to control it. The parameter will be set to false when the " +
				"value <= 0.5, true otherwise.")));
		}

		public void Set(IStorableParameter t)
		{
			// no-op
		}
	}


	class ColorStorableParameterUI : UI.Panel, IUIFactoryWidget<IStorableParameter>
	{
		private ColorStorableParameter param_ = null;
		private readonly IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly UI.ColorPicker color1_, color2_;

		public ColorStorableParameterUI()
		{
			color1_ = new UI.ColorPicker(S("Color 1"), OnColor1Changed);
			color2_ = new UI.ColorPicker(S("Color 2"), OnColor2Changed);

			Layout = new UI.HorizontalFlow(0);
			Add(color1_);
			Add(color2_);
		}

		public void Set(IStorableParameter t)
		{
			param_ = t as ColorStorableParameter;
			if (param_ == null)
				return;

			ignore_.Do(() =>
			{
				color1_.Color = param_.Color1;
				color2_.Color = param_.Color2;
			});
		}

		private void OnColor1Changed(Color c)
		{
			if (ignore_ || param_ == null)
				return;

			param_.Color1 = c;
		}

		private void OnColor2Changed(Color c)
		{
			if (ignore_ || param_ == null)
				return;

			param_.Color2 = c;
		}
	}


	class UrlStorableParameterUI : UI.Panel, IUIFactoryWidget<IStorableParameter>
	{
		public void Set(IStorableParameter t)
		{
		}
	}


	class StringStorableParameterUI : UI.Panel, IUIFactoryWidget<IStorableParameter>
	{
		public void Set(IStorableParameter t)
		{
		}
	}


	class StringChooserStorableParameterUI : UI.Panel, IUIFactoryWidget<IStorableParameter>
	{
		private StringChooserStorableParameter param_ = null;
		private readonly IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly UI.ListView<string> list_ = new UI.ListView<string>();
		private readonly UI.ComboBox<string> av_ = new UI.ComboBox<string>();
		private readonly UI.Button remove_, addAv_, moveUp_, moveDown_;
		private readonly UI.TextBox value_ = new UI.TextBox();

		public StringChooserStorableParameterUI()
		{
			remove_ = new UI.Button(UI.Utilities.RemoveSymbol, OnRemove);
			addAv_ = new UI.Button(UI.Utilities.AddSymbol, OnAddPredefined);
			moveUp_ = new UI.Button(UI.Utilities.UpArrow, OnMoveUp);
			moveDown_ = new UI.Button(UI.Utilities.DownArrow, OnMoveDown);

			var controls = new UI.Panel(new UI.HorizontalFlow(10));
			controls.Add(new UI.Button(UI.Utilities.AddSymbol, OnAdd));
			controls.Add(remove_);
			controls.Add(moveUp_);
			controls.Add(moveDown_);
			controls.Add(new UI.Spacer(50));
			controls.Add(new UI.Label("Predefined"));
			controls.Add(av_);
			controls.Add(addAv_);

			var value = new UI.Panel(new UI.VerticalFlow());
			value.Add(new UI.Label(S("Value")));
			value.Add(value_);

			var center = new UI.Panel(new UI.GridLayout(2, 20));
			center.Add(list_);
			center.Add(value);

			Layout = new UI.BorderLayout(10);
			Add(controls, UI.BorderLayout.Top);
			Add(center, UI.BorderLayout.Center);

			list_.SelectionIndexChanged += OnSelectionIndexChanged;
			value_.Changed += OnValueChanged;
		}

		public void Set(IStorableParameter t)
		{
			param_ = t as StringChooserStorableParameter;
			if (param_ == null)
				return;

			ignore_.Do(() =>
			{
				UpdateList();
				RebuildAvailableList();

				if (list_.Count > 0)
					UpdateSelection(0);
				else
					UpdateSelection(-1);
			});
		}

		public void Add(string value)
		{
			param_.AddString(value);

			ignore_.Do(() =>
			{
				list_.AddItem(value);
				list_.Select(list_.Count - 1);
				av_.RemoveItem(value);

				AvailableListChanged();
				UpdateSelection(list_.Count - 1);
			});
		}

		private void OnAdd()
		{
			if (param_ == null)
				return;

			Add(S("New string"));
		}

		private void OnRemove()
		{
			if (param_ == null)
				return;

			var sel = list_.SelectedIndex;
			if (sel < 0)
				return;

			param_.RemoveStringAt(sel);

			ignore_.Do(() =>
			{
				list_.RemoveItemAt(sel);
				RebuildAvailableList();
				UpdateSelection(list_.SelectedIndex);
			});
		}

		private void OnMoveUp()
		{
			if (param_ == null)
				return;

			var i = list_.SelectedIndex;
			if (i <= 0)
				return;

			var newList = param_.Strings;
			var item = newList[i];

			newList.RemoveAt(i);
			newList.Insert(i - 1, item);

			list_.SetItemAt(i - 1, newList[i - 1]);
			list_.SetItemAt(i, newList[i]);

			param_.Strings = newList;
			list_.Select(i - 1);
		}

		private void OnMoveDown()
		{
			if (param_ == null)
				return;

			var i = list_.SelectedIndex;
			if (i >= (list_.Count - 1))
				return;

			var newList = param_.Strings;
			var item = newList[i];

			newList.RemoveAt(i);
			newList.Insert(i + 1, item);

			list_.SetItemAt(i, newList[i]);
			list_.SetItemAt(i + 1, newList[i + 1]);

			param_.Strings = newList;
			list_.Select(i + 1);
		}

		private void OnAddPredefined()
		{
			if (param_ == null)
				return;

			var sel = av_.SelectedIndex;
			if (sel < 0)
				return;

			Add(av_.At(sel));
		}

		private void OnSelectionIndexChanged(int index)
		{
			if (ignore_)
				return;

			UpdateSelection(index);
		}

		private void OnValueChanged(string s)
		{
			if (ignore_ || param_ == null)
				return;

			var i = list_.SelectedIndex;
			if (i < 0 || i >= param_.Strings.Count)
				return;

			param_.SetStringAt(i, s);
			list_.SetItemAt(i, s);
		}

		private void UpdateList()
		{
			list_.SetItems(param_.Strings);
		}

		private void RebuildAvailableList()
		{
			av_.SetItems(param_.AvailableStrings.Except(param_.Strings).ToList());
			AvailableListChanged();
		}

		private void AvailableListChanged()
		{
			av_.Enabled = (av_.Count > 0);
			addAv_.Enabled = (av_.Count > 0);
		}

		private void UpdateSelection(int i)
		{
			ignore_.Do(() =>
			{
				bool validSelection = (i >= 0 && i < list_.Count);

				remove_.Enabled = validSelection;
				moveUp_.Enabled = validSelection && (i > 0);
				moveDown_.Enabled = validSelection && (i < (list_.Count - 1));

				if (validSelection)
				{
					value_.Text = list_.At(i);
					value_.Enabled = true;
				}
				else
				{
					value_.Text = "";
					value_.Enabled = false;
				}
			});
		}
	}


	class StorableModifierPanel : BasicModifierPanel
	{
		private StorableModifier modifier_ = null;

		private readonly IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly FilterList filter_ = new FilterList();
		private readonly AtomComboBox atom_ = new AtomComboBox();
		private readonly StorableList storable_ = new StorableList();
		private readonly ParameterList parameter_ = new ParameterList();
		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly MovementUI movement_ = new MovementUI();
		private readonly StorableParameterUIWidget ui_ = new StorableParameterUIWidget();

		public StorableModifierPanel()
		{
			var rangePanel = new UI.Panel(new UI.VerticalFlow(30));
			rangePanel.Add(movement_);

			tabs_.AddTab(S("Range"), rangePanel);
			tabs_.AddTab(S("Parameter"), ui_);

			var gl = new UI.GridLayout(2, 10);
			gl.HorizontalStretch = new List<bool>() { false, true };

			var panel = new UI.Panel(gl);
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

			Layout = new UI.BorderLayout(60);
			Add(panel, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);

			filter_.Changed += OnFilterChanged;
			atom_.AtomSelectionChanged += OnAtomChanged;
			storable_.StorableChanged += OnStorableChanged;
			parameter_.ParameterChanged += OnParameterChanged;
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

				movement_.Set(modifier_.Movement);
				ui_.Set(modifier_.Parameter);
			});
		}
	}
}
