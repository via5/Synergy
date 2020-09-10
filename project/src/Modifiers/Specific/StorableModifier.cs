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

		static public IStorableParameter Create(
			Atom a, JSONStorable st, JSONStorableParam p)
		{
			if (p is JSONStorableFloat)
				return new FloatStorableParameter(a, st, p as JSONStorableFloat);
			else if (p is JSONStorableBool)
				return new BoolStorableParameter(a, st, p as JSONStorableBool);
			else if (p is JSONStorableColor)
				return new ColorStorableParameter(a, st, p as JSONStorableColor);
			else if (p is JSONStorableUrl)
				return new UrlStorableParameter(a, st, p as JSONStorableUrl);
			else if (p is JSONStorableString)
				return new StringStorableParameter(a, st, p as JSONStorableString);
			else
				return null;
		}

		static public IStorableParameter Create(
			Atom a, string stName, string paramName)
		{
			var st = a.GetStorableByID(stName);

			var param = st.GetParam(paramName);
			if (param != null)
				return Create(a, st, param);

			var action = st.GetAction(paramName);
			if (action != null)
				return new ActionStorableParameter(a, st, action);

			return null;
		}
	}


	abstract class BasicStorableParameter : IStorableParameter
	{
		private Atom atom_ = null;
		private JSONStorable storable_ = null;


		protected BasicStorableParameter(Atom a, JSONStorable st)
		{
			atom_ = a;
			storable_ = st;
		}

		public abstract IStorableParameter Clone(int cloneFlags = 0);

		protected virtual void CopyTo(BasicStorableParameter p, int cloneFlags)
		{
			p.atom_ = atom_;
			p.storable_ = storable_;
		}

		public Atom Atom
		{
			get { return atom_; }
		}

		public JSONStorable Storable
		{
			get { return storable_; }
			set { storable_ = value; }
		}

		public virtual FloatRange PreferredRange
		{
			get
			{
				return new FloatRange(0, 1);
			}
		}

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


	class FloatStorableParameter : BasicStorableParameter
	{
		public override string GetFactoryTypeName() { return "float"; }
		public override string GetDisplayName() { return "Float"; }


		private JSONStorableFloat param_ = null;

		public FloatStorableParameter()
			: this(null, null, null)
		{
		}

		public FloatStorableParameter(Atom a, JSONStorable s, JSONStorableFloat p)
			: base(a, s)
		{
			param_ = p;
		}

		public override IStorableParameter Clone(int cloneFlags = 0)
		{
			var p = new FloatStorableParameter();
			CopyTo(p, cloneFlags);
			return p;
		}

		protected void CopyTo(FloatStorableParameter p, int cloneFlags)
		{
			base.CopyTo(p, cloneFlags);
			p.param_ = param_;
		}

		public override void Set(float magnitude)
		{
			if (param_ != null)
				param_.valNoCallback = magnitude;
		}

		public override void Reset()
		{
			if (param_ != null)
				param_.valNoCallback = param_.defaultVal;
		}
	}


	class BoolStorableParameter : BasicStorableParameter
	{
		public override string GetFactoryTypeName() { return "bool"; }
		public override string GetDisplayName() { return "Bool"; }


		private JSONStorableBool param_ = null;

		public BoolStorableParameter()
			: this(null, null, null)
		{
		}

		public BoolStorableParameter(Atom a, JSONStorable s, JSONStorableBool p)
			: base(a, s)
		{
			param_ = p;
		}

		public override IStorableParameter Clone(int cloneFlags = 0)
		{
			var p = new BoolStorableParameter();
			CopyTo(p, cloneFlags);
			return p;
		}

		protected void CopyTo(BoolStorableParameter p, int cloneFlags)
		{
			base.CopyTo(p, cloneFlags);
			p.param_ = param_;
		}

		public override void Set(float magnitude)
		{
			if (param_ != null)
				param_.valNoCallback = (magnitude > 0.5f);
		}

		public override void Reset()
		{
			if (param_ != null)
				param_.valNoCallback = param_.defaultVal;
		}
	}


	class ColorStorableParameter : BasicStorableParameter
	{
		public override string GetFactoryTypeName() { return "color"; }
		public override string GetDisplayName() { return "Color"; }


		private JSONStorableColor param_ = null;
		private Color c1_, c2_;

		public ColorStorableParameter()
			: this(null, null, null)
		{
		}

		public ColorStorableParameter(Atom a, JSONStorable s, JSONStorableColor p)
			: base(a, s)
		{
			param_ = p;
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
			p.param_ = param_;
		}

		public override void Set(float magnitude)
		{
			if (param_ != null)
			{
				var c = new Color(
					Interpolate(c1_.r, c2_.r, magnitude),
					Interpolate(c1_.g, c2_.g, magnitude),
					Interpolate(c1_.b, c2_.b, magnitude));

				param_.SetColor(c);
			}
		}

		private float Interpolate(float a, float b, float magnitude)
		{
			return a + (b - a) * magnitude;
		}

		public override void Reset()
		{
			if (param_ != null)
				param_.valNoCallback = param_.defaultVal;
		}
	}


	class StringStorableParameter : BasicStorableParameter
	{
		public override string GetFactoryTypeName() { return "string"; }
		public override string GetDisplayName() { return "String"; }


		private JSONStorableString param_ = null;
		private List<string> strings_ = new List<string>();
		private string last_ = null;

		public StringStorableParameter()
			: this(null, null, null)
		{
		}

		public StringStorableParameter(Atom a, JSONStorable s, JSONStorableString p)
			: base(a, s)
		{
			param_ = p;
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
			p.param_ = param_;
			p.strings_ = new List<string>(strings_);
		}

		public List<string> Strings
		{
			get { return strings_; }
		}


		public override void Set(float magnitude)
		{
			if (param_ != null)
			{
				if (strings_.Count == 0)
					return;

				var magPer = 1.0f / strings_.Count;
				var i = Math.Min((int)Math.Floor(magnitude / magPer), strings_.Count - 1);

				var next = strings_[i];

				if (next != last_)
				{
					last_ = next;
					param_.val = next;
					Synergy.LogError(next);
				}
			}
		}

		public override void Reset()
		{
			if (param_ != null)
				param_.val = param_.defaultVal;
		}
	}


	class UrlStorableParameter : StringStorableParameter
	{
		public override string GetFactoryTypeName() { return "url"; }
		public override string GetDisplayName() { return "URL"; }


		private JSONStorableUrl param_ = null;

		public UrlStorableParameter()
			: this(null, null, null)
		{
		}

		public UrlStorableParameter(Atom a, JSONStorable s, JSONStorableUrl p)
			: base(a, s, p)
		{
			param_ = p;
		}

		public override IStorableParameter Clone(int cloneFlags = 0)
		{
			var p = new UrlStorableParameter();
			CopyTo(p, cloneFlags);
			return p;
		}

		protected void CopyTo(UrlStorableParameter p, int cloneFlags)
		{
			base.CopyTo(p, cloneFlags);
			p.param_ = param_;
		}
	}


	class ActionStorableParameter : BasicStorableParameter
	{
		public override string GetFactoryTypeName() { return "action"; }
		public override string GetDisplayName() { return "Action"; }


		private JSONStorableAction param_ = null;
		private bool active_ = false;

		public ActionStorableParameter()
			: this(null, (JSONStorable)null, null)
		{
		}

		public ActionStorableParameter(Atom a, JSONStorable s, JSONStorableAction p)
			: base(a, s)
		{
			param_ = p;
		}

		public ActionStorableParameter(Atom a, string stName, string actionName)
			: base(a, null)
		{
			Storable = Atom.GetStorableByID(stName);
			if (Storable != null)
				param_ = Storable.GetAction(actionName);
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

		public override void Set(float magnitude)
		{
			if (param_ != null)
			{
				if (magnitude > 0.5f && !active_)
				{
					active_ = true;

					if (param_.actionCallback != null)
						param_.actionCallback();
				}
				else if (magnitude <= 0.5f && active_)
				{
					active_ = false;

					if (param_.actionCallback != null)
						param_.actionCallback();
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


		private IStorableParameter property_ = null;


		public StorableModifier(IStorableParameter property = null)
		{
			Property = property;
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
			m.Property = Property?.Clone(cloneFlags);
		}

		public override void Removed()
		{
			base.Removed();
			ResetProperty();
		}

		public IStorableParameter Property
		{
			get
			{
				return property_;
			}

			set
			{
				ResetProperty();
				property_ = value;
				FirePreferredRangeChanged();
			}
		}


		private void ResetProperty()
		{
			property_?.Reset();
		}

		public override FloatRange PreferredRange
		{
			get
			{
				if (property_ == null)
					return new FloatRange(0, 1);
				else
					return property_.PreferredRange;
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

			if (property_ != null)
				property_.Set(Movement.Magnitude);
		}

		protected override string MakeName()
		{
			string s = "S ";

			if (property_ == null)
				s += "none";
			else
				s += property_.GetDisplayName();

			return s;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("property", property_);

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
				"property", ref property_);

			return true;
		}
	}
}
