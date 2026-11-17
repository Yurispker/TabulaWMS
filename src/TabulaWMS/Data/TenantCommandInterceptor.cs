using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TabulaWMS.Services;

namespace TabulaWMS.Data;

public class TenantCommandInterceptor : DbCommandInterceptor
{
    private static readonly Regex ValidTenantRegex = new("^[a-z0-9_]+$", RegexOptions.Compiled);
    private readonly ITenantService _tenantService;

    public TenantCommandInterceptor(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        SetSearchPath(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await SetSearchPathAsync(command, cancellationToken);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        SetSearchPath(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await SetSearchPathAsync(command, cancellationToken);
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void SetSearchPath(DbCommand command)
    {
        using var searchPathCommand = CreateSearchPathCommand(command);
        searchPathCommand.ExecuteNonQuery();
    }

    private async Task SetSearchPathAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var searchPathCommand = CreateSearchPathCommand(command);
        await searchPathCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateSearchPathCommand(DbCommand command)
    {
        var tenantId = _tenantService.GetTenantId();
        var schema = (!string.IsNullOrWhiteSpace(tenantId) && ValidTenantRegex.IsMatch(tenantId))
            ? tenantId
            : "public";

        var searchPathCommand = command.Connection?.CreateCommand()
            ?? throw new InvalidOperationException("The database command has no connection.");
        searchPathCommand.Transaction = command.Transaction;
        searchPathCommand.CommandText = $"SET search_path TO \"{schema}\", public;";
        return searchPathCommand;
    }
}