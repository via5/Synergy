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
		private readonly UI.Button add_, clone_, clone0_, remove_, rename_;

		private Step step_ = null;
		private bool ignore_ = false;

		public ModifierControls()
		{
			modifiers_ = new ComboBox<ModifierContainer>(OnSelectionChanged);
			add_ = new UI.ToolButton("+", AddModifier);
			clone_ = new UI.ToolButton(S("+*"), () => CloneModifier(0));
			clone0_ = new UI.ToolButton(S("+*0"), () => CloneModifier(Utilities.CloneZero));
			remove_ = new UI.ToolButton("\x2013", RemoveModifier);       // en dash
			rename_ = new UI.ToolButton(S("Rename"), OnRename);

			add_.Tooltip.Text = S("Add a new modifier");
			clone_.Tooltip.Text = S("Clone this modifier");
			clone0_.Tooltip.Text = S("Clone this modifier and zero all values");
			remove_.Tooltip.Text = S("Remove this modifier");

			var p = new Panel(new UI.HorizontalFlow(20));
			p.Add(add_);
			p.Add(clone_);
			p.Add(clone0_);
			p.Add(remove_);
			p.Add(rename_);

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
			{
				step_.ModifiersChanged -= OnModifiersChanged;
				step_.ModifierNameChanged -= OnModifierNameChanged;
			}

			step_ = s;

			if (step_ != null)
			{
				step_.ModifiersChanged += OnModifiersChanged;
				step_.ModifierNameChanged += OnModifierNameChanged;
			}

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

		private void OnRename()
		{
			InputDialog.GetInput(
				GetRoot(), S("Rename modifier"), S("Modifier name"), Selected.Name,
				(v) =>
				{
					Selected.UserDefinedName = v;
				});
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

		private void OnModifierNameChanged(IModifier m)
		{
			modifiers_.UpdateItemsText();
		}
	}


	class ModifierPanel : UI.Panel
	{
		private readonly ModifierInfo info_ = new ModifierInfo();
		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly ModifierSyncPanel sync_ = new ModifierSyncPanel();
		private readonly List<BasicModifierPanel> modifierPanels_ =
			new List<BasicModifierPanel>();

		private ModifierContainer mc_ = null;

		public ModifierPanel()
		{
			Layout = new UI.BorderLayout(30);

			var sync = new UI.Panel();

			modifierPanels_.Add(new RigidbodyPanel());
			modifierPanels_.Add(new MorphPanel());

			tabs_.AddTab(S("Sync"), sync_);

			foreach (var p in modifierPanels_)
				tabs_.AddTab(p.Title, p);

			info_.ModifierTypeChanged += OnTypeChanged;

			Add(info_, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);
		}

		public void SelectTab(int i)
		{
			tabs_.Select(i);
		}

		public void Set(ModifierContainer mc)
		{
			mc_ = mc;

			info_.Set(mc);
			sync_.Set(mc);

			var sel = tabs_.SelectedWidget;
			bool needsReselect = false;
			UI.Widget acceptedPanel = null;

			for (int i=0; i<modifierPanels_.Count; ++i)
			{
				var p = modifierPanels_[i];

				if (mc.Modifier != null && p.Accepts(mc.Modifier))
				{
					acceptedPanel = p;
					tabs_.SetTabVisible(p, true);
				}
				else
				{
					if (sel == p)
						needsReselect = true;

					tabs_.SetTabVisible(p, false);
				}
			}

			if (needsReselect)
			{
				if (acceptedPanel == null)
					tabs_.Select(0);
				else
					tabs_.Select(acceptedPanel);
			}
		}

		private void OnTypeChanged()
		{
			Set(mc_);
		}
	}


	class ModifierInfo : UI.Panel
	{
		public event Callback ModifierTypeChanged;

		private readonly UI.CheckBox enabled_;
		private readonly FactoryComboBox<ModifierFactory, IModifier> type_;
		private ModifierContainer mc_ = null;
		private bool ignore_ = false;

		public ModifierInfo()
		{
			enabled_ = new CheckBox(S("Modifier enabled"));
			type_ = new FactoryComboBox<ModifierFactory, IModifier>(
				OnTypeChanged);

			Layout = new UI.VerticalFlow(20);

			var p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Modifier type")));
			p.Add(type_);
			p.Add(enabled_);
			Add(p);

			enabled_.Changed += OnEnabledChanged;
		}

		public void Set(ModifierContainer m)
		{
			using (new ScopedFlag((b) => ignore_ = b))
			{
				mc_ = m;
				enabled_.Checked = m.Enabled;
				type_.Select(m.Modifier);
			}
		}

		private void OnEnabledChanged(bool b)
		{
			if (ignore_)
				return;

			mc_.Enabled = b;
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
		private ModifierContainer modifier_ = null;

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

		public void Set(ModifierContainer mc)
		{
			modifier_ = mc;

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

			UpdateList();

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

		public void Select(Atom atom)
		{
			Select(atom?.uid);
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
		public delegate void RigidbodyCallback(Rigidbody atom);
		public event RigidbodyCallback RigidbodySelectionChanged;

		private Atom atom_ = null;
		private bool dirty_ = false;

		public RigidBodyComboBox()
		{
			UpdateList(null);

			SelectionChanged += (string uid) =>
			{
				RigidbodySelectionChanged?.Invoke(SelectedRigidbody);
			};
		}

		public void Set(Atom atom, Rigidbody rb)
		{
			atom_ = atom;
			UpdateList(rb?.name);
		}

		public Rigidbody SelectedRigidbody
		{
			get
			{
				var name = Selected;
				if (string.IsNullOrEmpty(name))
					return null;

				if (atom_ == null)
					return null;

				return Utilities.FindRigidbody(atom_, name);
			}
		}

		public void Select(Rigidbody rb)
		{
			Select(rb?.name);
		}

		protected override void OnOpen()
		{
			if (dirty_)
			{
				UpdateList(Selected);
				dirty_ = false;
			}

			base.OnOpen();
		}

		private void UpdateList(string sel)
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
			SetItems(list, sel);
		}
	}


	class DirectionPanel : UI.Panel
	{
		public delegate void DirectionCallback(Vector3 v);
		public event DirectionCallback Changed;

		private readonly UI.ComboBox<string> type_ = new ComboBox<string>();
		private readonly UI.TextSlider x_ = new UI.TextSlider();
		private readonly UI.TextSlider y_ = new UI.TextSlider();
		private readonly UI.TextSlider z_ = new UI.TextSlider();
		private readonly UI.Panel sliders_ = new Panel();

		private bool ignore_ = false;

		public DirectionPanel()
		{
			var gl = new GridLayout(2);
			gl.Stretch = new List<bool>()
			{
				false, true,
			};
			gl.Spacing = 20;
			Layout = gl;

			type_.AddItem(S("X"));
			type_.AddItem(S("Y"));
			type_.AddItem(S("Z"));
			type_.AddItem(S("Custom"));

			gl = new GridLayout(6);
			gl.Spacing = 20;

			// only stretch the sliders
			gl.Stretch = new List<bool>()
			{
				false, true,
				false, true,
				false, true
			};

			sliders_.Layout = gl;
			sliders_.Add(new UI.Label(S("X")));
			sliders_.Add(x_);
			sliders_.Add(new UI.Label(S("Y")));
			sliders_.Add(y_);
			sliders_.Add(new UI.Label(S("Z")));
			sliders_.Add(z_);


			x_.ValueChanged += OnChanged;
			y_.ValueChanged += OnChanged;
			z_.ValueChanged += OnChanged;

			var typePanel = new UI.Panel(new VerticalFlow(0, false));
			typePanel.Add(type_);

			Add(new UI.Label(S("Direction  ")));
			Add(typePanel);
			Add(new UI.Panel());
			Add(sliders_);

			type_.SelectionChanged += OnTypeChanged;

			ShowSliders(false);
		}

		public void Set(Vector3 v)
		{
			using (new ScopedFlag((b) => ignore_ = b))
			{
				var dirString = Utilities.LocalizedDirectionString(v);
				if (dirString == "")
					dirString = S("Custom");

				type_.Select(dirString);

				x_.Set(v.x, -1, 1);
				y_.Set(v.y, -1, 1);
				z_.Set(v.z, -1, 1);
			}
		}

		private void OnTypeChanged(string s)
		{
			if (ignore_)
				return;

			switch (type_.SelectedIndex)
			{
				case 0:
					Change(new Vector3(1, 0, 0));
					break;

				case 1:
					Change(new Vector3(0, 1, 0));
					break;

				case 2:
					Change(new Vector3(0, 0, 1));
					break;
			}

			ShowSliders(type_.SelectedIndex == 3);
		}

		private void OnChanged(float f)
		{
			if (ignore_)
				return;

			Change(new Vector3(x_.Value, y_.Value, z_.Value));
		}

		private void Change(Vector3 v)
		{
			Set(v);
			Changed?.Invoke(v);
		}

		private void ShowSliders(bool b)
		{
			sliders_.Visible = b;
		}
	}


	abstract class BasicModifierPanel : UI.Panel
	{
		public abstract string Title { get; }
		public abstract bool Accepts(IModifier m);
	}


	class RigidbodyPanel : BasicModifierPanel
	{
		private readonly AtomComboBox atom_ = new AtomComboBox(
			Utilities.AtomHasRigidbodies);

		private readonly RigidBodyComboBox receiver_ =
			new RigidBodyComboBox();

		private readonly FactoryComboBox<
			RigidbodyMovementTypeFactory, IRigidbodyMovementType>
				movementType_ = new FactoryComboBox<
					RigidbodyMovementTypeFactory, IRigidbodyMovementType>();

		private readonly FactoryComboBox<EasingFactory, IEasing> easing_ =
			new FactoryComboBox<EasingFactory, IEasing>();

		private readonly DirectionPanel dir_ = new DirectionPanel();

		private readonly MovementPanel min_ = new MovementPanel(S("Minimum"));
		private readonly MovementPanel max_ = new MovementPanel(S("Maximum"));

		private RigidbodyModifier modifier_ = null;
		private bool ignore_ = false;


		public RigidbodyPanel()
		{
			Layout = new UI.VerticalFlow(30);

			var w = new UI.Panel();
			var gl = new UI.GridLayout(4);
			gl.HorizontalSpacing = 20;
			gl.VerticalSpacing = 20;
			gl.Stretch = new List<bool>() { false, true, false, true };
			w.Layout = gl;

			w.Add(new UI.Label(S("Atom")));
			w.Add(atom_);
			w.Add(new UI.Label(S("Receiver")));
			w.Add(receiver_);
			w.Add(new UI.Label(S("Move type")));
			w.Add(movementType_);
			w.Add(new UI.Label(S("Easing")));
			w.Add(easing_);
			Add(w);
			Add(dir_);

			Add(min_);
			Add(max_);

			atom_.AtomSelectionChanged += OnAtomChanged;
			receiver_.RigidbodySelectionChanged += OnRigidbodyChanged;
			movementType_.FactoryTypeChanged += OnMovementTypeChanged;
			easing_.FactoryTypeChanged += OnEasingChanged;
			dir_.Changed += OnDirectionChanged;
		}

		public override string Title
		{
			get { return S("Rigidbody"); }
		}

		public override bool Accepts(IModifier m)
		{
			return m is RigidbodyModifier;
		}

		public void Set(IModifier m)
		{
			modifier_ = m as RigidbodyModifier;

			using (new ScopedFlag((b) => ignore_ = b))
			{
				atom_.Select(modifier_.Atom);
				receiver_.Set(modifier_.Atom, modifier_.Receiver);
				movementType_.Select(modifier_.Type);
				easing_.Select(modifier_.Movement.Easing);
				dir_.Set(modifier_.Direction);
				min_.Set(modifier_.Movement.Minimum);
				max_.Set(modifier_.Movement.Maximum);
			}
		}

		private void OnAtomChanged(Atom a)
		{
			if (ignore_)
				return;

			modifier_.Atom = a;
			modifier_.Receiver = Utilities.FindRigidbody(
				a, receiver_.Selected);

			using (new ScopedFlag((b) => ignore_ = b))
				receiver_.Set(modifier_.Atom, modifier_.Receiver);
		}

		private void OnRigidbodyChanged(Rigidbody rb)
		{
			if (ignore_)
				return;

			modifier_.Receiver = rb;
		}

		private void OnMovementTypeChanged(IRigidbodyMovementType type)
		{
			if (ignore_)
				return;

			modifier_.Type = type;
		}

		private void OnEasingChanged(IEasing easing)
		{
			if (ignore_)
				return;

			modifier_.Movement.Easing = easing;
		}

		private void OnDirectionChanged(Vector3 v)
		{
			modifier_.Direction = v;
		}
	}


	class MorphPanel : BasicModifierPanel
	{
		public override string Title
		{
			get { return S("Morph"); }
		}

		public override bool Accepts(IModifier m)
		{
			return m is MorphModifier;
		}
	}
}
