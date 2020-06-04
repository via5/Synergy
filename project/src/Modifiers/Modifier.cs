using System;
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
		void AboutToBeRemoved();
	}

	class ModifierFactory : BasicFactory<IModifier>
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
		private IModifierSync sync_ = null;

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
				return sync_;
			}

			set
			{
				if (sync_ != null)
					sync_.ParentModifier = null;

				sync_ = value;

				if (sync_ != null)
					sync_.ParentModifier = this;
			}
		}

		public virtual bool Finished
		{
			get
			{
				if (sync_ == null)
					return true;

				return sync_.Finished;
			}
		}

		public virtual float TimeRemaining
		{
			get
			{
				if (sync_ == null)
					return 0;

				return sync_.TimeRemaining;
			}
		}

		public virtual void Stop(float timeRemaining)
		{
			if (sync_ != null)
				sync_.StopWhenFinished(timeRemaining);
		}

		protected void CopyTo(BasicModifier m, int cloneFlags)
		{
			m.sync_ = sync_?.Clone(cloneFlags);
		}

		public virtual void Reset()
		{
			if (sync_ != null)
				sync_.Resume();

			sync_?.Reset();
		}

		public void Tick(float deltaTime, float stepProgress, bool stepFirstHalf)
		{
			if (sync_ == null)
				return;

			if (sync_.Tick(deltaTime))
			{
				DoTick(
					deltaTime,
					sync_.GetProgress(this, stepProgress, stepFirstHalf),
					sync_.IsInFirstHalf(this, stepProgress, stepFirstHalf));

				sync_.PostTick();
			}
		}

		public void Resume()
		{
			if (sync_ != null)
				sync_.Resume();

			DoResume();
		}

		public void TickPaused(float deltaTime)
		{
			sync_.TickPaused(deltaTime);
			DoTickPaused(deltaTime);
		}

		public void TickDelayed(float deltaTime, float stepProgress, bool stepFirstHalf)
		{
			if (sync_.TickDelayed(deltaTime))
				Tick(deltaTime, stepProgress, stepFirstHalf);
		}

		public void Set(bool paused)
		{
			DoSet(paused);
		}

		public virtual void AboutToBeRemoved()
		{
			// no-op
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
			o.Add("sync", sync_);

			return o;
		}

		public virtual bool FromJSON(J.Node n)
		{
			var o = n.AsObject("BasicModifier");
			if (o == null)
				return false;

			o.Opt("name", ref name_);
			o.Opt<ModifierSyncFactory, IModifierSync>("sync", ref sync_);

			if (sync_ != null)
				sync_.ParentModifier = this;

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
					if (J.Node.SaveType == SaveTypes.Preset)
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
		private Movement movement_ = null;

		public Movement Movement
		{
			get { return movement_; }
			set { movement_ = value; }
		}

		protected AtomWithMovementModifier()
		{
			movement_ = new Movement(0, 0);
		}

		protected void CopyTo(AtomWithMovementModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
			m.movement_ = movement_.Clone(cloneFlags);
		}

		public override void Reset()
		{
			base.Reset();
			movement_.Reset();
		}

		protected override void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			base.DoTick(deltaTime, progress, firstHalf);
			movement_.Tick(deltaTime, progress, firstHalf);
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("movement", movement_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("AtomWithMovementModifier");
			if (o == null)
				return false;

			o.Opt("movement", ref movement_);

			return true;
		}

		protected void FirePreferredRangeChanged()
		{
			PreferredRangeChanged?.Invoke();
		}
	}
}
