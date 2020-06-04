namespace Synergy
{
	class ModifierContainer : IJsonable
	{
		private Step step_ = null;
		private IModifier modifier_ = null;
		private bool enabled_ = true;

		public ModifierContainer()
			: this(null)
		{
		}

		public ModifierContainer(IModifier m)
		{
			modifier_ = m;
		}

		public ModifierContainer Clone(int cloneFlags = 0)
		{
			var m = new ModifierContainer();
			CopyTo(m, cloneFlags);
			return m;
		}

		protected void CopyTo(ModifierContainer m, int cloneFlags)
		{
			m.step_ = step_;
			m.modifier_ = modifier_?.Clone(cloneFlags);
			m.enabled_ = enabled_;
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

				if (modifier_ != null)
					modifier_.ParentStep = value;
			}
		}

		public IModifier Modifier
		{
			get
			{
				return modifier_;
			}

			set
			{
				if (modifier_ != null)
				{
					modifier_.AboutToBeRemoved();
					modifier_.ParentStep = null;
				}

				modifier_ = value;

				if (modifier_ != null)
					modifier_.ParentStep = step_;
			}
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				enabled_ = value;
				if (!enabled_ && modifier_ != null)
					modifier_.Reset();
			}
		}

		public string Name
		{
			get
			{
				if (modifier_ == null)
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
					return modifier_.Name;
				}
			}
		}

		public void AboutToBeRemoved()
		{
			modifier_?.AboutToBeRemoved();
		}

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("modifier", modifier_);
			o.Add("enabled", enabled_);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			var o = n.AsObject("StepModifier");
			if (o == null)
				return false;

			o.Opt<ModifierFactory, IModifier>("modifier", ref modifier_);
			o.Opt("enabled", ref enabled_);

			return true;
		}
	}
}
