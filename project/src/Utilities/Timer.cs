﻿using System.Collections.Generic;

namespace Synergy
{
	class TimerManager
	{
		private readonly List<Timer> timers_ = new List<Timer>();

		public Timer CreateTimer(float seconds, Timer.Callback f, int flags = 0)
		{
			var t = new Timer(this, seconds, f, flags);
			timers_.Add(t);
			return t;
		}

		public void RemoveTimer(Timer t)
		{
			timers_.Remove(t);
		}

		public void TickTimers(float deltaTime)
		{
			foreach (var t in timers_)
				t.Tick(deltaTime);
		}

		public void CheckTimers()
		{
			for (int i = timers_.Count - 1; i >= 0; i--)
			{
				var t = timers_[i];
				if (t.Ready)
					t.Fire();
			}
		}
	}


	class Timer
	{
		public delegate void Callback();
		public const float Immediate = float.MinValue;

		public const int Repeat = 0x01;

		private readonly TimerManager manager_;
		private readonly float time_;
		private readonly Callback callback_;
		private readonly int flags_;
		private float elapsed_ = 0;

		public Timer(TimerManager tm, float seconds, Callback f, int flags = 0)
		{
			manager_ = tm;
			time_ = seconds;
			callback_ = f;
			flags_ = flags;
		}

		public void Restart()
		{
			elapsed_ = 0;
		}

		public void Fire()
		{
			Utilities.Handler(() =>
			{
				callback_?.Invoke();
			});

			if (Bits.IsSet(flags_, Repeat))
				Restart();
			else
				Destroy();
		}

		public void Destroy()
		{
			if (manager_ != null)
				manager_.RemoveTimer(this);
		}

		public void Tick(float deltaTime)
		{
			elapsed_ += deltaTime;
		}

		public bool Ready
		{
			get
			{
				if (time_ == Immediate)
					return (elapsed_ > 0);

				return (elapsed_ >= time_);
			}
		}
	}
}
