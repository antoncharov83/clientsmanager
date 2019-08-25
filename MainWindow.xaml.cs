using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;
using System.Data;
using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;

namespace ClientsManager
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string oldData;
        SQLiteConnection conn;
        SQLiteDataAdapter adapter;
        SQLiteDataAdapter adapter_dates;
        SQLiteDataAdapter adapter_files;
        DataTable table_clients;
        DataTable table_dates;
        DataTable table_files;
        DataTable table; // для сохранения записи о клиенте
        string dbName = "\\clientsmanager.db";
        bool needRefresh = false;
        public MainWindow()
        {
            InitializeComponent();
            // рабочая директория с бд хранится в настройках
            if (Properties.Settings.Default.main_directory == "")
            {
                needRefresh = false;
                MessageBox.Show("Не задан путь к бд", "Настройки", MessageBoxButton.OK);
                DirectoryItem_Click(directoryItem, null);
                Focus();
            }

            if (!System.IO.File.Exists(Properties.Settings.Default.main_directory + dbName))
            {
                SQLiteConnection.CreateFile(Properties.Settings.Default.main_directory + dbName);
                CreateDb(Properties.Settings.Default.main_directory + dbName);
            }
            // иннициализация подключения
            string dbPath = Properties.Settings.Default.main_directory + dbName;

            SQLiteConnectionStringBuilder stringBuilder = new SQLiteConnectionStringBuilder();
            stringBuilder.DataSource = dbPath;
            stringBuilder.Version = 3;
            conn = new SQLiteConnection(stringBuilder.ToString());

            table_dates = new DataTable("dates");
            // привязываем событие изменения строки для сохранения в бд
            table_dates.RowChanged += new DataRowChangeEventHandler(onNewDates);

            table_files = new DataTable("files");
            // заполнение списка клиентов
            needRefresh = true;
            refreshClientsList();
        }
        private void chooseDirDb() { }
        // создем бд если в выбранной директории ее нет
        private void CreateDb(string dbPath) {
            SQLiteConnectionStringBuilder stringBuilder = new SQLiteConnectionStringBuilder();
            stringBuilder.DataSource = dbPath;
            stringBuilder.Version = 3;
            using (SQLiteConnection conn = new SQLiteConnection(stringBuilder.ToString()))
            {
                conn.Open();
                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    command.CommandText = @"CREATE TABLE [clients] (
                    [id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                    [name] char(255) NOT NULL,
                    [contacts] char(255) NOT NULL,
                    [status] char(255) NOT NULL,
                    [court] char(255) NOT NULL,
                    [number] char(50) NOT NULL,
                    [lawer] char(255) NOT NULL,
                    [result] char(255) NOT NULL,
                    [closed] integer default 0
                    );";
                    command.CommandType = System.Data.CommandType.Text;
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    command.CommandText = @"CREATE TABLE [dates] (
                    [id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                    [client_id] INTEGER NOT NULL,
                    [court_date] char(10) NOT NULL,
                    FOREIGN KEY (client_id) REFERENCES clients(id)
                    );";
                    command.CommandType = System.Data.CommandType.Text;
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    command.CommandText = @"CREATE TABLE [files] (
                    [id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                    [client_id] INTEGER NOT NULL,
                    [filename] TEXT NOT NULL,
                    [shortname] TEXT NOT NULL,
                    FOREIGN KEY (client_id) REFERENCES clients(id)
                    );";
                    command.CommandType = System.Data.CommandType.Text;
                    command.ExecuteNonQuery();
                }
                conn.Close();
            }
        }
        private void doConnection() {
            if (Properties.Settings.Default.main_directory == "")
            {
                needRefresh = false;
                MessageBox.Show("Не задан путь к бд", "Настройки", MessageBoxButton.OK);
                DirectoryItem_Click(directoryItem, null);
                Focus();
            }
            // иннициализация подключения
            string dbPath = Properties.Settings.Default.main_directory + dbName;

            SQLiteConnectionStringBuilder stringBuilder = new SQLiteConnectionStringBuilder();
            stringBuilder.DataSource = dbPath;
            stringBuilder.Version = 3;
            conn = new SQLiteConnection(stringBuilder.ToString());

            adapter = new SQLiteDataAdapter("SELECT * FROM clients", conn);

            table_clients = new DataTable("clients");
            adapter.Fill(table_clients);

            dgClients.ItemsSource = table_clients.DefaultView;
            SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adapter);

            adapter_dates = new SQLiteDataAdapter("SELECT * FROM dates WHERE client_id=@c", conn);
            adapter_dates.SelectCommand.Parameters.Add(new SQLiteParameter("@c", DbType.Int32, "clients_id"));

            SQLiteCommandBuilder builder_dates = new SQLiteCommandBuilder(adapter_dates);

            adapter_files = new SQLiteDataAdapter("SELECT * FROM dates WHERE client_id = @c", conn);
            adapter_files.SelectCommand.Parameters.Add(new SQLiteParameter("@c", DbType.Int32, "clients_id"));

            SQLiteCommandBuilder builder_files = new SQLiteCommandBuilder(adapter_files);
        }
        private void refreshClientsList()
        {

            if (conn == null)
                doConnection();
            else
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();

                adapter = new SQLiteDataAdapter("SELECT * FROM clients", conn);

                table_clients = new DataTable("clients");
                adapter.Fill(table_clients);

                /*for (int i = 8; i < table_clients.Columns.Count; ++i)
                {
                    DataGridColumn column = new DataGridTextColumn
                    {
                        Header = (i + 1).ToString() + ". " + Transliteration.Back(table_clients.Columns[i].ColumnName),
                        Binding = new Binding(string.Format("[{0}]", table_clients.Columns[i].ColumnName)),
                        CanUserSort = false
                    };
                    dgDataClient.Columns.Add(column);
                }*/

                dgClients.ItemsSource = table_clients.DefaultView;
                SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adapter);

                adapter_dates = new SQLiteDataAdapter("SELECT * FROM dates WHERE client_id=@c", conn);
                adapter_dates.SelectCommand.Parameters.Add(new SQLiteParameter("@c", DbType.Int32, "clients_id"));

                SQLiteCommandBuilder builder_dates = new SQLiteCommandBuilder(adapter_dates);

                adapter_files = new SQLiteDataAdapter("SELECT * FROM files WHERE client_id = @c", conn);
                adapter_files.SelectCommand.Parameters.Add(new SQLiteParameter("@c", DbType.Int32, "clients_id"));

                SQLiteCommandBuilder builder_files = new SQLiteCommandBuilder(adapter_files);
            }
        }

        // Создание нового клиента
        private void NewClientBtn_Click(object sender, RoutedEventArgs e)
        {
            NewClientWindow NCW = new NewClientWindow(conn);
            NCW.ShowDialog();

            table_clients.Clear();
            adapter.Fill(table_clients);

            dgClients.Items.Refresh();
            dgClients.SelectedIndex = dgClients.Items.Count - 1;
        }

        private void exitItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Настройки бд
        private void DirectoryItem_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку с бд";
                dialog.SelectedPath = Properties.Settings.Default.main_directory;
                do
                {
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        Properties.Settings.Default.main_directory = dialog.SelectedPath;
                        Properties.Settings.Default.Save();
                    }
                } while (!System.IO.Directory.Exists(dialog.SelectedPath));
                if(needRefresh)
                    refreshClientsList();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Выйти из программы?", "Выход", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                MessageBoxResult.OK) == MessageBoxResult.Cancel)
                e.Cancel = true;
            else {
                adapter.Update(table_clients);
                adapter_dates.Update(table_dates);
                adapter_files.Update(table_files);
                conn.Close();
            }
        }
        // удаление данных оо клиенте
        private void DelClientBtn_Click(object sender, RoutedEventArgs e)
        {
            if (dgClients.SelectedItem == null)
            {
                MessageBox.Show("Выделите клиента для удаления", "Ошибка!", MessageBoxButton.OK);
                return;
            }

            if (MessageBox.Show(String.Format("Вы хотите удалить {0}", table_clients.Rows[dgClients.SelectedIndex][1].ToString()), "Подтверждение", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
            {
                e.Handled = true;
                return;
            }

            if (conn == null)
                doConnection();

            if (conn.State == ConnectionState.Closed)
                conn.Open();

            SQLiteCommand command = new SQLiteCommand("DELETE FROM clients WHERE id=" +
                (dgClients.SelectedItem as DataRowView).Row[0].ToString(), conn);

            //(dgClients.SelectedItem as DataRowView).Row[0].ToString()

            command.ExecuteNonQuery();
            table_clients.Clear();
            adapter.Fill(table_clients);

            dgClients.Items.Refresh();

            dgDataClient.ItemsSource = null;

            dgDates.ItemsSource = null;

            dgFiles.ItemsSource = null;
        }
        // сохранение данных после изменения в таблице данных о клиенте
        private void DgClients_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            string newData = (e.EditingElement as TextBox).Text;
            //DataTable pivotTable = (dgDataClient.ItemsSource as DataView).Table;
            int selected_cell = dgDataClient.SelectedIndex;
            oldData = table.Rows[0][selected_cell + 1].ToString();
            table.Rows[0][selected_cell + 1] = newData;

            string dbPath = Properties.Settings.Default.main_directory + dbName;

            if (conn == null)
                doConnection();

            if (conn.State == ConnectionState.Closed)
                conn.Open();

            //table.Rows[0][selected_cell] = newData;

            lblInfo.Text = oldData + " - заменено - " + newData + " Обновлено записей - " + adapter.Update(table).ToString();

            table_clients.Clear();
            adapter.Fill(table_clients);
            // если изменили имя клиента - обновляем в списке слева
            if (e.Column.DisplayIndex == 0)
            {
                dgClients.Items.Refresh();
            }
        }
        private void CreateDir(string dir) {
            if (Directory.Exists(Properties.Settings.Default.main_directory)) {
                string newDir = Properties.Settings.Default.main_directory + "\\" + dgClients.SelectedItem.ToString();
                if (Directory.Exists(newDir)) {
                    newDir += "\\" + dir;
                    if (!Directory.Exists(newDir))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(newDir);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            MessageBox.Show("У вас нет прав доступа на запись в эту папку\n" + newDir
                                + '\n' + ex.Message, "Ошибка!", MessageBoxButton.OK);
                        }
                        catch (System.IO.IOException ex)
                        {
                            MessageBox.Show("При создании папки произошла ошибка" + ex.Message, "Ошибка!", MessageBoxButton.OK);
                        }
                    }
                    else {
                        MessageBox.Show("Выделите клиента для сохранения", "Ошибка!", MessageBoxButton.OK);
                    }
                }
            }
        }

        private void ClientsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClients.SelectedIndex == -1) return;

            if (conn == null)
                doConnection();

            if (conn.State == ConnectionState.Closed)
                conn.Open();
            // id  выделенного клиента
            long CLIENT_ID = (long)(dgClients.SelectedItem as DataRowView).Row[0];

            SQLiteCommand SelectCommand = new SQLiteCommand("SELECT * FROM clients WHERE id="
                + CLIENT_ID.ToString(), conn);

            using (SQLiteDataReader reader = SelectCommand.ExecuteReader())
            {
                table = new DataTable();
                table.Load(reader);

                if ((long)table.Rows[0][8] == 1)
                    is_closed.IsChecked = true;
                else
                    is_closed.IsChecked = false;

                is_closed.Visibility = Visibility.Visible;

                DataTable pivotTable = new DataTable("pivotDataClient");
                pivotTable.Columns.Add(new DataColumn("Header", System.Type.GetType("System.String")));
                pivotTable.Columns.Add(new DataColumn("Value", System.Type.GetType("System.String")));
                for (int i = 1; i < table.Columns.Count; ++i) {
                    DataRow row = pivotTable.NewRow();

                    switch (i)
                    {
                        case 1: row["Header"] = i.ToString() + ". Клиент"; break;
                        case 2: row["Header"] = i.ToString() + ". Контакты"; break;
                        case 3: row["Header"] = i.ToString() + ". Статус"; break;
                        case 4: row["Header"] = i.ToString() + ". Суд"; break;
                        case 5: row["Header"] = i.ToString() + ". Номер"; break;
                        case 6: row["Header"] = i.ToString() + ". Адвокат"; break;
                        case 7: row["Header"] = i.ToString() + ". Результат"; break;
                        case 8: continue;
                        default: row["Header"] = (i - 1).ToString() + ". " + Transliteration.Back(table.Columns[i].ColumnName); break;
                    }
                    
                    row["Value"] = table.Rows[0][i];
                    pivotTable.Rows.Add(row);
                }
                dgDataClient.ItemsSource = pivotTable.DefaultView;
            }

            lblInfo.Text = "Данные о клиенте - " + (dgClients.SelectedItem as DataRowView).Row[1].ToString();
            // выбираем все даты заседаний этого клиента
            adapter_dates.SelectCommand.Parameters["@c"].Value = CLIENT_ID;
            // обработчик изменений в этой таблице для сохранения
            table_dates.RowChanged -= new DataRowChangeEventHandler(onNewDates);
            table_dates.Clear();
            adapter_dates.Fill(table_dates);
            table_dates.RowChanged += new DataRowChangeEventHandler(onNewDates);

            dgDates.ItemsSource = table_dates.DefaultView;
            // аналогично с файлами
            adapter_files.SelectCommand.Parameters["@c"].Value = CLIENT_ID;

            table_files.Clear();
            adapter_files.Fill(table_files);

            dgFiles.ItemsSource = table_files.DefaultView;
        }

        private void DgDates_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            /*string newDate = (dgDates.SelectedItem as DataRowView).Row[2].ToString();
            DataTable table = (dgDates.ItemsSource as DataView).Table;
            //string oldDate = (dgDates.SelectedItem as DataRowView).Row[1].ToString();
            string date_id = (dgDates.SelectedItem as DataRowView).Row[0].ToString();
            //table.Rows[0][e.Column.DisplayIndex + 1] = newData;
            string dbPath = Properties.Settings.Default.main_directory + dbName;
            SQLiteCommand command;

            if (conn == null)
                doConnection();

            if (conn.State == ConnectionState.Closed)
                conn.Open();
            if(date_id == "")
                command = new SQLiteCommand("INSERT INTO dates(client_id, court_date) VALUES(" +
                (dgClients.SelectedItem as DataRowView).Row[0].ToString() + ",'"+newDate+"')", conn);
            else
                command = new SQLiteCommand("UPDATE dates SET court_date='" + newDate + "' WHERE id=" + date_id, conn);
            try
            {
                lblInfo.Text ="Заменено - " + newDate + " Обновлено записей - " + command.ExecuteNonQuery().ToString();
            }
            catch (SQLiteException ex) {
                lblInfo.Text = "Ошибка: " + ex.Message;
            }
            table_dates.Clear();
            adapter_dates.Fill(table_dates);
            dgDates.Items.Refresh();*/

        }
        static bool TextIsDate(string text)
        {
            var dateFormat = "dd.MM.yyyy";
            DateTime scheduleDate;
            if (DateTime.TryParseExact(text, dateFormat, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out scheduleDate))
            {
                return true;
            }
            return false;
        }
        // после изменений в дататэйбл с датами
        private void onNewDates(object sender, DataRowChangeEventArgs e) {
            if (dgDates.ItemsSource == null)
                return;
            if (e.Action != DataRowAction.Change && e.Action != DataRowAction.Add)
                return;

            if (!TextIsDate(e.Row[2].ToString()))
                return;

            DataTable table = (dgDates.ItemsSource as DataView).Table;
            
            if (e.Row[1].ToString() == "")
                e.Row[1] = (dgClients.SelectedItem as DataRowView).Row[0];

            try {
                adapter_dates.Update(table);
                table.AcceptChanges();
                lblInfo.Text = "Дата заседания изменена";
            }
            catch (InvalidOperationException ex) { return; }
        }
        // после нажатия кнопки del на таблице даты
        private void Grid_PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
            if (e.Command == DataGrid.DeleteCommand)
            {
                if (MessageBox.Show(String.Format("Вы хотите удалить {0}", (grid.SelectedItem as DataRowView).Row[2]), "Подтверждение", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                    e.Handled = true;
                else
                {// находим в таблице дат удаляемую запись и удаляем по ключу
                    DataTable table = (dgDates.ItemsSource as DataView).Table;
                    SQLiteCommand command = new SQLiteCommand("DELETE FROM dates WHERE id=" +
                        (grid.SelectedItem as DataRowView).Row[0].ToString(), conn);

                    if (command.ExecuteNonQuery() == 1)
                        lblInfo.Text = "Ссылка на файл удалена";
                    else
                        MessageBox.Show("Нет доступа к бд. Не могу удалить ссылку на файл.", "Ошибка!");

                    table_dates.Clear();
                    adapter_files.Fill(table_dates);
                    dgDates.ItemsSource = table_dates.DefaultView;
                    dgDates.Items.Refresh();
                }
            }
        }

        private void DgFiles_PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
            if (e.Command == DataGrid.DeleteCommand) {
                if (MessageBox.Show(String.Format("Вы хотите удалить {0}", (grid.SelectedItem as DataRowView).Row[2]), "Подтверждение", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                    e.Handled = true;
                else {
                    DataTable table = (dgFiles.ItemsSource as DataView).Table;
                    SQLiteCommand command = new SQLiteCommand("DELETE FROM files WHERE id=" +
                        (grid.SelectedItem as DataRowView).Row[0].ToString(), conn);

                    if (command.ExecuteNonQuery() == 1)
                        lblInfo.Text = "Ссылка на файл удалена";
                    else
                        MessageBox.Show("Нет доступа к бд. Не могу удалить ссылку на файл.","Ошибка!");

                    table_files.Clear();
                    adapter_files.Fill(table_files);
                    dgFiles.ItemsSource = table_files.DefaultView;
                    dgFiles.Items.Refresh();
                    //table.Rows[0].Table.PrimaryKey = new DataColumn[] { table.Columns[0] };
                    //table.Rows.Remove(table.Rows.Find((grid.SelectedItem as DataRowView).Row[0]));
                    //adapter_files.Update(table);
                }
            }
        }

        private void AddFileBtn_Click(object sender, RoutedEventArgs e)
        {
            if(dgClients.SelectedItem == null)
                return;

            OpenFileDialog openFileDlg = new OpenFileDialog();

            openFileDlg.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            Nullable<bool> result = openFileDlg.ShowDialog();

            if (result == true) {
                SQLiteCommand command = new SQLiteCommand("INSERT INTO files(filename, client_id, shortname)" +
                " VALUES('" + openFileDlg.FileName + "'," + (dgClients.SelectedItem as DataRowView).Row[0].ToString() +
                ",'" + System.IO.Path.GetFileName(openFileDlg.FileName) + "')", conn);

                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                command.Prepare();
                command.ExecuteNonQuery();

                table_files.Clear();
                adapter_files.Fill(table_files);

                dgFiles.Items.Refresh();
            }
        }

        private void DgFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgFiles.SelectedItem == null)
                return;

            string filename = (dgFiles.SelectedItem as DataRowView).Row[2].ToString();

            if (filename != "")
                try
                {
                    Process.Start(filename);
                }
                catch (System.ComponentModel.Win32Exception ex) {
                    MessageBox.Show("Нет приложений для открытия этого файла!","Ошибка!",MessageBoxButton.OK);
                }
        }
        // поиск клиента
        private void FindBtn_Click(object sender, RoutedEventArgs e)
        {
            DataView dataView = dgClients.ItemsSource as DataView;
            if (findTxt.Text != "")
            {                               
                dataView.RowFilter = "name like '%" + findTxt.Text + "%'";
                if (findOpen.IsChecked == true)
                    dataView.RowFilter += " and closed = 0";
                if (findClosed.IsChecked == true)
                    dataView.RowFilter += " and closed = 1";

                if (dataView.Count > 0)
                {
                    dgClients.SelectedIndex = 0;
                }
                else {
                    dgDataClient.ItemsSource = null;
                    dgDates.ItemsSource = null;
                    dgFiles.ItemsSource = null;
                }
                cancelFindBtn.Visibility = Visibility.Visible;
            }
            else {
                dataView.RowFilter = "";
            }
        }

        private void CancelFindBtn_Click(object sender, RoutedEventArgs e)
        {
            (dgClients.ItemsSource as DataView).RowFilter = "";
            dgDataClient.ItemsSource = null;
            dgDates.ItemsSource = null;
            dgFiles.ItemsSource = null;
            cancelFindBtn.Visibility = Visibility.Hidden;
            is_closed.Visibility = Visibility.Hidden;
        }

        private void OnOffEditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (dgDataClient.IsReadOnly)
            {
                dgDataClient.IsReadOnly = false;
                onOffEditBtn.Content = "Режим редактирования: ВКЛ";
            }
            else {
                dgDataClient.IsReadOnly = true;
                onOffEditBtn.Content = "Режим редактирования: ВЫКЛ";
            }
        }

        private void AddColumn_Click(object sender, RoutedEventArgs e)
        {
            EnterNameWindow enterNameWindow = new EnterNameWindow();
            
            if (enterNameWindow.ShowDialog() == true)
            {
                string columnName = Transliteration.Front(enterNameWindow.result);
                SQLiteCommand command = new SQLiteCommand("ALTER TABLE clients ADD COLUMN [" + columnName + "] TEXT", conn);
                try
                {
                    command.ExecuteNonQuery();

                    doConnection();

                    table_clients.Clear();
                    adapter.Fill(table_clients);

                    DataGridColumn column = new DataGridTextColumn
                    {
                        Header = table_clients.Columns.Count.ToString() + ". " + enterNameWindow.result,
                        Binding = new Binding(string.Format("[{0}]", columnName)),
                        CanUserSort = false
                    };
                    dgDataClient.Columns.Add(column);
                    dgClients.Items.Refresh();
                    dgDataClient.Items.Refresh();
                }
                catch (SQLiteException ex) {
                    MessageBox.Show("Ошибка - " + ex.Message,"Ошибка");
                }
            }
        }

        private void Закрыто_Click(object sender, RoutedEventArgs e)
        {
            if (is_closed.IsChecked == true)
                table.Rows[0][8] = 1;
            else
                table.Rows[0][8] = 0;
            adapter.Update(table);
        }

        private void FindOpen_Click(object sender, RoutedEventArgs e)
        {
            if (findOpen.IsChecked == true)
                findClosed.IsChecked = false;
        }

        private void FindClosed_Click(object sender, RoutedEventArgs e)
        {
            if (findClosed.IsChecked == true)
                findOpen.IsChecked = false;
        }

        private void DelColumn_Click(object sender, RoutedEventArgs e)
        {
            string selected;

            if (dgDataClient.SelectedIndex > 6)
                selected = table.Columns[dgDataClient.SelectedIndex + 2].ColumnName;
            else
            {
                MessageBox.Show("Удалить можно только поля созданные пользователем", "Предупреждение");
                return;
            }

            if (MessageBox.Show(String.Format("Вы хотите удалить {0}", selected), "Подтверждение", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    command.CommandText = @"CREATE TABLE [new_clients] (
                    [id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                    [name] char(255) NOT NULL,
                    [contacts] char(255) NOT NULL,
                    [status] char(255) NOT NULL,
                    [court] char(255) NOT NULL,
                    [number] char(50) NOT NULL,
                    [lawer] char(255) NOT NULL,
                    [result] char(255) NOT NULL,
                    [closed] integer default 0
                    );";
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();

                    for (int i = 9; i < table.Columns.Count; ++i) 
                        if (i != dgDataClient.SelectedIndex + 2) { 
                            command.CommandText = @"ALTER TABLE [new_clients] ADD COLUMN [" + table.Columns[i].ColumnName + "] TEXT";
                            command.ExecuteNonQuery();
                        }

                    command.CommandText = @"INSERT INTO new_clients SELECT id, name, contacts, status, court, number, lawer, result, closed";

                    for (int i = 9; i < table.Columns.Count; ++i)
                        if (i != dgDataClient.SelectedIndex + 2) {
                            command.CommandText += ", [" + table.Columns[i].ColumnName + "]";
                        }
                    command.CommandText += " FROM clients";
                    command.ExecuteNonQuery();

                    command.CommandText = "DROP TABLE IF EXISTS clients";
                    command.ExecuteNonQuery();

                    command.CommandText = "ALTER TABLE new_clients RENAME TO clients";
                    command.ExecuteNonQuery();
                }

                int selected_index = dgClients.SelectedIndex;

                doConnection();
                refreshClientsList();

                dgClients.SelectedIndex = selected_index;
            }

        }
    }
}
