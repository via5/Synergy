using System;
using System.Collections.Generic;
using System.Configuration;
using GPUTools.Common.Scripts.Utils;
using Leap;
using Leap.Unity;
using SimpleJSON;

namespace Synergy
{
	sealed class SelectedMorph : IJsonable
	{
		private const float NoMagnitude = float.MinValue;

		private Atom atom_ = null;
		private DAZMorph morph_ = null;

		private readonly BoolParameter enabled_ =
			new BoolParameter("Enabled", true);

		private readonly ExplicitHolder<Movement> movement_ =
			new ExplicitHolder<Movement>();

		private float magnitude_ = NoMagnitude;
		private bool moveToStart_ = false;


		public SelectedMorph()
		{
			morph_ = null;
			Movement = new Movement();
		}

		public static SelectedMorph Create(Atom atom, DAZMorph morph)
		{
			var sm = new SelectedMorph();

			sm.Atom = atom;
			sm.Morph = morph;

			return sm;
		}

		public FloatRange PreferredRange
		{
			get
			{
				if (morph_ == null)
					return new FloatRange(0, 1);
				else
					return new FloatRange(morph_.min, morph_.max);
			}
		}

		public Atom Atom
		{
			get { return atom_; }
			set { atom_ = value; }
		}

		public DAZMorph Morph
		{
			get { return morph_; }
			set { morph_ = value; }
		}

		public bool Enabled
		{
			get
			{
				return enabled_.Value;
			}

			set
			{
				enabled_.Value = value;

				if (!value)
				{
					ResetMorphValue();
					Reset();
				}
			}
		}

		public BoolParameter EnabledParameter
		{
			get { return enabled_; }
		}

		public Movement Movement
		{
			get
			{
				return movement_.HeldValue;
			}

			set
			{
				movement_.HeldValue?.Removed();
				movement_.Set(value);
			}
		}


		public string DisplayName
		{
			get
			{
				if (morph_ == null)
					return "";
				else
					return morph_.displayName;
			}
		}

		public SelectedMorph Clone(int cloneFlags)
		{
			var sm = new SelectedMorph();
			CopyTo(sm, cloneFlags);
			return sm;
		}

		public void CopyTo(SelectedMorph sm, int cloneFlags)
		{
			sm.atom_ = atom_;
			sm.morph_ = morph_;
			sm.enabled_.Value = enabled_.Value;
			sm.Movement = Movement?.Clone(cloneFlags);
			sm.magnitude_ = magnitude_;
		}

		public void Removed()
		{
			enabled_.Unregister();
			Movement = null;
			ResetMorphValue();
		}

		public void Resume()
		{
			// no-op
		}

		public void ResetMorphValue()
		{
			if (morph_ == null)
				return;

			SetInternal(morph_.startValue);
		}

		public void Reset()
		{
			magnitude_ = NoMagnitude;

			if (morph_ != null && Enabled)
			{
				var d = morph_.morphValue - morph_.startValue;

				if (Math.Abs(d) < 0.05f)
					SetInternal(morph_.startValue);
				else
					moveToStart_ = true;
			}
		}

		public void Tick(float deltaTime, float progress, bool firstHalf)
		{
			moveToStart_ = false;
			Movement.Tick(deltaTime, progress, firstHalf);
			magnitude_ = Movement.Magnitude;
		}

		public void TickPaused(float deltaTime)
		{
			if (morph_ == null)
				return;

			if (moveToStart_)
			{
				if (morph_.morphValue < morph_.startValue)
				{
					SetInternal(morph_.morphValue + deltaTime);
					if (morph_.morphValue >= (morph_.startValue - 0.01f))
					{
						morph_.morphValue = morph_.startValue;
						moveToStart_ = false;
					}
				}
				else
				{
					SetInternal(morph_.morphValue - deltaTime);
					if (morph_.morphValue <= (morph_.startValue + 0.01f))
					{
						morph_.morphValue = morph_.startValue;
						moveToStart_ = false;
					}
				}
			}
		}

		public void Set()
		{
			if (!Enabled)
				return;

			if (magnitude_ == NoMagnitude)
				Reset();
			else if (morph_ != null)
				SetInternal(magnitude_);
		}

		private void SetInternal(float f)
		{
			morph_.morphValue = f;
		}


