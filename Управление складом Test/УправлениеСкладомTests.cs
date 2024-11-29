using NUnit.Framework;
using System.Threading; // Для использования STA-потока
using System.Windows; // Пространство имен для Visibility
using System.Windows.Controls; // Пространство имен для PasswordBox
using УправлениеСкладом; // Пространство имен вашего приложения

namespace УправлениеСкладомTests
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class GeneralTests
	{
		// Тест проверяет корректность создания объекта User и установки его свойств.
		[Test]
		public void User_Creation_SetsPropertiesCorrectly()
		{
			//Подготовка данных для создания пользователя
			var username = "1"; // Имя пользователя
			var roleId = 1; // Идентификатор роли

			//Создание объекта User и установка свойств
			var user = new User { Username = username, RoleID = roleId };

			//Проверка, что свойства объекта User установлены правильно
			Assert.That(user.Username, Is.EqualTo(username));
			Assert.That(user.RoleID, Is.EqualTo(roleId));
		}

		// Тест проверяет, что конвертер преобразует true в Visibility.Visible.
		[Test]
		public void BoolToVisibilityConverter_ConvertsTrueToVisible()
		{
			//Создание объекта BoolToVisibilityConverter
			var converter = new BoolToVisibilityConverter();
			var value = true; // Входное значение для конвертера

			//Вызов метода Convert с входным значением
			var result = converter.Convert(value, null, null, null);

			//Проверка, что результат равен Visibility.Visible
			Assert.That(result, Is.EqualTo(Visibility.Visible));
		}

		// Тест проверяет, что конвертер преобразует false в Visibility.Collapsed.
		[Test]
		public void BoolToVisibilityConverter_ConvertsFalseToCollapsed()
		{
			//Создание объекта BoolToVisibilityConverter
			var converter = new BoolToVisibilityConverter();
			var value = false; // Входное значение для конвертера

			//Вызов метода Convert с входным значением
			var result = converter.Convert(value, null, null, null);

			//Проверка, что результат равен Visibility.Collapsed
			Assert.That(result, Is.EqualTo(Visibility.Collapsed));
		}

		// Тест проверяет, что фабрика окон возвращает корректное окно для роли "Менеджер".
		[Test]
		public void RoleWindowFactory_CreateWindow_ReturnsCorrectWindowForManager()
		{
			//Вызов метода CreateWindow с ролью "Менеджер"
			var window = RoleWindowFactory.CreateWindow("Менеджер");

			//Проверка, что возвращённое окно не null и его тип корректен
			Assert.That(window, Is.Not.Null);
			Assert.That(window.GetType().Name, Is.EqualTo("ManagerWindow"));
		}

		// Тест проверяет, что методы SetPassword и GetPassword работают правильно.
		[Test]
		public void PasswordHelper_SetPasswordProperty_WorksCorrectly()
		{
			//Создание объекта PasswordBox и установка пароля
			var passwordBox = new PasswordBox();
			var expectedPassword = "Secret"; // Ожидаемое значение пароля

			// Установка пароля и его извлечение
			PasswordHelper.SetPassword(passwordBox, expectedPassword);
			var actualPassword = PasswordHelper.GetPassword(passwordBox);

			// Проверка, что извлечённый пароль совпадает с установленным
			Assert.That(actualPassword, Is.EqualTo(expectedPassword));
		}
	}
}
