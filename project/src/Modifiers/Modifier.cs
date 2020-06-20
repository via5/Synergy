﻿using System;
using System.Collections.Generic;

namespace Synergy
{
	public delegate void ModifierNameChangedHandler();
	public delegate void PreferredRangeChangedHandler();

	interface IModifier : IFactoryObject
	{
		event ModifierNameChangedHandler NameChanged;

		bool Finished { get; }
		float TimeRemaining { get; }
		Step ParentStep { get; set; }
		string Name { get; }
		FloatRange PreferredRange { get; }
		IModifierSync ModifierSync { get; set; }

		IModifier Clone(int cloneFlags = 0);

		void Resume();
		void Reset();
		void Tick(float deltaTime, float progress, bool firstHalf);
		void TickPaused(float deltaTime);
		void TickDelayed(float deltaTime, float stepProgress, bool stepFirstHalf);
		void Set(bool paused);
		void Stop(float timeRemaining);
		void Removed();
	}

	sealed class ModifierFactory : BasicFactory<IModifier>
	{
		public override List<IModifier> GetAllObjects()
		{
			return new List<IModifier>()
			{
				new RigidbodyModifier(),
				new MorphModifier(),
				new LightModifier(),
				new AudioModifier()
			};
		}
	}

	abstract class BasicModifier : IModifier
	{
		public event ModifierNameChangedHandler NameChanged;

		private string name_ = null;
		private Step parent_ = null;

		private readonly ExplicitHolder<IModifierSync> sync_ =
			new ExplicitHolder<IModifierSync>();

		protected BasicModifier()
		{
			ModifierSync = new DurationSyncedModifier();
		}

		public abstract IModifier Clone(int cloneFlags = 0);
		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		public abstract FloatRange PreferredRange { get; }

		public Step ParentStep
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public string UserDefinedName
		{
			get { return name_; }
			set { name_ = value; }
		}

		public string Name
		{
			get
			{
				if (name_ == null)
				{
					if (parent_ == null)
					{
						return "Modifier";
					}
					else
					{
						var i = parent_.IndexOfModifier(this);
						return (i + 1).ToString() + ") " + MakeName();
					}
				}
				else
				{
					return name_;
				}
			}
		}

		public IModifierSync ModifierSync
		{
			get
			{
				return sync_.HeldValue;
			}

			set
			{
				sync_.HeldValue?.Removed();
				sync_.Set(value);

				if (sync_.HeldValue != null)
					sync_.HeldValue.ParentModifier = this;
			}
		}

		public virtual bool Finished
		{
			get
			{
				if (ModifierSync == null)
					return true;

				return ModifierSync.Finished;
			}
		}

		public virtual float TimeRemaining
		{
			get
			{
				if (ModifierSync == null)
					return 0;

				return ModifierSync.TimeRemaining;
			}
		}

		public virtual void Stop(float timeRemaining)
		{
			if (ModifierSync != null)
				ModifierSync.StopWhenFinished(timeRemaining);
		}

		protected void CopyTo(BasicModifier m, int cloneFlags)
		{
			m.ModifierSync = ModifierSync?.Clone(cloneFlags);
		}

		public virtual void Removed()
		{
			ParentStep = null;
			ModifierSync = null;
		}

		public virtual void Reset()
		{
			if (ModifierSync != null)
			{
				ModifierSync.Resume();
				ModifierSync.Reset();
			}
		}

		public void Tick(float deltaTime, float stepProgress, bool stepFirstHalf)
		{
			if (sync_ == null)
				return;

			if (ModifierSync.Tick(deltaTime))
			{
				DoTick(
					deltaTime,
					ModifierSync.GetProgress(this, stepProgress, stepFirstHalf),
					ModifierSync.IsInFirstHalf(this, stepProgress, stepFirstHalf));

				ModifierSync.PostTick();
			}
		}

		public void Resume()
		{
			if (ModifierSync != null)
				ModifierSync.Resume();

			DoResume();
		}

		public void TickPaused(float deltaTime)
		{
			ModifierSync.TickPaused(deltaTime);
			DoTickPaused(deltaTime);
		}

