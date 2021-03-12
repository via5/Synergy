using System;
using System.Collections.Generic;
using System.Text;

namespace Synergy
{
	public class Integration
	{
		public const int SettingIgnore = 0;
		public const int SettingEnable = 1;
		public const int SettingDisable = 2;

		public class Blink : IJsonable
		{
			private Atom atom_ = null;
			private JSONStorableBool toggle_ = null;
			private int setting_ = SettingIgnore;

			public Blink Clone(int cloneFlags = 0)
			{
				var b = new Blink();
				CopyTo(b, cloneFlags);
				return b;
			}

			private void CopyTo(Blink b, int cloneFlags)
			{
				b.atom_ = atom_;
				b.toggle_ = toggle_;
				b.setting_ = setting_;
			}

			public Atom Atom
			{
				get
				{
					return atom_;
				}

				set
				{
					if (atom_ != value)
					{
						atom_ = value;
						toggle_ = null;
					}
				}
			}

			public int Setting
			{
				get { return setting_; }
				set { setting_ = value; }
			}

			public bool Available
			{
				get
				{
					EnsureToggle();
					return (toggle_ != null);
				}
			}

			public void Check()
			{
				bool e;

				if (setting_ == SettingEnable)
					e = true;
				else if (setting_ == SettingDisable)
					e = false;
				else
					return;

				if (!SetEnabled(e))
				{
					Synergy.LogError(
						"blink: can't set value, changing setting to Ignore");

					setting_ = SettingIgnore;
					return;
				}
			}

			public J.Node ToJSON()
			{
				var o = new J.Object();

				o.Add("setting", setting_);

				return o;
			}

			public bool FromJSON(J.Node n)
			{
				var o = n.AsObject("Blink");
				if (o == null)
					return false;

				o.Opt("setting", ref setting_);

				return true;
			}

			private bool SetEnabled(bool b)
			{
				EnsureToggle();
				if (toggle_ == null)
					return false;

				try
				{
					toggle_.val = b;
					return true;
				}
				catch (Exception ex)
				{
					Synergy.LogError("blink: failed to set value, " + ex.Message);
					toggle_ = null;
					return false;
				}
			}

			private void EnsureToggle()
			{
				if (toggle_ == null)
				{
					if (atom_ == null)
						return;

					toggle_ = GetToggle();
				}
			}

			private JSONStorableBool GetToggle()
			{
				var ec = Atom.GetStorableByID("EyelidControl");
				if (ec == null)
				{
					Synergy.LogError(
						"blink: EyelidControl not found in atom " +
						"'" + Atom.uid + "'");

					return null;
				}

				var param = ec.GetBoolJSONParam("blinkEnabled");
				if (param == null)
				{
					Synergy.LogError(
						"blink: can't find blinkEnabled in atom " +
						"'" + Atom.uid + "'");
					return null;
				}

				return param;
			}
		}


		public class Gaze : IJsonable
		{
			private Atom atom_ = null;
			private JSONStorableBool toggle_ = null;
			private int setting_ = SettingIgnore;

			public Gaze Clone(int cloneFlags = 0)
			{
				var g = new Gaze();
				CopyTo(g, cloneFlags);
				return g;
			}

			private void CopyTo(Gaze g, int cloneFlags)
			{
				g.atom_ = atom_;
				g.toggle_ = toggle_;
				g.setting_ = setting_;
			}

			public Atom Atom
			{
				get
				{
					return atom_;
				}

				set
				{
					if (atom_ != value)
					{
						atom_ = value;
						toggle_ = null;
					}
				}
			}

			public int Setting
			{
				get { return setting_; }
				set { setting_ = value; }
			}

			public void Check()
			{
				bool e;

				if (setting_ == SettingEnable)
					e = true;
				else if (setting_ == SettingDisable)
					e = false;
				else
					return;

				if (!SetEnabled(e))
				{
					Synergy.LogError(
						"gaze: can't set value, changing setting to Ignore");

					setting_ = SettingIgnore;
				}
			}

			public bool Available
			{
				get
				{
					EnsureToggle();
					return (toggle_ != null);
				}
			}

			public J.Node ToJSON()
			{
				var o = new J.Object();

				o.Add("setting", setting_);

				return o;
			}

			public bool FromJSON(J.Node n)
			{
				var o = n.AsObject("Blink");
				if (o == null)
					return false;

				o.Opt("setting", ref setting_);

				return true;
			}

			private bool SetEnabled(bool b)
			{
				EnsureToggle();
				if (toggle_ == null)
					return false;

				try
				{
					toggle_.val = b;
					return true;
				}
				catch (Exception)
				{
					Synergy.LogError(
						"gaze: failed to change value, " +
						"assuming script is gone");

					toggle_ = null;
				}

				return false;
			}

			private void EnsureToggle()
			{
				if (toggle_ == null)
				{
					if (atom_ == null)
						return;

					toggle_ = GetToggle();
				}
			}

			private JSONStorableBool GetToggle()
			{
				Synergy.LogVerbose("gaze: searching for enabled parameter");

				foreach (var id in atom_.GetStorableIDs())
				{
					if (id.Contains("MacGruber.Gaze"))
					{
						var st = atom_.GetStorableByID(id);
						if (st == null)
						{
							Synergy.LogError("gaze: can't find storable " + id);
							continue;
						}

						var en = st.GetBoolJSONParam("enabled");
						if (en == null)
						{
							Synergy.LogError("gaze: no enabled param");
							continue;
						}

						return en;
					}
				}

				return null;
			}
		}


		public class Timeline
		{
			private Atom atom_ = null;
			private JSONStorable tl_ = null;
			private JSONStorableBool playing_ = null;
			private JSONStorableFloat remaining_ = null;
			private bool stale_ = false;

			public Timeline Clone()
			{
				var tl = new Timeline();
				CopyTo(tl);
				return tl;
			}

			private void CopyTo(Timeline tl)
			{
			}


			public Atom Atom
			{
				get
				{
					return atom_;
				}

				set
				{
					if (atom_ != value)
					{
						atom_ = value;
						stale_ = true;
					}
				}
			}

			public bool IsPlaying
			{
				get
				{
					Ensure();

					if (playing_ == null)
						return false;
					else
						return playing_.val;
				}
			}

			public void Play(string anim)
			{
				Ensure();

				var a = FindPlayAction(anim);
				if (a == null)
				{
					Synergy.LogError("timeline animation '" + anim + "' not found");
					return;
				}

				try
				{
					Synergy.LogError("playing '" + anim + "'");
					a.actionCallback?.Invoke();
				}
				catch (Exception e)
				{
				}
			}

			private void Ensure()
			{
				if (atom_ == null || !stale_)
					return;

				Synergy.LogError("timeline: ensure");

				tl_ = FindPlugin();
				if (tl_ == null)
					return;

				playing_ = tl_.GetBoolJSONParam("Is Playing");
				if (playing_ == null)
					Synergy.LogError("'Is Playing' param not found");

				remaining_ = tl_.GetFloatJSONParam("Time Remaining");
				if (remaining_ == null)
					Synergy.LogError("'Time Remaining' not found");

				Synergy.LogError("timeline: ensure ok");
				stale_ = false;
			}

			private JSONStorable FindPlugin()
			{
				foreach (var id in atom_.GetStorableIDs())
				{
					if (id.Contains("VamTimeline.AtomPlugin"))
						return atom_.GetStorableByID(id);
				}

				return null;
			}

			private JSONStorableAction FindPlayAction(string anim)
			{
				if (tl_ == null)
					return null;

				return tl_.GetAction("Play " + anim);
			}
		}
	}
}
