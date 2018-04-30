using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace Pontor
{
    class SqlManager
    {
        static String connectionString = "Data Source=data/Database.sqlite;Version=3;";
        public void SQL_CheckforDatabase()
        {
            if (!File.Exists("data/Database.sqlite"))
            {
                try
                {
                    SQL_CreateDatabase();
                    SQL_CreatePersonsTable();
                    SQL_CreateLogsTable();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }


        private void SQL_CreateDatabase()
        {
            SQLiteConnection.CreateFile("data/Database.sqlite");
        }

        private void SQL_CreatePersonsTable()
        {

            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(con))
                {
                    con.Open();
                    cmd.CommandText = "CREATE TABLE persons (personID INTEGER PRIMARY KEY, firstname VARCHAR(20),lastname VARCHAR(20),cnp VARCHAR(13))";
                    cmd.ExecuteNonQuery();
                }
            }

        }

        private void SQL_CreateLogsTable()
        {
            throw new NotImplementedException();
        }

        public void SQL_InsertIntoPersons(String firstName, String lastName, String CNP)
        {
            if(SQL_GetPersonId(CNP)!=-1)
            {
                throw new IndexOutOfRangeException();
            }
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(con))
                    {
                        con.Open();
                        cmd.CommandText = "INSERT INTO persons (firstname,lastname,cnp) VALUES($firstName,$lastName,$CNP);";
                        cmd.Parameters.AddWithValue("$firstName", firstName);
                        cmd.Parameters.AddWithValue("$lastName", lastName);
                        cmd.Parameters.AddWithValue("$CNP", CNP);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public int SQL_GetPersonId(String CNP)
        {
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(con))
                    {
                        con.Open();
                        cmd.CommandText = "SELECT personID from persons where cnp=$cnp";
                        cmd.Parameters.AddWithValue("$cnp", CNP);
                        var personId = cmd.ExecuteScalar();
                        if(personId!=null)
                        {
                            return (int)(Int64)personId;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return -1;
        }
    }
}
