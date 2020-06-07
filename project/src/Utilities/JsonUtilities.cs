using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace Synergy
{
	class SaveTypes
	{
		public const int None = 0;
		public const int Scene = 1;
		public const int Preset = 2;
	}

	interface IJsonable
	{
		J.Node ToJSON();
		bool FromJSON(J.Node n);
	}


	namespace J
	{
		class Node
		{
			protected JSONNode node_;

			protected Node(JSONNode n)
			{
				node_ = n;
			}

			public static int SaveType { get; set; } = SaveTypes.None;

			public static Node Wrap(JSONNode n)
			{
				return new Node(n);
			}

			public JSONNode Impl
			{
				get { return node_; }
			}

			public Object AsObject(string what="")
			{
				var o = node_ as JSONClass;
				if (o == null)
				{
					if (what != "")
						Synergy.LogError(what + " is not an object");

					return null;
				}

				return Object.Wrap(o);
			}

			public Array AsArray(string what = "")
			{
				var a = node_ as JSONArray;
				if (a == null)
				{
					if (what != "")
						Synergy.LogError(what + " is not an array");

					return null;
				}

				return Array.Wrap(a);
			}

			public string AsString(string what="")
			{
				if (node_ == null || (string.IsNullOrEmpty(node_.Value)))
				{
					if (what != "")
						Synergy.LogError(what + " is not a string");

					return null;
				}

				return node_.Value;
			}

			public void Save(string path)
			{
				var n = node_ as JSONClass;
				if (n == null)
				{
					Synergy.LogError("can't save, empty");
					return;
				}

				SuperController.singleton.SaveJSON(n, path);
			}
		}

		class Object : Node
		{
			private readonly JSONClass c_;

			public Object()
				: base(new JSONClass())
			{
				c_ = node_ as JSONClass;
			}

			private Object(JSONClass c)
				: base(c)
			{
				c_ = c;
			}


			public static Object Wrap(JSONClass c)
			{
				return new Object(c);
			}


			public bool HasKey(string s)
			{
				return (c_?.HasKey(s) ?? false);
			}

			public bool HasChildObject(string key)
			{
				if (c_ == null)
					return false;

				if (!c_.HasKey(key))
					return false;

				return (c_[key] is JSONClass);
			}

			public bool HasChildArray(string key)
			{
				if (c_ == null)
					return false;

				if (!c_.HasKey(key))
					return false;

				return (c_[key] is JSONArray);
			}

			public J.Node Get(string key)
			{
				return J.Node.Wrap(c_?[key]);
			}


			public void Add<T>(string key, List<T> list)
				where T : IJsonable
			{
				if (list.Count > 0)
				{
					var array = new JSONArray();

					foreach (var element in list)
					{
						var n = element.ToJSON();
						if (n?.Impl != null)
							array.Add(n.Impl);
					}

					c_?.Add(key, array);
				}
			}

			public void Add(string key, string v)
			{
				if (!string.IsNullOrEmpty(v))
					c_?.Add(key, new JSONData(v));
			}

			public void Add(string key, IJsonable v)
			{
				var n = v?.ToJSON()?.Impl;

				if (n != null)
					c_?.Add(key, n);
			}

			public void Add(string key, bool b)
			{
				c_?.Add(key, new JSONData(b));
			}

			public void Add(string key, BoolParameter b)
			{
				var o = new J.Object();
				o.Add("value", b.InternalValue);

				if (b.Registered)
					o.Add("parameter", b.Name);

				c_?.Add(key, o.Impl);
			}

			public void Add(string key, FloatParameter b)
			{
				var o = new J.Object();
				o.Add("value", b.InternalValue);

				if (b.Registered)
					o.Add("parameter", b.Name);

				c_?.Add(key, o.Impl);
			}

			public void Add(string key, IntParameter b)
			{
				var o = new J.Object();
				o.Add("value", b.InternalValue);

				if (b.Registered)
					o.Add("parameter", b.Name);

				c_?.Add(key, o.Impl);
			}

			public void Add(string key, float f)
			{
				c_?.Add(key, new JSONData(f));
			}

			public void Add(string key, int i)
			{
				c_?.Add(key, new JSONData(i));
			}

			public void Add(string key, Node n)
			{
				c_?.Add(key, n.Impl);
			}



			public void Opt<T>(string key, ref List<T> list)
				where T : IJsonable, new()
			{
				if (!HasKey(key))
					return;

				var array = c_[key] as JSONArray;
				if (array == null)
				{
					Synergy.LogError("key '" + key + "' is not an array");
					return;
				}

				list.Clear();

				foreach (JSONNode n in array)
				{
					var v = new T();

					if (v.FromJSON(Node.Wrap(n)))
						list.Add(v);
				}
			}

			public void Opt(string key, ref bool v)
			{
				if (HasKey(key))
					v = c_[key].AsBool;
			}

			public void Opt(string key, ref bool v, bool def)
			{
				if (HasKey(key))
					v = c_[key].AsBool;
				else
					v = def;
			}

			public void Opt(string key, ref BoolParameter v)
			{
				if (!HasKey(key))
					return;

				var node = c_[key];

				var o = node as JSONClass;

				if (o == null)
				{
					v.Value = node.AsBool;
				}
				else
				{
					v.Value = o["value"].AsBool;

					if (o.HasKey("parameter"))
					{
						v.Name = o["parameter"];
						v.Register();
					}
				}
			}

			public void Opt(string key, ref FloatParameter v)
			{
				if (!HasKey(key))
					return;

				var node = c_[key];

				var o = node as JSONClass;

				if (o == null)
				{
					v.Value = node.AsFloat;
				}
				else
				{
					v.Value = o["value"].AsFloat;

					if (o.HasKey("parameter"))
					{
						v.Name = o["parameter"];
						v.Register();
					}
				}
			}

			public void Opt(string key, ref IntParameter v)
			{
				if (!HasKey(key))
					return;

				var node = c_[key];

				var o = node as JSONClass;

				if (o == null)
				{
					v.Value = node.AsInt;
				}
				else
				{
					v.Value = o["value"].AsInt;

					if (o.HasKey("parameter"))
					{
						v.Name = o["parameter"];
						v.Register();
					}
				}
			}

			public void Opt(string key, ref float v)
			{
				if (HasKey(key))
					v = c_[key].AsFloat;
			}

			public void Opt(string key, ref float v, float def)
			{
				if (HasKey(key))
					v = c_[key].AsFloat;
				else
					v = def;
			}

			public void Opt(string key, ref int v)
			{
				if (HasKey(key))
					v = c_[key].AsInt;
			}

			public void Opt(string key, ref int v, int def)
			{
				if (HasKey(key))
					v = c_[key].AsInt;
				else
					v = def;
			}

			public void Opt(string key, ref string v)
			{
				if (HasKey(key))
					v = c_[key].Value;
			}

			public void Opt(string key, ref string v, string def)
			{
				if (HasKey(key))
					v = c_[key].Value;
				else
					v = def;
			}

			public void Opt<T>(string key, ref T v)
				where T : IJsonable
			{
				if (HasKey(key))
					v.FromJSON(Node.Wrap(c_[key]));
			}


			public void Add(string key, IFactoryObject v)
			{
				if (v == null || c_ == null)
					return;

				var o = v.ToJSON().AsObject();

				if (o == null)
				{
					Synergy.LogError(
						"Factory object ToJson() did not return an object");

					return;
				}

				o.Add("factoryTypeName", v.GetFactoryTypeName());

				c_.Add(key, o.Impl);
			}

			public void Opt<Factory, T>(string key, ref T v)
					where Factory : IFactory<T>, new()
					where T : IFactoryObject
			{
				if (!HasKey(key))
					return;

				var n = c_[key];
				var nc = n as JSONClass;

				if (nc == null)
					return;

				if (!nc.HasKey("factoryTypeName"))
					return;

				var typeName = nc["factoryTypeName"].Value;

				var temp = new Factory().Create(typeName);
				if (temp == null)
					return;

				if (!temp.FromJSON(Object.Wrap(nc)))
					return;

				v = temp;
			}


			public void OptRigidbody(string key, Atom atom, ref Rigidbody v)
			{
				if (atom == null)
					return;

				if (!HasKey(key))
					return;

				var name = c_[key].Value;
				var rb = Utilities.FindRigidbody(atom, name);

				if (rb == null)
				{
					Synergy.LogError(
						"receiver '" + name + "' not " +
						"found in atom '" + atom.uid + "'");

					return;
				}

				v = rb;
			}
		}


		class Array : Node
		{
			private readonly JSONArray a_;

			public Array()
				: base(new JSONArray())
			{
				a_ = node_ as JSONArray;
			}

			private Array(JSONArray a)
				: base(a)
			{
				a_ = a;
			}

			public static Array Wrap(JSONArray a)
			{
				return new Array(a);
			}


			public void Add(string v)
			{
				a_?.Add(new JSONData(v));
			}

			public void Add(IJsonable v)
			{
				if (v != null)
				{
					var n = v.ToJSON();
					if (n != null)
						a_?.Add(n.Impl);
				}
			}

			public void Add(bool b)
			{
				a_?.Add(new JSONData(b));
			}

			public void Add(float f)
			{
				a_?.Add(new JSONData(f));
			}

			public void Add(int i)
			{
				a_?.Add(new JSONData(i));
			}

			public void Add(Node n)
			{
				if (n?.Impl != null)
					a_?.Add(n.Impl);
			}

			public void ForEach(Action<J.Node> f)
			{
				if (a_ == null)
					return;

				foreach (JSONNode n in a_)
					f(J.Node.Wrap(n));
			}
		}


		class Wrappers
		{
			public static J.Node ToJSON(Vector3 v)
			{
				var o = new J.Object();

				o.Add("x", v.x);
				o.Add("y", v.y);
				o.Add("z", v.z);

				return o;
			}

			public static bool FromJSON(J.Node n, ref Vector3 v)
			{
				var o = n.AsObject("Vector3");
				if (o == null)
					return false;

				o.Opt("x", ref v.x);
				o.Opt("y", ref v.y);
				o.Opt("z", ref v.z);

				return true;
			}
		}
	}
}
