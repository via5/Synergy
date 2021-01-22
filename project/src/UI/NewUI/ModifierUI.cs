﻿using Synergy.UI;
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

			modifiers_.NavButtons = true;

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

			modifierPanels_.Add(new RigidbodyModifierPanel());
			modifierPanels_.Add(new MorphModifierPanel());
			modifierPanels_.Add(new LightModifierPanel());
			modifierPanels_.Add(new AudioModifierPanel());
			modifierPanels_.Add(new EyesModifierPanel());

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

			var sel = tabs_.Selected;
			bool needsReselect = false;
			int acceptedPanel = -1;

			for (int i=0; i<modifierPanels_.Count; ++i)
			{
				var p = modifierPanels_[i];

				if (mc.Modifier != null && p.Accepts(mc.Modifier))
				{
					acceptedPanel = tabs_.IndexOfWidget(p);
					p.Set(mc.Modifier);
					tabs_.SetTabVisible(p, true);
				}
				else
				{
					if (sel == tabs_.IndexOfWidget(p))
						needsReselect = true;

					tabs_.SetTabVisible(p, false);
				}
			}

			if (needsReselect)
			{
				if (acceptedPanel < 0)
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

		private readonly FactoryComboBox<ModifierFactory, IModifier> type_;
		private readonly UI.CheckBox enabled_;
		private readonly UI.Button disableOthers_, enableAll_;
		private ModifierContainer mc_ = null;
		private bool ignore_ = false;

		public ModifierInfo()
		{
			type_ = new FactoryComboBox<ModifierFactory, IModifier>(
				OnTypeChanged);
			enabled_ = new CheckBox(S("Enabled"));
			disableOthers_ = new UI.Button(S("Disable others"), OnDisableOthers);
			enableAll_ = new UI.Button(S("Enable all"), OnEnableAll);

			enabled_.Tooltip.Text = S("Whether this modifier is executed");

			Layout = new UI.VerticalFlow(20);

			var p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Modifier type")));
			p.Add(type_);
			p.Add(enabled_);
			p.Add(disableOthers_);
			p.Add(enableAll_);
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

		private void OnDisableOthers()
		{
			mc_?.ParentStep?.DisableAllExcept(mc_);
			enabled_.Checked = mc_?.Enabled ?? false;
		}

		private void OnEnableAll()
		{
			mc_?.ParentStep?.EnableAll();
			enabled_.Checked = true;
		}
	}



	class ModifierSyncPanel : UI.Panel
	{
		private ModifierContainer modifier_ = null;

		private readonly FactoryComboBox<
			ModifierSyncFactory, IModifierSync> type_;

		private FactoryObjectWidget<
			ModifierSyncFactory, IModifierSync, ModifierSyncUIFactory> ui_;

		private IgnoreFlag ignore_ = new IgnoreFlag();


		public ModifierSyncPanel()
		{
			type_ = new FactoryComboBox<ModifierSyncFactory, IModifierSync>(
				OnTypeChanged);

			ui_ = new FactoryObjectWidget<
				ModifierSyncFactory, IModifierSync, ModifierSyncUIFactory>();

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

			ignore_.Do(() =>
			{
				type_.Select(modifier_?.ModifierSync);
				ui_.Set(modifier_?.ModifierSync);
			});
		}

		private void OnTypeChanged(IModifierSync sync)
		{
			if (ignore_ || modifier_ == null)
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
		private readonly ComboBox<ModifierContainer> others_;
		private OtherModifierSyncedModifier sync_ = null;
		private bool ignore_ = false;

		public OtherModifierSyncedModifierUI()
		{
			others_ = new ComboBox<ModifierContainer>(OnSelectionChanged);

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
				others_.Select(sync_.OtherModifierContainer);
			}
		}

		private void UpdateList()
		{
			var list = new List<ModifierContainer>();

			list.Add(null);

			foreach (var mc in sync_.ParentStep.Modifiers)
			{
				if (mc != sync_.ParentModifierContainer)
					list.Add(mc);
			}

			others_.SetItems(list, sync_.OtherModifierContainer);
		}

		private void OnSelectionChanged(ModifierContainer mc)
		{
			if (ignore_)
				return;

			sync_.OtherModifierContainer = mc;
		}
	}


	class UnsyncedModifierUI : UI.Panel, IUIFactoryWidget<IModifierSync>
	{
		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly DurationPanel duration_ = new DurationPanel();
		private readonly DelayWidgets delay_ = new DelayWidgets();

		private UnsyncedModifier sync_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public UnsyncedModifierUI()
		{
			Layout = new UI.BorderLayout(20);

			Add(new UI.Label(S(
				"This modifier has its own duration and delay.")),
				BorderLayout.Top);
			Add(tabs_);


			tabs_.AddTab(S("Duration"), duration_);
			tabs_.AddTab(S("Delay"), delay_);

			duration_.Changed += OnDurationTypeChanged;
		}

		public void Set(IModifierSync o)
		{
			sync_ = o as UnsyncedModifier;

			ignore_.Do(() =>
			{
				duration_.Set(sync_.Duration);
				delay_.Set(sync_.Delay);
			});
		}

		private void OnDurationTypeChanged(IDuration d)
		{
			if (ignore_)
				return;

			sync_.Duration = d;
		}
	}


	class GotoButton : UI.ToolButton
	{
		public GotoButton(UI.Button.Callback cb = null)
			: base(S("G"), cb)
		{
		}
	}


	class AtomComboBox : UI.Panel
	{
		public delegate void AtomCallback(Atom atom);
		public event AtomCallback AtomSelectionChanged;

		public delegate bool AtomPredicate(Atom atom);
		private readonly AtomPredicate pred_;

		private readonly ComboBox<string> cb_;
		private readonly GotoButton goto_;

		public AtomComboBox(AtomPredicate pred = null)
		{
			pred_ = pred;
			cb_ = new ComboBox<string>();
			goto_ = new GotoButton(OnGoto);

			Layout = new UI.BorderLayout(5);
			Add(cb_, UI.BorderLayout.Center);
			Add(goto_, UI.BorderLayout.Right);

			cb_.SelectionChanged += (string uid) =>
			{
				goto_.Enabled = (SelectedAtom != null);
				AtomSelectionChanged?.Invoke(SelectedAtom);
			};

			cb_.Opened += OnOpen;

			UpdateList();

			SuperController.singleton.onAtomUIDRenameHandlers +=
				OnAtomUIDChanged;
		}

		public Atom SelectedAtom
		{
			get
			{
				var uid = cb_.Selected;
				if (string.IsNullOrEmpty(uid))
					return null;

				return Synergy.Instance.GetAtomById(uid);
			}
		}

		public void Select(Atom atom)
		{
			cb_.Select(atom?.uid);
		}

		public override void Dispose()
		{
			base.Dispose();

			SuperController.singleton.onAtomUIDRenameHandlers -=
				OnAtomUIDChanged;
		}

		private void OnAtomUIDChanged(string oldUID, string newUID)
		{
			var a = cb_.Selected;

			if (a == oldUID)
			{
				UpdateList();
				cb_.Select(newUID);
			}
		}

		private void OnGoto()
		{
			if (SelectedAtom == null)
				return;

			SuperController.singleton.SelectController(
				SelectedAtom.mainController);
		}

		private void OnOpen()
		{
			UpdateList();
		}

		private void UpdateList()
		{
			var items = new List<string>();

			items.Add(null);

			var player = Synergy.Instance.GetAtomById("Player");
			if (player != null)
				items.Add(player.uid);

			string sel = cb_.Selected;

			foreach (var a in Synergy.Instance.GetSceneAtoms())
			{
				if (pred_ != null)
				{
					if (!pred_(a))
						continue;
				}

				items.Add(a.uid);
			}

			items.Sort();
			cb_.SetItems(items, sel);
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
				var dirString = Utilities.DirectionString(v);
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
}
