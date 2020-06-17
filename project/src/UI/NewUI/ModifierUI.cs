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
		private readonly ItemControls controls_ = new ItemControls();
		private readonly UI.TypedListView<ModifierContainer> list_;

		private Step step_ = null;
		private bool ignore_ = false;

		public ModifierControls()
		{
			list_ = new TypedListView<ModifierContainer>();

			Layout = new UI.BorderLayout(5);

			Add(controls_, UI.BorderLayout.Top);
			Add(list_, UI.BorderLayout.Center);

			controls_.Added += OnAdd;
			controls_.Cloned += OnClone;
			controls_.Removed += OnRemove;
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

		private void UpdateModifiers()
		{
			if (step_ == null)
				list_.Clear();
			else
				list_.Items = step_.Modifiers;
		}

		private void OnAdd()
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

		private void OnClone(int flags)
		{
		}

		private void OnRemove()
		{
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
