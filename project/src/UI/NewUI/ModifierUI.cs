using Synergy.UI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
					p.Set(mc.Modifier);
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
			gl.HorizontalStretch = new List<bool>()
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
			gl.HorizontalStretch = new List<bool>()
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
		public abstract void Set(IModifier m);
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
			gl.HorizontalStretch = new List<bool>() { false, true, false, true };
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

		public override void Set(IModifier m)
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


		private readonly AtomComboBox atom_ = new AtomComboBox(
			Utilities.AtomHasMorphs);

		private readonly Tabs tabs_ = new Tabs();
		private readonly MorphProgressionTab progression_ =
			new MorphProgressionTab();
		private readonly SelectedMorphsTab morphs_ = new SelectedMorphsTab();
		private readonly AddMorphsTab addMorphs_ = new AddMorphsTab();

		private MorphModifier modifier_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public MorphPanel()
		{
			var p = new UI.Panel(new HorizontalFlow(10));
			p.Add(new UI.Label(S("Atom")));
			p.Add(atom_);

			Layout = new BorderLayout(20);
			Add(p, BorderLayout.Top);
			Add(tabs_, BorderLayout.Center);

			tabs_.AddTab(S("Progression"), progression_);
			tabs_.AddTab(S("Selected morphs"), morphs_);
			tabs_.AddTab(S("Add morphs"), addMorphs_);

			atom_.AtomSelectionChanged += OnAtomSelected;

			tabs_.Select(2);
		}

		public override bool Accepts(IModifier m)
		{
			return m is MorphModifier;
		}

		public override void Set(IModifier m)
		{
			modifier_ = m as MorphModifier;

			ignore_.Do(() =>
			{
				atom_.Select(modifier_.Atom);
				addMorphs_.Atom = modifier_.Atom;
			});
		}

		private void OnAtomSelected(Atom atom)
		{
			if (ignore_)
				return;

			addMorphs_.Atom = atom;
		}
	}


	class MorphProgressionTab : UI.Panel
	{
	}


	class SelectedMorphsTab : UI.Panel
	{
		public SelectedMorphsTab()
		{
			var gl = new GridLayout(1);
			gl.VerticalSpacing = 10;
			gl.UniformHeight = false;
			gl.VerticalStretch = new List<bool>() { false, false, true, false };

			var search = new UI.TextBox();
			search.Placeholder = "Search";

			var left = new Panel(gl);
			left.Add(new UI.Label(S("Selected morphs")));
			left.Add(new UI.ListView<string>());
			left.Add(search);


			var right = new Panel(new VerticalFlow());
			right.Add(new UI.CheckBox(S("Enabled")));

			Layout = new BorderLayout();
			Add(left, BorderLayout.Left);
			Add(right, BorderLayout.Center);
		}
	}


	class AddMorphsTab : UI.Panel
	{
		private class MorphItem
		{
			public DAZMorph morph;
			public bool selected;
			public int allIndex = -1;

			public MorphItem(DAZMorph m, bool sel)
			{
				morph = m;
				selected = sel;
			}

			public override string ToString()
			{
				if (selected)
					return "\u2713" + morph.displayName;
				else
					return "   " + morph.displayName;
			}
		}

		private const float SearchDelay = 0.7f;

		private readonly UI.Stack mainStack_, morphsStack_;
		private readonly UI.ListView<string> categories_;
		private readonly UI.ListView<MorphItem> morphs_, allMorphs_;
		private readonly UI.TextBox search_;
		private readonly UI.Button toggle_;
		private Timer searchTimer_ = null;

		private Atom atom_ = null;
		private readonly HashSet<DAZMorph> selection_ = new HashSet<DAZMorph>();
		private readonly List<MorphItem> items_ = new List<MorphItem>();
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public AddMorphsTab()
		{
			categories_ = new ListView<string>();
			categories_.SelectionIndexChanged += OnCategorySelected;

			var cats = new Panel(new BorderLayout());
			cats.Add(new UI.Label(S("Categories")), BorderLayout.Top);
			cats.Add(categories_, BorderLayout.Center);


			morphs_ = new ListView<MorphItem>();
			morphs_.SelectionChanged += OnMorphSelected;

			allMorphs_ = new ListView<MorphItem>();
			allMorphs_.SelectionChanged += OnAllMorphSelected;

			morphsStack_ = new Stack();
			morphsStack_.AddToStack(morphs_);
			morphsStack_.AddToStack(allMorphs_);

			var morphs = new Panel(new BorderLayout());
			var mp = new UI.Panel(new BorderLayout());
			mp.Add(new UI.Label(S("Morphs")), BorderLayout.Center);

			toggle_ = new UI.Button("", OnToggleMorph);
			toggle_.MinimumSize = new Size(250, DontCare);
			mp.Add(toggle_, BorderLayout.Right);

			morphs.Add(mp, BorderLayout.Top);
			morphs.Add(morphsStack_, BorderLayout.Center);


			var ly = new GridLayout(2);
			ly.HorizontalSpacing = 20;
			var lists = new UI.Panel(ly);
			lists.Add(cats);
			lists.Add(morphs);


			var top = new UI.Panel(new HorizontalFlow(20));

			var show = new UI.ComboBox<string>();
			show.Items = new List<string>()
			{
				"Show all", "Show morphs only", "Show poses only"
			};

			top.Add(show);

			search_ = new UI.TextBox();
			search_.Placeholder = "Search";
			search_.MinimumSize = new Size(300, DontCare);
			search_.Changed += OnSearchChanged;
			top.Add(search_);

			var mainPanel = new Panel();
			mainPanel.Layout = new BorderLayout(20);
			mainPanel.Add(top, BorderLayout.Top);
			mainPanel.Add(lists, BorderLayout.Center);


			var noAtomPanel = new Panel();
			noAtomPanel.Layout = new VerticalFlow();
			noAtomPanel.Add(new UI.Label(S("No atom selected")));


			mainStack_ = new Stack();
			mainStack_.AddToStack(noAtomPanel);
			mainStack_.AddToStack(mainPanel);


			Layout = new BorderLayout();
			Add(mainStack_, BorderLayout.Center);

			UpdateToggleButton();
			UpdateCategories();


			foreach (var morph in Utilities.GetAtomMorphs(atom_))
				items_.Add(new MorphItem(morph, false));
		}

		public Atom Atom
		{
			get
			{
				return atom_;
			}

			set
			{
				atom_ = value;

				UpdateCategories();
				morphs_.Clear();
			}
		}

		private void UpdateToggleButton()
		{
			var m = ActiveMorphList.Selected;

			if (m != null && m.selected)
				toggle_.Text = S("Remove morph");
			else
				toggle_.Text = S("Add morph");

			toggle_.Enabled = (m != null);
		}

		private void UpdateCategories()
		{
			if (atom_ == null)
			{
				mainStack_.Select(0);
				return;
			}

			mainStack_.Select(1);

			bool showMorphs = true;
			bool showPoses = true;


			var oldSel = categories_.Selected;
			categories_.Clear();


			var categoryNames = new HashSet<string>();
			var searchText = search_.Text;
			var searchPattern = Regex.Escape(searchText).Replace("\\*", ".*");
			var searchRe = new Regex(searchPattern, RegexOptions.IgnoreCase);

			categoryNames.Add(S("(All)"));

			int selIndex = -1;
			int i = 1;

			foreach (var mi in items_)
			{
				var morph = mi.morph;

				if (!showMorphs && !morph.isPoseControl)
					continue;

				if (!showPoses && morph.isPoseControl)
					continue;

				var path = morph.region;
				if (path == "")
					path = S("(No category)");

				if (!categoryNames.Contains(path))
				{
					if (searchRe.IsMatch(path))
					{
						if (path == oldSel)
							selIndex = i;

						categoryNames.Add(path);
						++i;
					}
				}
			}

			ignore_.Do(() =>
			{
				categories_.Items = categoryNames.ToList();
				categories_.Select(selIndex);
			});
		}

		private void UpdateMorphs(int catIndex)
		{
			bool showMorphs = true;
			bool showPoses = true;

			var searchText = search_.Text;
			var searchPattern = Regex.Escape(searchText).Replace("\\*", ".*");
			var searchRe = new Regex(searchPattern, RegexOptions.IgnoreCase);

			if (catIndex == 0)
			{
				// all

				if (allMorphs_.Count == 0)
				{
					var items = new List<MorphItem>();
					int i = 0;

					foreach (var mi in items_)
					{
						var morph = mi.morph;

						if (!showMorphs && !morph.isPoseControl)
							continue;

						if (!showPoses && morph.isPoseControl)
							continue;

						if (searchText.Length > 0)
						{
							if (!searchRe.IsMatch(morph.region + " " + morph.displayName))
								continue;
						}

						mi.allIndex = i;
						items.Add(mi);
					}

					allMorphs_.Items = items;
				}

				morphsStack_.Select(1);
			}
			else
			{
				morphsStack_.Select(0);
				morphs_.Clear();

				if (catIndex > 0)
				{
					var items = new List<MorphItem>();
					var catName = categories_.At(catIndex);

					foreach (var mi in items_)
					{
						var morph = mi.morph;

						if (!showMorphs && !morph.isPoseControl)
							continue;

						if (!showPoses && morph.isPoseControl)
							continue;

						var path = morph.region;
						if (path == "")
							path = S("(No category)");

						if (path == catName)
							items.Add(mi);
					}

					morphs_.Items = items;
				}
			}
		}

		private void OnCategorySelected(int catIndex)
		{
			if (ignore_)
				return;

			UpdateMorphs(catIndex);
		}

		private void OnSearchChanged(string s)
		{
			Synergy.LogError("search changed");

			if (searchTimer_ != null)
			{
				searchTimer_.Destroy();
				searchTimer_ = null;
			}

			searchTimer_ = Synergy.Instance.CreateTimer(
				SearchDelay, OnSearchTimer);
		}

		private void OnSearchTimer()
		{
			Synergy.LogError("searching '" + search_.Text + "'");

			UpdateCategories();
			UpdateMorphs(categories_.SelectedIndex);
		}

		private ListView<MorphItem> ActiveMorphList
		{
			get
			{
				if (morphsStack_.Selected == 0)
					return morphs_;
				else
					return allMorphs_;
			}
		}

		private void OnToggleMorph()
		{
			var m = ActiveMorphList.Selected;
			m.selected = !m.selected;
			ActiveMorphList.UpdateItemText(ActiveMorphList.SelectedIndex);
			UpdateToggleButton();
		}

		private void OnMorphSelected(MorphItem m)
		{
			UpdateToggleButton();
		}

		private void OnAllMorphSelected(MorphItem m)
		{
			UpdateToggleButton();
		}
	}
}
