using System.Collections.Generic;

namespace Synergy
{
	interface IFactory<T>
	{
		T Create(string typeName);
		List<T> GetAllObjects();
		List<string> GetAllDisplayNames();
		List<string> GetAllFactoryTypeNames();
	}

	abstract class BasicFactory<T> : IFactory<T>
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
	}

	interface IFactoryObject
	{
		string GetFactoryTypeName();
		string GetDisplayName();
		J.Node ToJSON();
		bool FromJSON(J.Node n);
	}
}
