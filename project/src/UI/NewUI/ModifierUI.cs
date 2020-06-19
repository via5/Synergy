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

			Add(controls_, UI.BorderLayout.Left);
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

		private readonly UI.Button add_, clone_, clone0_, remove_;
		private readonly UI.TypedListView<ModifierContainer> list_;

		private Step step_ = null;
		private bool ignore_ = false;

		public ModifierControls()
		{
			list_ = new TypedListView<ModifierContainer>(OnSelectionChanged);
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

			Layout = new UI.BorderLayout(5);

			Add(p, UI.BorderLayout.Top);
			Add(list_, UI.BorderLayout.Center);
		}

		public override void Dispose()
		{
			Set(null);
		}

		public ModifierContainer Selected
		{
			get
			{
				return list_.Selected;
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
			using (var sf = new ScopedFlag(b => ignore_ = b))
			{
				if (step_ != null)
				{
					var m = step_.AddEmptyModifier();
					list_.AddItem(m);
					list_.Select(m);
				}
			}
		}

		public void CloneModifier(int flags)
		{
			using (var sf = new ScopedFlag(b => ignore_ = b))
			{
				var m = Selected;
				if (step_ != null && m != null)
				{
					var m2 = m.Clone(flags);
					step_.AddModifier(m2);
					list_.AddItem(m2);
					list_.Select(m2);
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

				using (var sf = new ScopedFlag(b => ignore_ = b))
				{
					step_.DeleteModifier(m);
					list_.RemoveItem(m);
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
				list_.Clear();
			else
				list_.Items = step_.Modifiers;
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

		public void Set(ModifierContainer m)
		{
			info_.Set(m);
		}
	}


	class ModifierInfo : UI.Panel
	{
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
			using (var sf = new ScopedFlag((b) => ignore_ = b))
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
