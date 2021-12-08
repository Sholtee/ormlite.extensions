/********************************************************************************
* SchemaTests.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;
using System.Linq;

using NUnit.Framework;

using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions.Tests
{
    [TestFixture]
    public class SchemaTests
    {
        private static readonly IDbConnectionFactory ConnectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);

        public IDbConnection Connection { get; set; }

        public Schema Schema { get; set; }

        [SetUp]
        public void Setup()
        {
            Connection = ConnectionFactory.OpenDbConnection();
            Schema = new Schema(Connection, typeof(SchemaTests).Assembly);
        }

        [TearDown]
        public void Teardown() 
        {
            Schema.Drop();

            Connection.Dispose();
            Connection = null;
        }

        [DataTable]
        [Values("{A8273CE3-3F29-4F52-9B8A-E12650668FC1}", "Cica", 1986)]
        private class Table1
        {
            [PrimaryKey]
            public Guid Id { get; set; }

            public string StringField { get; set; }

            public int IntField { get; set; }
        }

        [DataTable]
        [Values("{1529C28D-0A0D-4D84-9FDF-563B814B23B6}", "{A8273CE3-3F29-4F52-9B8A-E12650668FC1}", "1986.10.26", null)]
        private class Table2_ReferencingTable1
        {
            [PrimaryKey]
            public Guid Id { get; set; }

            [References(typeof(Table1))]
            public Guid Table1 { get; set; }

            public DateTime Date { get; set; }

            public string Void { get; set; }
        }

        [DataTable]
        private class Table3_ReferencingNode1AndTable2
        {
            [PrimaryKey]
            public Guid Id { get; set; }

            [References(typeof(Table1))]
            public Guid Table1 { get; set; }

            [References(typeof(Table2_ReferencingTable1))]
            public Guid Table2 { get; set;  }

            public int Irrelevant { get; set; }
        }

        [Test]
        public void Initialize_ShouldCreateTables() 
        {
            Assert.DoesNotThrow(Schema.Initialize);
            Assert.That(Connection.TableExists<Table1>());
            Assert.That(Connection.TableExists<Table2_ReferencingTable1>());
            Assert.That(Connection.TableExists<Table3_ReferencingNode1AndTable2>());
        }

        [Test]
        public void Initialize_ShouldInsertTheWiredRows() 
        {
            Schema.Initialize();

            Table1 t1 = Connection.SelectByIds<Table1>(new[] { Guid.Parse("{A8273CE3-3F29-4F52-9B8A-E12650668FC1}") }).Single();
            Assert.That(t1.StringField, Is.EqualTo("Cica"));
            Assert.That(t1.IntField, Is.EqualTo(1986));

            Table2_ReferencingTable1 t2 = Connection.SelectByIds<Table2_ReferencingTable1>(new[] { Guid.Parse("{1529C28D-0A0D-4D84-9FDF-563B814B23B6}") }).Single();
            Assert.That(t2.Table1, Is.EqualTo(Guid.Parse("{A8273CE3-3F29-4F52-9B8A-E12650668FC1}")));
            Assert.That(t2.Date, Is.EqualTo(DateTime.Parse("1986-10-26 00:00:00").ToLocalTime()));
            Assert.That(t2.Void, Is.Null);
        }

        [Test]
        public void Initialize_ShouldThowIfTheSchemaHasAlreadyBeenInitialized()
        {
            Schema.Initialize();
            Assert.Throws<InvalidOperationException>(Schema.Initialize);
        }

        [Test]
        public void Drop_ShouldDropTheTables()
        {
            Assert.DoesNotThrow(Schema.Initialize);
            Assert.DoesNotThrow(Schema.Drop);

            Assert.That(!Connection.TableExists<Table1>());
            Assert.That(!Connection.TableExists<Table2_ReferencingTable1>());
            Assert.That(!Connection.TableExists<Table3_ReferencingNode1AndTable2>());
        }

        [Test]
        public void IsInitialized_ShouldReturnTrueIfTheSchemaHasAlreadyBeenInitialized()
        {
            Assert.IsFalse(Schema.IsInitialized);

            Schema.Initialize();

            Assert.That(Schema.IsInitialized);
            Assert.That(Schema.GetLastMigration(), Is.EqualTo(Schema.INITIAL_COMMIT));
        }

        [Test]
        public void Migrate_ShouldSaveTheMigrationStep()
        {
            Schema.Initialize();

            Connection.Insert(new Table1 { Id = Guid.NewGuid(), IntField = 10, StringField = "kutya" });

            const string migrationName = nameof(migrationName);

            Assert.That(Schema.Migrate(migrationName, "UPDATE \"Table1\" SET \"IntField\" = 0"));
            Assert.That(Connection.Select<Table1>().All(t => t.IntField == 0));
            Assert.That(Schema.GetLastMigration(), Is.EqualTo(migrationName));
        }
    }
}
