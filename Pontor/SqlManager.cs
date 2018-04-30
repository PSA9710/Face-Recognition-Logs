using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace Pontor
{
    class SqlManager
    {
        public void SQL_CheckforDatabase()
        {
            if (!File.Exists("data/Database.sqlite"))
            {
                SQL_CreateDatabase();
                SQL_CreatePersonsTable()
            }
        }


        private void SQL_CreateDatabase()
        {
            SQLiteConnection.CreateFile("data/Database.sqlite");
        }
        
        private void SQL_CreatePersonsTable()
        {
            try
            {
                using (SQLiteConnection con = new SQLiteConnection("Data Source=Database.sqlite;Version=3"))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand())
                    {
                        con.Open();
                        cmd.CommandText = "create table if not exists Persons(personID INTEGER PRIMARY KEY, firstname VARCHAR(20),lastname VARCHAR(20))";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }
        }
    }
}
