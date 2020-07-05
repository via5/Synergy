using System.Collections.Generic;

namespace Synergy.NewUI
{
	class AudioModifierPanel : BasicModifierPanel
	{
		class PlayTypeItem
		{
			public string text;
			public int type;

			public PlayTypeItem(string text, int type)
			{
				this.text = text;
				this.type = type;
			}

			public override string ToString()
			{
				return text;
			}
		}

		private readonly AtomComboBox atom_ = new AtomComboBox(
			Utilities.AtomCanPlayAudio);

		private readonly UI.ComboBox<PlayTypeItem> playType_;
		private readonly UI.Tabs tabs_;
		private readonly AudioClipsTab clips_;
		private readonly RandomDurationWidgets delay_;

		private AudioModifier modifier_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();


		public AudioModifierPanel()
		{
			playType_ = new UI.ComboBox<PlayTypeItem>(new List<PlayTypeItem>()
			{
				new PlayTypeItem(S("Play now"), AudioModifier.PlayNow),
				new PlayTypeItem(S("Play if clear"), AudioModifier.PlayIfClear)
			});

			tabs_ = new UI.Tabs();
			clips_ = new AudioClipsTab();
			delay_ = new RandomDurationWidgets();

			var gl = new UI.GridLayout(4);
			gl.Spacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true, false, true };

			var p = new UI.Panel(gl);
			p.Add(new UI.Label(S("Atom")));
			p.Add(atom_);
			p.Add(new UI.Label(S("Play type")));
			p.Add(playType_);

			Layout = new UI.BorderLayout(20);
			Add(p, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);

			tabs_.AddTab(S("Clips"), clips_);
			tabs_.AddTab(S("Delay"), delay_);

			tabs_.SelectionChanged += OnTabSelected;
			atom_.AtomSelectionChanged += OnAtomChanged;
			playType_.SelectionChanged += OnPlayTypeChanged;

			tabs_.Select(0);
		}

		public override string Title
		{
			get { return S("Audio"); }
		}

		public override bool Accepts(IModifier m)
		{
			return m is AudioModifier;
		}

		public override void Set(IModifier m)
		{
			modifier_ = m as AudioModifier;

			ignore_.Do(() =>
			{
				atom_.Select(modifier_.Atom);
				clips_.Set(modifier_);
				delay_.Set(modifier_.Delay);
			});
		}

		private void OnTabSelected(int index)
		{
			clips_.SetActive(index == tabs_.IndexOfWidget(clips_));
		}

		private void OnAtomChanged(Atom a)
		{
			if (ignore_)
				return;

			modifier_.Atom = a;
			clips_.AtomChanged();
		}

