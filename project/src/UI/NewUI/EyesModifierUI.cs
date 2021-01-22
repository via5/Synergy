using AssetBundles;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy.NewUI
{
	class EyesModifierPanel : BasicModifierPanel
	{
		private readonly AtomComboBox atom_ =
			new AtomComboBox(Utilities.AtomHasEyes);

		private EyesModifier modifier_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		private UI.Tabs tabs_ = new UI.Tabs();

		public EyesModifierPanel()
		{
			Layout = new UI.BorderLayout(10);

			var top = new UI.Panel(new UI.GridLayout(3, 10));
			top.Add(new UI.Label(S("Atom")));
			top.Add(atom_);
			top.Add(new UI.CheckBox(S("Previews")));
			Add(top, UI.BorderLayout.Top);

			Add(tabs_, UI.BorderLayout.Center);

			tabs_.AddTab(S("Options"), new EyesOptionsUI());
			tabs_.AddTab(S("Saccade"), new EyesSaccadeUI());
			tabs_.AddTab(S("Focus time"), new EyesFocusUI());
			tabs_.AddTab(S("Targets"), new EyesTargetsUI());

			atom_.AtomSelectionChanged += OnAtomChanged;
		}

		public override string Title
		{
			get { return S("Eyes"); }
		}

		public override bool Accepts(IModifier m)
		{
			return m is EyesModifier;
		}

		public override void Set(IModifier m)
		{
			modifier_ = m as EyesModifier;

			ignore_.Do(() =>
			{
				atom_.Select(modifier_.Atom);
				foreach (var t in tabs_.TabWidgets)
					((IEyesModifierTab)t).Set(modifier_);
			});
		}

		private void OnAtomChanged(Atom a)
		{
			if (ignore_)
				return;

			modifier_.Atom = a;
		}
	}


	class IntegrationSettingWidget : UI.Panel
	{
		public delegate void SettingChangedCallback(int setting);
		public event SettingChangedCallback SettingChanged;

		private UI.ComboBox<string> list_;

		public IntegrationSettingWidget(SettingChangedCallback callback=null)
		{
			Layout = new UI.BorderLayout();

			list_ = new UI.ComboBox<string>(OnChanged);

			Add(list_, UI.BorderLayout.Center);

			if (callback != null)
				SettingChanged += callback;
		}

		public void Set(bool available, int setting)
		{
			if (available)
			{
				list_.Items = new List<string>()
				{
					S("Don't change"),
					S("Enable"),
					S("Disable")
				};

				list_.Enabled = true;

				switch (setting)
				{
					case EyesModifier.SettingIgnore:
						list_.Select(0);
						break;

					case EyesModifier.SettingEnable:
						list_.Select(1);
						break;

					case EyesModifier.SettingDisable:
						list_.Select(2);
						break;
				}
			}
			else
			{
				list_.Items = new List<string>()
				{
					S("Not found")
				};

				list_.Enabled = false;
				list_.Select(0);
			}
		}

		private void OnChanged(int i)
		{
			SettingChanged?.Invoke(i);
		}
	}


	interface IEyesModifierTab
	{
		void Set(EyesModifier m);
	}

	class EyesOptionsUI : UI.Panel, IEyesModifierTab
	{
		private EyesModifier modifier_ = null;
		private EyesPreviews previews_ = new EyesPreviews();

		private IgnoreFlag ignore_ = new IgnoreFlag();
		private MovementWidgets minDistance_;
		private IntegrationSettingWidget gaze_;
		private IntegrationSettingWidget blink_;
		private UI.TextSlider previewAlpha_;

		public EyesOptionsUI()
		{
			Layout = new UI.BorderLayout();

			var top = new UI.Panel(new UI.GridLayout(2, 10));

			minDistance_ = new MovementWidgets(MovementWidgets.SmallMovement);
			gaze_ = new IntegrationSettingWidget(OnGazeChanged);
			blink_ = new IntegrationSettingWidget(OnBlinkChanged);
			previewAlpha_ = new UI.TextSlider(OnPreviewAlphaChanged);

			top.Add(new UI.Label(S("Minimum distance")));
			top.Add(minDistance_);
			top.Add(new UI.Label(S("MacGruber's Gaze")));
			top.Add(gaze_);
			top.Add(new UI.Label(S("Blink")));
			top.Add(blink_);
			top.Add(new UI.Label(S("Preview alpha")));
			top.Add(previewAlpha_);

			Add(top, UI.BorderLayout.Top);

			minDistance_.Changed += OnMinimumDistanceChanged;
		}

		public void Set(EyesModifier m)
		{
			modifier_ = m;
			previews_.Modifier = m;

			if (modifier_ == null)
				return;

			ignore_.Do(() =>
			{
				minDistance_.Set(modifier_.MinDistance);
				gaze_.Set(modifier_.GazeAvailable, modifier_.GazeSetting);
				blink_.Set(modifier_.BlinkAvailable, modifier_.BlinkSetting);
				previewAlpha_.Set(previews_.Alpha, 0, 1);
			});
		}

		private void OnMinimumDistanceChanged(float f)
		{
			if (ignore_)
				return;

			if (modifier_ != null)
				modifier_.MinDistance = f;
		}

		private void OnGazeChanged(int setting)
		{
			if (ignore_)
				return;

			if (modifier_ != null)
				modifier_.GazeSetting = setting;
		}

		private void OnBlinkChanged(int setting)
		{
			if (ignore_)
				return;

			if (modifier_ != null)
				modifier_.BlinkSetting = setting;
		}

		private void OnPreviewAlphaChanged(float f)
		{
			if (ignore_)
				return;

			previews_.Alpha = f;
		}
	}

	class EyesSaccadeUI : UI.Panel, IEyesModifierTab
	{
		private EyesModifier modifier_ = null;

		private IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly RandomizableTimePanel interval_;
		private readonly MovementWidgets min_, max_;

		public EyesSaccadeUI()
		{
			interval_ = new RandomizableTimePanel();
			min_ = new MovementWidgets(OnMinimumChanged, MovementWidgets.SmallMovement);
			max_ = new MovementWidgets(OnMaximumChanged, MovementWidgets.SmallMovement);

			var p = new UI.Panel(new UI.GridLayout(2, 10, 20));
			p.Add(new UI.Label(S("Minimum")));
			p.Add(min_);
			p.Add(new UI.Label(S("Maximum")));
			p.Add(max_);

			Layout = new UI.VerticalFlow(40);
			Add(interval_);
			Add(p);
		}

		public void Set(EyesModifier m)
		{
			modifier_ = m;
			if (modifier_ == null)
				return;

			ignore_.Do(() =>
			{
				interval_.Set(modifier_.SaccadeTime);
				min_.Set(modifier_.SaccadeMin);
				max_.Set(modifier_.SaccadeMax);
			});
		}

		private void OnMinimumChanged(float f)
		{
			if (ignore_)
				return;

			if (modifier_ != null)
				modifier_.SaccadeMin = f;
		}

		private void OnMaximumChanged(float f)
		{
			if (ignore_)
				return;

			if (modifier_ != null)
				modifier_.SaccadeMax = f;
		}
	}

	class EyesFocusUI : UI.Panel, IEyesModifierTab
	{
		private EyesModifier modifier_ = null;

		private IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly RandomizableTimePanel time_;


		public EyesFocusUI()
		{
			time_ = new RandomizableTimePanel();

			Layout = new UI.VerticalFlow();
			Add(time_);
		}

		public void Set(EyesModifier m)
		{
			modifier_ = m;
			if (modifier_ == null)
				return;

			ignore_.Do(() =>
			{
				time_.Set(m.FocusDuration);
			});
		}
	}

	class EyesTargetsUI : UI.Panel, IEyesModifierTab
	{
		private class Target
		{
			public EyesTargetContainer tc = null;

			public override string ToString()
			{
				return tc.Name;
			}
		};

		private EyesModifier modifier_ = null;

		private IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly UI.ListView<Target> targets_;
		private readonly UI.Panel panel_;
		private readonly UI.CheckBox enabled_;
		private readonly FactoryComboBox<EyesTargetFactory, IEyesTarget> type_;
		private readonly FactoryObjectWidget<EyesTargetFactory, IEyesTarget, EyesTargetUIFactory> ui_;


		public EyesTargetsUI()
		{
			targets_ = new UI.ListView<Target>(OnSelection);
			panel_ = new UI.Panel(new UI.VerticalFlow(10));
			enabled_ = new UI.CheckBox(S("Enabled"), OnEnabledChanged);
			type_ = new FactoryComboBox<EyesTargetFactory, IEyesTarget>(OnTypeChanged);
			ui_ = new FactoryObjectWidget<EyesTargetFactory, IEyesTarget, EyesTargetUIFactory>();

			var buttons = new UI.Panel(new UI.HorizontalFlow(10));
			buttons.Add(new UI.Button(S("+"), AddTarget));
			buttons.Add(new UI.Button(S("-"), RemoveTarget));

			var left = new UI.Panel(new UI.BorderLayout());
			//left.Add(buttons, UI.BorderLayout.Top);
			left.Add(targets_);

			panel_.Add(enabled_);
			panel_.Add(type_);
			panel_.Add(new UI.Spacer(40));
			panel_.Add(ui_);

			Layout = new UI.BorderLayout(10);
			Add(buttons, UI.BorderLayout.Top);
			Add(left, UI.BorderLayout.Left);
			Add(panel_, UI.BorderLayout.Center);
		}

		public void Set(EyesModifier m)
		{
			modifier_ = m;
			if (modifier_ == null)
				return;

			targets_.Clear();

			foreach (var tc in m.Targets)
			{
				var t = new Target();
				t.tc = tc;

				targets_.AddItem(t);
			}

			if (targets_.Count == 0)
				UpdateSelection(null);
			else
				targets_.Select(0);
		}

		public void AddTarget()
		{
			if (modifier_ == null)
				return;

			var t = new Target();
			t.tc = new EyesTargetContainer();

			modifier_.AddTarget(t.tc);
			targets_.AddItem(t);

			targets_.Select(targets_.Count - 1);
		}

		public void RemoveTarget()
		{
			if (modifier_ == null)
				return;

			var t = targets_.Selected;
			if (t == null)
				return;

			var d = new UI.MessageDialog(
				GetRoot(), UI.MessageDialog.Yes | UI.MessageDialog.No,
				S("Remove target"),
				S("Are you sure you want to delete target {0}?", t.tc.Name));

			d.RunDialog(() =>
			{
				if (d.Button != UI.MessageDialog.Yes)
					return;

				modifier_.RemoveTarget(t.tc);
				targets_.RemoveItem(t);
			});
		}

		private void OnSelection(Target t)
		{
			UpdateSelection(t);
		}

		private void OnEnabledChanged(bool b)
		{
			if (ignore_ || modifier_ == null)
				return;

			var t = targets_.Selected;
			if (t == null)
				return;

			t.tc.Enabled = b;
		}

		private void OnTypeChanged(IEyesTarget type)
		{
			if (ignore_ || modifier_ == null)
				return;

			var t = targets_.Selected;
			if (t == null)
				return;

			t.tc.Target = type;

			targets_.UpdateItemText(t);
			UpdateSelection(t);
		}

		private void UpdateSelection(Target t)
		{
			ignore_.Do(() =>
			{
				if (t == null)
				{
					panel_.Visible = false;
				}
				else
				{
					panel_.Visible = true;
					enabled_.Checked = t.tc.Enabled;
					type_.Select(t.tc.Target);
					ui_.Set(t.tc.Target);
				}
			});
		}
	}


	class EyesTargetUIFactory : IUIFactory<IEyesTarget>
	{
		public Dictionary<string, Func<IUIFactoryWidget<IEyesTarget>>> GetCreators()
		{
			return new Dictionary<string, Func<IUIFactoryWidget<IEyesTarget>>>()
			{
				{
					RigidbodyEyesTarget.FactoryTypeName,
					() => { return new RigidbodyEyesTargetUI(); }
				},

				{
					RandomEyesTarget.FactoryTypeName,
					() => { return new RandomEyesTargetUI(); }
				},

				{
					PlayerEyesTarget.FactoryTypeName,
					() => { return new PlayerEyesTargetUI(); }
				}
			};
		}
	}


	class RigidbodyEyesTargetUI : UI.Panel, IUIFactoryWidget<IEyesTarget>
	{
		private RigidbodyEyesTarget target_ = null;

		private IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly AtomComboBox atom_;
		private readonly RigidBodyComboBox receiver_;
		private readonly MovementWidgets offsetX_;
		private readonly MovementWidgets offsetY_;
		private readonly MovementWidgets offsetZ_;

		public RigidbodyEyesTargetUI()
		{
			atom_ = new AtomComboBox();
			receiver_ = new RigidBodyComboBox();
			offsetX_ = new MovementWidgets(OnOffsetXChanged, MovementWidgets.SmallMovement);
			offsetY_ = new MovementWidgets(OnOffsetYChanged, MovementWidgets.SmallMovement);
			offsetZ_ = new MovementWidgets(OnOffsetZChanged, MovementWidgets.SmallMovement);

			Layout = new UI.GridLayout(2, 10);
			Add(new UI.Label(S("Atom")));
			Add(atom_);
			Add(new UI.Label(S("Receiver")));
			Add(receiver_);
			Add(new UI.Label(S("Offset X")));
			Add(offsetX_);
			Add(new UI.Label(S("Offset Y")));
			Add(offsetY_);
			Add(new UI.Label(S("Offset Z")));
			Add(offsetZ_);

			atom_.AtomSelectionChanged += OnAtomChanged;
			receiver_.RigidbodySelectionChanged += OnReceiverChanged;
		}

		public void Set(IEyesTarget t)
		{
			target_ = t as RigidbodyEyesTarget;
			if (target_ == null)
				return;

			ignore_.Do(() =>
			{
				atom_.Select(target_.Atom);
				receiver_.Set(target_.Atom, target_.Receiver);
				offsetX_.Set(target_.Offset.x);
				offsetY_.Set(target_.Offset.y);
				offsetZ_.Set(target_.Offset.z);
			});
		}

		private void OnAtomChanged(Atom a)
		{
			if (ignore_ || target_ == null)
				return;

			target_.Atom = a;
			receiver_.Set(a, target_.Receiver);
		}

		private void OnReceiverChanged(Rigidbody rb)
		{
			if (ignore_ || target_ == null)
				return;

			target_.Receiver = rb;
		}

		private void OnOffsetXChanged(float f)
		{
			if (ignore_ || target_ == null)
				return;

			target_.Offset = new Vector3(f, target_.Offset.y, target_.Offset.z);
		}

		private void OnOffsetYChanged(float f)
		{
			if (ignore_ || target_ == null)
				return;

			target_.Offset = new Vector3(target_.Offset.x, f, target_.Offset.z);
		}

		private void OnOffsetZChanged(float f)
		{
			if (ignore_ || target_ == null)
				return;

			target_.Offset = new Vector3(target_.Offset.x, target_.Offset.y, f);
		}
	}


	class RandomEyesTargetUI : UI.Panel, IUIFactoryWidget<IEyesTarget>
	{
		public void Set(IEyesTarget t)
		{
		}
	}


	class PlayerEyesTargetUI : UI.Panel, IUIFactoryWidget<IEyesTarget>
	{
		public void Set(IEyesTarget t)
		{
		}
	}
}
