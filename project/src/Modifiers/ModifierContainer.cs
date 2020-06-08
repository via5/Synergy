namespace Synergy
{
	sealed class ModifierContainer : IJsonable
	{
		private Step step_ = null;

		private readonly ExplicitHolder<IModifier> modifier_ =
			new ExplicitHolder<IModifier>();

		private readonly BoolParameter enabled_ =
			new BoolParameter("Enabled", true);


		public ModifierContainer()
			: this(null)
		{
		}

		public ModifierContainer(IModifier m)
		{
			Modifier = m;
		}

		public ModifierContainer Clone(int cloneFlags = 0)
		{
			var m = new ModifierContainer();
			CopyTo(m, cloneFlags);
			return m;
		}

		private void CopyTo(ModifierContainer m, int cloneFlags)
		{
			m.step_ = step_;
			m.Modifier = Modifier?.Clone(cloneFlags);
			m.enabled_.Value = enabled_.Value;
		}


		public Step Step
		{
			get
			{
				return step_;
			}

			set
			{
				step_ = value;

				if (Modifier != null)
					Modifier.ParentStep = value;
			}
		}

		public IModifier Modifier
		{
			get
			{
				return modifier_.HeldValue;
			}

			set
			{
				modifier_.HeldValue?.Removed();
				modifier_.Set(value);

				if (modifier_.HeldValue != null)
					modifier_.HeldValue.ParentStep = step_;
			}
		}

		public bool Enabled
		{
			get
			{
				return enabled_.Value;
			}

			set
			{
				enabled_.Value = value;
				if (!enabled_.Value && Modifier != null)
					Modifier.Reset();
			}
		}

		public BoolParameter EnabledParameter
		{
			get { return enabled_; }
		}

		public string Name
		{
			get
			{
				if (Modifier == null)
				{
					if (step_ == null)
					{
						return "Modifier";
					}
					else
					{
						var i = step_.IndexOfModifier(this);
						return "Modifier " + (i + 1).ToString();
					}
				}
				else
				{
					return Modifier.Name;
				}
			}
		}

		public void Added()
		{
			enabled_.BaseName = Name;
		}

		public void Removed()
		{
			Modifier = null;
			enabled_.Unregister();
		}

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("modifier", Modifier);
			o.Add("enabled", enabled_);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			var o = n.AsObject("StepModifier");
			if (o == null)
				return false;

			IModifier m = null;
			o.Opt<ModifierFactory, IModifier>("modifier", ref m);
			Modifier = m;

			o.Opt("enabled", enabled_);

			return true;
		}
	}
}
