using System.Windows;

namespace УправлениеСкладом
{
	public partial class AdministratorWindow : Window, IRoleWindow
	{
		public AdministratorWindow()
		{
			InitializeComponent();
		}

		public void ShowWindow()
		{
			Show();
		}
	}
}
