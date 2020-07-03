using Battlehub.RTSaveLoad;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Synergy.NewUI
{
	class AudioModifierPanel : BasicModifierPanel
	{
		private readonly AtomComboBox atom_ = new AtomComboBox(
			Utilities.AtomCanPlayAudio);

		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly AudioClipsTab clips_ = new AudioClipsTab();
		private readonly DelayWidgets delay_ = new DelayWidgets();

		private AudioModifier modifier_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();


		public AudioModifierPanel()
		{
			var gl = new UI.GridLayout(4);
			gl.Spacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true, false, true };

			var p = new UI.Panel(gl);
			p.Add(new UI.Label(S("Atom")));
			p.Add(atom_);
			p.Add(new UI.Label(S("Play type")));
			p.Add(new UI.ComboBox<string>());

			Layout = new UI.BorderLayout(20);
			Add(p, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);

			tabs_.AddTab(S("Clips"), clips_);
			tabs_.AddTab(S("Delay"), delay_);

			tabs_.SelectionChanged += OnTabSelected;
			atom_.AtomSelectionChanged += OnAtomChanged;

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
		}
	}


	class AudioClipsTab : UI.Panel
	{
		class ClipItem
		{
			public readonly NamedAudioClip clip;

			public ClipItem(NamedAudioClip c)
			{
				clip = c;
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

				return text;
			}
		}

		class NoClipsItem : ClipItem
		{
			public NoClipsItem()
				: base(null)
			{
			}

			public override string ToString()
			{
				return S("(no clips in scene)");
			}
		}


		private const float UpdateTimer = 0.5f;
		private const int MaxTimerTries = 10;

		private readonly UI.Button refresh_, addFile_, addDir_;
		private readonly UI.ListView<ClipItem> list_ =
			new UI.ListView<ClipItem>();
		private readonly AudioPlayer player_ = new AudioPlayer();

		private bool active_ = false;
		private bool dirty_ = true;
		private Timer timer_ = null;
		private int timerTries_ = 0;
		private readonly List<ClipItem> needsUpdate_ = new List<ClipItem>();


		public AudioClipsTab()
		{
			refresh_ = new UI.Button(S("Refresh"), OnRefresh);
			addFile_ = new UI.Button(S("Add file..."), OnAddFile);
			addDir_ = new UI.Button(S("Add folder..."), OnAddDir);

			var top = new UI.Panel(new UI.HorizontalFlow(20));
			top.Add(refresh_);
			top.Add(addFile_);
			top.Add(addDir_);

			var right = new UI.Panel(new UI.VerticalFlow(20));
			right.MinimumSize = new UI.Size(300, DontCare);
			right.Add(new UI.CheckBox("Selected"));
			right.Add(player_);

			Layout = new UI.BorderLayout(20);
			Add(top, UI.BorderLayout.Top);
			Add(list_, UI.BorderLayout.Center);
			Add(right, UI.BorderLayout.Right);
		}

		public void SetActive(bool b)
		{
			active_ = b;

			if (dirty_)
				UpdateList();
		}

		private void OnRefresh()
		{
			UpdateList();
		}

		private void OnAddFile()
		{
			Utilities.AddAudioClip((NamedAudioClip clip) =>
			{
				var item = new ClipItem(clip);
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
					var item = new ClipItem(clip);

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
				list_.AddItem(new ClipItem(c));

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

		public AudioPlayer()
		{
			seek_.Minimum = 0;
			seek_.Maximum = 10;

			Layout = new UI.VerticalFlow();
			Add(new UI.Button(S("Play"), OnPlay));
			Add(current_);
			Add(seek_);
		}

		public AudioSourceControl AudioSource
		{
			get
			{
				return source_;
			}

			set
			{
				source_ = value;
				UpdateClip();
			}
		}

		public NamedAudioClip Clip
		{
			get
			{
				return clip_;
			}

			set
			{
				clip_ = value;
			}
		}

		private void UpdateClip()
		{
			if (source_?.playingClip?.sourceClip != null)
			{
				seek_.Maximum = source_.playingClip.sourceClip.length;
				seek_.Value = source_.audioSource.time;
			}
		}

		private void OnPlay()
		{
			if (source_ == null || clip_ == null)
				return;

			source_.PlayNow(clip_);
		}
	}
}
