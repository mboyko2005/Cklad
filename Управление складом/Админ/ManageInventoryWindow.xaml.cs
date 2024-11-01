using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class ManageInventoryWindow : Window, IThemeable
	{
		public class InventoryItem
		{
			public int Id { get; set; }
			public string Name { get; set; }
			public int Quantity { get; set; }
			public string Location { get; set; }
		}

		private List<InventoryItem> InventoryItems;
		private bool IsEditMode = false;
		private InventoryItem SelectedItem;
		private string connectionString = "Data Source=DESKTOP-Q11QP9V\\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True"; // Замените на вашу строку подключения

		public ManageInventoryWindow()
		{
			InitializeComponent();
			LoadInventory();
			UpdateThemeIcon();
		}

		private void LoadInventory()
		{
			InventoryItems = new List<InventoryItem>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = @"
                        SELECT sp.ПозицияID AS Id, t.Наименование AS Name, sp.Количество AS Quantity, s.Наименование AS Location
                        FROM СкладскиеПозиции sp
                        INNER JOIN Товары t ON sp.ТоварID = t.ТоварID
                        INNER JOIN Склады s ON sp.СкладID = s.СкладID";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								InventoryItems.Add(new InventoryItem
								{
									Id = reader.GetInt32(0),
									Name = reader.GetString(1),
									Quantity = reader.GetInt32(2),
									Location = reader.GetString(3)
								});
							}
						}
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			RefreshInventoryDataGrid();
		}

		private void RefreshInventoryDataGrid()
		{
			var filteredItems = InventoryItems.Where(item => !string.IsNullOrWhiteSpace(item.Name)).ToList();
			InventoryDataGrid.ItemsSource = null;
			InventoryDataGrid.ItemsSource = filteredItems;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void AddInventory_Click(object sender, RoutedEventArgs e)
		{
			IsEditMode = false;
			PanelTitle.Text = "Добавить складскую позицию";
			ClearInputFields();
			ShowPanel();
		}

		private void EditInventory_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedItem)
			{
				IsEditMode = true;
				SelectedItem = selectedItem;
				PanelTitle.Text = "Редактировать складскую позицию";
				PopulateInputFields(selectedItem);
				ShowPanel();
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите позицию для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void DeleteInventory_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedItem)
			{
				MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить позицию: {selectedItem.Name}?", "Удаление позиции", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						try
						{
							connection.Open();
							string query = "DELETE FROM СкладскиеПозиции WHERE ПозицияID = @Id";
							using (SqlCommand command = new SqlCommand(query, connection))
							{
								command.Parameters.AddWithValue("@Id", selectedItem.Id);
								command.ExecuteNonQuery();
							}

							InventoryItems.Remove(selectedItem);
							RefreshInventoryDataGrid();
							MessageBox.Show("Позиция успешно удалена.", "Удаление позиции", MessageBoxButton.OK, MessageBoxImage.Information);
						}
						catch (SqlException ex)
						{
							MessageBox.Show($"Ошибка удаления из базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите позицию для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void ShowPanel()
		{
			this.RootGrid.ColumnDefinitions[1].Width = new GridLength(300);
			Storyboard showStoryboard = (Storyboard)FindResource("ShowPanelStoryboard");
			showStoryboard.Begin();
		}

		private void HidePanel()
		{
			Storyboard hideStoryboard = (Storyboard)FindResource("HidePanelStoryboard");
			hideStoryboard.Completed += (s, e) => { this.RootGrid.ColumnDefinitions[1].Width = new GridLength(0); };
			hideStoryboard.Begin();
		}

		private void ClosePanel_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		private void CancelInventory_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		private void SaveInventory_Click(object sender, RoutedEventArgs e)
		{
			string name = NameTextBox.Text.Trim();
			string quantityText = QuantityTextBox.Text.Trim();
			string location = LocationTextBox.Text.Trim();

			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(quantityText) || string.IsNullOrEmpty(location))
			{
				MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
			{
				MessageBox.Show("Количество должно быть положительным целым числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					if (IsEditMode)
					{
						string query = @"
                            UPDATE СкладскиеПозиции
                            SET Количество = @Quantity, ДатаОбновления = GETDATE()
                            WHERE ПозицияID = @Id";
						using (SqlCommand command = new SqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@Id", SelectedItem.Id);
							command.Parameters.AddWithValue("@Quantity", quantity);
							command.ExecuteNonQuery();
						}

						SelectedItem.Name = name;
						SelectedItem.Quantity = quantity;
						SelectedItem.Location = location;
						MessageBox.Show("Позиция успешно обновлена.", "Редактирование позиции", MessageBoxButton.OK, MessageBoxImage.Information);
					}
					else
					{
						// Поиск первого доступного ID, который отсутствует в последовательности
						int newId = 1;
						for (int i = 1; i <= InventoryItems.Count + 1; i++)
						{
							if (!InventoryItems.Any(item => item.Id == i))
							{
								newId = i;
								break;
							}
						}

						string query = @"
                            INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество)
                            VALUES (@ItemId, @WarehouseId, @Quantity)";
						using (SqlCommand command = new SqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@ItemId", 1); // Замените 1 на ID вашего товара
							command.Parameters.AddWithValue("@WarehouseId", 1); // Замените 1 на ID вашего склада
							command.Parameters.AddWithValue("@Quantity", quantity);
							command.ExecuteNonQuery();
						}

						InventoryItems.Add(new InventoryItem { Id = newId, Name = name, Quantity = quantity, Location = location });
						MessageBox.Show("Позиция успешно добавлена.", "Добавление позиции", MessageBoxButton.OK, MessageBoxImage.Information);
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка сохранения в базу данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			RefreshInventoryDataGrid();
			HidePanel();
		}

		private void ClearInputFields()
		{
			NameTextBox.Text = string.Empty;
			QuantityTextBox.Text = string.Empty;
			LocationTextBox.Text = string.Empty;
		}

		private void PopulateInputFields(InventoryItem item)
		{
			NameTextBox.Text = item.Name;
			QuantityTextBox.Text = item.Quantity.ToString();
			LocationTextBox.Text = item.Location;
		}

		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}
	}
}
