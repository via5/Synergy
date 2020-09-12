using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Synergy
{
	sealed class Synergy : MVRScript
	{
		private static Synergy instance_ = null;
		private SuperController sc_ = null;
		private bool enabled_ = false;
		private bool frozen_ = false;
		private Manager manager_ = null;
		private Options options_ = null;
		private TimerManager timers_ = null;
		private MainUI ui_ = null;
		private List<IParameter> parameters_ = null;

		private bool deferredInitDone_ = false;
		private bool deferredUIDone_ = false;


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

		public override void Init()
		{
			base.Init();

			instance_ = this;
			sc_ = SuperController.singleton;
			enabled_ = false;
			frozen_ = false;
			options_ = new Options();
			timers_ = new TimerManager();
			ui_ = null;
			parameters_ = new List<IParameter>();
			manager_ = new Manager();

			deferredInitDone_ = false;
			deferredUIDone_ = false;

			SuperController.singleton.StartCoroutine(DeferredInit());
		}

		private IEnumerator DeferredInit()
		{
			yield return new WaitForEndOfFrame();

			if (this == null)
				yield break;

			while (SuperController.singleton.isLoading)
			{
				yield return 0;
				if (this == null)
					yield break;
			}

			manager_.DeferredInit();
			deferredInitDone_ = true;
		}

		public void Start()
		{
			deferredInitDone_ = false;
			deferredUIDone_ = false;

			Utilities.Handler(() =>
			{
				//LogError("===starting===");

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
			//options_.OverlapTime = 0;

			{
				var s = new Step();
				s.Duration = new RandomDuration(3);

				//var m = new RigidbodyModifier(a, "hip");
				//m.Movement = new Movement(0, 150);
				//m.Direction = new Vector3(0, 0, 1);
				//s.AddModifier(new ModifierContainer(m));

				//var m = new StorableModifier(
				//	a, "plugin#1_VamTimeline.AtomPlugin", "Set Time");
				//
				//m.Movement = new Movement(0, 2);
				//s.AddModifier(new ModifierContainer(m, new StepProgressSyncedModifier()));

				var m = new MorphModifier(a);
//				((OrderedMorphProgression)m.Progression).HoldHalfway = true;
				m.AddMorph("Right Fingers Fist", new Movement(0, 1));
				m.AddMorph("Smile Open Full Face", new Movement(0, 0.6f));
				s.AddModifier(new ModifierContainer(m));

				manager_.AddStep(s);
			}
			/*
			{
				var s = new Step();
				s.Duration = new RandomDuration(1);

				var m = new RigidbodyModifier(a, "head");
				m.Movement = new Movement(0, 150);
				m.Direction = new Vector3(1, 0, 0);
				s.AddModifier(new ModifierContainer(m));

				manager_.AddStep(s);
			}

			{
				var s = new Step();
				s.Duration = new RandomDuration(3);

				var m = new RigidbodyModifier(a, "lHand");
				m.Movement = new Movement(0, 150);
				m.Direction = new Vector3(0, 1, 0);
				s.AddModifier(new ModifierContainer(m));

				manager_.AddStep(s);
			}*/
		}

		public Timer CreateTimer(float seconds, Timer.Callback f)
		{
			return timers_.CreateTimer(seconds, f);
		}

		public void RemoveTimer(Timer t)
		{
			timers_.RemoveTimer(t);
		}

		public void RegisterParameter(BoolParameter p)
		{
			RegisterBool(p.Storable);
			RegisterFloat(p.StorableFloat);
			parameters_.Add(p);
		}

		public void UnregisterParameter(BoolParameter p)
		{
			DeregisterBool(p.Storable);
			DeregisterFloat(p.StorableFloat);
			parameters_.Remove(p);
		}

		public void RegisterParameter(FloatParameter p)
		{
			RegisterFloat(p.Storable);
			parameters_.Add(p);
		}

		public void UnregisterParameter(FloatParameter p)
		{
			DeregisterFloat(p.Storable);
			parameters_.Remove(p);
		}

		public void RegisterParameter(IntParameter p)
		{
			RegisterFloat(p.Storable);
			parameters_.Add(p);
		}

		public void UnregisterParameter(IntParameter p)
		{
			DeregisterFloat(p.Storable);
			parameters_.Remove(p);
		}

		public IParameter FindParameter(string name)
		{
			foreach (var p in parameters_)
			{
				if (p.Name == name)
					return p;
			}

			return null;
		}

		public string MakeParameterName(string baseName)
		{
			var p = FindParameter(baseName);
			if (p == null)
				return baseName;

			for (int i = 1; i < 100; ++i)
			{
				string name = baseName + " (" + i.ToString() + ")";

				p = FindParameter(name);
				if (p == null)
					return name;
			}

			return Guid.NewGuid().ToString();
		}

		public List<IParameter> Parameters
		{
			get { return parameters_; }
		}

		public void Update()
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

		public void FixedUpdate()
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

		public void OnGUI()
		{
			if (!enabled_)
				return;

			Utilities.Handler(() =>
			{
				if (deferredInitDone_ && !deferredUIDone_)
				{
					ui_.DeferredInit();
					deferredUIDone_ = true;
				}

				timers_.CheckTimers();
				ui_.Update();
			});
		}

		public void OnEnable()
		{
			Utilities.Handler(() =>
			{
				if (ui_ != null)
					ui_.PluginEnabled(true);
			});
		}

		public void OnDisable()
		{
			Utilities.Handler(() =>
			{
				if (ui_ != null)
					ui_.PluginEnabled(false);
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


		static public void Log(int level, string s)
		{
			if (instance_ != null && level > instance_.options_.LogLevel)
				return;

			if (level <= Options.LogLevelWarn)
				SuperController.LogError(s);
			else
				SuperController.LogMessage(s);
		}

		static public void LogError(string s)
		{
			Log(Options.LogLevelError, s);
		}

		static public void LogErrorST(string s)
		{
			Log(
				Options.LogLevelError,
				s + "\n" + new StackTrace(1).ToString());
		}

		static public void LogWarning(string s)
		{
			Log(Options.LogLevelWarn, s);
		}

		static public void LogInfo(string s)
		{
			Log(Options.LogLevelInfo, s);
		}

		static public void LogVerbose(string s)
		{
			Log(Options.LogLevelVerbose, s);
		}

		static public void LogOverlap(string s)
		{
			if (Instance.options_.LogOverlap)
				SuperController.LogMessage(s);
		}
	}
}
