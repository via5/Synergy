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
		public ToolButton(string text = "")
			: base(text)
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
			where FactoryType : IGenericFactory
			where ObjectType : class, IFactoryObject
	{
		public delegate void FactoryTypeCallback(ObjectType o);
		public event FactoryTypeCallback FactoryTypeChanged;

		public FactoryComboBox(FactoryType f)
		{
			foreach (var creator in f.GetAllCreators())
				AddItem(new FactoryComboBoxItem<ObjectType>(creator));

			SelectionChanged += OnSelectionChanged;
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

			steps_ = new UI.TypedComboBox<Step>();
			add_ = new ToolButton("+");
			clone_ = new ToolButton(S("Clone"));
			clone0_ = new ToolButton(S("Clone 0"));
			remove_ = new ToolButton("\x2013");  // en dash
			up_ = new ToolButton("\x25b2");      // up arrow
			down_ = new ToolButton("\x25bc");    // down arrow

			Add(new UI.Label(S("Step:")));
			Add(steps_);
			Add(add_);
			Add(clone_);
			Add(clone0_);
			Add(remove_);
			Add(up_);
			Add(down_);

			steps_.SelectionChanged += OnSelectionChanged;
			add_.Clicked += AddStep;
			clone_.Clicked += CloneStep;
			clone0_.Clicked += CloneStepZero;
			remove_.Clicked += RemoveStep;
			up_.Clicked += MoveStepUp;
			down_.Clicked += MoveStepDown;

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
		public TimeWidgets()
		{
			Layout = new UI.HorizontalFlow(5);

			Add(new UI.TextBox("1"));
			Add(new UI.Button("-1"));
			Add(new UI.Button("0"));
			Add(new UI.Button(S("Reset")));
			Add(new UI.Button("+1"));
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
		public RandomDurationWidgets(RandomDuration d = null)
		{
			var gl = new UI.GridLayout(2);
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;

			Layout = gl;

			Add(new UI.Label(S("Time")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Random range")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Random interval")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Cut-off")));
			Add(new UI.ComboBox());
		}

		public override bool Set(IDuration d)
		{
			var rd = (d as RandomDuration);
			if (rd == null)
				return false;

			return true;
		}
	}


	class RampDurationWidgets : DurationWidgets
	{
		public RampDurationWidgets(RampDuration d = null)
		{
			var gl = new UI.GridLayout(2);
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;

			Layout = gl;

			Add(new UI.Label(S("Time")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Minimum duration")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Maximum duration")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Hold maximum")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Easing")));
			Add(new UI.ComboBox());

			var ramps = new UI.Panel();
			ramps.Layout = new UI.HorizontalFlow();
			ramps.Add(new UI.CheckBox(S("Ramp up")));
			ramps.Add(new UI.CheckBox(S("Ramp down")));

			Add(new UI.Panel());
			Add(ramps);
		}

		public override bool Set(IDuration d)
		{
			var rd = (d as RampDuration);
			if (rd == null)
				return false;

			return true;
		}
	}


	class DurationPanel : UI.Panel
	{
		private readonly FactoryComboBox<DurationFactory, IDuration> type_;
		private DurationWidgets widgets_ = null;
		private IDuration duration_ = null;

		public DurationPanel()
		{
			Layout = new UI.VerticalFlow(50);

			type_ = new FactoryComboBox<DurationFactory, IDuration>(new DurationFactory());

			var p = new UI.Panel(new UI.HorizontalFlow(20));
			p.Add(new UI.Label(S("Duration type")));
			p.Add(type_);

			type_.FactoryTypeChanged += OnTypeChanged;

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
			Set(d);
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

			name_ = new UI.TextBox("Step 1");
			enabled_ = new UI.CheckBox(S("Step enabled"));
			halfMove_ = new UI.CheckBox(S("Half move"));

			Add(new UI.Label(S("Name")));
			Add(name_);
			Add(enabled_);
			Add(halfMove_);

			enabled_.Clicked += OnEnabled;
			halfMove_.Clicked += OnHalfMove;
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

		public StepTab()
		{
			Layout = new UI.BorderLayout(20);
			Layout.Spacing = 30;

			tabs_.AddTab(S("Duration"), duration_);
			tabs_.AddTab(S("Repeat"), repeat_);
			tabs_.AddTab(S("Delay"), delay_);

			Add(info_, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);
		}

		public void SetStep(Step s)
		{
			info_.SetStep(s);
			duration_.Set(s.Duration);
		}
	}


	class ModifierInfo : UI.Panel
	{
		public ModifierInfo()
		{
			Layout = new UI.VerticalFlow(20);

			var p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Name")));
			p.Add(new UI.TextBox("RT X head Person"));
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


	class RigidbodyPanel : UI.Panel
	{
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
			//for (int i = 0; i < 40; ++i)
			//	Synergy.Instance.Manager.AddStep();

			var tabs = new UI.Tabs();
			tabs.AddTab(S("Step"), stepTab_);
			tabs.AddTab(S("Modifiers"), modifiersTab_);

			root_.Layout = new UI.BorderLayout(30);
			root_.Add(steps_, UI.BorderLayout.Top);
			root_.Add(tabs, UI.BorderLayout.Center);

			steps_.SelectionChanged += OnStepSelected;

			root_.DoLayoutIfNeeded();
		}

		public void Tick()
		{
			root_.DoLayoutIfNeeded();
		}

		private void OnStepSelected(Step s)
		{
			stepTab_.SetStep(s);
			modifiersTab_.SetStep(s);
		}
	}
}
