using System.Collections.Generic;

namespace Synergy
{
	class AudioModifierUI : AtomModifierUI
	{
		private AudioModifier modifier_ = null;
		private readonly Collapsible delayCollapsible_;
		private readonly DurationWidgets delayWidgets_;
		private readonly StringList playType_;
		private readonly Button stop_;
		private readonly AudioClipsCheckboxes clips_;

		public override string ModifierType
		{
			get { return AudioModifier.FactoryTypeName; }
		}

		public AudioModifierUI(MainUI ui)
			: base(ui, Utilities.AtomCanPlayAudio)
		{
			delayCollapsible_ = new Collapsible("Delay", null, Widget.Right);
			delayWidgets_ = new DurationWidgets("Delay", DelayTypeChanged, Widget.Right);
			playType_ = new StringList(
				"Play type", "", PlayTypeStrings(),
				PlayTypeChanged, Widget.Right);
			stop_ = new Button("Stop audio", StopAudio, Widget.Right);
			clips_ = new AudioClipsCheckboxes("Clips", ClipsChanged, Widget.Right);
		}

		private List<string> PlayTypeStrings()
		{
			return new List<string>() { "Play now", "Play if clear" };
		}

		private int PlayTypeFromString(string s)
		{
			var ss = PlayTypeStrings();

			for (int i = 0; i < ss.Count; ++i)
			{
				if (ss[i] == s)
					return i;
			}

			return -1;
		}

		private string PlayTypeToString(int i)
		{
			var ss = PlayTypeStrings();
			if (i < 0 || i >= ss.Count)
				return "?";

			return ss[i];
		}

		public override void AddToTopUI(IModifier m)
		{
			base.AddToTopUI(m);

			modifier_ = m as AudioModifier;
			if (modifier_ == null)
				return;

			delayWidgets_.SetValue(modifier_.Delay);
			clips_.Value = modifier_.Clips;

			delayCollapsible_.Clear();
			delayCollapsible_.Add(delayWidgets_.GetWidgets());

			AddAtomWidgets(m);

			widgets_.AddToUI(playType_);
			playType_.Value = PlayTypeToString(modifier_.PlayType);

			delayCollapsible_.AddToUI();

			widgets_.AddToUI(new SmallSpacer(Widget.Right));
			widgets_.AddToUI(stop_);
			widgets_.AddToUI(clips_);
		}

		public override void RemoveFromUI()
		{
			base.RemoveFromUI();
			delayCollapsible_.RemoveFromUI();
		}


		private void PlayTypeChanged(string s)
		{
			if (modifier_ != null)
			{
				var p = PlayTypeFromString(s);
				if (p != -1)
					modifier_.PlayType = p;
			}
		}

		private void DelayTypeChanged(IDuration d)
		{
			if (modifier_ != null)
			{
				modifier_.Delay = d;
				Synergy.Instance.MainUI.NeedsReset("audio delay type changed");
			}
		}

		private void StopAudio()
		{
			if (modifier_ != null)
				modifier_.StopAudio();
		}

		private void ClipsChanged(List<NamedAudioClip> clips)
		{
			if (modifier_ != null)
				modifier_.SetClips(clips);
		}
	}
}
