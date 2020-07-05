using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	sealed class AudioModifier : AtomModifier
	{
		public const int StartingState = 0;
		public const int InDelayState = 1;
		public const int NoSourceState = 2;
		public const int NoClipsState = 3;
		public const int PlayingState = 4;
		public const int PausedState = 5;

		public const int PlayNow = 0;
		public const int PlayIfClear = 1;

		private AudioSourceControl source_ = null;
		private List<NamedAudioClip> clips_ = new List<NamedAudioClip>();
		private ShuffledOrder clipOrder_ = new ShuffledOrder();
		private int currentIndex_ = -1;
		private NamedAudioClip currentClip_ = null;
		private bool newClip_ = false;
		private readonly ExplicitHolder<IDuration> delay_ =
			new ExplicitHolder<IDuration>();
		private bool inDelay_ = false;
		private bool needsDelay_ = false;
		private int state_ = StartingState;
		private int playType_ = PlayNow;

		public AudioModifier(Atom a = null)
		{
			Atom = a;
			Delay = new RandomDuration(1);
			CheckSource();
		}

		public static string FactoryTypeName { get; } = "audio";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Audio";
		public override string GetDisplayName() { return DisplayName; }

		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new AudioModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		private void CopyTo(AudioModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
			m.source_ = source_;
			m.clips_ = new List<NamedAudioClip>(clips_);
			m.clipOrder_ = clipOrder_.Clone();
			m.currentIndex_ = currentIndex_;
			m.Delay = Delay?.Clone(cloneFlags);
		}

		public override void Removed()
		{
			base.Removed();
			Delay = null;
		}


		public override FloatRange PreferredRange
		{
			get
			{
				return new FloatRange(0, 1);
			}
		}

		public List<NamedAudioClip> Clips
		{
			get
			{
				return new List<NamedAudioClip>(clips_);
			}

			set
			{
				clips_ = new List<NamedAudioClip>(value);
				Reshuffle();
			}
		}

		public NamedAudioClip CurrentClip
		{
			get
			{
				return currentClip_;
			}
		}

		public AudioSourceControl Source
		{
			get
			{
				return source_;
			}
		}

		public IDuration Delay
		{
			get
			{
				return delay_.HeldValue;
			}

			set
			{
				delay_.HeldValue?.Removed();
				delay_.Set(value);

				if (value == null)
					inDelay_ = false;
			}
		}

		public int State
		{
			get
			{
				return state_;
			}
		}

		public int PlayType
		{
			get { return playType_; }
			set { playType_ = value; }
		}


		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			if (clips_.Count > 0)
			{
				var clipsArray = new J.Array();
				foreach (var clip in clips_)
					clipsArray.Add(clip.uid);

				o.Add("clips", clipsArray);
			}

			o.Add("playType", playType_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("AudioModifier");
			if (o == null)
				return false;

			if (o.HasChildArray("clips"))
			{
				var clipsArray = o.Get("clips").AsArray();

				if (clipsArray != null)
				{
					var cm = URLAudioClipManager.singleton;

					clipsArray.ForEach((clipNode) =>
					{
						var clipUID = clipNode?.AsString("Clip node");
						if (string.IsNullOrEmpty(clipUID))
							return;

						var clip = cm.GetClip(clipUID);

						if (clip == null)
							Synergy.LogError("clip '" + clipUID + "' not found");
						else
							clips_.Add(clip);
					});

					Reshuffle();
				}
			}

			o.Opt("playType", ref playType_);

			return true;
		}

		protected override void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			if (inDelay_)
			{
				Delay.Tick(deltaTime);

				if (Delay.Finished)
				{
					inDelay_ = false;
					Delay.Reset();
				}

				return;
			}


			base.DoTick(deltaTime, progress, firstHalf);

			if (source_ == null)
			{
				state_ = NoSourceState;
				return;
			}

			if (currentClip_ != null)
			{
				if (source_.audioSource.isPlaying || source_.audioSource.time > 0)
				{
					state_ = PlayingState;
					return;
				}
			}

			if (clips_.Count == 0)
			{
				state_ = NoClipsState;
				return;
			}

			if (newClip_)
			{
				state_ = PlayingState;
				return;
			}

			if (needsDelay_)
			{
				needsDelay_ = false;
				inDelay_ = true;
				state_ = InDelayState;
				return;
			}

			++currentIndex_;
			if (currentIndex_ >= clipOrder_.Count)
				Reshuffle();

			currentClip_ = clips_[clipOrder_.Get(currentIndex_)];
			newClip_ = true;
			needsDelay_ = true;

			state_ = PlayingState;
		}

		protected override void DoTickPaused(float deltaTime)
		{
			base.DoTickPaused(deltaTime);

			if (delay_.HeldValue != null && inDelay_)
				Delay.Tick(deltaTime);
		}

		protected override void DoSet(bool paused)
		{
			base.DoSet(paused);

			if (inDelay_)
				return;

			if (newClip_)
			{
				switch (playType_)
				{
					case PlayIfClear:
						Synergy.LogVerbose("play if clear: " + currentClip_.displayName);
						source_.PlayIfClear(currentClip_);
						break;

					case PlayNow:  // fall-through
					default:
						Synergy.LogVerbose("play now: " + currentClip_.displayName);
						source_.PlayNow(currentClip_);
						break;
				}

				if (source_.playingClip == currentClip_)
				{
					source_.audioSource.time = 0;
					newClip_ = false;
				}
			}
		}

		public void StopAudio()
		{
			if (source_ != null)
				source_.Stop();
		}

		public void AddClip(NamedAudioClip c)
		{
			if (c != null)
			{
				clips_.Add(c);
				Reshuffle();
			}
		}

		public void RemoveClip(NamedAudioClip c)
		{
			if (c != null && clips_.Contains(c))
			{
				if (currentClip_ == c)
				{
					StopAudio();
					needsDelay_ = false;
					currentClip_ = null;
				}

				clips_.Remove(c);
				Reshuffle();
			}
		}


		protected override string MakeName()
		{
			return "Audio";
		}

		protected override void AtomChanged()
		{
			CheckSource();
		}

		private void CheckSource()
		{
			source_ = Utilities.AtomAudioSource(Atom);
		}


		private void Reshuffle()
		{
			clipOrder_.Shuffle(clips_.Count);
			currentIndex_ = 0;
		}
	}
}
