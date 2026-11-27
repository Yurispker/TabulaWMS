# TabulaWMS

## Migrate tenant schemas

Stop the normal API process, then run this from the repository root:

```powershell
dotnet run --project src\TabulaWMS\TabulaWMS.csproj -- --migrate-tenants
```

The command reads tenant IDs from `public.TenantUserDirectory`, applies pending
EF Core migrations to each existing tenant schema, and logs each result. It
continues after an individual tenant fails and exits with a nonzero status if
any tenant migration fails. New tenants continue to receive migrations during
provisioning.