		public J.Node ToJSON()
		{
			var o = new J.Object();

			if (morph_ != null)
				o.Add("uid", morph_.uid);

			o.Add("movement", Movement);
			o.Add("enabled", enabled_);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			var o = n.AsObject("SelectedMorph");
			if (o == null)
				return false;

			string uid = "";
			o.Opt("uid", ref uid);

			if (Atom != null && uid != "")
			{
				morph_ = Utilities.FindMorph(Atom, uid);

				if (morph_ == null)
				{
					Synergy.LogError(
						"morph '" + uid + "' not found in " +
						"atom '" + Atom.uid + "'");

					return false;
				}
			}

			Movement m = null;
			o.Opt("movement", ref m);
			Movement = m;

			o.Opt("enabled", enabled_);

			return true;
		}
	}


	sealed class MorphModifier : AtomModifier
	{
		private readonly List<SelectedMorph> morphs_ =
			new List<SelectedMorph>();

		private readonly ExplicitHolder<IMorphProgression> progression_ =
			new ExplicitHolder<IMorphProgression>();


		public static string FactoryTypeName { get; } = "morph";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Morph";
		public override string GetDisplayName() { return DisplayName; }


		public MorphModifier()
		{
			Progression = new SequentialMorphProgression();

			if (!Utilities.AtomHasMorphs(Atom))
				Atom = null;
		}

		public MorphModifier(Atom a, DAZMorph m = null)
		{
			Progression = new SequentialMorphProgression();
			Atom = a;

			if (m != null)
				AddMorph(m);
		}

		public override FloatRange PreferredRange
		{
			get
			{
				if (morphs_.Count > 0)
					return morphs_[0].PreferredRange;
				else
					return new FloatRange(0, 1);
			}
		}

		public List<SelectedMorph> Morphs
		{
			get
			{
				return morphs_;
			}
		}

		public IMorphProgression Progression
		{
			get
			{
				return progression_.HeldValue;
			}

			set
			{
				if (progression_.HeldValue != null)
				{
					progression_.HeldValue.ParentModifier = null;
					progression_.HeldValue.Removed();
				}

				progression_.Set(value);

				if (progression_.HeldValue != null)
				{
					progression_.HeldValue.ParentModifier = this;
					progression_.HeldValue.Morphs = morphs_;
					progression_.HeldValue.MorphsChanged();
				}
			}
		}

		public override bool Finished
		{
			get
			{
				if (Progression.HasOwnDuration)
					return Progression.Finished;
				else
					return base.Finished;
			}
		}

		public override float TimeRemaining
		{
			get
			{
				if (Progression.HasOwnDuration)
					return Progression.TimeRemaining;
				else
					return base.TimeRemaining;
			}
		}

		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new MorphModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		private void CopyTo(MorphModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);

			foreach (var sm in morphs_)
				m.morphs_.Add(sm.Clone(cloneFlags));

			m.Progression = Progression.Clone(cloneFlags);
		}

		public override void PluginEnabled(bool b)
		{
			if (!b)
			{
				foreach (var m in morphs_)
					m.ResetMorphValue();
			}
		}

		public override void Removed()
		{
			base.Removed();

			Progression = null;

			foreach (var m in morphs_)
				m.Removed();
		}

		public SelectedMorph AddMorph(string id, Movement mv = null)
		{
			if (Atom == null)
			{
				Synergy.LogError("AddMorph: no atom");
				return null;
			}

			var m = Utilities.FindMorph(Atom, id);
			if (m == null)
			{
				Synergy.LogError(
					$"AddMorph: morph '{id}' not found in " +
					$"atom '{Atom.name}'");

				return null;
			}

			return AddMorph(m, mv);
		}

		public SelectedMorph AddMorph(string id, FloatRange r)
		{
			return AddMorph(id, new Movement(r));
		}

		public SelectedMorph AddMorph(DAZMorph m)
		{
			return AddMorph(m, new Movement());
		}

		public SelectedMorph AddMorph(DAZMorph m, FloatRange r)
		{
			return AddMorph(m, new Movement(r));
		}

		public SelectedMorph AddMorph(DAZMorph m, Movement mv)
		{
			if (m == null)
				return null;

			var sm = SelectedMorph.Create(Atom, m);

			if (mv != null)
				sm.Movement = mv;

			AddMorph(sm);

			return sm;
		}

