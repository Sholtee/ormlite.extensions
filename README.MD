# Solti.Utils.OrmLite.Extensions [![Build status](https://ci.appveyor.com/api/projects/status/vk8y761wbgwtusn4/branch/master?svg=true)](https://ci.appveyor.com/project/Sholtee/ormlite-extensions/branch/master) ![AppVeyor tests (branch)](https://img.shields.io/appveyor/tests/sholtee/ormlite-extensions/master) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/ormlite.extensions/badge.svg?branch=master)](https://coveralls.io/github/Sholtee/ormlite.extensions?branch=master) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/solti.utils.ormlite.extensions)](https://www.nuget.org/packages/solti.utils.ormlite.extensions ) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/sholtee/ormlite.extensions/master )
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

if (!schema.IsInitialized)
  schema.Initialze();
else
{
  schema.Migrate(step_1_timestamp, "sql...", "step_1");
  schema.Migrate(step_2_timestamp, "sql...", "step_2");
}
...
schema.Drop();
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
connection.Flush(); // or FlushAsync()
```

## EventRepository
```csharp
using Solti.Utils.OrmLite.Extension;
using Solti.Utils.OrmLite.Extension.EventStream;
...
// data table
[DataTable]
public class EventTable : Event<string>
{
}

// custom event
public class MyEvent1 
{
  public string Prop1 { get; set; }
}

// custom event
public class MyEvent2
{
  public int Prop2 { get; set; }
}

// materialized view
public class MyView : Aggregate<string, MyEvent1, MyEvent2>
{
  public string Prop1 { get; set; }
  public int? Prop2 { get; set; }
  public override void Apply(MyEvent1 evt) => Prop1 = evt.Prop1;
  public override void Apply(MyEvent2 evt) => Prop2 = evt.Prop2;
}

...

using IDbConnection conn = ConnectionFactory.OpenDbConnection();
conn.CreateTable<EventTable>();

IEventRepository<string, MyView> repo = new EventRepository<string, EventTable, MyView>(conn);

string streamId = "stream_1";

MyView state = await repo.CreateEvent(streamId, new MyEvent1 { Prop1 = "cica" });
state = await repo.CreateEvent(streamId, new MyEvent1 { Prop2 = 1986 });

MyView view = repo.QueryViewsByStreamId(default, "stream_1");
...
```

## DocumentRepository
```csharp
using Solti.Utils.OrmLite.Extension;
using Solti.Utils.OrmLite.Extension.EventStream;
...
// data table
[DataTable]
public class DocumentTable : Document<string>
{
}

// materialized view
public class MyView: IEntity<string>
{
  public int Prop { get; set; }
  public string StreamId { get; init; }
}

...

using IDbConnection conn = ConnectionFactory.OpenDbConnection();
conn.CreateTable<DocumentTable>();

IDocumentRepository<string, MyView> repo = new DocumentRepository<string, MyDocument, MyView>(conn);

await repo.InsertOrUpdate(new MyView { StreamId = "cica", Prop = 1986 });

...

IList<MyView> result = await repo.QueryBySimpleCondition<int>("$.Prop", prop => prop == 1986);
```