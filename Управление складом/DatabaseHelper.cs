using System;
using System.Data.SqlClient;
using System.Windows;

namespace УправлениеСкладом
{
	public static class DatabaseHelper
	{
		// Обновленная строка подключения
		private static string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public static User AuthenticateUser(string username, string password)
		{
			try
			{
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					// Обновленный SQL-запрос с правильными именами таблиц и столбцов
					string query = "SELECT РольID FROM dbo.Пользователи WHERE ИмяПользователя = @username AND Пароль = @password";
					SqlCommand command = new SqlCommand(query, connection);
					command.Parameters.AddWithValue("@username", username);
					command.Parameters.AddWithValue("@password", password); // В реальном приложении используйте хеширование паролей

					connection.Open();
					SqlDataReader reader = command.ExecuteReader();
					if (reader.Read())
					{
						User user = new User
						{
							Username = username,
							RoleID = Convert.ToInt32(reader["РольID"])
						};
						return user;
					}
					else
					{
						return null;
					}
				}
			}
			catch (SqlException ex)
			{
				// Логирование ошибки и информирование пользователя
				MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return null;
			}
		}
	}
}
