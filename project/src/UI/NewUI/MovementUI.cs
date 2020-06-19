namespace Synergy.NewUI
{
	class MovementWidgets : UI.Panel
	{
		public MovementWidgets()
		{
			Layout = new UI.HorizontalFlow(5);

			Add(new UI.TextBox());
			Add(new UI.ToolButton("-10"));
			Add(new UI.ToolButton("-1"));
			Add(new UI.ToolButton("0"));
			Add(new UI.ToolButton("+1"));
			Add(new UI.ToolButton("+10"));
			Add(new UI.ToolButton(S("Reset")));
		}
	}
}
