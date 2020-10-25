using System;
using System.Collections.Generic;
using System.Text;

namespace Synergy
{
	class StepExtenderModifier : AtomModifier
	{
		public static string FactoryTypeName { get; } = "stepextender";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Step extender";
		public override string GetDisplayName() { return DisplayName; }


		private StorableParameterHolder holder_ = new StorableParameterHolder();


		public StepExtenderModifier()
		{
			holder_.Atom = Atom;
		}

		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new StepExtenderModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		private void CopyTo(StepExtenderModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
			m.holder_ = holder_.Clone(cloneFlags);
		}

		public override FloatRange PreferredRange
		{
			get { return new FloatRange(0, 0); }
		}

		public override float TimeRemaining
		{
			get
			{
				var p = (FloatStorableParameter)holder_.Parameter;
				return p?.Value ?? 0;
			}
		}

		public override bool Finished
		{
			get
			{
				return TimeRemaining <= 0;
			}
		}

		public override bool HardDuration
		{
			get { return true; }
		}

		public override bool UsesSync
		{
			get { return false; }
		}

		public FloatStorableParameter Parameter
		{
			get
			{
				return (FloatStorableParameter)holder_.Parameter;
			}

			set
			{
				holder_.Parameter = value;
			}
		}

		public JSONStorable Storable
		{
			get { return holder_.Storable; }
		}

		public StorableParameterHolder Holder
		{
			get { return holder_; }
		}

		public void SetStorable(string id)
		{
			holder_.SetStorable(id);
		}

		protected override string MakeName()
		{
			string s = "StepExt ";

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

		public void SetParameter(string name)
		{
			holder_.SetParameter(name);
		}

		public void SetParameter(JSONStorableFloat sp)
		{
			holder_.SetParameter(sp);
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

			var o = n.AsObject("StepExtenderModifier");
			if (o == null)
				return false;

			holder_.FromJSON(Atom, o);

			return true;
		}
	}
}