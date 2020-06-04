using System.Collections.Generic;

namespace Synergy
{
	class AudioClipsCheckboxes : CompoundWidget
	{
		public delegate void AudioClipsCallback(List<NamedAudioClip> list);

		private readonly AudioClipsCallback callback_;
		private readonly Collapsible collapsible_;
		private readonly Button browse_;

		private readonly HashSet<NamedAudioClip> clips_ =
			new HashSet<NamedAudioClip>();
		private bool dirty_ = false;


		public AudioClipsCheckboxes(
			string name, AudioClipsCallback callback, int flags = 0)
				: base(flags)
		{
			callback_ = callback;

			collapsible_ = new Collapsible(name, Toggled, flags);
			browse_ = new Button("Add file...", Browse, flags);
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			if (dirty_)
			{
				UpdateList();
				dirty_ = false;
			}

			collapsible_.AddToUI();
		}

		protected override void DoRemoveFromUI()
		{
			collapsible_.RemoveFromUI();
		}

		public List<NamedAudioClip> Value
		{
			get
			{
				return new List<NamedAudioClip>(clips_);
			}

			set
			{
				clips_.Clear();

				foreach (var clip in value)
					clips_.Add(clip);
			}
		}

		private void UpdateList()
		{
			collapsible_.Clear();
			collapsible_.Add(browse_);

			var cm = URLAudioClipManager.singleton;

			if (cm != null)
			{
				var clips = cm.GetCategoryClips("web");

				if (clips != null)
				{
					foreach (var c in clips)
						AddClip(c);
				}
			}
		}

		private void AddClip(NamedAudioClip c)
		{
			bool isChecked = clips_.Contains(c);

			var time = "?";
			if (c.sourceClip != null)
				time = Utilities.SecondsToString(c.sourceClip.length);

			var name = "?";
			if (c.displayName != null)
				name = c.displayName;

			var text = "[" + time + "] " + name;

			var cb = new Checkbox(
				text, isChecked,
				b => ClipToggled(c, b), flags_ | Widget.Tall);

			collapsible_.Add(cb);
		}

		private void Browse()
		{
			Utilities.AddAudioClip((NamedAudioClip clip) =>
			{
				AddClip(clip);
				sc_.UI.NeedsReset("audio clips changed");
			});
		}

		private void Toggled(bool b)
		{
			if (b)
				UpdateList();
		}

		private void ClipToggled(NamedAudioClip c, bool b)
		{
			if (b)
				clips_.Add(c);
			else
				clips_.Remove(c);

			callback_?.Invoke(clips_.ToList());
		}
	}
}
