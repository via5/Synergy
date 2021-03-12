using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UI = SynergyUI;

namespace Synergy
{
	sealed class Synergy : MVRScript
	{
		public delegate void PluginStateCallback(bool b);
		public event PluginStateCallback PluginStateChanged;

		private static Synergy instance_ = null;
		private SuperController sc_ = null;
		private bool enabled_ = false;
		private bool frozen_ = false;
		private Manager manager_ = null;
		private Options options_ = null;
		private UI.TimerManager timers_ = null;
		private MainUI ui_ = null;
		private List<IParameter> parameters_ = null;

		private bool deferredInitDone_ = false;
		private bool deferredUIDone_ = false;
		private UI.Timer waitForUITimer_ = null;


		public Synergy()
		{
			instance_ = this;
		}

		public static Synergy Instance
		{
			get { return instance_; }
		}

		public MainUI MainUI
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
			timers_ = new UI.TimerManager();
			ui_ = null;
			parameters_ = new List<IParameter>();
			manager_ = new Manager();

			deferredInitDone_ = false;
			deferredUIDone_ = false;

			UI.Glue.Set(
				() => Instance.manager,
				() => Instance.UITransform.GetComponentInChildren<MVRScriptUI>(),
				(s, ps) => Strings.Get(s, ps),
				(s) => LogVerbose(s),
				(s) => LogInfo(s),
				(s) => LogWarning(s),
				(s) => LogError(s));

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

			CreateTimer(1, () =>
			{
				manager_.DeferredInit();
				deferredInitDone_ = true;
			});


			if (UI.Root.IsReady())
			{
				CreateUI();
			}
			else
			{
				// vam doesn't actually set up the size of the scrollview inside
				// the scrip ui until a person is selected, so start a timer and
				// check
				waitForUITimer_ = CreateTimer(0.5f, WaitForUI, UI.Timer.Repeat);
			}
		}

		private void WaitForUI()
		{
			if (!UI.Root.IsReady())
			{
				// still not ready
				return;
			}

			// ready, kill timer and create ui

			waitForUITimer_.Destroy();
			waitForUITimer_ = null;

			CreateUI();
		}

		private void CreateUI()
		{
			Utilities.Handler(() =>
			{
				ui_ = new MainUI();
				ui_.Create();
			});
		}

		public void Start()
		{
			deferredInitDone_ = false;
			deferredUIDone_ = false;

			Utilities.Handler(() =>
			{
				RegisterString(new JSONStorableString("dummy", ""));
				SetStringParamValue("dummy", "dummy");

				if (GetAtomById("synergyuitest") != null)
					CreateTestStuff(GetAtomById("synergyuitest"));
				else if (GetAtomById("synergyuitest1") != null)
					CreateTestStuff(GetAtomById("synergyuitest1"));

				LogVerbose("OK");
				enabled_ = true;
			});
		}

		private void CreateTestStuff(Atom a)
		{
			var s = manager_.AddStep(new Step("1"));

			var tl = s.AddModifier(new TimelineModifier());

			tl.Atom = a;
			tl.Delay.EndForwardsDuration = new RandomDuration(1);
			tl.Gaze.Setting = Integration.SettingDisable;
			tl.Blink.Setting = Integration.SettingDisable;
			tl.Animation = "1";
			tl.DisableEyeModifiers = true;
		}

		public UI.Timer CreateTimer(float seconds, UI.Timer.Callback f, int flags = 0)
		{
			return timers_.CreateTimer(seconds, f, flags);
		}

		public void RemoveTimer(UI.Timer t)
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
			if (!enabled_)
				return;

			// ui_ can be null while the ui timer is running

			if (ui_ != null)
			{
				if (deferredInitDone_ && !deferredUIDone_)
				{
					ui_.DeferredInit();
					deferredUIDone_ = true;
				}
			}

			timers_.TickTimers(deltaTime);
			timers_.CheckTimers();

			if (ui_ != null)
				ui_.Update();
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
			// this happens when the top menu is opened
			//
			// when the simulation is paused, unity doesn't reset forces on
			// each frame, so Set() must not be called because the forces will
			// compound until the menu is closed, at which point everything will
			// explode
			if (!Physics.autoSimulation)
				return;

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
			PluginStateChanged?.Invoke(b);
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

				J.Node.SaveContext = SaveContext.CreateForScene();
				o.Add("version", Version.String);
				o.Add("options", options_);
				o.Add("manager", manager_);
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

				J.Node.SaveContext = SaveContext.CreateForScene();
				o.Opt("options", ref options_);
				o.Opt("manager", ref manager_);
			});
		}


		static public void Log(int level, string s, bool force=false)
		{
			if (instance_.options_ != null)
			{
				if (instance_ != null && level > instance_.options_.LogLevel)
				{
					if (!force)
						return;
				}
			}

			string prefix = "[" + Time.realtimeSinceStartup.ToString("0.00") + "] ";

			if (level <= Options.LogLevelWarn)
				SuperController.LogError(prefix + s);
			else
				SuperController.LogMessage(prefix + s);
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
				Log(Options.LogLevelInfo, s, true);
		}
	}
}