		public void TickDelayed(float deltaTime, float stepProgress, bool stepFirstHalf)
		{
			if (ModifierSync.TickDelayed(deltaTime))
				Tick(deltaTime, stepProgress, stepFirstHalf);
		}

		public void Set(bool paused)
		{
			DoSet(paused);
		}

		public override string ToString()
		{
			return Name;
		}

		protected abstract string MakeName();

		protected virtual void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			// no-op
		}

		protected virtual void DoTickPaused(float deltaTime)
		{
			// no-op
		}

		protected virtual void DoResume()
		{
			// no-op
		}

		protected virtual void DoSet(bool paused)
		{
			// no-op
		}

		protected void FireNameChanged()
		{
			NameChanged?.Invoke();
		}

		public virtual J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("name", name_);
			o.Add("sync", ModifierSync);

			return o;
		}

		public virtual bool FromJSON(J.Node n)
		{
			var o = n.AsObject("BasicModifier");
			if (o == null)
				return false;

			o.Opt("name", ref name_);

			IModifierSync s = null;
			o.Opt<ModifierSyncFactory, IModifierSync>("sync", ref s);
			ModifierSync = s;

			return true;
		}
	}

	abstract class AtomModifier : BasicModifier, IDisposable
	{
		private Atom atom_ = null;

		public Atom Atom
		{
			get
			{
				return atom_;
			}

			set
			{
				atom_ = value;
				AtomChanged();
				FireNameChanged();
			}
		}

		protected AtomModifier()
		{
			atom_ = Synergy.Instance.DefaultAtom;

			SuperController.singleton.onAtomUIDRenameHandlers +=
				OnAtomUIDChanged;
		}

		protected void CopyTo(AtomModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
			m.atom_ = atom_;
		}

		public void Dispose()
		{
			SuperController.singleton.onAtomUIDRenameHandlers -=
				OnAtomUIDChanged;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			if (Atom != null)
			{
				if (J.Node.SaveType == SaveTypes.Preset)
					o.Add("atom", Utilities.PresetAtomPlaceholder);
				else
					o.Add("atom", Atom.uid);
			}

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("AtomModifier");
			if (o == null)
				return false;

			if (o.HasKey("atom"))
			{
				var atomUID = o.Get("atom").AsString();
				if (atomUID != null)
				{
					if (atomUID == Utilities.PresetAtomPlaceholder)
						Atom = Synergy.Instance.DefaultAtom;
					else
						Atom = SuperController.singleton.GetAtomByUid(atomUID);

					if (Atom == null)
						Synergy.LogError("atom '" + atomUID + "' not found");
				}
			}

			return true;
		}

		protected virtual void AtomChanged()
		{
			// no-op
		}

		private void OnAtomUIDChanged(string oldUID, string newUID)
		{
			if (atom_ != null)
			{
				if (atom_.name == newUID)
					FireNameChanged();
			}
		}
	}

	abstract class AtomWithMovementModifier : AtomModifier
	{
		public event PreferredRangeChangedHandler PreferredRangeChanged;
		private readonly ExplicitHolder<Movement> movement_ =
			new ExplicitHolder<Movement>();

		protected AtomWithMovementModifier()
		{
			Movement = new Movement(0, 0);
		}

		protected void CopyTo(AtomWithMovementModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
			m.Movement = Movement.Clone(cloneFlags);
		}

		public override void Removed()
		{
			base.Removed();
			Movement = null;
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

		public override void Reset()
		{
			base.Reset();
			Movement.Reset();
		}

		protected override void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			base.DoTick(deltaTime, progress, firstHalf);
			Movement.Tick(deltaTime, progress, firstHalf);
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("movement", Movement);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("AtomWithMovementModifier");
			if (o == null)
				return false;

			Movement m = null;
			o.Opt("movement", ref m);
			Movement = m;

			return true;
		}

		protected void FirePreferredRangeChanged()
		{
			PreferredRangeChanged?.Invoke();
		}
	}
}
