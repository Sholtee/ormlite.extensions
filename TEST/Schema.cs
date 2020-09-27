/********************************************************************************
* SchemaTests.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Data;

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

        [SetUp]
        public void Setup() => Connection = ConnectionFactory.OpenDbConnection();

        [TearDown]
        public void Teardown() 
        {
            Connection.Dispose();
            Connection = null;
        }

        [DataTable]
        private class Table1
        {
            public int Irrelevant { get; set; }
        }

        [DataTable]
        private class Table2_ReferencingTable1
        {
            [References(typeof(Table1))]
            public int Table1 { get; set; }
        }

        [DataTable]
        private class Table3_ReferencingNode1AndTable2
        {
            [References(typeof(Table1))]
            public int Table1 { get; set; }

            [References(typeof(Table2_ReferencingTable1))]
            public int Table2 { get; set;  }

            public int Irrelevant { get; set; }
        }

        [Test]
        public void CreateTablesCascaded_ShouldCreateTheTables() 
        {
            var schema = new Schema(Connection, typeof(SchemaTests).Assembly);

            Assert.DoesNotThrow(schema.CreateTablesCascaded);
            Assert.That(Connection.TableExists<Table1>());
            Assert.That(Connection.TableExists<Table2_ReferencingTable1>());
            Assert.That(Connection.TableExists<Table3_ReferencingNode1AndTable2>());
        }

        [Test]
        public void DropTablesCascaded_ShouldDropTheTables()
        {
            var schema = new Schema(Connection, typeof(SchemaTests).Assembly);

            Assert.DoesNotThrow(schema.CreateTablesCascaded);
            Assert.DoesNotThrow(schema.DropTablesCascaded);

            Assert.That(!Connection.TableExists<Table1>());
            Assert.That(!Connection.TableExists<Table2_ReferencingTable1>());
            Assert.That(!Connection.TableExists<Table3_ReferencingNode1AndTable2>());
        }
    }
}
