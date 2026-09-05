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

        await using var connection = await OpenAsync(SqliteOpenMode.ReadWrite, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, provider, display_name, email_address, mail_read_enabled, calendar_read_enabled FROM accounts ORDER BY id COLLATE BINARY";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var accounts = new List<Account>();
        while (await reader.ReadAsync(cancellationToken))
        {
            accounts.Add(new(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetBoolean(5)));
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
            INSERT INTO accounts (id, provider, display_name, email_address, mail_read_enabled, calendar_read_enabled)
            VALUES ($id, $provider, $name, $email, $mail, $calendar)
            ON CONFLICT(id) DO UPDATE SET
                provider = excluded.provider,
                display_name = excluded.display_name,
                email_address = excluded.email_address,
                mail_read_enabled = excluded.mail_read_enabled,
                calendar_read_enabled = excluded.calendar_read_enabled
            """;
        command.Parameters.AddWithValue("$id", account.Id);
        command.Parameters.AddWithValue("$provider", account.Provider);
        command.Parameters.AddWithValue("$name", account.DisplayName);
        command.Parameters.AddWithValue("$email", account.EmailAddress);
        command.Parameters.AddWithValue("$mail", account.MailReadEnabled);
        command.Parameters.AddWithValue("$calendar", account.CalendarReadEnabled);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        if (!File.Exists(_databasePath))
        {
            return false;
        }

        await using var connection = await OpenAsync(SqliteOpenMode.ReadWrite, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM accounts WHERE id = $id";
        command.Parameters.AddWithValue("$id", accountId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
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
                        email_address TEXT NOT NULL,
                        mail_read_enabled INTEGER NOT NULL DEFAULT 0,
                        calendar_read_enabled INTEGER NOT NULL DEFAULT 0
                    );
                    PRAGMA user_version = 2;
                    COMMIT;
                    """;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            else if (version == 1 && mode != SqliteOpenMode.ReadOnly)
            {
                command.CommandText = """
                    BEGIN IMMEDIATE;
                    ALTER TABLE accounts ADD COLUMN mail_read_enabled INTEGER NOT NULL DEFAULT 0;
                    ALTER TABLE accounts ADD COLUMN calendar_read_enabled INTEGER NOT NULL DEFAULT 0;
                    PRAGMA user_version = 2;
                    COMMIT;
                    """;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            else if (version != 2)
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
