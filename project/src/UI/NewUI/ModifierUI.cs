using Battlehub.RTCommon;
using Synergy.UI;
using System;
using System.Collections.Generic;

namespace Synergy.NewUI
{
	class ModifiersTab : UI.Panel
	{
		private readonly ModifierControls controls_ = new ModifierControls();
		private readonly ModifierPanel modifier_ = new ModifierPanel();

		public ModifiersTab()
		{
			Layout = new UI.BorderLayout(20);

			Add(controls_, UI.BorderLayout.Top);
			Add(modifier_, UI.BorderLayout.Center);

			controls_.SelectionChanged += OnModifierSelected;

			SelectModifier(null);
		}

		public void SetStep(Step s)
		{
			controls_.Set(s);
		}

		public void SelectModifier(ModifierContainer m)
		{
			if (m == null)
			{
				modifier_.Visible = false;
			}
			else
			{
				modifier_.Visible = true;
				modifier_.Set(m);
			}
		}

		private void OnModifierSelected(ModifierContainer m)
		{
			SelectModifier(m);
		}
	}


	class ModifierControls : UI.Panel
	{
		public delegate void ModifierCallback(ModifierContainer m);
		public event ModifierCallback SelectionChanged;

		private readonly UI.TypedComboBox<ModifierContainer> modifiers_;
		private readonly UI.Button add_, clone_, clone0_, remove_;

		private Step step_ = null;
		private bool ignore_ = false;

		public ModifierControls()
		{
			modifiers_ = new TypedComboBox<ModifierContainer>(OnSelectionChanged);
			add_ = new UI.ToolButton("+", AddModifier);
			clone_ = new UI.ToolButton(S("+*"), () => CloneModifier(0));
			clone0_ = new UI.ToolButton(S("+*0"), () => CloneModifier(Utilities.CloneZero));
			remove_ = new UI.ToolButton("\x2013", RemoveModifier);       // en dash

			add_.Tooltip.Text = S("Add a new modifier");
			clone_.Tooltip.Text = S("Clone this modifier");
			clone0_.Tooltip.Text = S("Clone this modifier and zero all values");
			remove_.Tooltip.Text = S("Remove this modifier");

			var p = new Panel(new UI.HorizontalFlow(20));
			p.Add(add_);
			p.Add(clone_);
			p.Add(clone0_);
			p.Add(remove_);

			Layout = new UI.HorizontalFlow(20);

			Add(new UI.Label(S("Modifiers:")));
			Add(modifiers_);
			Add(p);
		}

		public override void Dispose()
		{
			Set(null);
		}

		public ModifierContainer Selected
		{
			get
			{
				return modifiers_.Selected;
			}
		}

		public void Set(Step s)
		{
			if (step_ != null)
				step_.ModifiersChanged -= OnModifiersChanged;

			step_ = s;

			if (step_ != null)
				step_.ModifiersChanged += OnModifiersChanged;

			UpdateModifiers();
		}

		public void AddModifier()
		{
			using (new ScopedFlag(b => ignore_ = b))
			{
				if (step_ != null)
				{
					var m = step_.AddEmptyModifier();
					modifiers_.AddItem(m);
					modifiers_.Select(m);
				}
			}
		}

		public void CloneModifier(int flags)
		{
			using (new ScopedFlag(b => ignore_ = b))
			{
				var m = Selected;
				if (step_ != null && m != null)
				{
					var m2 = m.Clone(flags);
					step_.AddModifier(m2);
					modifiers_.AddItem(m2);
					modifiers_.Select(m2);
				}
			}
		}

		public void RemoveModifier()
		{
			var m = Selected;
			if (m == null)
				return;

			var d = new UI.MessageDialog(
				GetRoot(), S("Delete modifier"),
				S("Are you sure you want to delete modifier {0}?", m.Name));

			d.RunDialog(() =>
			{
				if (d.Button != UI.MessageDialog.OK)
					return;

				using (new ScopedFlag(b => ignore_ = b))
				{
					step_.DeleteModifier(m);
					modifiers_.RemoveItem(m);
				}
			});
		}

		private void OnSelectionChanged(ModifierContainer m)
		{
			SelectionChanged?.Invoke(m);
		}

		private void UpdateModifiers()
		{
			if (step_ == null)
				modifiers_.Clear();
			else
				modifiers_.Items = step_.Modifiers;
		}

