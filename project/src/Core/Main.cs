using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	class Synergy : MVRScript
	{
		private static Synergy instance_ = null;
		private readonly SuperController sc_ = SuperController.singleton;
		private bool enabled_ = false;
		private bool frozen_ = false;
		private Manager manager_ = new Manager();
		private Options options_ = new Options();
		private TimerManager timers_ = new TimerManager();
		private MainUI ui_ = null;
		private List<IParameter> parameters_ = new List<IParameter>();

		public Synergy()
		{
			instance_ = this;
		}

		public static Synergy Instance
		{
			get { return instance_; }
		}

		public MainUI UI
		{
			get { return ui_; }
		}

		public Atom DefaultAtom
		{
			get { return containingAtom; }
		}

		public Manager Manager
		{
			get { return manager_; }
		}

		public Options Options
		{
			get { return options_; }
		}

		public void Start()
		{
			Utilities.Handler(() =>
			{
				LogVerbose("starting");

				RegisterString(new JSONStorableString("dummy", ""));
				SetStringParamValue("dummy", "dummy");

				if (GetAtomById("synergytestatom") != null)
					CreateTestStuff(GetAtomById("synergytestatom"));

				ui_ = new MainUI();
				ui_.Create();

				LogVerbose("OK");
				enabled_ = true;
			});
		}

		private void CreateTestStuff(Atom a)
		{
			var s = new Step();

			//var rm = new RigidbodyModifier(a, Utilities.FindRigidbody(a, "head"));
			//rm.Movement.Maximum.Initial = 50;
			//s.AddModifier(new ModifierContainer(rm));

			var mm = new MorphModifier(a, Utilities.GetAtomMorph(a, "Mouth Open"));
			s.AddModifier(new ModifierContainer(mm));

			manager_.AddStep(s);
		}

		public Timer CreateTimer(float seconds, Timer.Callback callback)
		{
			return timers_.CreateTimer(seconds, callback);
		}

		public void RemoveTimer(Timer t)
		{
			timers_.RemoveTimer(t);
		}

		public void RegisterParameter(BoolParameter p)
		{
			RegisterBool(p.StorableBool);
			RegisterFloat(p.StorableFloat);
			parameters_.Add(p);
		}

		public void UnregisterParameter(BoolParameter p)
		{
			DeregisterBool(p.StorableBool);
			DeregisterFloat(p.StorableFloat);
			parameters_.Remove(p);
		}

		public List<IParameter> Parameters
		{
			get { return parameters_; }
		}

		protected void Update()
		{
			Utilities.Handler(() =>
			{
				DoUpdate(Time.deltaTime);
			});
		}

		private void DoUpdate(float deltaTime)
		{
			timers_.TickTimers(deltaTime);
		}

		protected void FixedUpdate()
		{
			if (!enabled_)
				return;

			Utilities.Handler(() =>
			{
				DoFixedUpdate(Time.deltaTime);
			});
		}

		private void DoFixedUpdate(float deltaTime)
		{
			bool tick = true;
			bool set = true;

			if (sc_.freezeAnimation)
			{
				if (options_.ResetValuesOnFreeze)
					set = false;

				tick = false;
				frozen_ = true;
			}
			else
			{
				if (frozen_)
				{
					if (options_.ResetCountersOnThaw)
						manager_.ResetAllSteps();

					frozen_ = false;
				}
			}

			if (tick)
				manager_.Tick(deltaTime);

			if (set)
				manager_.Set();
		}

		protected void OnGUI()
		{
			if (!enabled_)
				return;

			Utilities.Handler(() =>
			{
				timers_.CheckTimers();
				ui_.Update();
			});
		}

		public override JSONClass GetJSON(
			bool includePhysical = true,
			bool includeAppearance = true,
			bool forceStore = false)
		{
			JSONClass c = null;

			Utilities.Handler(() =>
			{
				c = base.GetJSON(includePhysical, includeAppearance);

				var o = J.Object.Wrap(c);
				J.Node.SaveType = SaveTypes.Scene;

				o.Add("version", Version.String);
				o.Add("options", options_);
				o.Add("manager", manager_);

				J.Node.SaveType = SaveTypes.None;
			});

			return c;
		}

		public override void RestoreFromJSON(
			JSONClass c,
			bool restorePhysical = true,
			bool restoreAppearance = true,
			JSONArray presetAtoms = null,
			bool setMissingToDefault = true)
		{
			Utilities.Handler(() =>
			{
				base.RestoreFromJSON(
					c, restorePhysical, restoreAppearance,
					presetAtoms, setMissingToDefault);

				var o = J.Object.Wrap(c);
				J.Node.SaveType = SaveTypes.Scene;

				o.Opt("options", ref options_);
				o.Opt("manager", ref manager_);

				J.Node.SaveType = SaveTypes.None;
			});
		}



		static public void LogError(string s)
		{
			SuperController.LogError(s);
		}

		static public void LogWarning(string s)
		{
			SuperController.LogError(s);
		}

		static public void LogInfo(string s)
		{
			SuperController.LogError(s);
		}

		static public void LogVerbose(string s)
		{
		   // if (instance_ == null || instance_.options_.VerboseLog)
		   //     SuperController.LogError(s);
		}
	}
}
