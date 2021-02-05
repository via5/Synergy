using Leap;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Synergy
{
	interface IStorableParameter : IFactoryObject
	{
		IStorableParameter Clone(int cloneFlags = 0);
		FloatRange PreferredRange { get; }
		string Name { get; }
		bool TryParameter(JSONStorableParam p);

		void Tick(float deltaTime, float progress, bool firstHalf);
		void Set(float magnitude, float normalizedMagnitude);
		void Reset();
		IEnumerable<string> GetStorableNames(Atom a, bool pluginsOnly);
		IEnumerable<string> GetParameterNames(JSONStorable s);
		void PostLoad(JSONStorable s);
	}


	sealed class StorableParameterFactory : BasicFactory<IStorableParameter>
	{
		public override List<IStorableParameter> GetAllObjects()
		{
			return new List<IStorableParameter>()
			{
				new FloatStorableParameter(),
				new ActionStorableParameter(),
				new BoolStorableParameter(),
				new ColorStorableParameter(),
				new UrlStorableParameter(),
				new StringStorableParameter(),
				new StringChooserStorableParameter()
			};
		}

		static public IStorableParameter Create(JSONStorableParam p)
		{
			if (p is JSONStorableFloat)
				return new FloatStorableParameter(p as JSONStorableFloat);
			else if (p is JSONStorableBool)
				return new BoolStorableParameter(p as JSONStorableBool);
			else if (p is JSONStorableColor)
				return new ColorStorableParameter(p as JSONStorableColor);
			else if (p is JSONStorableUrl)
				return new UrlStorableParameter(p as JSONStorableUrl);
			else if (p is JSONStorableString)
				return new StringStorableParameter(p as JSONStorableString);
			else if (p is JSONStorableStringChooser)
				return new StringChooserStorableParameter(p as JSONStorableStringChooser);
			else
				return null;
		}
	}


	abstract class BasicStorableParameter : IStorableParameter
	{
		protected BasicStorableParameter()
		{
		}

		public abstract IStorableParameter Clone(int cloneFlags = 0);

		protected virtual void CopyTo(BasicStorableParameter p, int cloneFlags)
		{
			// no-op
		}

		public virtual FloatRange PreferredRange
		{
			get
			{
				return new FloatRange(0, 1);
			}
		}

		public virtual bool TryParameter(JSONStorableParam p)
		{
			return false;
		}

		public abstract string Name { get; }

		public virtual J.Node ToJSON()
		{
			return new J.Object();
		}

		public virtual bool FromJSON(J.Node n)
		{
			return true;
		}

		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		public virtual void Tick(
			float deltaTime, float progress, bool firstHalf)
		{
			// no-op
		}

		public abstract void Set(float magnitude, float normalizedMagnitude);
		public abstract void Reset();

		public abstract IEnumerable<string> GetStorableNames(Atom a, bool pluginsOnly);
		public abstract IEnumerable<string> GetParameterNames(JSONStorable s);

		public abstract void PostLoad(JSONStorable s);

		protected T SafeGet<T>(string what, Func<T> f)
		{
			try
			{
				return f();
			}
			catch (Exception e)
			{
				Synergy.LogError($"failed to get {what} from storable, " + e.ToString());
				return default(T);
			}
		}

		protected T SafeGet<T>(string what, T def, Func<T> f)
		{
			try
			{
				return f();
			}
			catch (Exception e)
			{
				Synergy.LogError($"failed to get {what} from storable, " + e.Message);
				return def;
			}
		}

		protected void SafeSet(string what, Action f)
		{
			try
			{
				f();
			}
			catch (Exception e)
			{
				Synergy.LogError($"failed to set {what} in storable, " + e.Message);
			}
		}

		protected void SafeInvoke(string what, Action f)
		{
			try
			{
				f();
			}
			catch (Exception e)
			{
				Synergy.LogError($"failed to invoke {what} in storable, " + e.Message);
			}
		}
	}


	abstract class ParamDerivedStorableParameter<T> : BasicStorableParameter
		where T : JSONStorableParam
	{
		private T param_ = null;
		private string paramName_ = null;

		protected ParamDerivedStorableParameter(JSONStorableParam p = null)
		{
			param_ = (T)p;
		}

		protected void CopyTo(ParamDerivedStorableParameter<T> p, int cloneFlags)
		{
			base.CopyTo(p, cloneFlags);
			p.param_ = param_;
		}

		public override string Name
		{
			get { return param_?.name ?? ""; }
		}

		public T Parameter
		{
			get { return param_; }
			set { param_ = value; }
		}

		public override bool TryParameter(JSONStorableParam p)
		{
			if (p is T)
			{
				param_ = (T)p;
				return true;
			}

			return false;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			if (param_ != null)
				o.Add("param", param_.name);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("ParamDerivedStorableParameter");
			if (o == null)
				return false;

			o.Opt("param", ref paramName_);

			return true;
		}

		public override void PostLoad(JSONStorable s)
		{
			param_ = s.GetParam(paramName_) as T;

			if (param_ == null)
			{
				Synergy.LogError(
					$"PostLoad: param name {paramName_} not in " +
					$"storable {s.storeId}");
			}

			paramName_ = null;
		}
	}


	class FloatStorableParameter
		: ParamDerivedStorableParameter<JSONStorableFloat>
	{
		public static string FactoryTypeName { get; } = "float";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Float";
		public override string GetDisplayName() { return DisplayName; }

		public FloatStorableParameter(JSONStorableFloat p = null)
			: base(p)
		{
		}

		public override IStorableParameter Clone(int cloneFlags = 0)
		{
			var p = new FloatStorableParameter();
			CopyTo(p, cloneFlags);
			return p;
		}

		public override FloatRange PreferredRange
		{
			get
			{
				if (Parameter == null)
					return base.PreferredRange;

				return SafeGet("range", () =>
				{
					return new FloatRange(Parameter.min, Parameter.max);
				});
			}
		}

		public float Value
		{
			get
			{
				return SafeGet("val", () =>
				{
					return Parameter?.val ?? 0;
				});
			}
		}

		public override void Set(float magnitude, float normalizedMagnitude)
		{
			if (Parameter == null)
				return;

			SafeSet("val", () =>
			{
				Parameter.val = magnitude;
			});
		}

		public override void Reset()
		{
			if (Parameter == null)
				return;

			SafeSet("val", () =>
			{
				Parameter.val = Parameter.defaultVal;
			});
		}

		public override IEnumerable<string> GetStorableNames(Atom a, bool pluginsOnly)
		{
			if (a != null)
			{
				foreach (var id in a.GetStorableIDs())
				{
					if (!pluginsOnly || Utilities.StorableIsPlugin(id))
					{
						var s = a.GetStorableByID(id);
						if (s.GetFloatParamNames().Count > 0)
							yield return id;
					}
				}
			}
		}

		public override IEnumerable<string> GetParameterNames(JSONStorable s)
		{
			foreach (var n in s.GetFloatParamNames())
				yield return n;
		}
	}


	class BoolStorableParameter
		: ParamDerivedStorableParameter<JSONStorableBool>
	{
		public static string FactoryTypeName { get; } = "bool";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Bool";
		public override string GetDisplayName() { return DisplayName; }

		public BoolStorableParameter(JSONStorableBool p = null)
			: base(p)
		{
		}

		public override IStorableParameter Clone(int cloneFlags = 0)
		{
			var p = new BoolStorableParameter();
			CopyTo(p, cloneFlags);
			return p;
		}

		public override void Set(float magnitude, float normalizedMagnitude)
		{
			if (Parameter == null)
				return;

			SafeSet("val", () =>
			{
				Parameter.val = (magnitude > 0.5f);
			});
		}

		public override void Reset()
		{
			if (Parameter == null)
				return;

			SafeSet("val", () =>
			{
				Parameter.val = Parameter.defaultVal;
			});
		}

		public override IEnumerable<string> GetStorableNames(Atom a, bool pluginsOnly)
		{
			if (a != null)
			{
				foreach (var id in a.GetStorableIDs())
				{
					if (!pluginsOnly || Utilities.StorableIsPlugin(id))
					{
						var s = a.GetStorableByID(id);
						if (s.GetBoolParamNames().Count > 0)
							yield return id;
					}
				}
			}
		}

		public override IEnumerable<string> GetParameterNames(JSONStorable s)
		{
			foreach (var n in s.GetBoolParamNames())
				yield return n;
		}
	}


	class ColorStorableParameter
		: ParamDerivedStorableParameter<JSONStorableColor>
	{
		public static string FactoryTypeName { get; } = "color";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Color";
		public override string GetDisplayName() { return DisplayName; }

		private Color c1_, c2_;

		public ColorStorableParameter(JSONStorableColor p = null)
			: base(p)
		{
			c1_ = new Color(1, 0, 0);
			c2_ = new Color(0, 1, 0);
		}

		public override IStorableParameter Clone(int cloneFlags = 0)
		{
			var p = new ColorStorableParameter();
			CopyTo(p, cloneFlags);
			return p;
		}

		protected void CopyTo(ColorStorableParameter p, int cloneFlags)
		{
			base.CopyTo(p, cloneFlags);
			p.c1_ = c1_;
			p.c2_ = c2_;
		}

		public Color Color1
		{
			get { return c1_; }
			set { c1_ = value; }
		}

		public Color Color2
		{
			get { return c2_; }
			set { c2_ = value; }
		}

		public override void Set(float magnitude, float normalizedMagnitude)
		{
			if (Parameter == null)
				return;

			var c = new Color(
				Interpolate(c1_.r, c2_.r, magnitude),
				Interpolate(c1_.g, c2_.g, magnitude),
				Interpolate(c1_.b, c2_.b, magnitude));

			SafeSet("SetColor", () =>
			{
				Parameter.SetColor(c);
			});
		}

		private float Interpolate(float a, float b, float magnitude)
		{
			return a + (b - a) * magnitude;
		}

		public override void Reset()
		{
			if (Parameter == null)
				return;

			SafeSet("SetValToDefault", () =>
			{
				Parameter.SetValToDefault();
			});
		}

		public override IEnumerable<string> GetStorableNames(Atom a, bool pluginsOnly)
		{
			if (a != null)
			{
				foreach (var id in a.GetStorableIDs())
				{
					if (!pluginsOnly || Utilities.StorableIsPlugin(id))
					{
						var s = a.GetStorableByID(id);
						if (s.GetColorParamNames().Count > 0)
							yield return id;
					}
				}
			}
		}

		public override IEnumerable<string> GetParameterNames(JSONStorable s)
		{
			foreach (var n in s.GetColorParamNames())
				yield return n;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("color1", c1_);
			o.Add("color2", c2_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("ColorStorableParameter");
			if (o == null)
				return false;

			o.Opt("color1", ref c1_);
			o.Opt("color2", ref c2_);

			return true;
		}
	}


	interface IStringListStorableParameter
	{
		List<string> Strings { get; set; }
		bool HasAvailableStrings { get; }
		List<string> AvailableStrings { get; }
		void AddString(string s);
		void RemoveStringAt(int index);
		void SetStringAt(int i, string s);
	}


	abstract class StringListStorableParameter<T> :
		ParamDerivedStorableParameter<T>,
		IStringListStorableParameter
		where T : JSONStorableParam
	{
		private List<string> strings_ = new List<string>();
		private int current_ = -1;

		public StringListStorableParameter(T p = null)
			: base(p)
		{
		}

		protected void CopyTo(StringListStorableParameter<T> p, int cloneFlags)
		{
			base.CopyTo(p, cloneFlags);
			p.strings_ = new List<string>(strings_);
		}

		public List<string> Strings
		{
			get { return strings_; }
			set { strings_ = new List<string>(value); }
		}

		public void AddString(string s)
		{
			strings_.Add(s);
		}

		public void RemoveString(string s)
		{
			strings_.Remove(s);
		}

		public void RemoveStringAt(int index)
		{
			if (index >= strings_.Count)
				return;

			strings_.RemoveAt(index);
		}

		public void SetStringAt(int i, string s)
		{
			if (i < 0 || i >= strings_.Count)
				return;

			strings_[i] = s;
		}

		public abstract bool HasAvailableStrings { get; }
		public abstract List<string> AvailableStrings { get; }

		public string CurrentString
		{
			get
			{
				if (current_ < 0 || current_ >= strings_.Count)
					return "";

				return strings_[current_];
			}
		}

		public override void Set(float magnitude, float normalizedMagnitude)
		{
			if (Parameter == null)
				return;

			if (strings_.Count == 0)
				return;

			var magPer = 1.0f / strings_.Count;

			var i = Math.Min(
				(int)Math.Floor(normalizedMagnitude / magPer),
				strings_.Count - 1);

			if (i != current_)
			{
				current_ = i;

				SafeSet("SetValue", () =>
				{
					SetValue(strings_[i]);
				});
			}

			if (normalizedMagnitude >= 0.95f)
				current_ = -1;
		}

		protected abstract void SetValue(string s);

		public override void Reset()
		{
			// no-op
		}

		public override IEnumerable<string> GetStorableNames(Atom a, bool pluginsOnly)
		{
			if (a != null)
			{
				foreach (var id in a.GetStorableIDs())
				{
					if (!pluginsOnly || Utilities.StorableIsPlugin(id))
					{
						var s = a.GetStorableByID(id);
						if (s.GetStringChooserParamNames().Count > 0)
							yield return id;
					}
				}
			}
		}

		public override IEnumerable<string> GetParameterNames(JSONStorable s)
		{
			foreach (var n in s.GetStringChooserParamNames())
				yield return n;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("strings", strings_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("StringListStorableParameter");
			if (o == null)
				return false;

			o.Opt("strings", ref strings_);

			return true;
		}
	}


	class StringStorableParameter
		: StringListStorableParameter<JSONStorableString>
	{
		public static string FactoryTypeName { get; } = "string";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "String";
		public override string GetDisplayName() { return DisplayName; }

		public StringStorableParameter(JSONStorableString p = null)
			: base(p)
		{
		}

		public override IStorableParameter Clone(int cloneFlags = 0)
		{
			var p = new StringStorableParameter();
			CopyTo(p, cloneFlags);
			return p;
		}

		public override bool HasAvailableStrings
		{
			get { return false; }
		}

		public override List<String> AvailableStrings
		{
			get { return new List<string>(); }
		}

		public override IEnumerable<string> GetStorableNames(Atom a, bool pluginsOnly)
		{
			if (a != null)
			{
				foreach (var id in a.GetStorableIDs())
				{
					if (!pluginsOnly || Utilities.StorableIsPlugin(id))
					{
						var s = a.GetStorableByID(id);
						if (s.GetStringParamNames().Count > 0)
							yield return id;
					}
				}
			}
		}

		public override IEnumerable<string> GetParameterNames(JSONStorable s)
		{
			foreach (var n in s.GetStringParamNames())
				yield return n;
		}

		protected override void SetValue(string s)
		{
			if (Parameter == null)
				return;

			SafeSet("val", () =>
			{
				Parameter.val = s;
			});
		}
	}


	class StringChooserStorableParameter
		: StringListStorableParameter<JSONStorableStringChooser>
	{
		public static string FactoryTypeName { get; } = "stringchooser";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "String chooser";
		public override string GetDisplayName() { return DisplayName; }

		public StringChooserStorableParameter(JSONStorableStringChooser p = null)
			: base(p)
		{
		}

		public override IStorableParameter Clone(int cloneFlags = 0)
		{
			var p = new StringChooserStorableParameter();
			CopyTo(p, cloneFlags);
			return p;
		}

		public override bool HasAvailableStrings
		{
			get { return true; }
		}

		public override List<string> AvailableStrings
		{
			get
			{
				return SafeGet("choices", new List<string>(), () =>
				{
					return Parameter?.choices ?? new List<string>();
				});
			}
		}

		public override IEnumerable<string> GetStorableNames(Atom a, bool pluginsOnly)
		{
			if (a != null)
			{
				foreach (var id in a.GetStorableIDs())
				{
					if (!pluginsOnly || Utilities.StorableIsPlugin(id))
					{
						var s = a.GetStorableByID(id);
						if (s.GetStringChooserParamNames().Count > 0)
							yield return id;
					}
				}
			}
		}

		public override IEnumerable<string> GetParameterNames(JSONStorable s)
		{
			foreach (var n in s.GetStringChooserParamNames())
				yield return n;
		}

		protected override void SetValue(string s)
		{
			SafeSet("val", () =>
			{
				Parameter.val = s;
			});
		}
	}


	class UrlStorableParameter : StringStorableParameter
	{
		public static new string FactoryTypeName { get; } = "url";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static new string DisplayName { get; } = "URL";
		public override string GetDisplayName() { return DisplayName; }

		public UrlStorableParameter(JSONStorableUrl p = null)
			: base(p)
		{
		}

		public override IStorableParameter Clone(int cloneFlags = 0)
		{
			var p = new UrlStorableParameter();
			CopyTo(p, cloneFlags);
			return p;
		}

		public override IEnumerable<string> GetStorableNames(Atom a, bool pluginsOnly)
		{
			if (a != null)
			{
				foreach (var id in a.GetStorableIDs())
				{
					if (!pluginsOnly || Utilities.StorableIsPlugin(id))
					{
						var s = a.GetStorableByID(id);
						if (s.GetUrlParamNames().Count > 0)
							yield return id;
					}
				}
			}
		}

		public override IEnumerable<string> GetParameterNames(JSONStorable s)
		{
			foreach (var n in s.GetUrlParamNames())
				yield return n;
		}
	}


	class ActionStorableParameter : BasicStorableParameter
	{
		public static string FactoryTypeName { get; } = "action";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Action";
		public override string GetDisplayName() { return DisplayName; }


		public const int TriggerUp = 0x01;
		public const int TriggerDown = 0x02;
		public const int TriggerBoth = TriggerUp | TriggerDown;

		public const int StateNone = 0;
		public const int StateGoingUp = 1;
		public const int StateGoingDown = 2;
		public const int StateGoingUpTriggered = 3;
		public const int StateGoingUpIgnored = 4;
		public const int StateGoingDownTriggered = 5;
		public const int StateGoingDownIgnored = 6;


		public static List<int> TriggerTypes()
		{
			return new List<int>()
			{
				TriggerUp, TriggerDown, TriggerBoth
			};
		}

		public static List<string> TriggerTypeNames()
		{
			var list = new List<string>();

			foreach (var i in TriggerTypes())
				list.Add(TriggerTypeToString(i));

			return list;
		}

		public static string TriggerTypeToString(int i)
		{
			if (i == TriggerBoth)
				return "Both";
			else if (i == TriggerUp)
				return "Reaching";
			else if (i == TriggerDown)
				return "Leaving";
			else
				return "None";
		}


		private float triggerMag_ = 0;
		private int triggerType_ = TriggerUp;

		private JSONStorableAction param_ = null;
		private string paramName_ = null;

		private bool active_ = false;
		private bool goingUp_ = true;
		private int currentState_ = StateNone;
		private int lastState_ = StateNone;


		public ActionStorableParameter(JSONStorableAction p = null)
		{
			param_ = p;
		}

		public override IStorableParameter Clone(int cloneFlags = 0)
		{
			var p = new ActionStorableParameter();
			CopyTo(p, cloneFlags);
			return p;
		}

		protected void CopyTo(ActionStorableParameter p, int cloneFlags)
		{
			base.CopyTo(p, cloneFlags);
			p.triggerMag_ = triggerMag_;
			p.triggerType_ = triggerType_;
			p.param_ = param_;
		}

		public override string Name
		{
			get { return param_?.name ?? ""; }
		}

		public float TriggerMagnitude
		{
			get { return triggerMag_; }
			set { triggerMag_ = value; }
		}

		public int TriggerType
		{
			get { return triggerType_; }
			set { triggerType_ = value; }
		}

		public int CurrentState
		{
			get { return currentState_; }
		}

		public int LastState
		{
			get { return lastState_; }
		}

		public JSONStorableAction Parameter
		{
			get { return param_; }
			set { param_ = value; }
		}

		public override void Tick(
			float deltaTime, float progress, bool firstHalf)
		{
			base.Tick(deltaTime, progress, firstHalf);

			if (goingUp_ != firstHalf)
			{
				goingUp_ = firstHalf;
				active_ = false;
			}
		}

		public override void Set(float magnitude, float normalizedMagnitude)
		{
			if (Parameter == null)
				return;

			if (Math.Abs(triggerMag_ - magnitude) < 0.05f)
			{
				if (!active_)
				{
					active_ = true;
					MagnitudeReached();
				}
			}
			else
			{
				if (active_ && !goingUp_)
					MagnitudeReached();

				active_ = false;
				SetState(goingUp_ ? StateGoingUp : StateGoingDown);
			}
		}

		private void MagnitudeReached()
		{
			if (goingUp_)
			{
				if (Bits.IsSet(triggerType_, TriggerUp))
				{
					SafeInvoke("actionCallback", () =>
					{
						Parameter.actionCallback?.Invoke();
					});

					SetState(StateGoingUpTriggered);
				}
				else
				{
					SetState(StateGoingUpIgnored);
				}
			}
			else
			{
				if (Bits.IsSet(triggerType_, TriggerDown))
				{
					SafeInvoke("actionCallback", () =>
					{
						Parameter.actionCallback?.Invoke();
					});

					SetState(StateGoingDownTriggered);
				}
				else
				{
					SetState(StateGoingDownIgnored);
				}
			}
		}

		public override void Reset()
		{
		}

		public override IEnumerable<string> GetStorableNames(Atom a, bool pluginsOnly)
		{
			if (a != null)
			{
				foreach (var id in a.GetStorableIDs())
				{
					if (!pluginsOnly || Utilities.StorableIsPlugin(id))
					{
						var s = a.GetStorableByID(id);
						if (s.GetActionNames().Count > 0)
							yield return id;
					}
				}
			}
		}

		public override IEnumerable<string> GetParameterNames(JSONStorable s)
		{
			foreach (var n in s.GetActionNames())
				yield return n;
		}

		private void SetState(int i)
		{
			if (currentState_ != i)
			{
				lastState_ = currentState_;
				currentState_ = i;
			}
		}

		public override void PostLoad(JSONStorable s)
		{
			if (!string.IsNullOrEmpty(paramName_))
			{
				param_ = s.GetAction(paramName_);

				if (param_ == null)
				{
					Synergy.LogError(
						$"PostLoad: action name {paramName_} not in " +
						$"storable {s.storeId}");
				}

				paramName_ = null;
			}
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("triggerMag", triggerMag_);
			o.Add("triggerType", triggerType_);

			if (param_ != null)
				o.Add("param", param_.name);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("ActionStorableParameter");
			if (o == null)
				return false;

			o.Opt("triggerMag", ref triggerMag_);
			o.Opt("triggerType", ref triggerType_);
			o.Opt("param", ref paramName_);

			return true;
		}
	}


	class StorableParameterHolder
	{
		private Atom atom_ = null;
		private JSONStorable storable_ = null;
		private string storableId_ = null;
		private IStorableParameter parameter_ = null;

		public StorableParameterHolder Clone(int cloneFlags = 0)
		{
			var h = new StorableParameterHolder();
			h.storable_ = storable_;
			h.storableId_ = storableId_;
			h.parameter_ = parameter_?.Clone(cloneFlags);
			return h;
		}

		public Atom Atom
		{
			get
			{
				return atom_;
			}

			set
			{
				if (value != atom_)
					AtomChanged(value);
			}
		}

		public void DeferredInit()
		{
			if (storableId_ != null)
			{
				SetStorable(storableId_);
				storableId_ = null;
			}

			if (parameter_ != null && storable_ != null)
				parameter_.PostLoad(storable_);
		}

		public IStorableParameter Parameter
		{
			get { return parameter_; }
			set { parameter_ = value; }
		}

		public JSONStorable Storable
		{
			get
			{
				if (storable_ == null)
					return null;

				try
				{
					var s = storable_.name;
				}
				catch (NullReferenceException)
				{
					Synergy.LogError("storable died");
					storable_ = null;
					parameter_ = null;
				}

				return storable_;
			}
		}

		public void SetStorable(string id)
		{
			if (atom_ == null || string.IsNullOrEmpty(id))
			{
				storable_ = null;
				Parameter = null;
				return;
			}

			var s = atom_.GetStorableByID(id);
			if (s == null)
			{
				Synergy.LogError(
					$"storable id '{id}' not found in atom '{atom_.uid}'");

				return;
			}

			storable_ = s;

			if (Parameter != null)
			{
				if (!SetParameterImpl(parameter_.Name))
					Parameter = null;
			}
		}

		public void SetParameter(string name)
		{
			if (!SetParameterImpl(name))
			{
				Parameter = null;

				Synergy.LogError(
					$"parameter '{name}' not found in storable " +
					$"'{storable_.name}'");
			}
		}

		public void SetParameter(JSONStorableParam sp)
		{
			if (Parameter != null && Parameter.TryParameter(sp))
				return;

			var p = StorableParameterFactory.Create(sp);
			if (p == null)
			{
				Synergy.LogError("unknown parameter type " + sp.ToString());
				return;
			}

			Parameter = p;
		}

		public void SetParameter(JSONStorableAction a)
		{
			if (Parameter is ActionStorableParameter)
				((ActionStorableParameter)Parameter).Parameter = a;
			else
				Parameter = new ActionStorableParameter(a);
		}

		private void AtomChanged(Atom newAtom)
		{
			string oldStorable = storable_?.storeId ?? "";
			string oldParameter = Parameter?.Name ?? "";

			storable_ = Utilities.FindStorableInNewAtom(newAtom, oldStorable);

			if (storable_ == null || oldParameter == "")
			{
				Parameter = null;
			}
			else
			{
				if (!SetParameterImpl(oldParameter))
					Parameter = null;
			}

			atom_ = newAtom;
		}

		private bool SetParameterImpl(string name)
		{
			if (storable_ == null || string.IsNullOrEmpty(name))
			{
				Parameter = null;
				return true;
			}

			var p = storable_.GetParam(name);
			if (p != null)
			{
				SetParameter(p);
				return true;
			}

			var a = storable_.GetAction(name);
			if (a != null)
			{
				SetParameter(a);
				return true;
			}

			return false;
		}

		public void ToJSON(J.Object o)
		{
			if (storable_ != null)
				o.Add("storable", storable_.storeId);

			o.Add("parameter", parameter_);
		}

		public void FromJSON(Atom a, J.Object o)
		{
			atom_ = a;

			if (atom_ != null)
			{
				if (o.HasKey("storable"))
				{
					string id = "";
					o.Opt("storable", ref id);

					if (id != "")
					{
						if (J.Node.SaveContext.ForPreset)
							SetStorable(id);
						else
							storableId_ = id;
					}
				}
			}

			o.Opt<StorableParameterFactory, IStorableParameter>(
				"parameter", ref parameter_);

			if (parameter_ != null && storable_ != null)
			{
				if (J.Node.SaveContext.ForPreset)
					parameter_.PostLoad(storable_);
			}
		}
	}


	sealed class StorableModifier : AtomWithMovementModifier
	{
		public static string FactoryTypeName { get; } = "storable";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Storable";
		public override string GetDisplayName() { return DisplayName; }

		private StorableParameterHolder holder_ = new StorableParameterHolder();

		public StorableModifier()
		{
			holder_.Atom = Atom;
			Movement = new Movement(0, 1);
		}

		public StorableModifier(Atom a, string storable, string parameter)
		{
			Atom = a;
			holder_.Atom = a;
			SetStorable(storable);
			SetParameter(parameter);
			Movement = new Movement(0, 1);
		}

		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new StorableModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		private void CopyTo(StorableModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
			m.holder_ = holder_.Clone(cloneFlags);
		}

		public override void Removed()
		{
			base.Removed();
			ResetParameter();
		}

		public IStorableParameter Parameter
		{
			get
			{
				return holder_.Parameter;
			}

			set
			{
				ResetParameter();
				holder_.Parameter = value;
				FirePreferredRangeChanged();
			}
		}

		public JSONStorable Storable
		{
			get
			{
				return holder_.Storable;
			}
		}

		public StorableParameterHolder Holder
		{
			get { return holder_; }
		}

		public void SetStorable(string id)
		{
			holder_.SetStorable(id);
			FireNameChanged();
		}

		public void SetParameter(string name)
		{
			holder_.SetParameter(name);
			FireNameChanged();
		}

		public void SetParameter(JSONStorableParam sp)
		{
			holder_.SetParameter(sp);
			FireNameChanged();
		}

		public void SetParameter(JSONStorableAction a)
		{
			Parameter = new ActionStorableParameter(a);
			FireNameChanged();
		}

		private void ResetParameter()
		{
			Parameter?.Reset();
		}

		public override FloatRange PreferredRange
		{
			get
			{
				if (Parameter == null)
					return new FloatRange(0, 1);
				else
					return Parameter.PreferredRange;
			}
		}

		public override void Reset()
		{
			base.Reset();
		}

		protected override void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			base.DoTick(deltaTime, progress, firstHalf);

			if (Parameter != null)
				Parameter.Tick(deltaTime, progress, firstHalf);
		}

		protected override void DoSet(bool paused)
		{
			base.DoSet(paused);

			if (paused)
				return;

			if (Parameter != null)
				Parameter.Set(Movement.Magnitude, Movement.NormalizedMagnitude);
		}

		protected override string MakeName()
		{
			string s = "S ";

			if (Parameter == null)
				s += "none";
			else
				s += Parameter.GetDisplayName();

			return s;
		}

		public override void DeferredInit()
		{
			holder_.DeferredInit();
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();
			holder_.ToJSON(o);
			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("StorableModifier");
			if (o == null)
				return false;

			holder_.FromJSON(Atom, o);

			return true;
		}

		protected override void AtomChanged()
		{
			holder_.Atom = Atom;
		}
	}
}
