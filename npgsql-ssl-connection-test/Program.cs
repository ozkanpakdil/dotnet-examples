using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectDir = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;
        
        Environment.SetEnvironmentVariable("PGSSLCERT", Path.Combine(projectDir, "certs", "client.crt"));
        Environment.SetEnvironmentVariable("PGSSLKEY", Path.Combine(projectDir, "certs", "client.key"));
        Environment.SetEnvironmentVariable("PGSSLROOTCERT", Path.Combine(projectDir, "certs", "server.crt"));
        
        string connectionString = "host=127.0.0.1;port=5434;database=redgatemonitor;username=redgatemonitor;sslmode=VerifyFull;";

        await testConnection(connectionString);
		
		//Environment.SetEnvironmentVariable("PGSSLCERT", Path.Combine(projectDir, "certs", "client1.crt"));
		System.IO.File.Move(Path.Combine(projectDir, "certs", "client.crt"), Path.Combine(projectDir, "certs", "client1.crt"));
		connectionString = "host=127.0.0.1;port=5434;database=redgatemonitor;username=redgatemonitor;sslmode=VerifyFull;Pooling=false";
		
		await testConnection(connectionString);
		System.IO.File.Move(Path.Combine(projectDir, "certs", "client1.crt"), Path.Combine(projectDir, "certs", "client.crt"));
	
    }
	
	static async Task testConnection(string connectionString){
		try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            Console.WriteLine("Connected to PostgreSQL successfully!");

            await using var command = new NpgsqlCommand("SELECT version()", connection);
            var result = await command.ExecuteScalarAsync();

            Console.WriteLine($"PostgreSQL Version: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(connectionString);
            Console.WriteLine($"Error: {ex.Message}");
        }
	}
}