		private void OnPlayTypeChanged(PlayTypeItem i)
		{
			modifier_.PlayType = i.type;
		}
	}


	class AudioClipsTab : UI.Panel
	{
		class ClipItem
		{
			public readonly NamedAudioClip clip;
			public bool selected = false;

			public ClipItem(NamedAudioClip c, bool sel)
			{
				clip = c;
				selected = sel;
			}

			public override string ToString()
			{
				if (clip == null)
					return "(no clip)";

				var time = "?s";
				if (clip.sourceClip != null)
					time = Utilities.SecondsToString(clip.sourceClip.length);

				var name = "?";
				if (clip.displayName != null)
					name = clip.displayName;

				var text = "[" + time + "] " + name;

				if (selected)
					text = "\u2713" + text;
				else
					text = "   " + text;

				return text;
			}
		}

		class NoClipsItem : ClipItem
		{
			public NoClipsItem()
				: base(null, false)
			{
			}

			public override string ToString()
			{
				return S("(no clips in scene)");
			}
		}


		private const float UpdateTimer = 0.5f;
		private const int MaxTimerTries = 10;

		private readonly UI.ListView<ClipItem> list_;
		private readonly UI.CheckBox selected_;
		private readonly AudioPlayer player_ = new AudioPlayer();

		private AudioModifier modifier_ = null;
		private bool active_ = false;
		private bool dirty_ = true;
		private Timer timer_ = null;
		private int timerTries_ = 0;
		private readonly List<ClipItem> needsUpdate_ = new List<ClipItem>();
		private readonly IgnoreFlag ignore_ = new IgnoreFlag();


		public AudioClipsTab()
		{
			list_ = new UI.ListView<ClipItem>();
			selected_ = new UI.CheckBox(S("Selected"), OnSelected);

			var top = new UI.Panel(new UI.HorizontalFlow(20));
			top.Add(new UI.Button(S("Refresh"), OnRefresh));
			top.Add(new UI.Button(S("Add file..."), OnAddFile));
			top.Add(new UI.Button(S("Add folder..."), OnAddDir));
			top.Add(new UI.Button(S("Toggle all"), OnToggleAll));

			var right = new UI.Panel(new UI.VerticalFlow(20));
			right.MinimumSize = new UI.Size(300, DontCare);
			right.MaximumSize = new UI.Size(300, DontCare);
			right.Add(selected_);
			right.Add(new UI.Spacer());
			right.Add(player_);

			var bl = new UI.BorderLayout(20);
			Layout = bl;
			Add(top, UI.BorderLayout.Top);
			Add(list_, UI.BorderLayout.Center);
			Add(right, UI.BorderLayout.Right);

			list_.SelectionChanged += OnSelectionChanged;
		}

		public void SetActive(bool b)
		{
			active_ = b;

			if (dirty_)
				UpdateList();
		}

		public void Set(AudioModifier m)
		{
			modifier_ = m;

			foreach (var item in list_.Items)
				item.selected = m.Clips.Contains(item.clip);

			list_.UpdateItemsText();

			player_.AudioSource = m.Source;
			player_.Clip = list_.Selected?.clip;
		}

		public void AtomChanged()
		{
			player_.AudioSource = modifier_?.Source;
		}

		private void OnSelected(bool b)
		{
			if (ignore_)
				return;

			var item = list_.Selected;
			if (item == null)
				return;

			item.selected = b;
			list_.UpdateItemText(item);

			if (b)
				modifier_.AddClip(item.clip);
			else
				modifier_.RemoveClip(item.clip);
		}

		private void OnRefresh()
		{
			UpdateList();
		}

		private void OnAddFile()
		{
			Utilities.AddAudioClip((NamedAudioClip clip) =>
			{
				var item = new ClipItem(clip, false);
				StartTimerIfNeeded(item);
				list_.AddItem(item);
			});
		}

		private void OnAddDir()
		{
			Utilities.AddAudioClipDirectory((List<NamedAudioClip> clips) =>
			{
				var notLoaded = new List<ClipItem>();

				foreach (var clip in clips)
				{
					var item = new ClipItem(clip, false);

					if (!IsLoaded(item))
						notLoaded.Add(item);

					list_.AddItem(item);
				}

				if (notLoaded.Count > 0)
				{
					needsUpdate_.AddRange(notLoaded);
					RestartTimer();
				}
			});
		}

		private void OnToggleAll()
		{
			var items = list_.Items;
			if (items.Count == 0)
				return;

			var list = new List<NamedAudioClip>();
			bool b = !items[0].selected;

			foreach (var item in items)
			{
				if (b)
					list.Add(item.clip);

				item.selected = b;
			}

			list_.UpdateItemsText();
			UpdateWidgets();

			modifier_.Clips = list;
		}

		private bool IsLoaded(ClipItem item)
		{
			if (item?.clip?.sourceClip == null)
				return false;

			switch (item.clip.sourceClip.loadState)
			{
				case UnityEngine.AudioDataLoadState.Unloaded:
				case UnityEngine.AudioDataLoadState.Loading:
					return false;

				case UnityEngine.AudioDataLoadState.Loaded:
				case UnityEngine.AudioDataLoadState.Failed:
				default:
					return true;
			}
		}

		private void StartTimerIfNeeded(ClipItem item)
		{
			if (IsLoaded(item))
				return;

			needsUpdate_.Add(item);
			RestartTimer();
		}

		private void RestartTimer()
		{
			if (timer_ != null)
			{
				timer_.Destroy();
				timer_ = null;
			}

			timer_ = Synergy.Instance.CreateTimer(UpdateTimer, OnTimer);
		}

		private void StopTimer()
		{
			if (timer_ != null)
			{
				timer_.Destroy();
				timer_ = null;
			}

			timerTries_ = 0;
		}

		private void OnSelectionChanged(ClipItem item)
		{
			player_.Clip = item?.clip;
			UpdateWidgets();
		}

		private void OnTimer()
		{
			timer_ = null;

			var notLoaded = new List<ClipItem>();

			foreach (var item in needsUpdate_)
			{
				if (!IsLoaded(item))
					notLoaded.Add(item);
				else
					list_.UpdateItemText(item);
			}

			if (notLoaded.Count != 0)
			{
				++timerTries_;

				if (timerTries_ >= MaxTimerTries)
				{
					StopTimer();
					needsUpdate_.Clear();
				}
				else
				{
					RestartTimer();
				}
			}
		}

		private void UpdateList()
		{
			dirty_ = false;
			list_.Clear();

			if (!DoUpdateList())
				list_.AddItem(new NoClipsItem());
		}

		private void UpdateWidgets()
		{
			ignore_.Do(() =>
			{
				var i = list_.Selected;

				if (i == null)
				{
					selected_.Checked = false;
					selected_.Enabled = false;
				}
				else
				{
					selected_.Checked = i.selected;
					selected_.Enabled = true;
				}
			});
		}

		private bool DoUpdateList()
		{
			var cm = URLAudioClipManager.singleton;
			if (cm == null)
				return false;

			var clips = cm.GetCategoryClips("web");
			if (clips == null)
				return false;

			if (clips.Count == 0)
				return false;

			foreach (var c in clips)
			{
				var sel = (modifier_ != null && modifier_.Clips.Contains(c));
				list_.AddItem(new ClipItem(c, sel));
			}

			return true;
		}
	}


	class AudioPlayer : UI.Panel
	{
		private NamedAudioClip clip_ = null;
		private AudioSourceControl source_ = null;
		private Timer timer_ = null;

		private readonly UI.Label current_ = new UI.Label();
		private readonly UI.Slider seek_ = new UI.Slider();
		private readonly UI.Label time_ = new UI.Label();

		private readonly IgnoreFlag ignore_ = new IgnoreFlag();

		public AudioPlayer()
		{
			Layout = new UI.VerticalFlow();

			var p = new UI.Panel(new UI.HorizontalFlow());
			p.Add(new UI.Button(S("Play"), OnPlay));
			p.Add(new UI.Button(S("Stop"), OnStop));

			Add(p);
			Add(current_);
			Add(seek_);
			Add(time_);

			seek_.ValueChanged += OnSeek;

			timer_ = Synergy.Instance.CreateTimer(
				0.1f, OnTimer, Timer.Repeat);
		}

		public AudioSourceControl AudioSource
		{
			get { return source_; }
			set { source_ = value; }
		}

		public NamedAudioClip Clip
		{
			get { return clip_; }
			set { clip_ = value; }
		}

		private void OnPlay()
		{
			if (source_ == null || clip_ == null)
				return;

			Synergy.LogError("playing " + clip_.displayName);

			source_.PlayNow(clip_);

			if (source_.playingClip == clip_)
				source_.audioSource.time = 0;
		}

		private void OnStop()
		{
			if (source_ == null)
				return;

			source_.Stop();
		}

		private void Update()
		{
			ignore_.Do(() =>
			{
				if (source_?.playingClip?.sourceClip != null)
				{
					current_.Text = source_.playingClip.displayName;
					seek_.Minimum = 0;
					seek_.Maximum = source_.playingClip.sourceClip.length;
					seek_.Value = source_.audioSource.time;
					time_.Text =
						source_.audioSource.time.ToString("0.0") + "/" +
						source_.playingClip.sourceClip.length.ToString("0.0");
				}
				else
				{
					current_.Text = "(nothing playing)";
					seek_.Minimum = 0;
					seek_.Maximum = 0;
					seek_.Value = 0;
					time_.Text = "0/0";
				}
			});
		}

		private void OnSeek(float v)
		{
			if (ignore_)
				return;

			if (source_?.audioSource == null)
				return;

			Synergy.LogError("seeking to " + v.ToString());
			source_.audioSource.time = v;
		}

		private void OnTimer()
		{
			Update();
		}
	}
}
