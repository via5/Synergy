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
			: this(null, null, null)
		{
		}

		public ModifierContainer(string name)
			: this(name, null, null)
		{
			UserDefinedName = name;
		}

		public ModifierContainer(IModifier m, IModifierSync sync = null)
			: this(null, m, sync)
		{
		}

		public ModifierContainer(string name, IModifier m, IModifierSync sync)
		{
			UserDefinedName = name;
			Modifier = m;
			ModifierSync = sync ?? new DurationSyncedModifier();
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
			m.name_ = CloneName();
			m.Modifier = Modifier?.Clone(cloneFlags);
			m.ModifierSync = ModifierSync?.Clone(cloneFlags);
			m.enabled_.Value = enabled_.Value;
		}

		private string CloneName()
		{
			return Utilities.CloneName(name_, (newName) =>
			{
				if (parent_ == null)
					return true;

				return (parent_.FindModifier(newName) == null);
			});
		}

		public void DeferredInit()
		{
			Modifier?.DeferredInit();
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

				FireNameChanged();
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
				if (string.IsNullOrEmpty(value))
					name_ = null;
				else
					name_ = value;

				FireNameChanged();
			}
		}

		public string Name
		{
			get
			{
				if (name_ != null)
					return name_;

				if (Modifier != null)
					return Modifier.Name;

				return "Empty modifier";
			}
		}

		public override string ToString()
		{
			if (ParentStep == null)
				return "?";

			string s = Name;

			var i = ParentStep.IndexOfModifier(this);
			if (i >= 0)
				s = "#" + (i + 1).ToString() + " " + s;

			return s;
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
					sync_.HeldValue.ParentModifierContainer = this;
			}
		}

		public void PluginEnabled(bool b)
		{
			Modifier?.PluginEnabled(b);
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
