namespace УправлениеСкладом
{
	public static class RoleWindowFactory
	{
		public static IRoleWindow CreateWindow(string role)
		{
			switch (role)
			{
				case "Администратор":
					return new AdministratorWindow();
				case "Менеджер":
					return new ManagerWindow();
				case "Сотрудник склада":
					return new WarehouseStaffWindow();
				default:
					return null;
			}
		}
	}
}