		private void OnModifiersChanged()
		{
			if (ignore_)
				return;

			UpdateModifiers();
		}
	}


	class ModifierPanel : UI.Panel
	{
		private readonly ModifierInfo info_ = new ModifierInfo();
		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly ModifierSyncPanel sync_ = new ModifierSyncPanel();

		private ModifierContainer mc_ = null;

		public ModifierPanel()
		{
			Layout = new UI.BorderLayout(30);

			var sync = new UI.Panel();
			var rigidbody = new UI.Panel();
			var morph = new UI.Panel();

			tabs_.AddTab(S("Sync"), sync_);
			tabs_.AddTab(S("Rigidbody"), new RigidbodyPanel());
			tabs_.AddTab(S("Morph"), new MorphPanel());

			info_.ModifierTypeChanged += OnTypeChanged;

			Add(info_, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);
		}

		public void Set(ModifierContainer m)
		{
			mc_ = m;

			info_.Set(m);

			if (m.Modifier == null)
			{
				tabs_.Visible = false;
			}
			else
			{
				tabs_.Visible = true;
				sync_.Set(m.Modifier);
			}
		}

		private void OnTypeChanged()
		{
			Set(mc_);
		}
	}


	class ModifierInfo : UI.Panel
	{
		public delegate void Callback();
		public event Callback ModifierTypeChanged;

		private readonly UI.TextBox name_;
		private readonly UI.CheckBox enabled_;
		private readonly FactoryComboBox<ModifierFactory, IModifier> type_;
		private ModifierContainer mc_ = null;
		private bool ignore_ = false;

		public ModifierInfo()
		{
			name_ = new UI.TextBox();
			enabled_ = new CheckBox(S("Modifier enabled"));
			type_ = new FactoryComboBox<ModifierFactory, IModifier>(
				OnTypeChanged);

			Layout = new UI.VerticalFlow(20);

			var p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Name")));
			p.Add(name_);
			p.Add(enabled_);
			Add(p);

			p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Modifier type")));
			p.Add(type_);
			Add(p);

			name_.MinimumSize = new UI.Size(300, DontCare);
		}

		public void Set(ModifierContainer m)
		{
			using (new ScopedFlag((b) => ignore_ = b))
			{
				mc_ = m;
				name_.Text = m.Name;
				enabled_.Checked = m.Enabled;
				type_.Select(m.Modifier);
			}
		}

		private void OnTypeChanged(IModifier m)
		{
			if (ignore_)
				return;

			mc_.Modifier = m;
			ModifierTypeChanged?.Invoke();
		}
	}



	class ModifierSyncPanel : UI.Panel
	{
		private IModifier modifier_ = null;

		private readonly FactoryComboBox<ModifierSyncFactory, IModifierSync> type_;

		private FactoryObjectWidget<
			ModifierSyncFactory, IModifierSync, ModifierSyncUIFactory> ui_ =
				new FactoryObjectWidget<
					ModifierSyncFactory, IModifierSync, ModifierSyncUIFactory>();

		private bool ignore_ = false;


		public ModifierSyncPanel()
		{
			type_ = new FactoryComboBox<ModifierSyncFactory, IModifierSync>(
				OnTypeChanged);

			Layout = new BorderLayout(20);

			var p = new Panel(new HorizontalFlow(20));
			p.Add(new UI.Label(S("Sync type:")));
			p.Add(type_);

			Add(p, BorderLayout.Top);
			Add(ui_, BorderLayout.Center);
		}

		public void Set(IModifier m)
		{
			modifier_ = m;

			using (new ScopedFlag((bool b) => ignore_ = b))
			{
				type_.Select(modifier_?.ModifierSync);
				ui_.Set(modifier_?.ModifierSync);
			}
		}

		private void OnTypeChanged(IModifierSync sync)
		{
			if (ignore_)
				return;

			modifier_.ModifierSync = sync;
			ui_.Set(sync);
		}
	}


