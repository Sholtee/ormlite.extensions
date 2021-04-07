# Solti.Utils.OrmLite.Extensions [![Build status](https://ci.appveyor.com/api/projects/status/vk8y761wbgwtusn4/branch/master?svg=true)](https://ci.appveyor.com/project/Sholtee/ormlite-extensions/branch/master) ![AppVeyor tests (branch)](https://img.shields.io/appveyor/tests/sholtee/ormlite-extensions/master) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/ormlite.extensions/badge.svg?branch=master)](https://coveralls.io/github/Sholtee/ormlite.extensions?branch=master) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/solti.utils.ormlite.extensions)](https://www.nuget.org/packages/solti.utils.ormlite.extensions )
> [ServiceStack.OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite ) extensions

## Schema API
```csharp
using ServiceStack.DataAnnotations;
using Solti.Utils.OrmLite.Extension;
...
[DataTable]
[Values(1, "{4399A489-B140-4A04-BCBC-8C31082F431F}")] // predefined row
private class Table1
{
  [PrimaryKey]
  public int Id { get; set; }
  public Guid Data {get; set;}
}

[DataTable]
[Values(1, 1)]
private class Table2_ReferencingTable1
{
  [PrimaryKey]
  public int Id { get; set; }
  [References(typeof(Table1))]
  public int Table1 { get; set; }
}

[DataTable]
private class Table3_ReferencingNode1AndTable2
{
  [PrimaryKey]
  public int Id { get; set; }
  [References(typeof(Table1))]
  public int Table1 { get; set; }
  [References(typeof(Table2_ReferencingTable1))]
  public int Table2 { get; set;  }
}
...
var schema = new Schema(Connection, typeof(Table1).Assembly);
schema.CreateTablesCascaded();
...
schema.DropTablesCascaded();
```

## Bulked write operations
```csharp
using ServiceStack.OrmLite;
using Solti.Utils.OrmLite.Extension;
...
using IBulkedDbConnection connection = Connection.CreateBulkedDbConnection();
...
connection.Update(...);
connection.Insert(...);
connection.Delte(...);
...
connection.Flush();
```