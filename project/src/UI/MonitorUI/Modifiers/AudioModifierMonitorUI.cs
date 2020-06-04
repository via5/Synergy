using UnityEngine;

namespace Synergy
{
	class AudioModifierMonitor : BasicModifierMonitor
	{
		public override string ModifierType
		{
			get { return AudioModifier.FactoryTypeName; }
		}

		private readonly Label state_;
		private readonly Label clip_;
		private readonly Label sourceClip_;
		private readonly FloatSlider seek_;
		private readonly Button pauseToggle_;

		private IDurationMonitor delay_ = null;
		private AudioModifier modifier_ = null;

		public AudioModifierMonitor()
		{
			state_ = new Label("", Widget.Right);
			clip_ = new Label("", Widget.Right);
			sourceClip_ = new Label("", Widget.Right);

			seek_ = new FloatSlider(
				"Seek", 0, new FloatRange(0, 0), Seek, Widget.Right);

			pauseToggle_ = new Button("", PauseToggle, Widget.Right);

			UpdatePauseToggle();
		}

		public override void AddToUI(IModifier m)
		{
			modifier_ = m as AudioModifier;
			if (modifier_ == null)
				return;

			if (modifier_ != null)
			{
				if (delay_ == null ||
					delay_.DurationType != modifier_.Delay.GetFactoryTypeName())
				{
					delay_ = MonitorUI.CreateDurationMonitor(
						"Delay", modifier_.Delay, Widget.Right);
				}
			}
			else
			{
				delay_ = null;
			}

			if (delay_ != null)
				delay_.AddToUI(modifier_?.Delay);

			widgets_.AddToUI(state_);
			widgets_.AddToUI(clip_);
			widgets_.AddToUI(sourceClip_);
			widgets_.AddToUI(seek_);
			widgets_.AddToUI(pauseToggle_);

			clip_.Height = 100;
			sourceClip_.Height = 100;

			pauseToggle_.BackgroundColor = Color.red;
			pauseToggle_.TextColor = Color.white;
		}

		public override void RemoveFromUI()
		{
			base.RemoveFromUI();

			if (delay_ != null)
				delay_.RemoveFromUI();
		}

		public override void Update()
		{
			if (modifier_ == null)
				return;

			state_.Text = "State: " + StateToString(modifier_.State);

			var clip = modifier_.CurrentClip;

			if (clip == null)
				clip_.Text = "modifier: (nothing playing)";
			else
				clip_.Text = "modifier: " + clip.displayName;


			var source = modifier_.Source?.audioSource;
			var sclip = modifier_.Source?.playingClip;
			var uclip = source?.clip;

			if (source == null)
			{
				sourceClip_.Text = "(no source)";
				seek_.Set(0, 0, 0);
				seek_.Enabled = false;
			}
			else if (uclip == null)
			{
				sourceClip_.Text = "source: (no clip)";
				seek_.Set(0, 0, 0);
				seek_.Enabled = false;
			}
			else
			{
				if (sclip != null)
					sourceClip_.Text = "source: " + sclip.displayName;
				else
					sourceClip_.Text = "source: " + uclip.name;

				seek_.Set(0, uclip.length, source.time);
			}

			if (delay_ != null)
				delay_.Update();

			UpdatePauseToggle();
		}

		private string StateToString(int s)
		{
			switch (s)
			{
				case AudioModifier.StartingState: return "starting";
				case AudioModifier.InDelayState: return "in delay";
				case AudioModifier.NoSourceState: return "no source";
				case AudioModifier.NoClipsState: return "no clips";
				case AudioModifier.PlayingState: return "playing";
				case AudioModifier.PausedState: return "paused";
				default: return "?";
			}
		}


		private void Seek(float f)
		{
			var s = modifier_?.Source?.audioSource;
			if (s == null)
				return;

			s.time = f;
		}

		private void PauseToggle()
		{
			var s = modifier_?.Source?.audioSource;
			if (s == null)
				return;

			if (s.isPlaying)
				s.Pause();
			else
				s.UnPause();

			UpdatePauseToggle();
		}

		private void UpdatePauseToggle()
		{
			var s = modifier_?.Source?.audioSource;
			var isPlaying = (s != null && s.isPlaying && s.time > 0);

			if (isPlaying)
			{
				pauseToggle_.Text = "Pause";
				pauseToggle_.BackgroundColor = Color.red;
				pauseToggle_.TextColor = Color.white;
			}
			else
			{
				pauseToggle_.Text = "Resume";
				pauseToggle_.BackgroundColor = Color.green;
				pauseToggle_.TextColor = Color.black;
			}
		}
	}
}
