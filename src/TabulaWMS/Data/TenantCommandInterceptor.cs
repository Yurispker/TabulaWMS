using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TabulaWMS.Services;

namespace TabulaWMS.Data;

public class TenantCommandInterceptor : DbCommandInterceptor
{
    private static readonly Regex ValidTenantRegex = new("^[a-z0-9_]+$", RegexOptions.Compiled);
    private readonly ITenantService _tenantService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantCommandInterceptor(ITenantService tenantService, IHttpContextAccessor httpContextAccessor)
    {
        _tenantService = tenantService;
        _httpContextAccessor = httpContextAccessor;
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

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        SetSearchPath(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        await SetSearchPathAsync(command, cancellationToken);
        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
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
        
        if (searchPathCommand == null)
    {
        return; // Safely exit if there's no HttpContext (startup/migrations)
    }
        searchPathCommand.ExecuteNonQuery();
    }

    private async Task SetSearchPathAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var searchPathCommand = CreateSearchPathCommand(command);

        if (searchPathCommand == null)
    {
        return; // Safely exit if there's no HttpContext (startup/migrations)
    }
        await searchPathCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateSearchPathCommand(DbCommand command)
    {

        // If there is no active HTTP request (e.g. startup seeding or migrations), 
        // bypass the tenant search_path safely without throwing.
        if (_httpContextAccessor.HttpContext == null)
        {
            return null!;
        }

        var tenantId = _tenantService.GetTenantId();
        
        if (string.IsNullOrWhiteSpace(tenantId) || !ValidTenantRegex.IsMatch(tenantId))
            {
                throw new InvalidOperationException($"Invalid tenant ID: {tenantId}");
            }

        var searchPathCommand = command.Connection?.CreateCommand()
            ?? throw new InvalidOperationException("The database command has no connection.");
        searchPathCommand.Transaction = command.Transaction;
        searchPathCommand.CommandText = $"SET search_path TO \"{tenantId}\", public;";
        return searchPathCommand;
    }
}