		public void AddMorph(SelectedMorph sm)
		{
			morphs_.Add(sm);
			Progression.MorphAdded(morphs_.Count - 1);
			FireNameChanged();
		}

		public void RemoveMorph(DAZMorph m)
		{
			for (int i = 0; i < morphs_.Count; ++i)
			{
				if (morphs_[i].Morph == m)
				{
					var sm = morphs_[i];
					morphs_.RemoveAt(i);

					sm.Removed();
					Progression.MorphRemoved(i);

					break;
				}
			}

			FireNameChanged();
		}

		public bool HasMorph(DAZMorph m)
		{
			foreach (var sm in morphs_)
			{
				if (sm.Morph == m)
					return true;
			}

			return false;
		}

		public void SetMorphs(List<DAZMorph> morphs)
		{
			var fixedMorphs = new List<SelectedMorph>();

			foreach (var m in morphs)
			{
				bool found = false;

				foreach (var sm in morphs_)
				{
					if (sm.Morph == m)
					{
						fixedMorphs.Add(sm);
						found = true;
						break;
					}
				}

				if (!found)
				{
					var nsm = SelectedMorph.Create(Atom, m);
					nsm.Movement = new Movement(0, 1);
					fixedMorphs.Add(nsm);
				}
			}


			int i = 0;
			while (i < morphs_.Count)
			{
				if (fixedMorphs.Contains(morphs_[i]))
				{
					++i;
				}
				else
				{
					morphs_[i].Removed();
					morphs_.RemoveAt(i);
				}
			}

			morphs_.Clear();
			morphs_.AddRange(fixedMorphs);

			Progression.MorphsChanged();
			FireNameChanged();
		}

		protected override void AtomChanged()
		{
			base.AtomChanged();

			var fixedList = new List<SelectedMorph>();

			if (Atom != null)
			{
				foreach (var sm in morphs_)
				{
					var newMorph = Utilities.FindMorphInNewAtom(Atom, sm.Morph);
					if (newMorph == null)
					{
						sm.Removed();
						continue;
					}

					sm.ResetMorphValue();
					sm.Reset();
					sm.Atom = Atom;
					sm.Morph = newMorph;

					fixedList.Add(sm);
				}
			}

			morphs_.Clear();
			morphs_.AddRange(fixedList);

			Progression.MorphsChanged();
		}

		protected override void DoResume()
		{
			Progression.Resume();
		}

		protected override void DoSet(bool paused)
		{
			base.DoSet(paused);
			Progression.Set(paused);
		}

		protected override void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			base.DoTick(deltaTime, progress, firstHalf);
			Progression.Tick(deltaTime, progress, firstHalf);
		}

		protected override void DoTickPaused(float deltaTime)
		{
			base.DoTickPaused(deltaTime);

			if (Progression.HasOwnDuration)
				Progression.Stop();

			Progression.TickPaused(deltaTime);
		}

		public override void Reset()
		{
			base.Reset();
			Progression.Reset();

			if (!ParentContainer.Enabled)
			{
				foreach (var sm in morphs_)
					sm.ResetMorphValue();
			}
		}

		protected override string MakeName()
		{
			const int Max = 3;

			string n = "M ";

			if (Atom == null)
				n += "none";
			else
				n += Atom.name;

			for (int i = 0; i < Max; ++i)
			{
				if (i >= morphs_.Count)
					break;

				if (n != "")
					n += ", ";

				n += morphs_[i].Morph.displayName;
			}

			if (n == "")
				n = "none";

			return n;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("selectedMorphs", morphs_);
			o.Add("progression", Progression);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("MorphModifier");
			if (o == null)
				return false;

			if (Atom != null)
			{
				var morphsArray = o.Get("selectedMorphs").AsArray();

				if (morphsArray != null)
				{
					morphsArray.ForEach((morphNode) =>
					{
						var sm = SelectedMorph.Create(Atom, null);

						if (sm.FromJSON(morphNode))
							morphs_.Add(sm);
					});
				}

				IMorphProgression p = null;
				o.Opt<MorphProgressionFactory, IMorphProgression>(
					"progression", ref p);

				Progression = p;
			}

			return true;
		}
	}
}
