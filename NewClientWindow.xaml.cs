using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Data.SQLite;

namespace ClientsManager
{
    /// <summary>
    /// Логика взаимодействия для NewClientWindow.xaml
    /// </summary>
    public partial class NewClientWindow : Window
    {
        SQLiteConnection conn;
        public NewClientWindow(SQLiteConnection conn)
        {
            InitializeComponent();
            this.conn = conn;
        }

        private void CreateNewClientBtn_Click(object sender, RoutedEventArgs e)
        {
            SQLiteCommand command = new SQLiteCommand("INSERT INTO clients(name, contacts, status, court, number, lawer, result)" +
                " VALUES('" + newClientTxt.Text + "','','','','','','')", conn);

            if (conn.State == System.Data.ConnectionState.Closed)
                conn.Open();
            command.Prepare();
            command.ExecuteNonQuery();

            Close();
        }
    }
}
