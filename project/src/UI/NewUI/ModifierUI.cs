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
		}

		public void SetStep(Step s)
		{
			controls_.Set(s);
		}
	}


	class ModifierControls : UI.Panel
	{
		private readonly UI.Button add_, clone_, clone0_, remove_;
		private readonly UI.TypedListView<ModifierContainer> list_;

		private Step step_ = null;
		private bool ignore_ = false;

		public ModifierControls()
		{
			list_ = new TypedListView<ModifierContainer>();
			add_ = new UI.ToolButton("+", AddModifier);
			clone_ = new UI.ToolButton(S("+*"), () => CloneModifier(0));
			clone0_ = new UI.ToolButton(S("+*0"), () => CloneModifier(Utilities.CloneZero));
			remove_ = new UI.ToolButton("\x2013", RemoveModifier);       // en dash

			add_.Tooltip.Text = S("Add a new modifier");
			clone_.Tooltip.Text = S("Clone this modifier");
			clone0_.Tooltip.Text = S("Clone this modifier and zero all values");
			remove_.Tooltip.Text = S("Remove this modifier");
			list_.Tooltip.Text = "bleh bleh";

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
				return null;
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
					list_.AddItem(m, true);
				}
			}
		}

		public void CloneModifier(int flags)
		{
		}

		public void RemoveModifier()
		{
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
	}


	class ModifierInfo : UI.Panel
	{
		private readonly UI.TextBox name_;

		public ModifierInfo()
		{
			name_ = new UI.TextBox();

			Layout = new UI.VerticalFlow(20);

			var p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Name")));
			p.Add(name_);
			p.Add(new UI.CheckBox(S("Modifier enabled")));
			Add(p);

			p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Modifier type")));
			p.Add(new UI.ComboBox());
			Add(p);

			name_.MinimumSize = new UI.Size(300, DontCare);
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
