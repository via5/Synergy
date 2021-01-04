using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	interface IStorableParameterUI
	{
		string ParameterType { get; }
		void AddToUI(IStorableParameter p);
		void RemoveFromUI();
	}


	abstract class BasicStorableParameterUI : IStorableParameterUI
	{
		protected WidgetList widgets_ = new WidgetList();

		public abstract string ParameterType { get; }

		public virtual void AddToUI(IStorableParameter p)
		{
			// no-op
		}

		public virtual void RemoveFromUI()
		{
			widgets_.RemoveFromUI();
		}
	}


	class FloatStorableParameterUI : BasicStorableParameterUI
	{
		public override string ParameterType
		{
			get { return FloatStorableParameter.FactoryTypeName; }
		}

		public override void AddToUI(IStorableParameter p)
		{
			base.AddToUI(p);

			widgets_.AddToUI(new Label(
				"This is a float parameter. Use the\n" +
				"movement sliders below to control it.",
				Widget.Right));
		}
	}


	class BoolStorableParameterUI : BasicStorableParameterUI
	{
		public override string ParameterType
		{
			get { return BoolStorableParameter.FactoryTypeName; }
		}

		public override void AddToUI(IStorableParameter p)
		{
			base.AddToUI(p);

			widgets_.AddToUI(new Label(
				"This is a bool parameter. The\n" +
				"movement sliders below will set\n" +
				"it to false <= 0.5, true > 0.5 \n" +
				"(value is normalized).",
				Widget.Right));
		}
	}


	class ColorStorableParameterUI : BasicStorableParameterUI
	{
		private ColorStorableParameter param_ = null;

		private readonly ColorPicker color1_;
		private readonly ColorPicker color2_;

		public ColorStorableParameterUI()
		{
			color1_ = new ColorPicker(
				"Color 1", Color.black, Color1Changed, Widget.Right);

			color2_ = new ColorPicker(
				"Color 2", Color.black, Color2Changed, Widget.Right);
		}

		public override string ParameterType
		{
			get { return ColorStorableParameter.FactoryTypeName; }
		}

		public override void AddToUI(IStorableParameter p)
		{
			base.AddToUI(p);

			param_ = p as ColorStorableParameter;
			if (param_ == null)
				return;

			color1_.Value = param_.Color1;
			color2_.Value = param_.Color2;

			widgets_.AddToUI(new Label(
				"This is a color parameter. The\n" +
				"movement sliders below will set\n" +
				"it to color 1 <= 0.5, color 2 > 0.5 \n" +
				"(value is normalized).",
				Widget.Right));

			widgets_.AddToUI(color1_);
			widgets_.AddToUI(color2_);
		}

		private void Color1Changed(Color c)
		{
			if (param_ != null)
				param_.Color1 = c;
		}

		private void Color2Changed(Color c)
		{
			if (param_ != null)
				param_.Color2 = c;
		}
	}


	class StringStorableParameterUI : BasicStorableParameterUI
	{
		public override string ParameterType
		{
			get { return StringStorableParameter.FactoryTypeName; }
		}

		private readonly StringList strings_;
		private readonly Textbox current_;
		private readonly Button save_, add_;
		private readonly ConfirmableButton delete_;

		private StringStorableParameter param_ = null;
		private int sel_ = -1;


		public StringStorableParameterUI()
		{
			strings_ = new StringList("Strings", null, Widget.Right);
			current_ = new Textbox("Selected", "", CurrentChanged, Widget.Right);
			save_ = new Button("Save changes", SaveChanges, Widget.Right);
			add_ = new Button("Add new", AddNew, Widget.Right);
			delete_ = new ConfirmableButton(
				"Delete selected", DeleteSelected, Widget.Right);

			strings_.SelectionIndexChanged += StringSelected;
		}

		public override void AddToUI(IStorableParameter p)
		{
			base.AddToUI(p);

			param_ = p as StringStorableParameter;
			if (param_ == null)
				return;

			current_.Placeholder = "No selection";

			widgets_.AddToUI(new Label(
				"This is a string parameter. Each string\n" +
				"in the list will be set for an equal part\n" +
				"of the movement range (value is\n" +
				"normalized).",
				Widget.Right));

			widgets_.AddToUI(strings_);
			widgets_.AddToUI(current_);
			widgets_.AddToUI(save_);
			widgets_.AddToUI(add_);
			widgets_.AddToUI(delete_);

			save_.Enabled = false;
		}

		public void SaveChanges()
		{
			if (param_ == null)
				return;

			var list = strings_.Choices;
			if (sel_ < 0 || sel_ >= list.Count)
				return;

			list[sel_] = current_.Value;
			strings_.Choices = list;
			strings_.Value = current_.Value;
			param_.Strings = list;
			save_.Enabled = false;
		}

		public void AddNew()
		{
			var list = strings_.Choices;

			string s = "New string";
			for (int i = 1; i < 100; ++i)
			{
				if (list.IndexOf(s) == -1)
					break;

				s = "New string (" + i.ToString() + ")";
			}

			list.Add(s);
			strings_.Choices = list;
			strings_.Value = s;
			current_.Value = s;
			sel_ = list.Count - 1;
			param_.Strings = list;
		}

		public void DeleteSelected()
		{
			if (param_ == null)
				return;

			var list = strings_.Choices;
			if (sel_ < 0 || sel_ >= list.Count)
				return;

			list.RemoveAt(sel_);

			strings_.Choices = list;
			param_.Strings = list;

			if (list.Count == 0)
			{
				current_.Value = "";
				sel_ = -1;
				strings_.Value = "";
			}
			else
			{
				if (sel_ >= list.Count)
					sel_ = list.Count - 1;

				current_.Value = list[sel_];
				strings_.Value = current_.Value;
			}
		}

		private void CurrentChanged(string s)
		{
			save_.Enabled = true;
		}

		private void StringSelected(int i)
		{
			sel_ = i;
			current_.Value = strings_.Choices[i];
			save_.Enabled = false;
		}
	}


	class UrlStorableParameterUI : StringStorableParameterUI
	{
		public override string ParameterType
		{
			get { return UrlStorableParameter.FactoryTypeName; }
		}
	}


	class ActionStorableParameterUI : BasicStorableParameterUI
	{
		public override string ParameterType
		{
			get { return ActionStorableParameter.FactoryTypeName; }
		}

		private ActionStorableParameter param_ = null;
		private readonly FloatSlider triggerMag_;
		private readonly StringList triggerType_;

		public ActionStorableParameterUI()
		{
			triggerMag_ = new FloatSlider(
				"Trigger at", TriggerMagChanged, Widget.Right);

			triggerType_ = new StringList(
				"Trigger type", TriggerTypeChanged, Widget.Right);


			triggerType_.DisplayChoices =
				ActionStorableParameter.TriggerTypeNames();


			var list = new List<string>();
			foreach (var i in ActionStorableParameter.TriggerTypes())
				list.Add(i.ToString());

			triggerType_.Choices = list;
		}

		public override void AddToUI(IStorableParameter p)
		{
			base.AddToUI(p);

			param_ = p as ActionStorableParameter;
			if (param_ == null)
				return;

			triggerMag_.Value = param_.TriggerMagnitude;
			triggerType_.Value = param_.TriggerType.ToString();

			widgets_.AddToUI(triggerMag_);
			widgets_.AddToUI(triggerType_);
		}

		private void TriggerMagChanged(float f)
		{
			if (param_ != null)
				param_.TriggerMagnitude = f;
		}

		private void TriggerTypeChanged(string s)
		{
			int i = 0;
			if (!int.TryParse(s, out i))
			{
				Synergy.LogError($"can't parse trigger type '{s}'");
				return;
			}

			if (param_ != null)
				param_.TriggerType = i;
		}
	}


	class StorableWidgets
	{
		public const string PluginSuffix = "_plugins";

		private StorableParameterHolder holder_ = null;
		private readonly StringList storables_;
		private readonly StringList parameters_;
		private bool storablesStale_ = true;
		private bool parametersStale_ = true;
		private string type_ = "";

		public delegate void Handler();
		public event Handler OnParameterChanged;

		public StorableWidgets(int flags)
		{
			storables_ = new StringList(
				"Storable", StorableChanged, flags | Widget.Filterable);

			parameters_ = new StringList(
				"Parameter", ParameterChanged, flags | Widget.Filterable);

			storables_.OnOpen += UpdateStorables;
			parameters_.OnOpen += UpdateParameters;
		}

		public string Type
		{
			get
			{
				return type_;
			}

			set
			{
				type_ = value;
				storablesStale_ = true;
				parametersStale_ = true;
			}
		}

		public void AtomChanged()
		{
			storables_.Value = holder_?.Storable?.name ?? "";
			parameters_.Value = holder_?.Parameter?.Name ?? "";
			storablesStale_ = true;
			parametersStale_ = true;
		}

		public void DeferredInit(StorableParameterHolder h)
		{
			holder_ = h;
			storables_.Value = holder_?.Storable?.storeId ?? "";
			parameters_.Value = holder_?.Parameter?.Name ?? "";
		}

		public void AddToUI(StorableParameterHolder h)
		{
			if (holder_ != h)
			{
				storablesStale_ = true;
				parametersStale_ = true;
			}

			holder_ = h;

			storables_.Value = holder_?.Storable?.storeId ?? "";
			parameters_.Value = holder_?.Parameter?.Name ?? "";

			storables_.AddToUI();
			parameters_.AddToUI();
		}

		public void RemoveFromUI()
		{
			storables_.RemoveFromUI();
			parameters_.RemoveFromUI();
		}

		private void StorableChanged(string s)
		{
			if (holder_ == null)
				return;

			holder_.SetStorable(s);
			parameters_.Value = holder_.Parameter?.Name ?? "";
			parametersStale_ = true;
			OnParameterChanged?.Invoke();
		}

		private void ParameterChanged(string s)
		{
			holder_.SetParameter(s);
			OnParameterChanged?.Invoke();
		}

		private void UpdateStorables()
		{
			if (!storablesStale_)
				return;

			List<string> list = null;

			var a = holder_?.Atom;

			if (a != null)
			{
				bool pluginsOnly = false;
				string type = type_;

				if (type.EndsWith(PluginSuffix))
				{
					type = type.Substring(0, type.Length - PluginSuffix.Length);
					pluginsOnly = true;
				}

				list = new List<string>();

				if (type == "")
				{
					foreach (var id in a.GetStorableIDs())
					{
						if (!pluginsOnly || Utilities.StorableIsPlugin(id))
							list.Add(id);
					}
				}
				else
				{
					var p = new StorableParameterFactory().Create(type);
					if (p == null)
						Synergy.LogError($"unknown type {type}");
					else
						list = new List<string>(p.GetStorableNames(a, pluginsOnly));
				}
			}

			if (list == null)
				list = new List<string>();

			Utilities.NatSort(list);
			storables_.Choices = list;

			storablesStale_ = false;
		}

		private void UpdateParameters()
		{
			if (!parametersStale_)
				return;

			List<string> list = null;

			var s = holder_?.Storable;
			if (s != null)
			{
				string type = type_;

				if (type.EndsWith(PluginSuffix))
					type = type.Substring(0, type.Length - PluginSuffix.Length);

				if (type == "")
				{
					list = s.GetAllParamAndActionNames();
				}
				else
				{
					var p = new StorableParameterFactory().Create(type);
					if (p == null)
						Synergy.LogError($"unknown type {type_}");
					else
						list = new List<string>(p.GetParameterNames(s));
				}
			}

			if (list == null)
				list = new List<string>();

			Utilities.NatSort(list);
			parameters_.Choices = list;

			parametersStale_ = false;
		}
	}


	class StorableTypesList : StringList
	{
		public delegate void TypeHandler(string type);
		public event TypeHandler OnTypeChanged;

		public StorableTypesList(int flags)
			: this(new List<string>(), flags)
		{
		}

		public StorableTypesList(List<string> types, int flags)
			: base("Show only", null, flags)
		{
			SelectionChanged += TypeChanged;
			UpdateList(types);
		}

		private void UpdateList(List<string> filter)
		{
			var displayNames = new StorableParameterFactory().GetAllDisplayNames();
			var typeNames = new StorableParameterFactory().GetAllFactoryTypeNames();

			var displayChoices = new List<string>();
			var choices = new List<string>();

			if (filter.Count == 0 || filter.Contains("all"))
			{
				displayChoices.Add("All (plugins only)");
				choices.Add(StorableWidgets.PluginSuffix);
			}


			// plugins only
			for (int i = 0; i < typeNames.Count; ++i)
			{
				if (filter.Count == 0 || filter.Contains(typeNames[i]))
				{
					displayChoices.Add(displayNames[i] + " (plugins only)");
					choices.Add(typeNames[i] + StorableWidgets.PluginSuffix);
				}
			}


			// any
			for (int i = 0; i < typeNames.Count; ++i)
			{
				if (filter.Count == 0 || filter.Contains(typeNames[i]))
				{
					displayChoices.Add(displayNames[i]);
					choices.Add(typeNames[i]);
				}
			}


			if (filter.Count == 0 || filter.Contains("all"))
			{
				displayChoices.Add("All (very slow)");
				choices.Add("");
			}


			DisplayChoices = displayChoices;
			Choices = choices;

			// todo
			Value = "action" + StorableWidgets.PluginSuffix;
		}

		private void TypeChanged(string s)
		{
			OnTypeChanged?.Invoke(s);
		}
	}


	class StorableModifierUI : AtomWithMovementUI
	{
		public override string ModifierType
		{
			get { return StorableModifier.FactoryTypeName; }
		}

		private StorableModifier modifier_ = null;
		private readonly StorableTypesList types_;
		private readonly StorableWidgets storableWidgets_;
		private IStorableParameterUI parameterUI_ = null;


		public StorableModifierUI(MainUI ui)
			: base(ui)
		{
			types_ = new StorableTypesList(Widget.Right);
			types_.OnTypeChanged += TypeChanged;

			storableWidgets_ = new StorableWidgets(Widget.Right);
			storableWidgets_.OnParameterChanged += UpdateParameter;
			storableWidgets_.Type = types_.Value;
		}

		public override void DeferredInit()
		{
			storableWidgets_.DeferredInit(modifier_?.Holder);
		}

		public override void AddToTopUI(IModifier m)
		{
			modifier_ = m as StorableModifier;
			if (modifier_ == null)
				return;

			var p = modifier_?.Parameter;
			if (p == null)
			{
				parameterUI_ = null;
			}
			else
			{
				if (parameterUI_ == null ||
					parameterUI_.ParameterType != p.GetFactoryTypeName())
				{
					parameterUI_ = CreateParameterUI(p);
				}
			}


			AddAtomWidgets(m);

			widgets_.AddToUI(types_);
			storableWidgets_.AddToUI(modifier_.Holder);

			if (parameterUI_ != null)
			{
				widgets_.AddToUI(new SmallSpacer(Widget.Right));
				parameterUI_.AddToUI(p);
			}

			AddAtomWithMovementWidgets(m);

			base.AddToTopUI(m);
		}

		public override void RemoveFromUI()
		{
			base.RemoveFromUI();

			storableWidgets_.RemoveFromUI();

			if (parameterUI_ != null)
				parameterUI_.RemoveFromUI();
		}

		private IStorableParameterUI CreateParameterUI(IStorableParameter p)
		{
			if (p is FloatStorableParameter)
				return new FloatStorableParameterUI();
			else if (p is BoolStorableParameter)
				return new BoolStorableParameterUI();
			else if (p is ColorStorableParameter)
				return new ColorStorableParameterUI();
			else if (p is UrlStorableParameter)
				return new UrlStorableParameterUI();
			else if (p is StringStorableParameter)
				return new StringStorableParameterUI();
			else if (p is ActionStorableParameter)
				return new ActionStorableParameterUI();
			else
				return null;
		}


		private void TypeChanged(string s)
		{
			storableWidgets_.Type = s;
		}

		protected override void AtomChanged(Atom atom)
		{
			base.AtomChanged(atom);

			if (modifier_ == null)
				return;

			storableWidgets_.AtomChanged();
			UpdateParameter();
		}

		private void UpdateParameter()
		{
			PreferredRangeChanged();
			ui_.NeedsReset("storable parameter changed");
		}
	}
}
