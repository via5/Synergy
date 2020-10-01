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
				new BoolStorableParameter(),
				new ColorStorableParameter(),
				new UrlStorableParameter(),
				new StringStorableParameter(),
				new ActionStorableParameter()
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
				else
					return new FloatRange(Parameter.min, Parameter.max);
			}
		}

		public override void Set(float magnitude, float normalizedMagnitude)
		{
			if (Parameter != null)
				Parameter.val = magnitude;
		}

		public override void Reset()
		{
			if (Parameter != null)
				Parameter.val = Parameter.defaultVal;
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
			if (Parameter != null)
				Parameter.valNoCallback = (normalizedMagnitude > 0.5f);
		}

		public override void Reset()
		{
			if (Parameter != null)
				Parameter.valNoCallback = Parameter.defaultVal;
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
			if (Parameter != null)
			{
				var c = new Color(
					Interpolate(c1_.r, c2_.r, normalizedMagnitude),
					Interpolate(c1_.g, c2_.g, normalizedMagnitude),
					Interpolate(c1_.b, c2_.b, normalizedMagnitude));

				Parameter.SetColor(c);
			}
		}

		private float Interpolate(float a, float b, float magnitude)
		{
			return a + (b - a) * magnitude;
		}

		public override void Reset()
		{
			if (Parameter != null)
				Parameter.SetValToDefault();
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


	class StringStorableParameter
		: ParamDerivedStorableParameter<JSONStorableString>
	{
		public static string FactoryTypeName { get; } = "string";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "String";
		public override string GetDisplayName() { return DisplayName; }

		private List<string> strings_ = new List<string>();
		private string current_ = null;

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

		protected void CopyTo(StringStorableParameter p, int cloneFlags)
		{
			base.CopyTo(p, cloneFlags);
			p.strings_ = new List<string>(strings_);
		}

		public List<string> Strings
		{
			get { return strings_; }
			set { strings_ = new List<string>(value); }
		}

		public string Current
		{
			get { return current_; }
		}

		public override void Set(float magnitude, float normalizedMagnitude)
		{
			if (Parameter != null)
			{
				if (strings_.Count == 0)
					return;

				var magPer = 1.0f / strings_.Count;

				var i = Math.Min(
					(int)Math.Floor(normalizedMagnitude / magPer),
					strings_.Count - 1);

				var next = strings_[i];

				if (next != current_)
				{
					current_ = next;
					Parameter.val = next;
				}
			}
		}

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

			var o = n.AsObject("StringStorableParameter");
			if (o == null)
				return false;

			o.Opt("strings", ref strings_);

			return true;
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


		private float triggerMag_ = 1;
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
			if (param_ == null)
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
					param_.actionCallback?.Invoke();
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
					param_.actionCallback?.Invoke();
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


	sealed class StorableModifier : AtomWithMovementModifier
	{
		public static string FactoryTypeName { get; } = "storable";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Storable";
		public override string GetDisplayName() { return DisplayName; }


		private JSONStorable storable_ = null;
		private string storableId_ = null;
		private IStorableParameter parameter_ = null;


		public StorableModifier()
		{
		}

		public StorableModifier(Atom a, string storable, string parameter)
		{
			Atom = a;
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
			m.storable_ = storable_;
			m.parameter_ = parameter_?.Clone(cloneFlags);
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
				return parameter_;
			}

			set
			{
				ResetParameter();
				parameter_ = value;
				FirePreferredRangeChanged();
			}
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
			if (Atom == null || string.IsNullOrEmpty(id))
				return;

			var s = Atom.GetStorableByID(id);
			if (s == null)
			{
				Synergy.LogError(
					$"storable id '{id}' not found in atom '{Atom.uid}'");

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
					$"'{storable_.name}' from atom '{Atom.uid}'");
			}
		}

		private bool SetParameterImpl(string name)
		{
			if (Atom == null || storable_ == null || string.IsNullOrEmpty(name))
				return true;

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

		public void SetParameter(JSONStorableParam sp)
		{
			var p = StorableParameterFactory.Create(sp);
			if (p == null)
			{
				Synergy.LogError("unknown parameter type");
				return;
			}

			Parameter = p;
		}

		public void SetParameter(JSONStorableAction a)
		{
			Parameter = new ActionStorableParameter(a);
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
			if (storableId_ != null)
			{
				SetStorable(storableId_);
				storableId_ = null;
			}

			if (parameter_ != null && storable_ != null)
				parameter_.PostLoad(storable_);
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			if (storable_ != null)
				o.Add("storable", storable_.storeId);

			o.Add("parameter", parameter_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("StorableModifier");
			if (o == null)
				return false;

			if (Atom != null)
			{
				if (o.HasKey("storable"))
				{
					string id = "";
					o.Opt("storable", ref id);

					if (id != "")
					{
						if (J.Node.SaveType == SaveTypes.Preset)
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
				if (J.Node.SaveType == SaveTypes.Preset)
					parameter_.PostLoad(storable_);
			}

			return true;
		}

		protected override void AtomChanged()
		{
			string oldStorable = storable_?.name ?? "";
			string oldParameter = Parameter?.Name ?? "";


			if (oldStorable == "")
				storable_ = null;
			else
				storable_ = Atom.GetStorableByID(oldStorable);


			if (storable_ == null || oldParameter == "")
			{
				Parameter = null;
			}
			else
			{
				if (!SetParameterImpl(oldParameter))
					Parameter = null;
			}
		}
	}
}
