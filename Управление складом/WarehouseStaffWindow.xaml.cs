using System.Windows;

namespace УправлениеСкладом
{
	public partial class WarehouseStaffWindow : Window, IRoleWindow
	{
		public WarehouseStaffWindow()
		{
			InitializeComponent();
		}

		public void ShowWindow()
		{
			Show();
		}
	}
}
