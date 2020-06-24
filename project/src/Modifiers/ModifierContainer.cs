namespace Synergy
{
	sealed class ModifierContainer : IJsonable
	{
		public delegate void ModifierNameChangedHandler(IModifier m);
		public event ModifierNameChangedHandler NameChanged;

		private Step parent_ = null;
		private string name_ = null;

		private readonly ExplicitHolder<IModifier> modifier_ =
			new ExplicitHolder<IModifier>();

		private readonly ExplicitHolder<IModifierSync> sync_ =
			new ExplicitHolder<IModifierSync>();

		private readonly BoolParameter enabled_ =
			new BoolParameter("Enabled", true);


		public ModifierContainer()
			: this(null)
		{
		}

		public ModifierContainer(IModifier m)
		{
			Modifier = m;
			ModifierSync = new DurationSyncedModifier();
		}

		public ModifierContainer Clone(int cloneFlags = 0)
		{
			var m = new ModifierContainer();
			CopyTo(m, cloneFlags);
			return m;
		}

		private void CopyTo(ModifierContainer m, int cloneFlags)
		{
			m.parent_ = parent_;
			m.ModifierSync = ModifierSync?.Clone(cloneFlags);
			m.Modifier = Modifier?.Clone(cloneFlags);
			m.enabled_.Value = enabled_.Value;
		}


		public Step ParentStep
		{
			get
			{
				return parent_;
			}

			set
			{
				parent_ = value;
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
					modifier_.HeldValue.ParentContainer = this;
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

		public string UserDefinedName
		{
			get
			{
				return name_;
			}

			set
			{
				name_ = value;
				FireNameChanged();
			}
		}

		public string Name
		{
			get
			{
				if (name_ == null)
				{
					if (Modifier == null)
					{
						if (parent_ == null)
						{
							return "Modifier";
						}
						else
						{
							var i = parent_.IndexOfModifier(this);
							return "Modifier " + (i + 1).ToString();
						}
					}
					else
					{
						return Modifier.Name;
					}
				}
				else
				{
					return name_;
				}
			}
		}

		public IModifierSync ModifierSync
		{
			get
			{
				return sync_.HeldValue;
			}

			set
			{
				sync_.HeldValue?.Removed();
				sync_.Set(value);

				if (sync_.HeldValue != null)
					sync_.HeldValue.ParentModifier = Modifier;
			}
		}

		public override string ToString()
		{
			return Name;
		}

		public void Added()
		{
			enabled_.BaseName = Name;
		}

		public void Removed()
		{
			Modifier = null;
			ParentStep = null;
			ModifierSync = null;
			enabled_.Unregister();
		}

		public void FireNameChanged()
		{
			if (parent_ != null)
				parent_.FireModifierNameChanged(Modifier);

			NameChanged?.Invoke(Modifier);
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

			if (Modifier != null)
				Modifier.PostLoad();

			o.Opt("enabled", enabled_);

			return true;
		}
	}
}
