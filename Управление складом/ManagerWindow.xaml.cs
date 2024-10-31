using System.Windows;

namespace УправлениеСкладом
{
	public partial class ManagerWindow : Window, IRoleWindow
	{
		public ManagerWindow()
		{
			InitializeComponent();
		}

		public void ShowWindow()
		{
			Show();
		}
	}
}
