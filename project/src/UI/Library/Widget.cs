﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Widget
	{
		static public float DontCare = -1;

		private Widget parent_ = null;
		private string name_ = "";
		private readonly List<Widget> children_ = new List<Widget>();
		private Layout layout_ = null;
		private Rectangle bounds_ = new Rectangle();
		private Size minSize_ = new Size(0, 0);
		private GameObject object_ = null;

		public Widget(string name = "")
		{
			name_ = name;
		}

		public Layout Layout
		{
			get
			{
				return layout_;
			}

			set
			{
				layout_ = value;

				if (layout_ != null)
					layout_.Parent = this;
			}
		}

		public GameObject Object
		{
			get { return object_; }
		}

		public T Add<T>(T w, LayoutData d = null)
			where T : Widget
		{
			w.parent_ = this;
			children_.Add(w);
			layout_?.Add(w, d);
			return w;
		}

		public void DoLayout()
		{
			layout_?.DoLayout();

			foreach (var w in children_)
				w.DoLayout();
		}

		public void Create()
		{
			object_ = CreateGameObject();

			SetupGameObject();
			DoCreate();

			foreach (var w in children_)
				w.Create();
		}

		public Size PreferredSize
		{
			get
			{
				var s = new Size();

				if (layout_ != null)
					s = layout_.PreferredSize;

				s = Size.Max(s, GetPreferredSize());
				s = Size.Max(s, MinimumSize);

				return s;
			}
		}

		public Size MinimumSize
		{
			get { return minSize_; }
			set { minSize_ = value; }
		}


		protected virtual void DoCreate()
		{
			// no-op
		}

		protected virtual Size GetPreferredSize()
		{
			return new Size(DontCare, DontCare);
		}

		protected virtual GameObject CreateGameObject()
		{
			var o = new GameObject();

			o.AddComponent<RectTransform>();
			o.AddComponent<LayoutElement>();

			return o;
		}

		protected virtual void SetupGameObject()
		{
			object_.transform.SetParent(Root.PluginParent, false);

			var rect = object_.GetComponent<RectTransform>();
			rect.offsetMin = new Vector2(Bounds.Left, Bounds.Top);
			rect.offsetMax = new Vector2(Bounds.Right, Bounds.Bottom);
			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0, 0);
			rect.anchoredPosition = new Vector2(Bounds.Center.X, -Bounds.Center.Y);

			var layoutElement = object_.GetComponent<LayoutElement>();
			layoutElement.minWidth = Bounds.Width;
			layoutElement.preferredWidth = Bounds.Width;
			layoutElement.flexibleWidth = Bounds.Width;
			layoutElement.minHeight = Bounds.Height;
			layoutElement.preferredHeight = Bounds.Height;
			layoutElement.flexibleHeight = Bounds.Height;
			layoutElement.ignoreLayout = true;
		}

		public Rectangle Bounds
		{
			get { return bounds_; }
			set { bounds_ = value; }
		}

		public void Dump(int indent = 0)
		{
			Synergy.LogError(new string(' ', indent * 2) + DebugLine);

			foreach (var w in children_)
				w.Dump(indent + 1);
		}

		public string Name
		{
			get
			{
				return name_;
			}

			set
			{
				name_ = value;
			}
		}

		public virtual string DebugLine
		{
			get
			{
				var list = new List<string>();

				list.Add(TypeName);
				list.Add(name_);
				list.Add(Bounds.ToString());
				list.Add(PreferredSize.ToString());

				return string.Join(" ", list.ToArray());
			}
		}

		public virtual string TypeName
		{
			get
			{
				return "widget";
			}
		}
	}
}
