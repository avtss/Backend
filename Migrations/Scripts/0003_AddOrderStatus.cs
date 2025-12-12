using System;
using FluentMigrator;

[Migration(3)]
public class AddOrderStatus : Migration
{
    public override void Up()
    {
        var sql = @"
            alter table orders
                add column if not exists order_status text not null default 'created';

            alter type v1_order
                add attribute order_status text;
        ";

        Execute.Sql(sql);
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }
}
