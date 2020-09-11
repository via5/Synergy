using System;
using System.Collections.Generic;
using System.Text;

namespace Synergy
{
	interface IStorableParameterMonitorUI
	{
		string ParameterType { get; }
		void AddToUI(IStorableParameter p);
		void RemoveFromUI();
		void Update();
	}


	abstract class BasicStorableParameterMonitorUI : IStorableParameterMonitorUI
	{
		protected WidgetList widgets_ = new WidgetList();

		private readonly Label paramName_;
		private BasicStorableParameter parameter_ = null;


		public BasicStorableParameterMonitorUI()
		{
			paramName_ = new Label("", Widget.Right);
		}

		public abstract string ParameterType { get; }

		public virtual void AddToUI(IStorableParameter p)
		{
			parameter_ = p as BasicStorableParameter;
			widgets_.AddToUI(paramName_);
		}

		public virtual void RemoveFromUI()
		{
			widgets_.RemoveFromUI();
		}

		public virtual void Update()
		{
			if (parameter_ == null)
				paramName_.Text = "Parameter: none";
			else
				paramName_.Text = "Parameter: " + parameter_.Name;
		}
	}


	class FloatStorableParameterMonitorUI : BasicStorableParameterMonitorUI
	{
		public override string ParameterType
		{
			get { return FloatStorableParameter.FactoryTypeName; }
		}

		private FloatStorableParameter parameter_ = null;
		private readonly FloatSlider paramValue_;

		public FloatStorableParameterMonitorUI()
		{
			paramValue_ = new FloatSlider(
				"Parameter value", null, Widget.Right | Widget.Disabled);
		}

		public override void AddToUI(IStorableParameter p)
		{
			base.AddToUI(p);

			parameter_ = p as FloatStorableParameter;
			if (parameter_ == null)
				return;

			widgets_.AddToUI(paramValue_);
		}

		public override void Update()
		{
			base.Update();

			var p = parameter_?.Parameter;

			if (p == null)
				paramValue_.Value = 0;
			else
				paramValue_.Set(p.min, p.max, p.val);
		}
	}


	class BoolStorableParameterMonitorUI : BasicStorableParameterMonitorUI
	{
		public override string ParameterType
		{
			get { return BoolStorableParameter.FactoryTypeName; }
		}

		private BoolStorableParameter parameter_ = null;
		private readonly Checkbox paramValue_;

		public BoolStorableParameterMonitorUI()
		{
			paramValue_ = new Checkbox(
				"Parameter value", null, Widget.Right | Widget.Disabled);
		}

		public override void AddToUI(IStorableParameter p)
		{
			base.AddToUI(p);

			parameter_ = p as BoolStorableParameter;
			if (parameter_ == null)
				return;

			widgets_.AddToUI(paramValue_);
		}

		public override void Update()
		{
			var p = parameter_?.Parameter;

			if (p == null)
				paramValue_.Value = false;
			else
				paramValue_.Value = p.val;
		}
	}


	class StringStorableParameterMonitorUI : BasicStorableParameterMonitorUI
	{
		public override string ParameterType
		{
			get { return StringStorableParameter.FactoryTypeName; }
		}

		private StringStorableParameter parameter_;
		private readonly Label currentString_;

		public StringStorableParameterMonitorUI()
		{
			currentString_ = new Label("", Widget.Right);
		}

		public override void AddToUI(IStorableParameter p)
		{
			base.AddToUI(p);

			parameter_ = p as StringStorableParameter;
			if (parameter_ == null)
				return;

			widgets_.AddToUI(currentString_);
		}

		public override void Update()
		{
			base.Update();
			currentString_.Text = "Current: " + (parameter_?.Current ?? " (none)");
		}
	}


	class ActionStorableParameterMonitorUI : BasicStorableParameterMonitorUI
	{
		public override string ParameterType
		{
			get { return ActionStorableParameter.FactoryTypeName; }
		}

		private ActionStorableParameter parameter_;
		private readonly FloatSlider triggerMag_;
		private readonly Label triggerType_;
		private readonly Label actionCurrentState_, actionLastState_;

		public ActionStorableParameterMonitorUI()
		{
			triggerMag_ = new FloatSlider(
				"Trigger at", null, Widget.Right | Widget.Disabled);

			triggerType_ = new Label("", Widget.Right);
			actionCurrentState_ = new Label("", Widget.Right);
			actionLastState_ = new Label("", Widget.Right);
		}

		public override void AddToUI(IStorableParameter p)
		{
			base.AddToUI(p);

			parameter_ = p as ActionStorableParameter;
			if (parameter_ == null)
				return;

			widgets_.AddToUI(triggerMag_);
			widgets_.AddToUI(triggerType_);
			widgets_.AddToUI(actionCurrentState_);
			widgets_.AddToUI(actionLastState_);
		}

		public override void Update()
		{
			if (parameter_ == null)
				return;

			triggerMag_.Value = parameter_.TriggerMagnitude;

			triggerType_.Text =
				"Trigger type: " +
				ActionStorableParameter.TriggerTypeToString(parameter_.TriggerType);

			actionCurrentState_.Text =
				"State: " + StateToString(parameter_.CurrentState);

			actionLastState_.Text =
				"Last: " + StateToString(parameter_.LastState);
		}

		private string StateToString(int i)
		{
			switch (i)
			{
				case ActionStorableParameter.StateGoingUp:
					return "going up";

				case ActionStorableParameter.StateGoingDown:
					return "going down";

				case ActionStorableParameter.StateGoingUpTriggered:
					return "mag reached, triggered";

				case ActionStorableParameter.StateGoingUpIgnored:
					return "mag reached (ignored)";

				case ActionStorableParameter.StateGoingDownTriggered:
					return "mag left, triggered";

				case ActionStorableParameter.StateGoingDownIgnored:
					return "mag left(ignored)";

				case ActionStorableParameter.StateNone:
				default:
					return "none";
			}
		}
	}


	class StorableModifierMonitor : ModifierWithMovementMonitor
	{
		public override string ModifierType
		{
			get { return StorableModifier.FactoryTypeName; }
		}


		private StorableModifier modifier_ = null;
		private IStorableParameterMonitorUI parameterUI_ = null;

		public StorableModifierMonitor()
		{
		}

		public override void AddToUI(IModifier m)
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
					parameterUI_ = CreateParameterMonitorUI(p);
				}
			}

			if (parameterUI_ != null)
			{
				parameterUI_.AddToUI(p);
				widgets_.AddToUI(new SmallSpacer(Widget.Right));
			}

			base.AddToUI(m);
		}

		public override void RemoveFromUI()
		{
			base.RemoveFromUI();

			if (parameterUI_ != null)
				parameterUI_.RemoveFromUI();
		}

		private IStorableParameterMonitorUI CreateParameterMonitorUI(
			IStorableParameter p)
		{
			if (p is FloatStorableParameter)
				return new FloatStorableParameterMonitorUI();
			else if (p is BoolStorableParameter)
				return new BoolStorableParameterMonitorUI();
			else if (p is ColorStorableParameter)
				return null;// new ColorStorableParameterUI();
			else if (p is UrlStorableParameter)
				return new StringStorableParameterMonitorUI();
			else if (p is StringStorableParameter)
				return new StringStorableParameterMonitorUI();
			else if (p is ActionStorableParameter)
				return new ActionStorableParameterMonitorUI();
			else
				return null;
		}

		public override void Update()
		{
			base.Update();

			if (parameterUI_ != null)
				parameterUI_.Update();
		}
	}
}
