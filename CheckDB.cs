using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "Data Source=ALGAE/ALGAE-dev.db";
        
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            
            // Check if Games table exists and count rows
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Games;";
            
            var count = command.ExecuteScalar();
            Console.WriteLine($"Number of games in database: {count}");
            
            // If there are games, show the first few
            if (Convert.ToInt32(count) > 0)
            {
                command.CommandText = "SELECT GameId, Name, Publisher, InstallPath FROM Games LIMIT 5;";
                using var reader = command.ExecuteReader();
                
                Console.WriteLine("\nFirst 5 games:");
                Console.WriteLine("GameId | Name | Publisher | InstallPath");
                Console.WriteLine("-------|------|-----------|------------");
                
                while (reader.Read())
                {
                    Console.WriteLine($"{reader["GameId"]} | {reader["Name"]} | {reader["Publisher"]} | {reader["InstallPath"]}");
                }
            }
            else
            {
                Console.WriteLine("Database is empty. You need to add some games first.");
                Console.WriteLine("Use the 'Add Game' button in the ALGAE application to add games.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing database: {ex.Message}");
            Console.WriteLine($"Connection string used: {connectionString}");
            
            // Check if the database file exists
            if (System.IO.File.Exists("ALGAE/ALGAE-dev.db"))
            {
                Console.WriteLine("Database file exists.");
            }
            else
            {
                Console.WriteLine("Database file does not exist.");
            }
        }
    }
}
