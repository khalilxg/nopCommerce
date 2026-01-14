using FluentMigrator;
using Nop.Core.Domain.Orders;
using Nop.Data.Extensions;

namespace Nop.Data.Migrations.UpgradeTo500;

[NopSchemaMigration("2026-01-13 00:00:01", "SchemaMigration for 5.00.0")]
public class SchemaMigration : ForwardOnlyMigration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        //#7386
        this.AddOrAlterColumnFor<Order>(t => t.DesiredDeliveryDateUtc)
            .AsDateTime()
            .Nullable();
    }
}
