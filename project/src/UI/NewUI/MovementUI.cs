namespace Synergy.NewUI
{
	class MovementWidgets : UI.Panel
	{
		public MovementWidgets()
		{
			Layout = new UI.HorizontalFlow(5);

			Add(new UI.TextBox());
			Add(new UI.Button("-10"));
			Add(new UI.Button("-1"));
			Add(new UI.Button("0"));
			Add(new UI.Button("+1"));
			Add(new UI.Button("+10"));
			Add(new UI.Button(S("Reset")));
		}
	}
}
