using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Класс для управления контактами
    /// </summary>
    public class ContactManager
    {
        private readonly string connectionString;
        private readonly int currentUserId;
        private readonly ListBox contactsListBox;

        /// <summary>
        /// Конструктор класса
        /// </summary>
        public ContactManager(string connectionString, int currentUserId, ListBox contactsListBox)
        {
            this.connectionString = connectionString;
            this.currentUserId = currentUserId;
            this.contactsListBox = contactsListBox;
        }

        /// <summary>
        /// Загрузка контактов
        /// </summary>
        public void LoadContacts(string searchText = "")
        {
            contactsListBox.Items.Clear();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql;
                    SqlCommand command;
                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        sql = @"
                            SELECT DISTINCT u.ПользовательID, u.ИмяПользователя, r.Наименование AS Роль
                            FROM Пользователи u
                            INNER JOIN Роли r ON u.РольID = r.РольID
                            WHERE u.ПользовательID IN (
	                            SELECT CASE WHEN ОтправительID = @currentUserId THEN ПолучательID ELSE ОтправительID END
	                            FROM Сообщения
	                            WHERE (ОтправительID = @currentUserId OR ПолучательID = @currentUserId)
                                  AND (
                                      (ОтправительID = @currentUserId AND СкрытоОтправителем = 0)
                                      OR
                                      (ПолучательID = @currentUserId AND СкрытоПолучателем = 0)
                                  )
                            )";
                        command = new SqlCommand(sql, connection);
                        command.Parameters.AddWithValue("@currentUserId", currentUserId);
                    }
                    else
                    {
                        sql = @"
                            SELECT u.ПользовательID, u.ИмяПользователя, r.Наименование AS Роль
                            FROM Пользователи u
                            INNER JOIN Роли r ON u.РольID = r.РольID
                            WHERE u.ИмяПользователя LIKE @searchText AND u.ПользовательID <> @currentUserId";
                        command = new SqlCommand(sql, connection);
                        command.Parameters.AddWithValue("@searchText", "%" + searchText.Trim() + "%");
                        command.Parameters.AddWithValue("@currentUserId", currentUserId);
                    }

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            string userName = reader.GetString(1);
                            string role = reader.GetString(2);

                            var contact = new ContactInfo
                            {
                                UserId = userId,
                                Login = userName,
                                Role = role,
                                UnreadCount = GetUnreadMessagesCount(userId)
                            };

                            contactsListBox.Items.Add(contact);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки контактов: " + ex.Message);
            }
        }

        /// <summary>
        /// Получение количества непрочитанных сообщений
        /// </summary>
        public int GetUnreadMessagesCount(int contactId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = @"
                        SELECT COUNT(*)
                        FROM Сообщения
                        WHERE ОтправительID = @contactId
                          AND ПолучательID = @currentUserId
                          AND Прочитано = 0
                          AND СкрытоПолучателем = 0";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@contactId", contactId);
                        command.Parameters.AddWithValue("@currentUserId", currentUserId);
                        return Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении непрочитанных сообщений: " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Обновляет статус непрочитанных сообщений для контакта
        /// </summary>
        public void ResetUnreadCount(int contactId)
        {
            try
            {
                foreach (var item in contactsListBox.Items)
                {
                    if (item is ContactInfo contact && contact.UserId == contactId)
                    {
                        contact.UnreadCount = 0;
                        contactsListBox.Items.Refresh();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении статуса сообщений: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Класс контакта с информацией о непрочитанных сообщениях
    /// </summary>
    public class ContactInfo : INotifyPropertyChanged
    {
        private int _userId;
        private string _login;
        private string _role;
        private int _unreadCount;

        /// <summary>
        /// ID пользователя
        /// </summary>
        public int UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged(nameof(UserId));
            }
        }

        /// <summary>
        /// Логин пользователя
        /// </summary>
        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged(nameof(Login));
            }
        }

        /// <summary>
        /// Роль пользователя
        /// </summary>
        public string Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged(nameof(Role));
            }
        }

        /// <summary>
        /// Количество непрочитанных сообщений
        /// </summary>
        public int UnreadCount
        {
            get => _unreadCount;
            set
            {
                _unreadCount = value;
                OnPropertyChanged(nameof(UnreadCount));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(UnreadVisibility));
            }
        }

        /// <summary>
        /// Статус сообщений (количество непрочитанных или галочка)
        /// </summary>
        public string Status => UnreadCount > 0 ? UnreadCount.ToString() : "✓";

        /// <summary>
        /// Видимость индикатора непрочитанных сообщений
        /// </summary>
        public Visibility UnreadVisibility => UnreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Событие изменения свойств для привязки данных
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Метод вызова события изменения свойства
        /// </summary>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 