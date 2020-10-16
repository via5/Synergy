using System;
using System.Collections.Generic;
using System.Text;

namespace Synergy.src.Modifiers.Specific
{
	class StepExtenderModifier : AtomModifier
	{
		public static string FactoryTypeName { get; } = "stepextender";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Step extender";
		public override string GetDisplayName() { return DisplayName; }


		private FloatStorableParameter storable_;


		public StepExtenderModifier()
		{
			storable_ = new FloatStorableParameter(
				Atom.GetStorableByID("plugin#1_VamTimeline.AtomPlugin")
					.GetFloatJSONParam("Time Remaining"));
		}

		public override FloatRange PreferredRange
		{
			get { return new FloatRange(0, 0); }
		}

		public override float TimeRemaining
		{
			get
			{
				return storable_.Value;
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

		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new StepExtenderModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		protected override string MakeName()
		{
			return "Step extender";
		}
	}
}