	class ModifierSyncUIFactory : IUIFactory<IModifierSync>
	{
		public Dictionary<string, Func<IUIFactoryWidget<IModifierSync>>> GetCreators()
		{
			return new Dictionary<string, Func<IUIFactoryWidget<IModifierSync>>>()
			{
				{
					DurationSyncedModifier.FactoryTypeName,
					() => { return new DurationSyncedModifierUI(); }
				},

				{
					StepProgressSyncedModifier.FactoryTypeName,
					() => { return new StepProgressSyncedModifierUI(); }
				},

				{
					OtherModifierSyncedModifier.FactoryTypeName,
					() => { return new OtherModifierSyncedModifierUI(); }
				},

				{
					UnsyncedModifier.FactoryTypeName,
					() => { return new UnsyncedModifierUI(); }
				},
			};
		}
	}


	class DurationSyncedModifierUI : UI.Panel, IUIFactoryWidget<IModifierSync>
	{
		public DurationSyncedModifierUI()
		{
			Layout = new UI.BorderLayout();
			Add(new UI.Label(
				S("This modifier is synchronized with the step duration.")),
				BorderLayout.Top);
		}

		public void Set(IModifierSync o)
		{
			// no-op
		}
	}


	class StepProgressSyncedModifierUI : UI.Panel, IUIFactoryWidget<IModifierSync>
	{
		public StepProgressSyncedModifierUI()
		{
			Layout = new UI.BorderLayout();

			Add(new UI.Label(S(
				"This modifier is synchronized with the step progress. For " +
				"ramp durations, the modifier will be at 50% when ramping " +
				" up finishes.")),
				BorderLayout.Top);
		}

		public void Set(IModifierSync o)
		{
			// no-op
		}
	}


	class OtherModifierSyncedModifierUI : UI.Panel, IUIFactoryWidget<IModifierSync>
	{
		private readonly TypedComboBox<IModifier> others_;
		private OtherModifierSyncedModifier sync_ = null;
		private bool ignore_ = false;

		public OtherModifierSyncedModifierUI()
		{
			others_ = new TypedComboBox<IModifier>(OnSelectionChanged);

			var p = new UI.Panel(new UI.HorizontalFlow(20));
			p.Add(new UI.Label(S("Modifier:")));
			p.Add(others_);

			Layout = new UI.BorderLayout(20);
			Add(new UI.Label(S(
				"This modifier is synced to the duration of another " +
				"modifier.")),
				BorderLayout.Top);
			Add(p, BorderLayout.Center);
		}

		public void Set(IModifierSync o)
		{
			sync_ = o as OtherModifierSyncedModifier;

			using (new ScopedFlag((bool b) => ignore_ = b))
			{
				UpdateList();
				others_.Select(sync_.OtherModifier);
			}
		}

		private void UpdateList()
		{
			var list = new List<IModifier>();

			list.Add(null);

			foreach (var mc in sync_.ParentModifier.ParentStep.Modifiers)
			{
				if (mc.Modifier != null && mc.Modifier != sync_.ParentModifier)
					list.Add(mc.Modifier);
			}

			others_.SetItems(list, sync_.OtherModifier);
		}

		private void OnSelectionChanged(IModifier m)
		{
			if (ignore_)
				return;

			sync_.OtherModifier = m;
		}
	}


	class UnsyncedModifierUI : UI.Panel, IUIFactoryWidget<IModifierSync>
	{
		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly DurationPanel duration_ = new DurationPanel();
		private readonly DelayWidgets delay_ = new DelayWidgets();

		private UnsyncedModifier sync_ = null;
		private bool ignore_ = false;

		public UnsyncedModifierUI()
		{
			Layout = new UI.BorderLayout(20);

			Add(new UI.Label(S(
				"This modifier is has its own duration and delay.")),
				BorderLayout.Top);
			Add(tabs_);


			tabs_.AddTab(S("Duration"), duration_);
			tabs_.AddTab(S("Delay"), delay_);

			duration_.Changed += OnDurationTypeChanged;
		}

		public void Set(IModifierSync o)
		{
			sync_ = o as UnsyncedModifier;

			using (new ScopedFlag((bool b) => ignore_ = b))
			{
				duration_.Set(sync_.Duration);
				delay_.Set(sync_.Delay);
			}
		}

		private void OnDurationTypeChanged(IDuration d)
		{
			if (ignore_)
				return;

			sync_.Duration = d;
		}
	}


	class RigidbodyPanel : UI.Panel
	{
		public RigidbodyPanel()
		{
			Layout = new UI.VerticalFlow(30);

			var w = new UI.Panel();
			var gl = new UI.GridLayout(4);
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
			gl = new UI.GridLayout(2);
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
			gl = new UI.GridLayout(2);
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
}
