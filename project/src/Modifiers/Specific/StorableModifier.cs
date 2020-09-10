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

		void Set(float magnitude);
		void Reset();
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

		public J.Node ToJSON()
		{
			return new J.Object();
		}

		public bool FromJSON(J.Node n)
		{
			return true;
		}

		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		public abstract void Set(float magnitude);
		public abstract void Reset();
	}


	abstract class ParamDerivedStorableParameter<T> : BasicStorableParameter
		where T : JSONStorableParam
	{
		private T param_ = null;

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
	}


	class FloatStorableParameter
		: ParamDerivedStorableParameter<JSONStorableFloat>
	{
		public override string GetFactoryTypeName() { return "float"; }
		public override string GetDisplayName() { return "Float"; }

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

		public override void Set(float magnitude)
		{
			if (Parameter != null)
				Parameter.valNoCallback = magnitude;
		}

		public override void Reset()
		{
			if (Parameter != null)
				Parameter.valNoCallback = Parameter.defaultVal;
		}
	}


	class BoolStorableParameter
		: ParamDerivedStorableParameter<JSONStorableBool>
	{
		public override string GetFactoryTypeName() { return "bool"; }
		public override string GetDisplayName() { return "Bool"; }

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

		public override void Set(float magnitude)
		{
			if (Parameter != null)
				Parameter.valNoCallback = (magnitude > 0.5f);
		}

		public override void Reset()
		{
			if (Parameter != null)
				Parameter.valNoCallback = Parameter.defaultVal;
		}
	}


	class ColorStorableParameter
		: ParamDerivedStorableParameter<JSONStorableColor>
	{
		public override string GetFactoryTypeName() { return "color"; }
		public override string GetDisplayName() { return "Color"; }

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

		public override void Set(float magnitude)
		{
			if (Parameter != null)
			{
				var c = new Color(
					Interpolate(c1_.r, c2_.r, magnitude),
					Interpolate(c1_.g, c2_.g, magnitude),
					Interpolate(c1_.b, c2_.b, magnitude));

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
				Parameter.valNoCallback = Parameter.defaultVal;
		}
	}


	class StringStorableParameter
		: ParamDerivedStorableParameter<JSONStorableString>
	{
		public override string GetFactoryTypeName() { return "string"; }
		public override string GetDisplayName() { return "String"; }

		private List<string> strings_ = new List<string>();
		private string last_ = null;

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
		}

		public override void Set(float magnitude)
		{
			if (Parameter != null)
			{
				if (strings_.Count == 0)
					return;

				var magPer = 1.0f / strings_.Count;
				var i = Math.Min((int)Math.Floor(magnitude / magPer), strings_.Count - 1);

				var next = strings_[i];

				if (next != last_)
				{
					last_ = next;
					Parameter.val = next;
					Synergy.LogError(next);
				}
			}
		}

		public override void Reset()
		{
			if (Parameter != null)
				Parameter.val = Parameter.defaultVal;
		}
	}


	class UrlStorableParameter : StringStorableParameter
	{
		public override string GetFactoryTypeName() { return "url"; }
		public override string GetDisplayName() { return "URL"; }

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
	}


	class ActionStorableParameter : BasicStorableParameter
	{
		public override string GetFactoryTypeName() { return "action"; }
		public override string GetDisplayName() { return "Action"; }


		private JSONStorableAction param_ = null;
		private bool active_ = false;

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
			p.param_ = param_;
		}

		public override string Name
		{
			get { return param_?.name ?? ""; }
		}

		public override void Set(float magnitude)
		{
			if (param_ != null)
			{
				if (magnitude > 0.5f && !active_)
				{
					active_ = true;
					param_.actionCallback?.Invoke();
				}
				else if (magnitude <= 0.5f && active_)
				{
					active_ = false;
					param_.actionCallback?.Invoke();
				}
			}
		}

		public override void Reset()
		{
		}
	}



	sealed class StorableModifier : AtomWithMovementModifier
	{
		public static string FactoryTypeName { get; } = "storable";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Storable";
		public override string GetDisplayName() { return DisplayName; }


		private JSONStorable storable_ = null;
		private IStorableParameter parameter_ = null;


		public StorableModifier()
			: this(null, null)
		{
		}

		public StorableModifier(JSONStorable s, IStorableParameter p)
		{
			storable_ = s;
			parameter_ = p;
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
			get { return storable_; }
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

			if (parameter_ != null)
			{
				if (!SetParameterImpl(parameter_.Name))
					parameter_ = null;
			}
		}

		public void SetParameter(string name)
		{
			if (!SetParameterImpl(name))
			{
				parameter_ = null;

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
			parameter_?.Reset();
		}

		public override FloatRange PreferredRange
		{
			get
			{
				if (parameter_ == null)
					return new FloatRange(0, 1);
				else
					return parameter_.PreferredRange;
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
		}

		protected override void DoSet(bool paused)
		{
			base.DoSet(paused);

			if (parameter_ != null)
				parameter_.Set(Movement.Magnitude);
		}

		protected override string MakeName()
		{
			string s = "S ";

			if (parameter_ == null)
				s += "none";
			else
				s += parameter_.GetDisplayName();

			return s;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

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

			o.Opt<StorableParameterFactory, IStorableParameter>(
				"parameter", ref parameter_);

			return true;
		}

		protected override void AtomChanged()
		{
			string oldStorable = storable_?.name ?? "";
			string oldParameter = parameter_?.Name ?? "";


			if (oldStorable == "")
				storable_ = null;
			else
				storable_ = Atom.GetStorableByID(oldStorable);


			if (storable_ == null || oldParameter == "")
			{
				parameter_ = null;
			}
			else
			{
				if (!SetParameterImpl(oldParameter))
					parameter_ = null;
			}
		}
	}
}
