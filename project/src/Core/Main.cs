using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	sealed class Synergy : MVRScript
	{
		private static Synergy instance_ = null;
		private readonly SuperController sc_ = SuperController.singleton;
		private bool enabled_ = false;
		private bool frozen_ = false;
		private Manager manager_ = new Manager();
		private Options options_ = new Options();
		private readonly TimerManager timers_ = new TimerManager();
		private MainUI ui_ = null;
		private List<IParameter> parameters_ = new List<IParameter>();

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

		public override void Init()
		{
			base.Init();
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

		private void CreateTestStuff(Atom a)
		{
			//options_.OverlapTime = 0;
			/*
			var s = new Step();
			s.Duration = new RampDuration(1, 0.3f, 10, 5);


			var sm = new StorableModifier(
				a, "plugin#2_MacGruber.Breathing", "QueueOrgasm");

			sm.Movement = new Movement(0, 1);

			((ActionStorableParameter)sm.Parameter).TriggerType =
				ActionStorableParameter.TriggerDown;

			s.AddModifier(new ModifierContainer(
				sm, new StepProgressSyncedModifier()));


			sm = new StorableModifier(
				a, "plugin#2_MacGruber.Breathing", "Intensity");

			sm.Movement = new Movement(0.5f, 1);

			s.AddModifier(new ModifierContainer(
				sm, new StepProgressSyncedModifier()));


			var m = new RigidbodyModifier(a, "hip");
			m.Movement = new Movement(0, 150);
			m.Direction = new Vector3(0, 0, 1);
			s.AddModifier(new ModifierContainer(m));

			manager_.AddStep(s);*/
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
		    if (instance_ == null || instance_.options_.VerboseLog)
		        SuperController.LogError(s);
		}

		static public void LogOverlap(string s)
		{
			//SuperController.LogError(s);
		}
	}
}
