/********************************************************************************
* DocumentRepositoryTests.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using NUnit.Framework;

using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions.EventStream.Tests
{
    [TestFixture]
    public class DocumentRepositoryTests
    {
        private static readonly IDbConnectionFactory ConnectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);

        public IDbConnection Connection { get; set; }

        public IDocumentRepository<string, MyView> Repository { get; set; }

        [SetUp]
        public void Setup()
        {
            OrmLiteConnection conn = (OrmLiteConnection) ConnectionFactory.Open();

            SQLiteConnection sqliteConn = (SQLiteConnection) conn.DbConnection;
            sqliteConn.EnableExtensions(true);
            sqliteConn.LoadExtension("SQLite.Interop.dll", "sqlite3_json_init");

            Connection = conn;
            Connection.CreateTable<MyDocument>();
            Connection.ExecuteSql("SELECT JSON('{\"a\": \"b\"}')");

            Repository = new DocumentRepository<string, MyDocument, MyView>(Connection);
        }

        [TearDown]
        public void TearDown()
        {
            if (Connection is not null)
            {
                Connection.DropTable<MyDocument>();
                Connection.Dispose();
            }
        }

        public class MyView: IEntity<string>
        {
            public int Prop { get; set; }
            public string StreamId { get; init; }
        }

        public class MyDocument : Document<string>
        {
        }

        [Test]
        public async Task QueryBySimpleCondition_ShouldReturnTheProperViews()
        {
            Connection.Insert(new MyDocument { StreamId = "cica", Type = typeof(MyView).AssemblyQualifiedName, Payload = JsonSerializer.Serialize(new MyView { Prop = 1986 }) });
            Connection.Insert(new MyDocument { StreamId = "kutya", Type = typeof(MyView).AssemblyQualifiedName, Payload = JsonSerializer.Serialize(new MyView { Prop = 2000 }) });

            IList<MyView> result = await Repository.QueryBySimpleCondition<int>("$.Prop", prop => prop == 1986);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Prop, Is.EqualTo(1986));
        }

        [Test]
        public async Task QueryBySimpleCondition_ShouldReturnTheProperViews2()
        {
            Connection.Insert(new MyDocument { StreamId = "cica", Type = typeof(MyView).AssemblyQualifiedName, Payload = JsonSerializer.Serialize(new MyView { Prop = 1986 }) });
            Connection.Insert(new MyDocument { StreamId = "kutya", Type = typeof(MyView).AssemblyQualifiedName, Payload = JsonSerializer.Serialize(new MyView { Prop = 2000 }) });

            IList<MyView> result = await Repository.QueryBySimpleCondition<int>("$.Prop", prop => prop >= 1986);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(view => view.Prop == 1986));
            Assert.That(result.Any(view => view.Prop == 2000));
        }

        [Test]
        public async Task QueryBySimpleCondition_ShouldReturnAnEmptyListIfThereIsNoResult()
        {
            Connection.Insert(new MyDocument { StreamId = "cica", Type = typeof(MyView).AssemblyQualifiedName, Payload = JsonSerializer.Serialize(new MyView { Prop = 1986 }) });
            Connection.Insert(new MyDocument { StreamId = "kutya", Type = typeof(MyView).AssemblyQualifiedName, Payload = JsonSerializer.Serialize(new MyView { Prop = 2000 }) });

            IList<MyView> result = await Repository.QueryBySimpleCondition<int>("$.Prop", prop => prop > 2000);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task InsertOrUpdate_ShouldInsertANewRowIfThereIsNoEntityToUpdate()
        {
            await Repository.InsertOrUpdate(new MyView { StreamId = "cica", Prop = 1986 });
            Assert.That(Connection.Count<MyDocument>(doc => doc.StreamId == "cica"), Is.EqualTo(1));
        }

        [Test]
        public async Task InsertOrUpdate_ShouldUpdateTheEntity()
        {
            await Repository.InsertOrUpdate(new MyView { StreamId = "cica", Prop = 1986 });

            Assert.That(Connection.Count<MyDocument>(doc => doc.StreamId == "cica"), Is.EqualTo(1));
            Assert.DoesNotThrowAsync(() => Repository.InsertOrUpdate(new MyView { StreamId = "cica", Prop = 2000 }));
            Assert.That(Connection.Count<MyDocument>(), Is.EqualTo(1));
            Assert.That(await Repository.QueryBySimpleCondition<int>("$.Prop", prop => prop == 2000), Has.Length.EqualTo(1));
        }
    }
}
