using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	public class RepositoryBase: IDisposable, IAsyncDisposable
	{
		private bool _disposed;
		private readonly bool _ownsResources;

		/// <summary>The underlying connection. Owned by this repository unless supplied through an external transaction.
		/// May be opened/closed transparently by individual <c>Read*</c>/<c>Execute*</c> methods.</summary>
		public SqlConnection? Connection { get; private set; }

		/// <summary>The connection string passed to the constructor. When borrowing an external transaction,
		/// its existing connection is used even if this string describes a different connection.</summary>
		public string ConnectionString { get; private set; }

		/// <summary>The current ambient transaction, or <c>null</c> if no transaction is in flight.
		/// Methods that issue commands automatically attach this transaction when present.</summary>
		public SqlTransaction? Transaction { get; set; }


		/// <summary>Creates a repository using the existing <paramref name="transaction"/> and its connection,
		/// or a new owned connection bound to <paramref name="connectionString"/> when the transaction is null.</summary>
		/// <remarks>An external transaction and its connection remain owned by the caller. Disposing this repository
		/// does not commit, roll back, close, or dispose either resource.</remarks>
		/// <exception cref="ArgumentException">The supplied transaction has completed or has been disposed.</exception>
		public RepositoryBase
		(
			SqlTransaction? transaction,
			string connectionString
		)
		{
			ConnectionString = connectionString;

			_ownsResources = transaction == null;
			Connection = transaction == null
				? new SqlConnection(ConnectionString)
				: transaction.Connection ?? throw new ArgumentException("The transaction must have an active connection.", nameof(transaction));

			Transaction = transaction;
		}

		/// <summary>Creates a repository bound to <paramref name="connectionString"/> with no ambient transaction.</summary>
		public RepositoryBase(string connectionString)
			: this(null, connectionString) {}


		/// <summary>
		/// <para>Low-level transaction helper. Opens the connection if it was closed,
		/// begins a <see cref="SqlTransaction"/> with the given <paramref name="isolationLevel"/>
		/// and passes it to <paramref name="action"/>.</para>
		///
		/// <para><b>The caller is fully responsible for committing the transaction.</b>
		/// If <paramref name="action"/> returns without calling
		/// <see cref="SqlTransaction.Commit"/>, the transaction is disposed on exit
		/// and all changes are rolled back. This is by design: the method does not
		/// know whether the work inside <paramref name="action"/> is meant to be
		/// persisted or not.</para>
		///
		/// <para>If <paramref name="action"/> throws, unfinished work is rolled back on dispose
		/// and the original exception is rethrown even if cleanup fails. An already committed
		/// transaction cannot be rolled back.</para>
		///
		/// <para>On exit — whether successful or not — the transaction is disposed,
		/// <see cref="Transaction"/> is set to <c>null</c>, and the connection is
		/// closed if it was opened by this method.</para>
		///
		/// <para>Prefer <see cref="RunInTransaction(IsolationLevel, Action{SqlTransaction})"/>
		/// if you want automatic commit-on-success / rollback-on-exception semantics.</para>
		/// </summary>
		/// <param name="isolationLevel">The isolation level under which the transaction runs.</param>
		/// <param name="action">The code to execute inside the transaction. Must call
		/// <see cref="SqlTransaction.Commit"/> explicitly to persist any changes.</param>
		/// <exception cref="InvalidOperationException">A transaction is already attached to this repository.</exception>
		/// <example>
		/// <code><![CDATA[
		/// // Manual-commit pattern: call tran.Commit() to persist; otherwise everything rolls back.
		/// repo.BeginTransaction(IsolationLevel.RepeatableRead, tran =>
		/// {
		///     var balance = repo.ReadTo<decimal>("select Balance from Accounts where Id = @Id",
		///         new SqlParameter("@Id", accountId));
		///
		///     if (balance < amount) return; // implicit rollback
		///
		///     repo.Execute("update Accounts set Balance -= @A where Id = @Id",
		///         new SqlParameter("@A", amount), new SqlParameter("@Id", accountId));
		///     tran.Commit();
		/// });
		/// ]]></code>
		/// </example>
		public void BeginTransaction(IsolationLevel isolationLevel, Action<SqlTransaction> action)
		{
			RunTransactionCore(isolationLevel, action, commitOnSuccess: false);
		}

		/// <summary>
		/// <para>Same as <see cref="BeginTransaction(IsolationLevel, Action{SqlTransaction})"/>
		/// but with <see cref="IsolationLevel.Unspecified"/>, which lets SQL Server apply
		/// its default isolation level (normally <c>READ COMMITTED</c>).</para>
		///
		/// <para><b>The caller is fully responsible for committing the transaction</b> —
		/// see the overload for details.</para>
		///
		/// <para>Prefer <see cref="RunInTransaction(Action{SqlTransaction})"/> if you want
		/// automatic commit-on-success / rollback-on-exception semantics.</para>
		/// </summary>
		/// <param name="action">The code to execute inside the transaction. Must call
		/// <see cref="SqlTransaction.Commit"/> explicitly to persist any changes.</param>
		public void BeginTransaction(Action<SqlTransaction> action)
		{
			BeginTransaction(IsolationLevel.Unspecified, action);
		}

		/// <summary>
		/// <para>Runs <paramref name="action"/> inside a <see cref="SqlTransaction"/>
		/// with automatic commit/rollback semantics.</para>
		///
		/// <para>Opens the connection if it was closed, begins a transaction with the
		/// given <paramref name="isolationLevel"/>, and passes it to <paramref name="action"/>.</para>
		///
		/// <para>If <paramref name="action"/> completes normally, the transaction is
		/// committed automatically. If it throws, unfinished work is rolled back on dispose and
		/// the original exception is rethrown even if cleanup fails.</para>
		///
		/// <para>On exit — whether successful or not — the transaction is disposed,
		/// <see cref="Transaction"/> is set to <c>null</c>, and the connection is
		/// closed if it was opened by this method.</para>
		///
		/// <para>The caller must not call <see cref="SqlTransaction.Commit"/> or
		/// <see cref="SqlTransaction.Rollback()"/> inside <paramref name="action"/> —
		/// that is handled by this method.</para>
		/// </summary>
		/// <param name="isolationLevel">The isolation level under which the transaction runs.</param>
		/// <param name="action">The code to execute inside the transaction.</param>
		/// <exception cref="InvalidOperationException">A transaction is already attached to this repository.</exception>
		/// <example>
		/// <code><![CDATA[
		/// // Auto-commit pattern: returning normally commits; throwing rolls back.
		/// repo.RunInTransaction(IsolationLevel.Serializable, tran =>
		/// {
		///     repo.Execute("dbo.MoveFunds",
		///         new SqlParameter("@From", fromId),
		///         new SqlParameter("@To",   toId),
		///         new SqlParameter("@Amt",  amount));
		///
		///     repo.Execute("dbo.RecordTransfer",
		///         new SqlParameter("@From", fromId),
		///         new SqlParameter("@To",   toId),
		///         new SqlParameter("@Amt",  amount));
		///
		///     // No tran.Commit() call — RunInTransaction commits automatically.
		/// });
		/// ]]></code>
		/// </example>
		public void RunInTransaction(IsolationLevel isolationLevel, Action<SqlTransaction> action)
		{
			RunTransactionCore(isolationLevel, action, commitOnSuccess: true);
		}

		/// <summary>
		/// <para>Same as <see cref="RunInTransaction(IsolationLevel, Action{SqlTransaction})"/>
		/// but with <see cref="IsolationLevel.Unspecified"/>, which lets SQL Server apply
		/// its default isolation level (normally <c>READ COMMITTED</c>).</para>
		///
		/// <para>Commits on successful completion of <paramref name="action"/>,
		/// rolls back on exception. The caller must not commit or roll back manually.</para>
		/// </summary>
		/// <param name="action">The code to execute inside the transaction.</param>
		public void RunInTransaction(Action<SqlTransaction> action)
		{
			RunInTransaction(IsolationLevel.Unspecified, action);
		}

		/// <summary>
		/// <para>Asynchronous counterpart of
		/// <see cref="BeginTransaction(IsolationLevel, Action{SqlTransaction})"/>.</para>
		///
		/// <para><b>The caller is fully responsible for committing the transaction</b>
		/// (via <c>await transaction.CommitAsync(ct)</c>). If <paramref name="asyncAction"/>
		/// returns without committing, the transaction is rolled back on dispose.</para>
		///
		/// <para>Prefer <see cref="RunInTransactionAsync(IsolationLevel, Func{SqlTransaction, CancellationToken, Task}, CancellationToken)"/>
		/// for automatic commit-on-success / rollback-on-exception semantics.</para>
		/// </summary>
		/// <exception cref="InvalidOperationException">A transaction is already attached to this repository.</exception>
		public async Task BeginTransactionAsync(IsolationLevel isolationLevel, Func<SqlTransaction, CancellationToken, Task> asyncAction, CancellationToken cancellationToken = default)
		{
			await RunTransactionCoreAsync(isolationLevel, asyncAction, commitOnSuccess: false, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Same as <see cref="BeginTransactionAsync(IsolationLevel, Func{SqlTransaction, CancellationToken, Task}, CancellationToken)"/>
		/// but with <see cref="IsolationLevel.Unspecified"/>. The caller is fully responsible for committing.
		/// </summary>
		public Task BeginTransactionAsync(Func<SqlTransaction, CancellationToken, Task> asyncAction, CancellationToken cancellationToken = default)
		{
			return BeginTransactionAsync(IsolationLevel.Unspecified, asyncAction, cancellationToken);
		}

		/// <summary>
		/// <para>Asynchronous counterpart of
		/// <see cref="RunInTransaction(IsolationLevel, Action{SqlTransaction})"/>:
		/// commits <paramref name="asyncAction"/> on successful completion,
		/// rolls back on exception.</para>
		///
		/// <para>The caller must not commit or roll back manually.</para>
		/// </summary>
		/// <exception cref="InvalidOperationException">A transaction is already attached to this repository.</exception>
		public async Task RunInTransactionAsync(IsolationLevel isolationLevel, Func<SqlTransaction, CancellationToken, Task> asyncAction, CancellationToken cancellationToken = default)
		{
			await RunTransactionCoreAsync(isolationLevel, asyncAction, commitOnSuccess: true, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Same as <see cref="RunInTransactionAsync(IsolationLevel, Func{SqlTransaction, CancellationToken, Task}, CancellationToken)"/>
		/// but with <see cref="IsolationLevel.Unspecified"/>.
		/// </summary>
		public Task RunInTransactionAsync(Func<SqlTransaction, CancellationToken, Task> asyncAction, CancellationToken cancellationToken = default)
		{
			return RunInTransactionAsync(IsolationLevel.Unspecified, asyncAction, cancellationToken);
		}

		private void RunTransactionCore(IsolationLevel isolationLevel, Action<SqlTransaction> action, bool commitOnSuccess)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));
			EnsureNoTransaction();
			var connection = Connection!;
			var closeConnection = connection.State == ConnectionState.Closed;
			SqlTransaction? transaction = null;
			var failed = false;
			try
			{
				if (closeConnection) connection.Open();
				transaction = connection.BeginTransaction(isolationLevel);
				Transaction = transaction;
				action(transaction);
				if (commitOnSuccess) transaction.Commit();
			}
			catch
			{
				failed = true;
				throw;
			}
			finally
			{
				Transaction = null;
				Exception? cleanupError = null;
				// Dispose rolls back unfinished work, including after cancellation. Do not
				// issue Rollback against a transaction already completed by the callback.
				try { transaction?.Dispose(); }
				catch (Exception ex) { cleanupError = ex; }
				try { if (closeConnection) connection.Close(); }
				catch (Exception ex) { cleanupError ??= ex; }
				if (!failed && cleanupError != null)
					ExceptionDispatchInfo.Capture(cleanupError).Throw();
			}
		}

		private async Task RunTransactionCoreAsync(IsolationLevel isolationLevel, Func<SqlTransaction, CancellationToken, Task> asyncAction, bool commitOnSuccess, CancellationToken cancellationToken)
		{
			if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));
			EnsureNoTransaction();
			var connection = Connection!;
			var closeConnection = connection.State == ConnectionState.Closed;
			SqlTransaction? transaction = null;
			var failed = false;
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (closeConnection) await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
				transaction = (SqlTransaction)await connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
				Transaction = transaction;
				await asyncAction(transaction, cancellationToken).ConfigureAwait(false);
				if (commitOnSuccess) await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				failed = true;
				throw;
			}
			finally
			{
				Transaction = null;
				Exception? cleanupError = null;
				// Cleanup deliberately does not use the cancelled operation token.
				try { if (transaction != null) await transaction.DisposeAsync().ConfigureAwait(false); }
				catch (Exception ex) { cleanupError = ex; }
				try { if (closeConnection) await connection.CloseAsync().ConfigureAwait(false); }
				catch (Exception ex) { cleanupError ??= ex; }
				if (!failed && cleanupError != null)
					ExceptionDispatchInfo.Capture(cleanupError).Throw();
			}
		}

		private void EnsureNoTransaction()
		{
			if (Transaction != null)
				throw new InvalidOperationException("A transaction is already attached to this repository. Complete and detach it before starting another transaction.");
		}


		/// <summary>Creates a new <see cref="SqlCommand"/> bound to <see cref="Connection"/>, with the
		/// ambient <see cref="Transaction"/> (if any) automatically attached.</summary>
		public SqlCommand CreateCommand()
		{
			var cmd = Connection!.CreateCommand();

			if (Transaction != null)
				cmd.Transaction = Transaction;

			return cmd;
		}


		/// <summary>Creates a command pre-configured with <paramref name="sql"/> (auto-detected as a
		/// stored-procedure name or a text batch) and the supplied <paramref name="sqlParameters"/>.
		/// Null entries, such as an omitted TVP from <c>SqlParam.Table</c>, are skipped.</summary>
		public SqlCommand CreateCommand(string sql, params SqlParameter?[] sqlParameters)
		{
			var cmd = CreateCommand();

			cmd.ConfigureCommand(sql, sqlParameters);

			return cmd;
		}


		/// <summary>Creates a command pre-configured with <paramref name="sql"/>, then yields it to
		/// <paramref name="action"/> for parameter setup — the lambda style avoids an array allocation
		/// and reads naturally with named <c>Add*Param</c> calls.</summary>
		public SqlCommand CreateCommand(string sql, Action<SqlCommand> action)
		{
			var cmd = CreateCommand();

			cmd.ConfigureCommand(sql, action);

			return cmd;
		}


		/// <summary>Creates a fresh <see cref="SqlCommand"/>, hands it to <paramref name="func"/>, and returns
		/// whatever <paramref name="func"/> produces. The command is disposed on exit.</summary>
		/// <remarks>
		/// This is the most general entry point: configure the command (<c>UseProcedure</c>/<c>UseSql</c> + <c>AddXxxParam</c>),
		/// then call <c>cmd.ReadTo&lt;T&gt;()</c> / <c>cmd.ReadToList&lt;T&gt;()</c> / etc. and return the result.
		/// For one-line calls without parameters, prefer the shortcut overloads on <see cref="RepositoryBase"/>
		/// such as <see cref="ReadTo{T}(string, SqlParameter[])"/>.
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// var users = repo.GetByCommand(cmd =>
		/// {
		///     cmd.UseProcedure("dbo.GetUsersByRole");
		///     cmd.AddTinyIntParam("@RoleId", roleId);
		///     return cmd.ReadToList<User>();
		/// });
		/// ]]></code>
		/// </example>
		public T GetByCommand<T>(Func<SqlCommand, T> func)
		{
			using var cmd = CreateCommand();
			return func(cmd);
		}

		/// <inheritdoc cref="GetByCommand{T}(Func{SqlCommand, T})"/>
		public async Task<T> GetByCommandAsync<T>(Func<SqlCommand, Task<T>> funcAsync)
		{
			using var cmd = CreateCommand();
			return await funcAsync(cmd).ConfigureAwait(false);
		}

		/// <inheritdoc cref="GetByCommand{T}(Func{SqlCommand, T})"/>
		public async Task<T> GetByCommandAsync<T>(Func<SqlCommand, CancellationToken, Task<T>> funcAsync, CancellationToken cancellationToken)
		{
			using var cmd = CreateCommand();
			return await funcAsync(cmd, cancellationToken).ConfigureAwait(false);
		}

		private static int ExecuteCommand(SqlCommand cmd)
		{
			var returnValueParam = cmd.ReturnValueParam();
			var isConnectionClosed = true;

			try
			{
				isConnectionClosed = cmd.Connection.State == ConnectionState.Closed;

				if (isConnectionClosed)
					cmd.Connection.Open();

				cmd.ExecuteNonQuery();
			}
			finally
			{
				if (isConnectionClosed)
					cmd.Connection.Close();
			}

			return (int) returnValueParam.Value!;  // ReturnValueParam() ensured a valid return parameter before execution.
		}


		/// <summary>Creates a command, lets <paramref name="action"/> configure it (procedure name + parameters),
		/// runs <c>ExecuteNonQuery</c>, and returns the TSQL <c>RETURN</c> value.
		/// After configuration, reuses a supplied int return parameter or creates one if absent.</summary>
		/// <remarks>
		/// <para>Three sibling entry points fire <c>ExecuteNonQuery</c> behind the scenes — pick the one that
		/// matches what you need:</para>
		/// <list type="bullet">
		/// <item><see cref="ExecuteCommand(Action{SqlCommand})"/> — full control over both <c>UseProcedure</c>/<c>UseSql</c>
		/// and parameters; returns the <c>RETURN</c> value.</item>
		/// <item><see cref="Execute(string, SqlParameter[])"/> / <see cref="Execute(string, Action{SqlCommand})"/> —
		/// shortcut: pass the procedure-or-SQL string up front; returns the <c>RETURN</c> value.</item>
		/// <item><see cref="RunCommand(Action{SqlCommand})"/> — when you don't care about <c>RETURN</c>
		/// (e.g. raw <c>ExecuteScalar</c>/<c>ExecuteReader</c> inside the lambda); returns nothing.</item>
		/// </list>
		/// </remarks>
		public Int32 ExecuteCommand (Action<SqlCommand> action)
		{
			using var cmd = CreateCommand();

			action(cmd);

			return ExecuteCommand(cmd);
		}

		/// <summary>Executes <paramref name="sql"/> (procedure or text batch) with the given parameters
		/// and returns the TSQL <c>RETURN</c> value (0 if there was no <c>RETURN</c>).</summary>
		/// <remarks>See <see cref="ExecuteCommand(Action{SqlCommand})"/> for how this relates to
		/// <c>ExecuteCommand</c> and <see cref="RunCommand(Action{SqlCommand})"/>.</remarks>
		/// <example>
		/// <code><![CDATA[
		/// // Stored procedure that returns 1 / 2 / 0 from RETURN.
		/// var status = repo.Execute("dbo.DeleteUser", new SqlParameter("@UserId", userId));
		///
		/// if (status == 1) throw new InvalidOperationException("User cannot be deleted");
		/// if (status == 2) throw new KeyNotFoundException("User not found");
		/// ]]></code>
		/// </example>
		public Int32 Execute (string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);

			return ExecuteCommand(cmd);
		}

		/// <inheritdoc cref="Execute(string, SqlParameter[])"/>
		public Int32 Execute (string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);

			return ExecuteCommand(cmd);
		}
	
		private static async Task<Int32> ExecuteCommandAsync (SqlCommand cmd, CancellationToken cancellationToken = default)
		{
			var returnValueParam = cmd.ReturnValueParam();
			var isConnectionClosed = true;

			try
			{
				isConnectionClosed = cmd.Connection.State == ConnectionState.Closed;

				if (isConnectionClosed)
					await cmd.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);

				await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				if (isConnectionClosed)
					await cmd.Connection.CloseAsync().ConfigureAwait(false);
			}

			return (int) returnValueParam.Value!;  // ReturnValueParam() ensured a valid return parameter before execution.
		}
	
		/// <inheritdoc cref="ExecuteCommand(Action{SqlCommand})"/>
		public async Task<Int32> ExecuteCommandAsync (Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand();

			action(cmd);

			return await ExecuteCommandAsync(cmd, cancellationToken).ConfigureAwait(false);
		}
	
		/// <inheritdoc cref="Execute(string, SqlParameter[])"/>
		public async Task<Int32> ExecuteAsync (string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);

			return await ExecuteCommandAsync(cmd).ConfigureAwait(false);
		}

		/// <inheritdoc cref="Execute(string, SqlParameter[])"/>
		public async Task<Int32> ExecuteAsync (string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);

			return await ExecuteCommandAsync(cmd, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="Execute(string, SqlParameter[])"/>
		public async Task<Int32> ExecuteAsync (string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);

			return await ExecuteCommandAsync(cmd, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>Creates a fresh <see cref="SqlCommand"/>, hands it to <paramref name="action"/>, and
		/// disposes it on exit. Returns nothing — use this when you don't need the TSQL <c>RETURN</c> value.</summary>
		/// <remarks>
		/// Differs from <see cref="ExecuteCommand(Action{SqlCommand})"/> in two ways: no <c>@ReturnValue</c>
		/// parameter is added, and the lambda is responsible for opening the connection and calling
		/// <c>Execute*</c> itself. Useful for <c>ExecuteReader</c>/<c>ExecuteScalar</c> patterns where the
		/// return value is irrelevant. See also the
		/// <a href="https://github.com/lobodava/artisan-orm/wiki/RepositoryBase-methods-for-SqlCommand-initialization#runcommand">wiki</a>.
		/// </remarks>
		public void RunCommand(Action<SqlCommand> action)
		{
			using var cmd = CreateCommand();
			action(cmd);
		}

		/// <inheritdoc cref="RunCommand(Action{SqlCommand})"/>
		public async Task RunCommandAsync(Func<SqlCommand, Task> asyncAction)
		{
			using var cmd = CreateCommand();
			await asyncAction(cmd).ConfigureAwait(false);
		}

		/// <inheritdoc cref="RunCommand(Action{SqlCommand})"/>
		public async Task RunCommandAsync(Func<SqlCommand, CancellationToken, Task> asyncAction, CancellationToken cancellationToken)
		{
			using var cmd = CreateCommand();
			await asyncAction(cmd, cancellationToken).ConfigureAwait(false);
		}

		#region [ BulkCopy ]

		/// <summary>
		/// Bulk-inserts <paramref name="rows"/> into <paramref name="destinationTable"/> using a registered mapper.
		/// Uses the repository's current <see cref="Transaction"/> if one is active.
		/// Returns the number of rows written.
		/// </summary>
		public int BulkCopy<T>(
			IEnumerable<T> rows,
			string destinationTable,
			SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
			int batchSize = 0,
			int timeout = 30)
		{
			return Connection!.BulkCopy<T>(rows, destinationTable, Transaction, options, batchSize, timeout);
		}

		/// <summary>
		/// Bulk-inserts <paramref name="rows"/> into <paramref name="destinationTable"/> using auto-mapping.
		/// Uses the repository's current <see cref="Transaction"/> if one is active.
		/// Returns the number of rows written.
		/// </summary>
		public int BulkCopyAs<T>(
			IEnumerable<T> rows,
			string destinationTable,
			SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
			int batchSize = 0,
			int timeout = 30)
		{
			return Connection!.BulkCopyAs<T>(rows, destinationTable, Transaction, options, batchSize, timeout);
		}

		/// <summary>
		/// Asynchronously bulk-inserts <paramref name="rows"/> into <paramref name="destinationTable"/> using a registered mapper.
		/// Uses the repository's current <see cref="Transaction"/> if one is active.
		/// Returns the number of rows written.
		/// </summary>
		public Task<int> BulkCopyAsync<T>(
			IEnumerable<T> rows,
			string destinationTable,
			CancellationToken cancellationToken = default,
			SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
			int batchSize = 0,
			int timeout = 30)
		{
			return Connection!.BulkCopyAsync<T>(rows, destinationTable, cancellationToken, Transaction, options, batchSize, timeout);
		}

		/// <summary>
		/// Asynchronously bulk-inserts <paramref name="rows"/> into <paramref name="destinationTable"/> using auto-mapping.
		/// Uses the repository's current <see cref="Transaction"/> if one is active.
		/// Returns the number of rows written.
		/// </summary>
		public Task<int> BulkCopyAsAsync<T>(
			IEnumerable<T> rows,
			string destinationTable,
			CancellationToken cancellationToken = default,
			SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
			int batchSize = 0,
			int timeout = 30)
		{
			return Connection!.BulkCopyAsAsync<T>(rows, destinationTable, cancellationToken, Transaction, options, batchSize, timeout);
		}

		#endregion


		#region [ ReadTo, ReadAs ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadTo{T}(SqlCommand)"/>
		public T? ReadTo<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadTo<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadTo{T}(SqlCommand)"/>
		public T? ReadTo<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadTo<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadTo{T}(SqlCommand)"/>
		public async Task<T?> ReadToAsync<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToAsync<T>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadTo{T}(SqlCommand)"/>
		public async Task<T?> ReadToAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadTo{T}(SqlCommand)"/>
		public async Task<T?> ReadToAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAs{T}(SqlCommand)"/>
		public T? ReadAs<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAs<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAs{T}(SqlCommand)"/>
		public T? ReadAs<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAs<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAs{T}(SqlCommand)"/>
		public async Task<T?> ReadAsAsync<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsAsync<T>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAs{T}(SqlCommand)"/>
		public async Task<T?> ReadAsAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAs{T}(SqlCommand)"/>
		public async Task<T?> ReadAsAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		#endregion

		#region [ ReadToList, ReadAsList ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadToList{T}(SqlCommand)"/>
		public IList<T> ReadToList<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToList<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IList<T> ReadToList<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToList<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<T>> ReadToListAsync<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListAsync<T>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<T>> ReadToListAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<T>> ReadToListAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToListAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IList<T> ReadAsList<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsList<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IList<T> ReadAsList<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsList<T>();
		}
	
		/// <inheritdoc cref="SqlCommandExtensions.ReadAsList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<T>> ReadAsListAsync<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsListAsync<T>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<T>> ReadAsListAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsListAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<T>> ReadAsListAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsListAsync<T>(cancellationToken).ConfigureAwait(false);
		}


		// --- IList<T>? list overloads (pre-populate an existing list) ---

		/// <inheritdoc cref="SqlCommandExtensions.ReadToList{T}(SqlCommand, IList{T})"/>
		public IList<T> ReadToList<T>(string sql, IList<T>? list, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToList<T>(list);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IList<T> ReadToList<T>(string sql, IList<T>? list, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToList<T>(list);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<T>> ReadToListAsync<T>(string sql, IList<T>? list, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListAsync<T>(list, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<T>> ReadToListAsync<T>(string sql, IList<T>? list, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToListAsync<T>(list, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IList<T> ReadAsList<T>(string sql, IList<T>? list, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsList<T>(list);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IList<T> ReadAsList<T>(string sql, IList<T>? list, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsList<T>(list);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<T>> ReadAsListAsync<T>(string sql, IList<T>? list, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsListAsync<T>(list, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsList{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<T>> ReadAsListAsync<T>(string sql, IList<T>? list, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsListAsync<T>(list, cancellationToken).ConfigureAwait(false);
		}

		#endregion

		#region [ ReadToLists ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2}(SqlCommand)"/>
		public (IList<T1>, IList<T2>) ReadToLists<T1, T2>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToLists<T1, T2>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2}(SqlCommand)"/>
		public (IList<T1>, IList<T2>) ReadToLists<T1, T2>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToLists<T1, T2>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2}(SqlCommand)"/>
		public async Task<(IList<T1>, IList<T2>)> ReadToListsAsync<T1, T2>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListsAsync<T1, T2>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2}(SqlCommand)"/>
		public async Task<(IList<T1>, IList<T2>)> ReadToListsAsync<T1, T2>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListsAsync<T1, T2>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2}(SqlCommand)"/>
		public async Task<(IList<T1>, IList<T2>)> ReadToListsAsync<T1, T2>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToListsAsync<T1, T2>(cancellationToken).ConfigureAwait(false);
		}


		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2, T3}(SqlCommand)"/>
		public (IList<T1>, IList<T2>, IList<T3>) ReadToLists<T1, T2, T3>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToLists<T1, T2, T3>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2, T3}(SqlCommand)"/>
		public (IList<T1>, IList<T2>, IList<T3>) ReadToLists<T1, T2, T3>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToLists<T1, T2, T3>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2, T3}(SqlCommand)"/>
		public async Task<(IList<T1>, IList<T2>, IList<T3>)> ReadToListsAsync<T1, T2, T3>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListsAsync<T1, T2, T3>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2, T3}(SqlCommand)"/>
		public async Task<(IList<T1>, IList<T2>, IList<T3>)> ReadToListsAsync<T1, T2, T3>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListsAsync<T1, T2, T3>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2, T3}(SqlCommand)"/>
		public async Task<(IList<T1>, IList<T2>, IList<T3>)> ReadToListsAsync<T1, T2, T3>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToListsAsync<T1, T2, T3>(cancellationToken).ConfigureAwait(false);
		}


		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2, T3, T4}(SqlCommand)"/>
		public (IList<T1>, IList<T2>, IList<T3>, IList<T4>) ReadToLists<T1, T2, T3, T4>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToLists<T1, T2, T3, T4>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2, T3, T4}(SqlCommand)"/>
		public (IList<T1>, IList<T2>, IList<T3>, IList<T4>) ReadToLists<T1, T2, T3, T4>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToLists<T1, T2, T3, T4>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2, T3, T4}(SqlCommand)"/>
		public async Task<(IList<T1>, IList<T2>, IList<T3>, IList<T4>)> ReadToListsAsync<T1, T2, T3, T4>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListsAsync<T1, T2, T3, T4>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2, T3, T4}(SqlCommand)"/>
		public async Task<(IList<T1>, IList<T2>, IList<T3>, IList<T4>)> ReadToListsAsync<T1, T2, T3, T4>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToListsAsync<T1, T2, T3, T4>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToLists{T1, T2, T3, T4}(SqlCommand)"/>
		public async Task<(IList<T1>, IList<T2>, IList<T3>, IList<T4>)> ReadToListsAsync<T1, T2, T3, T4>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToListsAsync<T1, T2, T3, T4>(cancellationToken).ConfigureAwait(false);
		}

		#endregion

		#region [ ReadToArray, ReadAsArray ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadToArray{T}(SqlCommand)"/>
		public T[] ReadToArray<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToArray<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public T[] ReadToArray<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToArray<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public T[] ReadToArray<T>(string sql, Func<SqlDataReader, T> createFunc, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToArray<T>(createFunc);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public T[] ReadToArray<T>(string sql, Func<SqlDataReader, T> createFunc, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToArray<T>(createFunc);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<T[]> ReadToArrayAsync<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToArrayAsync<T>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<T[]> ReadToArrayAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToArrayAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<T[]> ReadToArrayAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToArrayAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<T[]> ReadToArrayAsync<T>(string sql, Func<SqlDataReader, T> createFunc, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToArrayAsync<T>(createFunc, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<T[]> ReadToArrayAsync<T>(string sql, Func<SqlDataReader, T> createFunc, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToArrayAsync<T>(createFunc, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public T[] ReadAsArray<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsArray<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public T[] ReadAsArray<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsArray<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<T[]> ReadAsArrayAsync<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsArrayAsync<T>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<T[]> ReadAsArrayAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsArrayAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsArray{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<T[]> ReadAsArrayAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsArrayAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		#endregion

		#region [ ReadDynamic, ReadDynamicList, ReadDynamicArray ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamic(SqlCommand)"/>
		public dynamic? ReadDynamic(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadDynamic();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamic(Microsoft.Data.SqlClient.SqlCommand)"/>
		public dynamic? ReadDynamic(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadDynamic();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamic(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<dynamic?> ReadDynamicAsync(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadDynamicAsync().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamic(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<dynamic?> ReadDynamicAsync(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadDynamicAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamic(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<dynamic?> ReadDynamicAsync(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadDynamicAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamicList(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IList<dynamic> ReadDynamicList(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadDynamicList();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamicList(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IList<dynamic> ReadDynamicList(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadDynamicList();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamicList(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<dynamic>> ReadDynamicListAsync(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadDynamicListAsync().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamicList(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<dynamic>> ReadDynamicListAsync(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadDynamicListAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamicList(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IList<dynamic>> ReadDynamicListAsync(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadDynamicListAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamicArray(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IList<dynamic> ReadDynamicArray(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadDynamicArray();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamicArray(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IList<dynamic> ReadDynamicArray(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadDynamicArray();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamicArray(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<dynamic[]> ReadDynamicArrayAsync(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadDynamicArrayAsync().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamicArray(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<dynamic[]> ReadDynamicArrayAsync(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadDynamicArrayAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadDynamicArray(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<dynamic[]> ReadDynamicArrayAsync(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadDynamicArrayAsync(cancellationToken).ConfigureAwait(false);
		}

		#endregion

		#region [ ReadToObjectRow, ReadAsObjectRow ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRow(SqlCommand, Func{SqlDataReader, ObjectRow})"/>
		public ObjectRow? ReadToObjectRow(string sql, Func<SqlDataReader, ObjectRow> createFunc, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToObjectRow(createFunc);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRow{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public ObjectRow? ReadToObjectRow(string sql, Func<SqlDataReader, ObjectRow> createFunc, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToObjectRow(createFunc);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRow{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public ObjectRow? ReadToObjectRow<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToObjectRow<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRow{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public ObjectRow? ReadToObjectRow<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToObjectRow<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRow{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRow?> ReadToObjectRowAsync(string sql, Func<SqlDataReader, ObjectRow> createFunc, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowAsync(createFunc).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRow{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRow?> ReadToObjectRowAsync(string sql, Func<SqlDataReader, ObjectRow> createFunc, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowAsync(createFunc, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRow{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRow?> ReadToObjectRowAsync(string sql, Func<SqlDataReader, ObjectRow> createFunc, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToObjectRowAsync(createFunc, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRow{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRow?> ReadToObjectRowAsync<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowAsync<T>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRow{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRow?> ReadToObjectRowAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRow{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRow?> ReadToObjectRowAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToObjectRowAsync<T>(cancellationToken).ConfigureAwait(false);
		}
	
		/// <inheritdoc cref="SqlCommandExtensions.ReadAsObjectRow(Microsoft.Data.SqlClient.SqlCommand)"/>
		public ObjectRow? ReadAsObjectRow(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsObjectRow();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsObjectRow(Microsoft.Data.SqlClient.SqlCommand)"/>
		public ObjectRow? ReadAsObjectRow(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsObjectRow();
		}
	
		/// <inheritdoc cref="SqlCommandExtensions.ReadAsObjectRow(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRow?> ReadAsObjectRowAsync(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsObjectRowAsync().ConfigureAwait(false);
		}
		/// <inheritdoc cref="SqlCommandExtensions.ReadAsObjectRow(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRow?> ReadAsObjectRowAsync(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsObjectRowAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsObjectRow(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRow?> ReadAsObjectRowAsync(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsObjectRowAsync(cancellationToken).ConfigureAwait(false);
		}

	
		#endregion

		#region [ ReadToObjectRows, ReadAsObjectRows ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRows(SqlCommand, Func{SqlDataReader, ObjectRow})"/>
		public ObjectRows ReadToObjectRows(string sql, Func<SqlDataReader, ObjectRow> createFunc, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToObjectRows(createFunc);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRows{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public ObjectRows ReadToObjectRows(string sql, Func<SqlDataReader, ObjectRow> createFunc, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToObjectRows(createFunc);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRows{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public ObjectRows ReadToObjectRows<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToObjectRows<T>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRows{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public ObjectRows ReadToObjectRows<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToObjectRows<T>();
		}
	
		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRows{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRows> ReadToObjectRowsAsync(string sql, Func<SqlDataReader, ObjectRow> createFunc, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowsAsync(createFunc).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRows{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRows> ReadToObjectRowsAsync(string sql, Func<SqlDataReader, ObjectRow> createFunc, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowsAsync(createFunc, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRows{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRows> ReadToObjectRowsAsync(string sql, Func<SqlDataReader, ObjectRow> createFunc, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToObjectRowsAsync(createFunc, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRows{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRows> ReadToObjectRowsAsync<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowsAsync<T>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRows{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRows> ReadToObjectRowsAsync<T>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToObjectRowsAsync<T>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToObjectRows{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRows> ReadToObjectRowsAsync<T>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToObjectRowsAsync<T>(cancellationToken).ConfigureAwait(false);
		}
	
		/// <inheritdoc cref="SqlCommandExtensions.ReadAsObjectRows(Microsoft.Data.SqlClient.SqlCommand)"/>
		public ObjectRows ReadAsObjectRows(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsObjectRows();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsObjectRows(Microsoft.Data.SqlClient.SqlCommand)"/>
		public ObjectRows ReadAsObjectRows(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsObjectRows();
		}
	
		/// <inheritdoc cref="SqlCommandExtensions.ReadAsObjectRows(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRows> ReadAsObjectRowsAsync(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsObjectRowsAsync().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsObjectRows(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRows> ReadAsObjectRowsAsync(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsObjectRowsAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsObjectRows(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<ObjectRows> ReadAsObjectRowsAsync(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsObjectRowsAsync(cancellationToken).ConfigureAwait(false);
		}

		#endregion
	
		#region [ ReadToDictionary ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadToDictionary{TKey, TValue}(SqlCommand)"/>
		public IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(string sql, params SqlParameter?[] sqlParameters) where TKey : notnull
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToDictionary<TKey, TValue>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToDictionary{TKey, TValue}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(string sql, Action<SqlCommand> action) where TKey : notnull
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToDictionary<TKey, TValue>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsDictionary{TKey, TValue}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IDictionary<TKey, TValue> ReadAsDictionary<TKey, TValue>(string sql, params SqlParameter?[] sqlParameters) where TKey : notnull
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadAsDictionary<TKey, TValue>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsDictionary{TKey, TValue}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IDictionary<TKey, TValue> ReadAsDictionary<TKey, TValue>(string sql, Action<SqlCommand> action) where TKey : notnull
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadAsDictionary<TKey, TValue>();
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToDictionary{TKey, TValue}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(string sql, params SqlParameter?[] sqlParameters) where TKey : notnull
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToDictionaryAsync<TKey, TValue>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToDictionary{TKey, TValue}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters) where TKey : notnull
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToDictionaryAsync<TKey, TValue>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToDictionary{TKey, TValue}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default) where TKey : notnull
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToDictionaryAsync<TKey, TValue>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsDictionary{TKey, TValue}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IDictionary<TKey, TValue>> ReadAsDictionaryAsync<TKey, TValue>(string sql, params SqlParameter?[] sqlParameters) where TKey : notnull
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsDictionaryAsync<TKey, TValue>().ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsDictionary{TKey, TValue}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IDictionary<TKey, TValue>> ReadAsDictionaryAsync<TKey, TValue>(string sql, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters) where TKey : notnull
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadAsDictionaryAsync<TKey, TValue>(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsDictionary{TKey, TValue}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public async Task<IDictionary<TKey, TValue>> ReadAsDictionaryAsync<TKey, TValue>(string sql, Action<SqlCommand> action, CancellationToken cancellationToken = default) where TKey : notnull
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadAsDictionaryAsync<TKey, TValue>(cancellationToken).ConfigureAwait(false);
		}

		#endregion 

		#region [ ReadToEnumerable, ReadAsEnumerable ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadToEnumerable{T}(SqlCommand)"/>
		public IEnumerable<T> ReadToEnumerable<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			foreach (var item in cmd.ReadToEnumerable<T>())
				yield return item;
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToEnumerable{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IEnumerable<T> ReadToEnumerable<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			foreach (var item in cmd.ReadToEnumerable<T>())
				yield return item;
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsEnumerable{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IEnumerable<T> ReadAsEnumerable<T>(string sql, params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			foreach (var item in cmd.ReadAsEnumerable<T>())
				yield return item;
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadAsEnumerable{T}(Microsoft.Data.SqlClient.SqlCommand)"/>
		public IEnumerable<T> ReadAsEnumerable<T>(string sql, Action<SqlCommand> action)
		{
			using var cmd = CreateCommand(sql, action);
			foreach (var item in cmd.ReadAsEnumerable<T>())
				yield return item;
		}
	
		#endregion

		#region [ ReadToAsyncEnumerable, ReadAsAsyncEnumerable ]

		/// <summary>
		/// Streams rows from the SQL result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using a registered mapping function for <typeparamref name="T"/> (or scalar conversion for simple types).
		/// Rows are yielded one at a time — no buffering into a list.
		/// Supports cancellation via <c>.WithCancellation(ct)</c> or the <paramref name="cancellationToken"/> parameter.
		/// </summary>
		public async IAsyncEnumerable<T> ReadToAsyncEnumerable<T>(
			string sql,
			[EnumeratorCancellation] CancellationToken cancellationToken = default,
			params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			await foreach (var item in cmd.ReadToAsyncEnumerable<T>(cancellationToken).ConfigureAwait(false))
				yield return item;
		}

		/// <summary>
		/// Streams rows from the SQL result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using a registered mapping function for <typeparamref name="T"/> (or scalar conversion for simple types).
		/// Rows are yielded one at a time — no buffering into a list.
		/// Supports cancellation via <c>.WithCancellation(ct)</c> or the <paramref name="cancellationToken"/> parameter.
		/// </summary>
		public async IAsyncEnumerable<T> ReadToAsyncEnumerable<T>(
			string sql,
			Action<SqlCommand> action,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			await foreach (var item in cmd.ReadToAsyncEnumerable<T>(cancellationToken).ConfigureAwait(false))
				yield return item;
		}

		/// <summary>
		/// Streams rows from the SQL result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using auto-mapping (reflection-based, result cached after first execution).
		/// Rows are yielded one at a time — no buffering into a list.
		/// Supports cancellation via <c>.WithCancellation(ct)</c> or the <paramref name="cancellationToken"/> parameter.
		/// </summary>
		public async IAsyncEnumerable<T> ReadAsAsyncEnumerable<T>(
			string sql,
			[EnumeratorCancellation] CancellationToken cancellationToken = default,
			params SqlParameter?[] sqlParameters)
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			await foreach (var item in cmd.ReadAsAsyncEnumerable<T>(cancellationToken).ConfigureAwait(false))
				yield return item;
		}

		/// <summary>
		/// Streams rows from the SQL result set as an <see cref="IAsyncEnumerable{T}"/>,
		/// using auto-mapping (reflection-based, result cached after first execution).
		/// Rows are yielded one at a time — no buffering into a list.
		/// Supports cancellation via <c>.WithCancellation(ct)</c> or the <paramref name="cancellationToken"/> parameter.
		/// </summary>
		public async IAsyncEnumerable<T> ReadAsAsyncEnumerable<T>(
			string sql,
			Action<SqlCommand> action,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			using var cmd = CreateCommand(sql, action);
			await foreach (var item in cmd.ReadAsAsyncEnumerable<T>(cancellationToken).ConfigureAwait(false))
				yield return item;
		}

		#endregion

		#region [ ReadToTree, ReadToTreeList ]

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTree{T}(SqlCommand, IList{T}, bool)"/>
		public T? ReadToTree<T>(string sql, bool hierarchicallySorted = false, params SqlParameter?[] sqlParameters) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToTree<T>(hierarchicallySorted);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTree{T}(Microsoft.Data.SqlClient.SqlCommand, System.Collections.Generic.IList{T}, bool)"/>
		public T? ReadToTree<T>(string sql, Action<SqlCommand> action, bool hierarchicallySorted = false) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToTree<T>(hierarchicallySorted);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTreeList{T}(Microsoft.Data.SqlClient.SqlCommand, System.Func{Microsoft.Data.SqlClient.SqlDataReader, T}, System.Collections.Generic.IList{T}, bool)"/>
		public IList<T> ReadToTreeList<T>(string sql, bool hierarchicallySorted = false, params SqlParameter?[] sqlParameters) where T: class, INode<T>
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return cmd.ReadToTreeList<T>(hierarchicallySorted);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTreeList{T}(Microsoft.Data.SqlClient.SqlCommand, System.Func{Microsoft.Data.SqlClient.SqlDataReader, T}, System.Collections.Generic.IList{T}, bool)"/>
		public IList<T> ReadToTreeList<T>(string sql, Action<SqlCommand> action, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			using var cmd = CreateCommand(sql, action);
			return cmd.ReadToTreeList<T>(hierarchicallySorted);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTree{T}(Microsoft.Data.SqlClient.SqlCommand, System.Collections.Generic.IList{T}, bool)"/>
		public async Task<T?> ReadToTreeAsync<T>(string sql, bool hierarchicallySorted = false, params SqlParameter?[] sqlParameters) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToTreeAsync<T>(hierarchicallySorted).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTree{T}(Microsoft.Data.SqlClient.SqlCommand, System.Collections.Generic.IList{T}, bool)"/>
		public async Task<T?> ReadToTreeAsync<T>(string sql, CancellationToken cancellationToken = default, bool hierarchicallySorted = false, params SqlParameter?[] sqlParameters) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToTreeAsync<T>(hierarchicallySorted, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTree{T}(Microsoft.Data.SqlClient.SqlCommand, System.Collections.Generic.IList{T}, bool)"/>
		public async Task<T?> ReadToTreeAsync<T>(string sql, Action<SqlCommand> action, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToTreeAsync<T>(hierarchicallySorted, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTree{T}(Microsoft.Data.SqlClient.SqlCommand, System.Collections.Generic.IList{T}, bool)"/>
		public async Task<T?> ReadToTreeAsync<T>(string sql, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToTreeAsync<T>(createFunc, hierarchicallySorted, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTree{T}(Microsoft.Data.SqlClient.SqlCommand, System.Collections.Generic.IList{T}, bool)"/>
		public async Task<T?> ReadToTreeAsync<T>(string sql, Func<SqlDataReader, T> createFunc, Action<SqlCommand> action, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToTreeAsync<T>(createFunc, hierarchicallySorted, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTreeList{T}(Microsoft.Data.SqlClient.SqlCommand, System.Func{Microsoft.Data.SqlClient.SqlDataReader, T}, System.Collections.Generic.IList{T}, bool)"/>
		public async Task<IList<T>> ReadToTreeListAsync<T>(string sql, bool hierarchicallySorted = false, params SqlParameter?[] sqlParameters) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToTreeListAsync<T>(hierarchicallySorted).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTreeList{T}(Microsoft.Data.SqlClient.SqlCommand, System.Func{Microsoft.Data.SqlClient.SqlDataReader, T}, System.Collections.Generic.IList{T}, bool)"/>
		public async Task<IList<T>> ReadToTreeListAsync<T>(string sql, CancellationToken cancellationToken = default, bool hierarchicallySorted = false, params SqlParameter?[] sqlParameters) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToTreeListAsync<T>(hierarchicallySorted, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTreeList{T}(Microsoft.Data.SqlClient.SqlCommand, System.Func{Microsoft.Data.SqlClient.SqlDataReader, T}, System.Collections.Generic.IList{T}, bool)"/>
		public async Task<IList<T>> ReadToTreeListAsync<T>(string sql, Action<SqlCommand> action, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToTreeListAsync<T>(hierarchicallySorted, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTreeList{T}(Microsoft.Data.SqlClient.SqlCommand, System.Func{Microsoft.Data.SqlClient.SqlDataReader, T}, System.Collections.Generic.IList{T}, bool)"/>
		public async Task<IList<T>> ReadToTreeListAsync<T>(string sql, Func<SqlDataReader, T> createFunc, bool hierarchicallySorted = false, CancellationToken cancellationToken = default, params SqlParameter?[] sqlParameters) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, sqlParameters);
			return await cmd.ReadToTreeListAsync<T>(createFunc, hierarchicallySorted, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc cref="SqlCommandExtensions.ReadToTreeList{T}(Microsoft.Data.SqlClient.SqlCommand, System.Func{Microsoft.Data.SqlClient.SqlDataReader, T}, System.Collections.Generic.IList{T}, bool)"/>
		public async Task<IList<T>> ReadToTreeListAsync<T>(string sql, Func<SqlDataReader, T> createFunc, Action<SqlCommand> action, bool hierarchicallySorted = false, CancellationToken cancellationToken = default) where T : class, INode<T>
		{
			using var cmd = CreateCommand(sql, action);
			return await cmd.ReadToTreeListAsync<T>(createFunc, hierarchicallySorted, cancellationToken).ConfigureAwait(false);
		}

		#endregion

		/// <summary>
		/// Adds parameters to <paramref name="cmd"/> by enumerating public properties of
		/// <paramref name="parameters"/> (typically an anonymous type, e.g. <c>new { Id = 1, Name = "Foo" }</c>).
		/// Each property becomes a parameter named after the property, with the property's value.
		/// </summary>
		/// <param name="cmd">The command to add parameters to.</param>
		/// <param name="parameters">
		/// An object whose public properties describe the parameters to add.
		/// Must not be <c>null</c>.
		/// </param>
		public static void AddParams(SqlCommand cmd, object parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			var dict = new Dictionary<string, object>();

			foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(parameters))
			{
				dict.Add(descriptor.Name, descriptor.GetValue(parameters) ?? DBNull.Value);  // convert null property values to DBNull for SQL
			}

			cmd.AddParams(dict);
		}

		/// <summary>Reads the first column of the current result set as a <see cref="DataReply"/> status code.
		/// If the code maps to a non-OK status (e.g. <c>FAIL</c>, <c>MISSING</c>), advances to the next result set
		/// (which is expected to contain validation messages) and throws a <see cref="DataReplyException"/>.
		/// On OK, simply advances to the next result set so the caller can keep reading.</summary>
		/// <exception cref="DataReplyException">Thrown when the leading status code indicates a non-OK reply.</exception>
		public static void CheckForDataReplyException(SqlDataReader dr)
		{
			var statusCode = dr.ReadTo<string>(getNextResult: false);

			var dataReplyStatus = DataReply.ParseStatus(statusCode);

			if (dataReplyStatus != null && dataReplyStatus != DataReplyStatus.Ok)
			{
				if (dr.NextResult())
					throw new DataReplyException(dataReplyStatus.Value, dr.ReadToArray<DataReplyMessage>());

				throw new DataReplyException(dataReplyStatus.Value);
			}

			dr.NextResult();
		}

		/// <summary>Disposes owned resources and detaches borrowed resources without disposing them.</summary>
		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed) return;

			if (disposing)
			{
				if (_ownsResources)
				{
					Transaction?.Dispose();
					Connection?.Dispose();
				}

				Transaction = null;
				Connection = null;
			}

			_disposed = true;
		}

		/// <summary>Asynchronously disposes owned resources and detaches borrowed resources without disposing them.</summary>
		public async ValueTask DisposeAsync()
		{
			await DisposeAsyncCore().ConfigureAwait(false);

			// The managed-resource branch of Dispose(bool) is a no-op after
			// DisposeAsyncCore has released everything, but we still call
			// Dispose(false) so derived classes that override it to release
			// unmanaged resources get their callback.
			Dispose(disposing: false);
			GC.SuppressFinalize(this);
		}

		protected virtual async ValueTask DisposeAsyncCore()
		{
			if (_disposed) return;

			if (_ownsResources)
			{
				if (Transaction != null)
					await Transaction.DisposeAsync().ConfigureAwait(false);

				if (Connection != null)
					await Connection.DisposeAsync().ConfigureAwait(false);
			}

			Transaction = null;
			Connection = null;
			_disposed = true;
		}
	}

}

