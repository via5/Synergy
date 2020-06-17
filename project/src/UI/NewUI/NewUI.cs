using Battlehub.RTHandles;
using Leap.Unity;
using Synergy.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy.NewUI
{
	public class Strings
	{
		public static string Get(string s)
		{
			return s;
		}
	}


	class ToolButton : UI.Button
	{
		public ToolButton(string text = "", UI.Button.Callback clicked = null)
			: base(text, clicked)
		{
			MinimumSize = new UI.Size(50, DontCare);
		}
	}


	class FactoryComboBoxItem<ObjectType>
		where ObjectType : IFactoryObject
	{
		private readonly IFactoryObjectCreator creator_;

		public FactoryComboBoxItem(IFactoryObjectCreator creator)
		{
			creator_ = creator;
		}

		public ObjectType CreateFactoryObject()
		{
			return (ObjectType)creator_.Create();
		}

		public string FactoryTypeName
		{
			get { return creator_.FactoryTypeName; }
		}

		public override string ToString()
		{
			return creator_.DisplayName;
		}
	}


	class FactoryComboBox<FactoryType, ObjectType>
		: UI.TypedComboBox<FactoryComboBoxItem<ObjectType>>
			where FactoryType : IGenericFactory, new()
			where ObjectType : class, IFactoryObject
	{
		public delegate void FactoryTypeCallback(ObjectType o);
		public event FactoryTypeCallback FactoryTypeChanged;

		public FactoryComboBox(FactoryTypeCallback factoryTypeChanged = null)
		{
			var f = new FactoryType();

			foreach (var creator in f.GetAllCreators())
				AddItem(new FactoryComboBoxItem<ObjectType>(creator));

			SelectionChanged += OnSelectionChanged;

			if (factoryTypeChanged != null)
				FactoryTypeChanged += factoryTypeChanged;
		}

		private void OnSelectionChanged(FactoryComboBoxItem<ObjectType> item)
		{
			if (item == null)
				FactoryTypeChanged?.Invoke(null);
			else
				FactoryTypeChanged?.Invoke(item.CreateFactoryObject());
		}

		public void Select(ObjectType d)
		{
			Select(IndexOf(d));
		}

		public int IndexOf(ObjectType d)
		{
			if (d == null)
				return -1;

			var items = Items;

			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].FactoryTypeName == d.GetFactoryTypeName())
					return i;
			}

			return -1;
		}
	}


	class StepControls : UI.Panel
	{
		public delegate void StepCallback(Step s);
		public event StepCallback SelectionChanged;

		private readonly UI.TypedComboBox<Step> steps_;
		private readonly UI.Button add_, clone_, clone0_, remove_, up_, down_;

		public StepControls()
		{
			Layout = new UI.HorizontalFlow(20);

			steps_ = new UI.TypedComboBox<Step>(OnSelectionChanged);
			add_ = new ToolButton("+", AddStep);
			clone_ = new ToolButton(S("Clone"), CloneStep);
			clone0_ = new ToolButton(S("Clone 0"), CloneStepZero);
			remove_ = new ToolButton("\x2013", RemoveStep);  // en dash
			up_ = new ToolButton("\x25b2", MoveStepUp);      // up arrow
			down_ = new ToolButton("\x25bc", MoveStepDown);  // down arrow

			Add(new UI.Label(S("Step:")));
			Add(steps_);
			Add(add_);
			Add(clone_);
			Add(clone0_);
			Add(remove_);
			Add(up_);
			Add(down_);

			Synergy.Instance.Manager.StepsChanged += UpdateSteps;
			UpdateSteps();
		}

		public Step Selected
		{
			get
			{
				return steps_.Selected;
			}
		}

		public void AddStep()
		{
			var s = Synergy.Instance.Manager.AddStep();
			steps_.AddItem(s, true);
		}

		public void CloneStep()
		{
			var s = steps_.Selected;
			if (s != null)
			{
				var ns = Synergy.Instance.Manager.AddStep(s.Clone());
				steps_.AddItem(s, true);
			}
		}

		public void CloneStepZero()
		{
			var s = steps_.Selected;
			if (s != null)
			{
				var ns = Synergy.Instance.Manager.AddStep(
					s.Clone(global::Synergy.Utilities.CloneZero));

				steps_.AddItem(s, true);
			}
		}

		public void RemoveStep()
		{
			var s = steps_.Selected;
			if (s != null)
			{
				Synergy.Instance.Manager.DeleteStep(s);
				steps_.RemoveItem(s);
			}
		}

		public void MoveStepUp()
		{
		}

		public void MoveStepDown()
		{
		}

		private void OnSelectionChanged(Step s)
		{
			SelectionChanged?.Invoke(s);
		}

		private void UpdateSteps()
		{
			steps_.SetItems(
				new List<Step>(Synergy.Instance.Manager.Steps),
				steps_.Selected);
		}
	}


	class TimeWidgets : UI.Panel
	{
		public delegate void ValueCallback(float f);
		public event ValueCallback Changed;

		private readonly UI.TextBox text_;
		private float reset_ = 0;
		private float current_ = 0;

		public TimeWidgets(ValueCallback changed = null)
		{
			text_ = new TextBox();
			text_.Validate += OnValidate;
			text_.Changed += OnChanged;

			Layout = new UI.HorizontalFlow(5);

			Add(text_);
			Add(new UI.Button("-1", () => AddValue(-1)));
			Add(new UI.Button("-.1", () => AddValue(-0.1f)));
			Add(new UI.Button("-.01", () => AddValue(-0.01f)));
			Add(new UI.Button("0", () => SetValue(0)));
			Add(new UI.Button("+.01", () => AddValue(+0.01f)));
			Add(new UI.Button("+.1", () => AddValue(+0.1f)));
			Add(new UI.Button("+1", () => AddValue(+1)));
			Add(new UI.Button(S("Reset"), () => Reset()));

			if (changed != null)
				Changed += changed;
		}

		public void Set(float f)
		{
			reset_ = f;
			current_ = f;

			text_.Text = f.ToString();
		}

		public void AddValue(float d)
		{
			SetValue(current_ + d);
		}

		public void Reset()
		{
			SetValue(reset_);
		}

		private void OnValidate(UI.TextBox.Validation v)
		{
			float r;
			v.valid = float.TryParse(v.text, out r);
		}

		private void OnChanged(string s)
		{
			float r;
			if (float.TryParse(s, out r))
				SetValue(r);
		}

		private void SetValue(float v)
		{
			current_ = v;
			text_.Text = v.ToString();
			Changed?.Invoke(v);
		}
	}


	abstract class DurationWidgets : UI.Panel
	{
		public abstract bool Set(IDuration d);

		public static DurationWidgets Create(IDuration d)
		{
			if (d is RandomDuration)
				return new RandomDurationWidgets(d as RandomDuration);
			else if (d is RampDuration)
				return new RampDurationWidgets(d as RampDuration);
			else
				return null;
		}
	}


	class RandomDurationWidgets : DurationWidgets
	{
		private readonly TimeWidgets time_, range_, interval_;
		private readonly ComboBox cutoff_;
		private RandomDuration duration_ = null;

		public RandomDurationWidgets(RandomDuration d = null)
		{
			time_ = new TimeWidgets(OnInitialChanged);
			range_ = new TimeWidgets(OnRangeChanged);
			interval_ = new TimeWidgets(OnIntervalChanged);
			cutoff_ = new ComboBox(
				RandomizableTime.GetCutoffNames(), OnCutoffChanged);

			var gl = new UI.GridLayout(2);
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;

			Layout = gl;

			Add(new UI.Label(S("Time")));
			Add(time_);

			Add(new UI.Label(S("Random range")));
			Add(range_);

			Add(new UI.Label(S("Random interval")));
			Add(interval_);

			Add(new UI.Label(S("Cut-off")));
			Add(cutoff_);
			Set(d);
		}

		public override bool Set(IDuration d)
		{
			duration_ = d as RandomDuration;
			if (duration_ == null)
				return false;

			time_.Set(duration_.Time.Initial);
			range_.Set(duration_.Time.Range);
			interval_.Set(duration_.Time.Interval);
			cutoff_.Select(RandomizableTime.CutoffToString(duration_.Time.Cutoff));

			return true;
		}

		private void OnInitialChanged(float f)
		{
			duration_.Time.Initial = f;
		}

		private void OnRangeChanged(float f)
		{
			duration_.Time.Range = f;
		}

		private void OnIntervalChanged(float f)
		{
			duration_.Time.Interval = f;
		}

		private void OnCutoffChanged(string s)
		{
			var c = RandomizableTime.CutoffFromString(s);
			if (c == -1)
			{
				Synergy.LogError("bad cutoff '" + s + "'");
				return;
			}

			duration_.Time.Cutoff = c;
		}
	}


	class RampDurationWidgets : DurationWidgets
	{
		private readonly TimeWidgets over_, min_, max_, hold_;
		private readonly FactoryComboBox<EasingFactory, IEasing> easing_;
		private readonly UI.CheckBox rampUp_, rampDown_;

		private RampDuration duration_ = null;

		public RampDurationWidgets(RampDuration d = null)
		{
			over_ = new TimeWidgets(OnOverChanged);
			min_ = new TimeWidgets(OnMinimumChanged);
			max_ = new TimeWidgets(OnMaximumChanged);
			hold_ = new TimeWidgets(OnHoldChanged);
			easing_ = new FactoryComboBox<EasingFactory, IEasing>(
				OnEasingChanged);
			rampUp_ = new UI.CheckBox(S("Ramp up"), OnRampUpChanged);
			rampDown_ = new UI.CheckBox(S("Ramp down"), OnRampDownChanged);

			var gl = new UI.GridLayout(2);
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;
			Layout = gl;

			Add(new UI.Label(S("Time")));
			Add(over_);

			Add(new UI.Label(S("Minimum duration")));
			Add(min_);

			Add(new UI.Label(S("Maximum duration")));
			Add(max_);

			Add(new UI.Label(S("Hold maximum")));
			Add(hold_);

			Add(new UI.Label(S("Easing")));
			Add(easing_);

			var ramps = new UI.Panel();
			ramps.Layout = new UI.HorizontalFlow();
			ramps.Add(rampUp_);
			ramps.Add(rampDown_);

			Add(new UI.Panel());
			Add(ramps);

			Set(d);
		}

		public override bool Set(IDuration d)
		{
			duration_ = (d as RampDuration);
			if (duration_ == null)
				return false;

			over_.Set(duration_.Over);
			min_.Set(duration_.Minimum);
			max_.Set(duration_.Maximum);
			hold_.Set(duration_.Hold);
			easing_.Select(duration_.Easing);
			rampUp_.Checked = duration_.RampUp;
			rampDown_.Checked = duration_.RampDown;

			return true;
		}

		private void OnOverChanged(float f)
		{
			duration_.Over = f;
		}

		private void OnMinimumChanged(float f)
		{
			duration_.Minimum = f;
		}

		private void OnMaximumChanged(float f)
		{
			duration_.Maximum = f;
		}

		private void OnHoldChanged(float f)
		{
			duration_.Hold = f;
		}

		private void OnEasingChanged(IEasing e)
		{
			duration_.Easing = e;
		}

		private void OnRampUpChanged(bool b)
		{
			duration_.RampUp = b;
		}

		private void OnRampDownChanged(bool b)
		{
			duration_.RampDown = b;
		}
	}


	class DurationPanel : UI.Panel
	{
		public delegate void Callback(IDuration d);
		public event Callback Changed;

		private readonly FactoryComboBox<DurationFactory, IDuration> type_;
		private DurationWidgets widgets_ = null;
		private IDuration duration_ = null;

		public DurationPanel()
		{
			type_ = new FactoryComboBox<DurationFactory, IDuration>(
				OnTypeChanged);

			Layout = new UI.VerticalFlow(50);

			var p = new UI.Panel(new UI.HorizontalFlow(20));
			p.Add(new UI.Label(S("Duration type")));
			p.Add(type_);

			Add(p);
		}

		public void Set(IDuration d)
		{
			duration_ = d;

			if (widgets_ == null || !widgets_.Set(d))
				SetWidgets(DurationWidgets.Create(d));

			type_.Select(d);
		}

		private void SetWidgets(DurationWidgets p)
		{
			if (widgets_ != null)
				widgets_.Remove();

			widgets_ = p;

			if (widgets_ != null)
				Add(widgets_);
		}

		private void OnTypeChanged(IDuration d)
		{
			Changed?.Invoke(d);
		}
	}


	class RepeatWidgets : UI.Panel
	{
		private UI.Panel widgets_ = new RandomDurationWidgets();

		public RepeatWidgets()
		{
			Layout = new UI.VerticalFlow();
			Add(widgets_);
		}
	}


	class DelayWidgets : UI.Panel
	{
		private readonly UI.CheckBox halfWay_, end_;
		private readonly DurationPanel duration_ = new DurationPanel();

		public DelayWidgets()
		{
			Layout = new UI.VerticalFlow(30);

			halfWay_ = new UI.CheckBox(S("Halfway"));
			end_ = new UI.CheckBox(S("End"));

			var p = new UI.Panel(new UI.HorizontalFlow());
			p.Add(halfWay_);
			p.Add(end_);

			Add(p);
			Add(duration_);
		}
	}



	class StepInfo : UI.Panel
	{
		private readonly UI.TextBox name_;
		private readonly UI.CheckBox enabled_, halfMove_;
		private Step step_ = null;

		public StepInfo()
		{
			Layout = new UI.HorizontalFlow(10);

			name_ = new UI.TextBox();
			enabled_ = new UI.CheckBox(S("Step enabled"));
			halfMove_ = new UI.CheckBox(S("Half move"));

			Add(new UI.Label(S("Name")));
			Add(name_);
			Add(enabled_);
			Add(halfMove_);

			enabled_.Changed += OnEnabled;
			halfMove_.Changed += OnHalfMove;
		}

		public void SetStep(Step s)
		{
			step_ = s;
			name_.Text = s.Name;
			enabled_.Checked = s.Enabled;
			halfMove_.Checked = s.HalfMove;
		}

		private void OnEnabled(bool b)
		{
			if (step_ != null)
				step_.Enabled = b;
		}

		private void OnHalfMove(bool b)
		{
			if (step_ != null)
				step_.HalfMove = b;
		}
	}


	class StepTab : UI.Panel
	{
		private readonly StepInfo info_ = new StepInfo();
		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly DurationPanel duration_ = new DurationPanel();
		private readonly RepeatWidgets repeat_ = new RepeatWidgets();
		private readonly DelayWidgets delay_ = new DelayWidgets();

		private Step step_ = null;

		public StepTab()
		{
			Layout = new UI.BorderLayout(20);
			Layout.Spacing = 30;

			tabs_.AddTab(S("Duration"), duration_);
			tabs_.AddTab(S("Repeat"), repeat_);
			tabs_.AddTab(S("Delay"), delay_);

			Add(info_, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);

			duration_.Changed += OnDurationTypeChanged;
		}

		public void SetStep(Step s)
		{
			step_ = s;
			info_.SetStep(s);
			duration_.Set(s.Duration);
		}

		private void OnDurationTypeChanged(IDuration d)
		{
			step_.Duration = d;
			duration_.Set(d);
		}
	}


	class ModifierInfo : UI.Panel
	{
		public ModifierInfo()
		{
			Layout = new UI.VerticalFlow(20);

			var p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Name")));
			p.Add(new UI.TextBox());
			p.Add(new UI.CheckBox(S("Modifier enabled")));
			Add(p);

			p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Modifier type")));
			p.Add(new UI.ComboBox());
			Add(p);
		}
	}


	class ModifierSyncPanel : UI.Panel
	{
		public ModifierSyncPanel()
		{
		}
	}


	class MovementWidgets : UI.Panel
	{
		public MovementWidgets()
		{
			Layout = new HorizontalFlow(5);

			Add(new UI.TextBox());
			Add(new UI.Button("-10"));
			Add(new UI.Button("-1"));
			Add(new UI.Button("0"));
			Add(new UI.Button("+1"));
			Add(new UI.Button("+10"));
			Add(new UI.Button(S("Reset")));
		}
	}


	class RigidbodyPanel : UI.Panel
	{
		public RigidbodyPanel()
		{
			Layout = new VerticalFlow(30);

			var w = new UI.Panel();
			var gl = new GridLayout(4);
			gl.HorizontalSpacing = 20;
			gl.VerticalSpacing = 20;
			w.Layout = gl;

			w.Add(new UI.Label(S("Atom")));
			w.Add(new UI.ComboBox());
			w.Add(new UI.Label(S("Receiver")));
			w.Add(new UI.ComboBox());
			w.Add(new UI.Label(S("Move type")));
			w.Add(new UI.ComboBox());
			w.Add(new UI.Label(S("Easing")));
			w.Add(new UI.ComboBox());
			w.Add(new UI.Label(S("Direction")));
			w.Add(new UI.ComboBox());
			w.Add(new UI.Panel());
			w.Add(new UI.Panel());
			Add(w);

			Add(new UI.Label("Minimum"));

			w = new UI.Panel();
			gl = new GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.VerticalSpacing = 20;
			w.Layout = gl;

			w.Add(new UI.Label(S("Value")));
			w.Add(new MovementWidgets());
			w.Add(new UI.Label(S("Range")));
			w.Add(new MovementWidgets());
			w.Add(new UI.Label(S("Interval")));
			w.Add(new MovementWidgets());
			Add(w);


			Add(new UI.Label("Maximum"));

			w = new UI.Panel();
			gl = new GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.VerticalSpacing = 20;
			w.Layout = gl;

			w.Add(new UI.Label(S("Value")));
			w.Add(new MovementWidgets());
			w.Add(new UI.Label(S("Range")));
			w.Add(new MovementWidgets());
			w.Add(new UI.Label(S("Interval")));
			w.Add(new MovementWidgets());
			Add(w);
		}
	}


	class MorphPanel : UI.Panel
	{
	}


	class ModifierPanel : UI.Panel
	{
		private readonly ModifierInfo info_ = new ModifierInfo();
		private readonly UI.Tabs tabs_ = new UI.Tabs();

		public ModifierPanel()
		{
			Layout = new UI.BorderLayout(30);

			var sync = new UI.Panel();
			var rigidbody = new UI.Panel();
			var morph = new UI.Panel();

			tabs_.AddTab(S("Sync"), new ModifierSyncPanel());
			tabs_.AddTab(S("Rigidbody"), new RigidbodyPanel());
			tabs_.AddTab(S("Morph"), new MorphPanel());

			Add(info_, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);
		}
	}


	class ModifiersTab : UI.Panel
	{
		private readonly UI.ListView list_ = new UI.ListView();
		private readonly ModifierPanel modifier_ = new ModifierPanel();

		public ModifiersTab()
		{
			Layout = new UI.BorderLayout(20);

			Add(list_, UI.BorderLayout.Left);
			Add(modifier_, UI.BorderLayout.Center);

			var list = new List<string>();
			for (int i = 0; i < 30; ++i)
				list.Add("item " + i.ToString());
			list_.Items = list;
		}

		public void SetStep(Step s)
		{
		}
	}


	class NewUI
	{
		private UI.Root root_ = new UI.Root();
		private StepControls steps_ = new StepControls();
		private StepTab stepTab_ = new StepTab();
		private ModifiersTab modifiersTab_ = new ModifiersTab();

		public static string S(string s)
		{
			return Strings.Get(s);
		}

		public NewUI()
		{
			var s = Synergy.Instance.Manager.AddStep();
			s.Duration = new RampDuration();
			Synergy.Instance.Manager.AddStep();

			var tabs = new UI.Tabs();
			tabs.AddTab(S("Step"), stepTab_);
			tabs.AddTab(S("Modifiers"), modifiersTab_);

			root_.Layout = new UI.BorderLayout(30);
			root_.Add(steps_, UI.BorderLayout.Top);
			root_.Add(tabs, UI.BorderLayout.Center);

			steps_.SelectionChanged += OnStepSelected;

			if (Synergy.Instance.Manager.Steps.Count > 0)
				SelectStep(Synergy.Instance.Manager.Steps[0]);

			root_.DoLayoutIfNeeded();
		}

		public void SelectStep(Step s)
		{
			stepTab_.SetStep(s);
			modifiersTab_.SetStep(s);
		}

		public void Tick()
		{
			root_.DoLayoutIfNeeded();
		}

		private void OnStepSelected(Step s)
		{
			SelectStep(s);
		}
	}
}
