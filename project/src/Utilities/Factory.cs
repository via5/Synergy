using System.Collections.Generic;

namespace Synergy
{
	public interface IFactoryObjectCreator
	{
		IFactoryObject Create();
		string FactoryTypeName { get; }
		string DisplayName { get; }
	}

	public class FactoryObjectCreator<FactoryType, ObjectType> : IFactoryObjectCreator
		where FactoryType : IFactory<ObjectType>
		where ObjectType : IFactoryObject
	{
		private readonly FactoryType factory_;
		private readonly string typeName_;
		private readonly string displayName_;

		public FactoryObjectCreator(
			FactoryType factory, string typeName, string displayName)
		{
			factory_ = factory;
			typeName_ = typeName;
			displayName_ = displayName;
		}

		public IFactoryObject Create()
		{
			return factory_.Create(typeName_);
		}

		public string FactoryTypeName
		{
			get { return typeName_; }
		}

		public string DisplayName
		{
			get { return displayName_; }
		}
	}


	public interface IGenericFactory
	{
		List<string> GetAllDisplayNames();
		List<string> GetAllFactoryTypeNames();
		List<IFactoryObjectCreator> GetAllCreators();
	}

	public interface IFactory<T> : IGenericFactory
	{
		T Create(string typeName);
		List<T> GetAllObjects();
	}

	public abstract class BasicFactory<T> : IFactory<T>
		where T : class, IFactoryObject
	{
		public abstract List<T> GetAllObjects();

		public virtual T Create(string s)
		{
			foreach (var e in GetAllObjects())
			{
				if (e.GetFactoryTypeName() == s)
					return e;
			}

			Synergy.LogError("factory object type '" + s + "' not found");
			return null;
		}

		public virtual List<string> GetAllDisplayNames()
		{
			var names = new List<string>();

			foreach (var e in GetAllObjects())
				names.Add(e.GetDisplayName());

			return names;
		}

		public virtual List<string> GetAllFactoryTypeNames()
		{
			var names = new List<string>();

			foreach (var e in GetAllObjects())
				names.Add(e.GetFactoryTypeName());

			return names;
		}

		public virtual List<IFactoryObjectCreator> GetAllCreators()
		{
			var displayNames = GetAllDisplayNames();
			var typeNames = GetAllFactoryTypeNames();
			var creators = new List<IFactoryObjectCreator>();

			for (int i = 0; i < displayNames.Count; ++i)
			{
				creators.Add(new FactoryObjectCreator<BasicFactory<T>, T>(
					this, typeNames[i], displayNames[i]));
			}

			return creators;
		}
	}

	public interface IFactoryObject
	{
		string GetFactoryTypeName();
		string GetDisplayName();
		J.Node ToJSON();
		bool FromJSON(J.Node n);
	}
}
