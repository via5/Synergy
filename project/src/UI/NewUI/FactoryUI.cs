using System;
using System.Collections.Generic;

namespace Synergy.NewUI
{
	interface IUIFactoryWidget<ObjectType> : UI.IWidget
		where ObjectType : IFactoryObject
	{
		void Set(ObjectType o);
	}

	interface IUIFactory<ObjectType>
		where ObjectType : IFactoryObject
	{
		Dictionary<string, Func<IUIFactoryWidget<ObjectType>>> GetCreators();
	}



	class FactoryObjectWidget<FactoryType, ObjectType, UIFactoryType> : UI.Panel
		where FactoryType : IFactory<ObjectType>
		where ObjectType : IFactoryObject
		where UIFactoryType : IUIFactory<ObjectType>, new()
	{
		private readonly UIFactoryType factory_ = new UIFactoryType();
		private IUIFactoryWidget<ObjectType> widget_ = null;
		private string currentType_ = "";

		public FactoryObjectWidget()
			: base(new UI.BorderLayout())
		{
		}

		public IUIFactoryWidget<ObjectType> FactoryWidget
		{
			get { return widget_; }
		}

		public void Set(ObjectType o)
		{
			if (o == null)
			{
				if (widget_ != null)
				{
					widget_.Remove();
					widget_ = null;
				}

				currentType_ = "";
			}
			else
			{
				var type = o.GetFactoryTypeName();

				if (currentType_ == type)
				{
					widget_.Set(o);
				}
				else
				{
					if (widget_ != null)
					{
						widget_.Remove();
						widget_ = null;
					}

					currentType_ = "";

					widget_ = Create(type);

					if (widget_ != null)
					{
						currentType_ = type;
						AddGeneric(widget_, UI.BorderLayout.Center);
						widget_.Set(o);
					}
				}
			}
		}

		private IUIFactoryWidget<ObjectType> Create(string factoryTypeName)
		{
			var creators = factory_.GetCreators();

			Func<IUIFactoryWidget<ObjectType>> creator = null;
			if (creators.TryGetValue(factoryTypeName, out creator))
				return creator();

			Synergy.LogError(
				"ui factory: bad type name '" + factoryTypeName + "'");

			return null;
		}
	}


	class FactoryComboBox<FactoryType, ObjectType>
		: UI.ComboBox<FactoryComboBoxItem<ObjectType>>
			where FactoryType : IGenericFactory, new()
			where ObjectType : class, IFactoryObject
	{
		public delegate void FactoryTypeCallback(ObjectType o);
		public event FactoryTypeCallback FactoryTypeChanged;

		public FactoryComboBox(FactoryTypeCallback factoryTypeChanged = null)
		{
			var f = new FactoryType();

			foreach (var creator in f.GetAllCreators())
				AddItem(new FactoryComboBoxItem<ObjectType>(creator));

			SelectionChanged += OnSelectionChanged;

			if (factoryTypeChanged != null)
				FactoryTypeChanged += factoryTypeChanged;
		}

		private void OnSelectionChanged(FactoryComboBoxItem<ObjectType> item)
		{
			if (item == null)
				FactoryTypeChanged?.Invoke(null);
			else
				FactoryTypeChanged?.Invoke(item.CreateFactoryObject());
		}

		public void Select(ObjectType d)
		{
			Select(IndexOf(d));
		}

		public int IndexOf(ObjectType d)
		{
			if (d == null)
				return -1;

			var items = Items;

			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].FactoryTypeName == d.GetFactoryTypeName())
					return i;
			}

			return -1;
		}
	}
}
