using MailMeUp.Core;
using Microsoft.Data.Sqlite;

namespace MailMeUp.Storage;

/// <summary>Stores account metadata only. No tokens, message bodies, or OAuth cache blobs are accepted.</summary>
public sealed class SqliteAccountStore(string directory) : IAccountStore
{
    private readonly string _databasePath = Path.Combine(DataDirectory.Resolve(directory), "accounts.db");

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken = default)
    {
        // A fresh installation has no registry. Reading it must not create files or directories.
        if (!File.Exists(_databasePath))
        {
            return [];
        }

        await using var connection = await OpenAsync(SqliteOpenMode.ReadOnly, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, provider, display_name, email_address FROM accounts ORDER BY id COLLATE BINARY";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var accounts = new List<Account>();
        while (await reader.ReadAsync(cancellationToken))
        {
            accounts.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }

        return accounts;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Account account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentException.ThrowIfNullOrWhiteSpace(account.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(account.Provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(account.DisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(account.EmailAddress);

        var parent = Path.GetDirectoryName(_databasePath)!;
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(parent);
        }
        else
        {
            Directory.CreateDirectory(parent, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        await using var connection = await OpenAsync(SqliteOpenMode.ReadWriteCreate, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO accounts (id, provider, display_name, email_address) VALUES ($id, $provider, $name, $email)
            ON CONFLICT(id) DO UPDATE SET provider = excluded.provider, display_name = excluded.display_name, email_address = excluded.email_address
            """;
        command.Parameters.AddWithValue("$id", account.Id);
        command.Parameters.AddWithValue("$provider", account.Provider);
        command.Parameters.AddWithValue("$name", account.DisplayName);
        command.Parameters.AddWithValue("$email", account.EmailAddress);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenAsync(SqliteOpenMode mode, CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = mode,
            Pooling = false,
            DefaultTimeout = 15
        }.ToString());

        try
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version";
            var version = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (version == 0 && mode == SqliteOpenMode.ReadWriteCreate)
            {
                command.CommandText = """
                    BEGIN IMMEDIATE;
                    CREATE TABLE IF NOT EXISTS accounts (
                        id TEXT PRIMARY KEY NOT NULL,
                        provider TEXT NOT NULL,
                        display_name TEXT NOT NULL,
                        email_address TEXT NOT NULL
                    );
                    PRAGMA user_version = 1;
                    COMMIT;
                    """;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            else if (version != 1)
            {
                throw new InvalidOperationException("Unsupported account database schema. Upgrade MailMeUp or select a new data directory.");
            }

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
