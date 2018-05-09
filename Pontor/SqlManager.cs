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
    public static class SqlManager
    {
        static String connectionString = "Data Source=data/Database.sqlite;Version=3;";
        public static void SQL_CheckforDatabase()
        {
            if (!File.Exists("data/Database.sqlite"))
            {
                try
                {
                    SQL_CreateDatabase();
                    SQL_CreateTables();

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }


        private static void SQL_CreateDatabase()
        {
            SQLiteConnection.CreateFile("data/Database.sqlite");
        }

        private static void SQL_CreateTables()
        {
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(con))
                    {
                        con.Open();
                        cmd.CommandText = "CREATE TABLE persons (personID INTEGER PRIMARY KEY, firstname VARCHAR(20),lastname VARCHAR(20),cnp VARCHAR(13))";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "CREATE TABLE logs(logID INTEGER PRIMARY KEY, datetime TEXT,personID INTEGER, FOREIGN KEY(personID) REFERENCES persons(personID))";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }



        public static void SQL_InsertIntoPersons(String firstName, String lastName, String CNP)
        {
            if (SQL_GetPersonId(CNP) != -1)
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

        public static long SQL_InsertIntoLogs(String datetime, int id)
        {
            if (id < 1)
            {
                throw new IndexOutOfRangeException();
            }
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                using (SQLiteCommand cmd = new SQLiteCommand(con))
                {
                    con.Open();
                    cmd.CommandText = "INSERT INTO logs (datetime,personID) VALUES ($datetime,$personID)";
                    cmd.Parameters.AddWithValue("$datetime", datetime);
                    cmd.Parameters.AddWithValue("personID", id);
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "SELECT last_insert_rowid()";
                    long value = (long) cmd.ExecuteScalar();
                    return value;


                    
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return -1;
            
        }

        public static int SQL_GetPersonId(String CNP)
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
                        if (personId != null)
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

        public static string SQL_GetPersonName(string id)
        {
            int ID = Convert.ToInt32(id);
            using (SQLiteConnection con = new SQLiteConnection(connectionString))
            using (SQLiteCommand cmd = new SQLiteCommand(con))
            {
                con.Open();
                cmd.CommandText = "SELECT firstname,lastname FROM persons WHERE personID=$ID";
                cmd.Parameters.AddWithValue("$ID", ID);
                SQLiteDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string name = reader["firstname"].ToString() + " " + reader["lastname"].ToString();
                    return name;
                }
            }
            return "UNKNOWN";
        }
    }
}
