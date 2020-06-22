using Battlehub.RTCommon;
using Synergy.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

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

		public void SelectTab(int i)
		{
			modifier_.SelectTab(i);
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

		private readonly UI.ComboBox<ModifierContainer> modifiers_;
		private readonly UI.Button add_, clone_, clone0_, remove_;

		private Step step_ = null;
		private bool ignore_ = false;

		public ModifierControls()
		{
			modifiers_ = new ComboBox<ModifierContainer>(OnSelectionChanged);
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
				GetRoot(), UI.MessageDialog.Yes | UI.MessageDialog.No,
				S("Delete modifier"),
				S("Are you sure you want to delete modifier {0}?", m.Name));

			d.RunDialog(() =>
			{
				if (d.Button != UI.MessageDialog.Yes)
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

		private readonly RigidbodyPanel rigidbody_ = new RigidbodyPanel();

		private ModifierContainer mc_ = null;

		public ModifierPanel()
		{
			Layout = new UI.BorderLayout(30);

			var sync = new UI.Panel();
			var rigidbody = new UI.Panel();
			var morph = new UI.Panel();

			tabs_.AddTab(S("Sync"), sync_);
			tabs_.AddTab(S("Rigidbody"), rigidbody_);
			tabs_.AddTab(S("Morph"), new MorphPanel());

			info_.ModifierTypeChanged += OnTypeChanged;

			Add(info_, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);
		}

		public void SelectTab(int i)
		{
			tabs_.Select(i);
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
				rigidbody_.Set(m.Modifier);
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
				"up finishes.")),
				BorderLayout.Top);
		}

		public void Set(IModifierSync o)
		{
			// no-op
		}
	}


	class OtherModifierSyncedModifierUI : UI.Panel, IUIFactoryWidget<IModifierSync>
	{
		private readonly ComboBox<IModifier> others_;
		private OtherModifierSyncedModifier sync_ = null;
		private bool ignore_ = false;

		public OtherModifierSyncedModifierUI()
		{
			others_ = new ComboBox<IModifier>(OnSelectionChanged);

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


	class AtomComboBox : ComboBox<string>
	{
		public delegate void AtomCallback(Atom atom);
		public event AtomCallback AtomSelectionChanged;

		public delegate bool AtomPredicate(Atom atom);
		private readonly AtomPredicate pred_;


		public AtomComboBox(AtomPredicate pred = null)
		{
			pred_ = pred;

			SelectionChanged += (string uid) =>
			{
				AtomSelectionChanged?.Invoke(SelectedAtom);
			};

			SuperController.singleton.onAtomUIDRenameHandlers +=
				OnAtomUIDChanged;
		}

		public Atom SelectedAtom
		{
			get
			{
				var uid = Selected;
				if (string.IsNullOrEmpty(uid))
					return null;

				return Synergy.Instance.GetAtomById(uid);
			}
		}

		public override void Dispose()
		{
			base.Dispose();

			SuperController.singleton.onAtomUIDRenameHandlers -=
				OnAtomUIDChanged;
		}

		private void OnAtomUIDChanged(string oldUID, string newUID)
		{
			var a = Selected;

			if (a == oldUID)
			{
				UpdateList();
				Select(newUID);
			}
		}

		protected override void OnOpen()
		{
			UpdateList();
			base.OnOpen();
		}

		private void UpdateList()
		{
			var ignore = new HashSet<string>()
			{
				"[camerarig]"
			};

			var items = new List<string>();

			items.Add(null);

			var player = Synergy.Instance.GetAtomById("Player");
			if (player != null)
				items.Add(player.uid);

			string sel = Selected;

			foreach (var a in Synergy.Instance.GetSceneAtoms())
			{
				if (ignore.Contains(a.name.ToLower()))
					continue;

				if (pred_ != null)
				{
					if (!pred_(a))
						continue;
				}

				items.Add(a.uid);
			}

			items.Sort();
			SetItems(items, sel);
		}
	}


	class RigidBodyComboBox : UI.ComboBox<string>
	{
		private Atom atom_ = null;
		private bool dirty_ = false;

		public Atom Atom
		{
			get
			{
				return atom_;
			}

			set
			{
				atom_ = value;
				dirty_ = true;
			}
		}

		protected override void OnOpen()
		{
			if (dirty_)
			{
				UpdateList();
				dirty_ = false;
			}

			base.OnOpen();
		}

		private void UpdateList()
		{
			var list = new List<string>();

			list.Add(null);

			if (atom_ != null)
			{
				foreach (var fr in atom_.forceReceivers)
				{
					var rb = fr.GetComponent<Rigidbody>();
					if (rb != null)
						list.Add(rb.name);
				}
			}

			list.Sort();
			SetItems(list, Selected);
		}
	}


	class RigidbodyPanel : UI.Panel
	{
		private readonly AtomComboBox atom_ = new AtomComboBox(
			Utilities.AtomHasRigidbodies);

		private readonly RigidBodyComboBox rigidbodies_ =
			new RigidBodyComboBox();

		private RigidbodyModifier modifier_ = null;


		public RigidbodyPanel()
		{
			Layout = new UI.VerticalFlow(30);

			var w = new UI.Panel();
			var gl = new UI.GridLayout(4);
			gl.HorizontalSpacing = 20;
			gl.VerticalSpacing = 20;
			w.Layout = gl;

			w.Add(new UI.Label(S("Atom")));
			w.Add(atom_);
			w.Add(new UI.Label(S("Receiver")));
			w.Add(rigidbodies_);
			w.Add(new UI.Label(S("Move type")));
			w.Add(new UI.ComboBox<string>());
			w.Add(new UI.Label(S("Easing")));
			w.Add(new UI.ComboBox<string>());
			w.Add(new UI.Label(S("Direction")));
			w.Add(new UI.ComboBox<string>());
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

			atom_.AtomSelectionChanged += OnAtomChanged;
		}

		public void Set(IModifier m)
		{
			modifier_ = m as RigidbodyModifier;
		}

		private void OnAtomChanged(Atom a)
		{
			modifier_.Atom = a;
			rigidbodies_.Atom = a;
		}
	}


	class MorphPanel : UI.Panel
	{
	}
}
