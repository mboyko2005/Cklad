using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.IconPacks;
using УправлениеСкладом;
using УправлениеСкладом.QR;
using Управление_складом.Themes;

namespace УправлениеСкладомTests
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ComprehensiveTests
	{
		#region User и конвертер Visibility

		[Test]
		public void User_Creation_ValidProperties()
		{
			// Создаем пользователя с именем "TestUser" и ролью с ID 2
			var username = "TestUser";
			var roleId = 2;
			var user = new User { Username = username, RoleID = roleId };

			// Проверяем, что имя пользователя установлено корректно
			Assert.That(user.Username, Is.EqualTo(username));
			// Проверяем, что идентификатор роли установлен корректно
			Assert.That(user.RoleID, Is.EqualTo(roleId));
		}

		[Test]
		public void BoolToVisibilityConverter_ConvertsTrueToVisible()
		{
			// Создаем экземпляр конвертера
			var converter = new BoolToVisibilityConverter();
			// Преобразуем значение true в Visibility
			var result = converter.Convert(true, null, null, null);
			// Ожидаем, что результат будет Visibility.Visible
			Assert.That(result, Is.EqualTo(Visibility.Visible));
		}

		[Test]
		public void BoolToVisibilityConverter_ConvertsFalseToCollapsed()
		{
			// Создаем экземпляр конвертера
			var converter = new BoolToVisibilityConverter();
			// Преобразуем значение false в Visibility
			var result = converter.Convert(false, null, null, null);
			// Ожидаем, что результат будет Visibility.Collapsed
			Assert.That(result, Is.EqualTo(Visibility.Collapsed));
		}

		[Test]
		public void BoolToVisibilityConverter_InvertConversion_Works()
		{
			// Создаем экземпляр конвертера
			var converter = new BoolToVisibilityConverter();
			// При передаче параметра "true" инвертируется логика:
			// true становится Collapsed, а false – Visible.
			var resultTrue = converter.Convert(true, null, "true", null);
			var resultFalse = converter.Convert(false, null, "true", null);
			Assert.That(resultTrue, Is.EqualTo(Visibility.Collapsed));
			Assert.That(resultFalse, Is.EqualTo(Visibility.Visible));
		}

		#endregion

		#region PasswordHelper

		[Test]
		public void PasswordHelper_SetAndGetPassword_Works()
		{
			// Создаем PasswordBox и устанавливаем пароль
			var passwordBox = new PasswordBox();
			string expected = "Secret123";
			PasswordHelper.SetPassword(passwordBox, expected);
			// Получаем пароль и проверяем, что он соответствует установленному
			var actual = PasswordHelper.GetPassword(passwordBox);
			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void PasswordHelper_GetPassword_ReturnsEmptyIfNotSet()
		{
			// Создаем PasswordBox без установки пароля
			var passwordBox = new PasswordBox();
			// Проверяем, что возвращается пустая строка
			var result = PasswordHelper.GetPassword(passwordBox);
			Assert.That(result, Is.EqualTo(string.Empty));
		}

		#endregion

		#region RoleWindowFactory

		[Test]
		public void RoleWindowFactory_ReturnsCorrectWindows_ForKnownRoles()
		{
			// Проверяем, что для роли "Администратор" фабрика возвращает окно типа AdministratorWindow
			var adminWindow = RoleWindowFactory.CreateWindow("Администратор");
			Assert.That(adminWindow, Is.Not.Null);
			Assert.That(adminWindow.GetType().Name, Is.EqualTo("AdministratorWindow"));

			// Проверяем, что для роли "Менеджер" фабрика возвращает окно типа ManagerWindow
			var managerWindow = RoleWindowFactory.CreateWindow("Менеджер");
			Assert.That(managerWindow, Is.Not.Null);
			Assert.That(managerWindow.GetType().Name, Is.EqualTo("ManagerWindow"));

			// Проверяем, что для роли "Сотрудник склада" фабрика возвращает окно типа WarehouseStaffWindow
			var warehouseWindow = RoleWindowFactory.CreateWindow("Сотрудник склада");
			Assert.That(warehouseWindow, Is.Not.Null);
			Assert.That(warehouseWindow.GetType().Name, Is.EqualTo("WarehouseStaffWindow"));
		}

		[Test]
		public void RoleWindowFactory_InvalidRole_ReturnsNull()
		{
			// Для неизвестной роли фабрика должна вернуть null
			var window = RoleWindowFactory.CreateWindow("Неизвестная роль");
			Assert.That(window, Is.Null);
		}

		#endregion

		#region QRCodeHelper

		[Test]
		public void QRCodeHelper_GenerateQrBytes_ReturnsNonEmptyByteArray()
		{
			// Генерация QR-кода для тестового текста
			string content = "Test QR Code";
			var qrBytes = QRCodeHelper.GenerateQrBytes(content);
			Assert.That(qrBytes, Is.Not.Null);
			// Проверяем, что сгенерированный массив байт не пустой
			Assert.That(qrBytes.Length, Is.GreaterThan(0));
		}

		[Test]
		public void QRCodeHelper_BytesToBitmapImage_ConvertsSuccessfully()
		{
			// Генерация QR-кода и преобразование его в BitmapImage
			string content = "Another Test QR Code";
			var qrBytes = QRCodeHelper.GenerateQrBytes(content);
			var bmpImage = QRCodeHelper.BytesToBitmapImage(qrBytes);
			Assert.That(bmpImage, Is.Not.Null);
		}

		[Test]
		public void QRCodeHelper_BytesToBitmapImage_ReturnsNull_OnEmptyArray()
		{
			// Если передается пустой массив байт, ожидается null
			var bmpImage = QRCodeHelper.BytesToBitmapImage(new byte[0]);
			Assert.That(bmpImage, Is.Null);
		}

		[Test]
		public void QRCodeHelper_GenerateQrBytes_IsDeterministic()
		{
			// Для одного и того же текста генерация QR-кода должна давать одинаковый результат
			string content = "DeterministicTest";
			var bytes1 = QRCodeHelper.GenerateQrBytes(content);
			var bytes2 = QRCodeHelper.GenerateQrBytes(content);
			Assert.That(bytes1.SequenceEqual(bytes2), Is.True);
		}

		#endregion

		#region ThemeManager

		[Test]
		public void ThemeManager_ToggleTheme_ChangesStateAndBack()
		{
			// Если Application.Current еще не создан, создаем его
			if (Application.Current == null)
			{
				new Application();
			}

			bool initial = ThemeManager.IsDarkTheme;
			// Переключаем тему и проверяем, что состояние изменилось
			ThemeManager.ToggleTheme();
			Assert.That(ThemeManager.IsDarkTheme, Is.Not.EqualTo(initial));
			// Переключаем тему обратно и проверяем, что вернулось исходное состояние
			ThemeManager.ToggleTheme();
			Assert.That(ThemeManager.IsDarkTheme, Is.EqualTo(initial));
		}

		[Test]
		public async Task ThemeManager_ToggleTheme_DoesNotThrow_WhenCalledRepeatedly()
		{
			// Если Application.Current еще не создан, создаем его
			if (Application.Current == null)
			{
				new Application();
			}

			// Проверяем, что многократное переключение темы не вызывает исключений
			Assert.DoesNotThrowAsync(async () =>
			{
				for (int i = 0; i < 5; i++)
				{
					ThemeManager.ToggleTheme();
					await Task.Delay(50);
				}
			});
		}

		#endregion

		#region DatabaseHelper

		[Test]
		public void DatabaseHelper_AuthenticateUser_InvalidCredentials_ReturnsNull()
		{
			// При неверном логине/пароле метод должен возвращать null
			var user = DatabaseHelper.AuthenticateUser("nonexistentUser", "wrongPassword");
			Assert.That(user, Is.Null);
		}

		[Test]
		public void DatabaseHelper_AuthenticateUser_NullCredentials_ThrowsException()
		{
			// При передаче null-значений ожидаем ArgumentNullException
			Assert.Throws<ArgumentNullException>(() =>
			{
				DatabaseHelper.AuthenticateUser(null, null);
			});
		}

		#endregion


		#region Асинхронные тесты

		[Test]
		public async Task ThemeManager_ToggleTheme_DoesNotThrow_WhenCalledRepeatedlyAsync()
		{
			// Проверяем, что многократное переключение темы не вызывает исключений
			Assert.DoesNotThrowAsync(async () =>
			{
				for (int i = 0; i < 5; i++)
				{
					ThemeManager.ToggleTheme();
					await Task.Delay(50);
				}
			});
		}

		#endregion
	}
}
