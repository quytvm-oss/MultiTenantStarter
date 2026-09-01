dotnet ef migrations add Add-Multitenant --project ../MultiTenantStarter.Migrations.PostgreSQL --startup-project . --context TenantDbContext -o Multitenancy
dotnet ef migrations add Add-Identity --project ../MultiTenantStarter.Migrations.PostgreSQL --startup-project . --context IdentityDbContext -o Identity
dotnet ef migrations add Init-Auditing --project ../MultiTenantStarter.Migrations.PostgreSQL --startup-project . --context AuditDbContext -o Auditing
dotnet ef migrations add Init-Webhooks --project ../MultiTenantStarter.Migrations.PostgreSQL --startup-project . --context WebhookDbContext -o Webhooks
