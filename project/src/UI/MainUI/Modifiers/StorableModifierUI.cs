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


	class StorableModifierUI : AtomWithMovementUI
	{
		public override string ModifierType
		{
			get { return StorableModifier.FactoryTypeName; }
		}

		private StorableModifier modifier_ = null;
		private readonly StringList types_;
		private readonly StringList storables_;
		private readonly StringList parameters_;
		private IStorableParameterUI parameterUI_ = null;
		private bool storablesStale_ = true;
		private bool parametersStale_ = true;


		public StorableModifierUI(MainUI ui)
			: base(ui)
		{
			types_ = new StringList("Show only", TypeChanged, Widget.Right);

			storables_ = new StringList(
				"Storable", StorableChanged, Widget.Right);

			parameters_ = new StringList(
				"Parameter", ParameterChanged, Widget.Right);

			storables_.OnOpen += UpdateStorables;
			parameters_.OnOpen += UpdateParameters;


			var list = new StorableParameterFactory().GetAllDisplayNames();
			list.Insert(0, "All (very slow)");
			types_.DisplayChoices = list;

			list = new StorableParameterFactory().GetAllFactoryTypeNames();
			list.Insert(0, "");
			types_.Choices = list;

			types_.Value = "float";
		}

		public override void AddToTopUI(IModifier m)
		{
			if (modifier_ != m)
			{
				storablesStale_ = true;
				parametersStale_ = true;
			}

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


			storables_.Value = modifier_?.Storable?.storeId ?? "";
			parameters_.Value = modifier_?.Parameter?.Name ?? "";

			AddAtomWidgets(m);

			widgets_.AddToUI(types_);
			widgets_.AddToUI(storables_);
			widgets_.AddToUI(parameters_);

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

		private void UpdateStorables()
		{
			if (!storablesStale_)
				return;

			List<string> list = null;

			var a = modifier_?.Atom;

			if (a != null)
			{
				if (types_.Value == "")
				{
					list = a.GetStorableIDs();
				}
				else
				{
					var p = new StorableParameterFactory().Create(types_.Value);
					if (p == null)
						Synergy.LogError($"unknown type {types_.Value}");
					else
						list = new List<string>(p.GetStorableNames(a));
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

			var s = modifier_?.Storable;
			if (s != null)
			{
				if (types_.Value == "")
				{
					list = s.GetAllParamAndActionNames();
				}
				else
				{
					var p = new StorableParameterFactory().Create(types_.Value);
					if (p == null)
						Synergy.LogError($"unknown type {types_.Value}");
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

		private void TypeChanged(string s)
		{
			storablesStale_ = true;
			parametersStale_ = true;
		}

		private void StorableChanged(string s)
		{
			if (modifier_ == null)
				return;

			modifier_.SetStorable(s);
			parameters_.Value = modifier_.Parameter?.Name ?? "";
			parametersStale_ = true;
			UpdateParameter();
		}

		private void ParameterChanged(string s)
		{
			modifier_.SetParameter(s);
			UpdateParameter();
		}

		protected override void AtomChanged(Atom atom)
		{
			base.AtomChanged(atom);

			if (modifier_ == null)
				return;

			storables_.Value = modifier_.Storable?.name ?? "";
			parameters_.Value = modifier_.Parameter?.Name ?? "";
			storablesStale_ = true;
			parametersStale_ = true;
			UpdateParameter();
		}

		private void UpdateParameter()
		{
			PreferredRangeChanged();
			ui_.NeedsReset("storable parameter changed");
		}
	}
}
