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

			Utilities.RandomProvider = new UnityRandomProvider();
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
			var s = new Step();

			var m = new ModifierContainer();
			var rm1 = new RigidbodyModifier();
			m.Modifier = rm1;
			s.AddModifier(m);

			m = new ModifierContainer();
			m.Modifier = new RigidbodyModifier();
			m.ModifierSync = new OtherModifierSyncedModifier(rm1);
			s.AddModifier(m);

			manager_.AddStep(s);
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
				DoPluginEnabled(true);
			});
		}

		public void OnDisable()
		{
			Utilities.Handler(() =>
			{
				DoPluginEnabled(false);
			});
		}

		private void DoPluginEnabled(bool b)
		{
			ui_?.PluginEnabled(b);
			manager_?.PluginEnabled(b);
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
