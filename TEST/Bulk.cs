/********************************************************************************
* Bulk.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;
using System.Data.SqlClient;

using Moq;
using NUnit.Framework;

using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions.Tests
{
    [TestFixture]
    public sealed class BulkTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup() => OrmLiteConfig.DialectProvider = SqliteDialect.Provider;

        [Test]
        public void Bulk_ShouldInterceptWriteCommands()
        {
            const string
                CMD_1 = "DELETE FROM 'KUTYA'",
                CMD_2 = "DELETE FROM 'CICA'";

            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection.Setup(c => c.CreateCommand()).Returns(() => new SqlCommand());

            using (IBulkedDbConnection bulk = mockDbConnection.Object.CreateBulkedDbConnection())
            {
                foreach(string command in new[] {CMD_1, CMD_2})
                {
                    using (IDbCommand cmd = bulk.CreateCommand())
                    {
                        cmd.CommandText = command;
                        cmd.ExecuteNonQuery();
                    }
                }

                Assert.That(bulk.ToString(), Is.EqualTo($"{CMD_1};\r\n{CMD_2};\r\n"));
            }
        }

        [Test]
        public void Bulk_ShouldFlush()
        {
            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection.Setup(c => c.CreateCommand()).Returns(() => new SqlCommand());

            using (IBulkedDbConnection bulk = mockDbConnection.Object.CreateBulkedDbConnection())
            {
                using (IDbCommand cmd = bulk.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM 'KUTYA'";
                    cmd.ExecuteNonQuery();
                }

                string bulkCmd = bulk.ToString();

                var mockDbCommand = new Mock<IDbCommand>(MockBehavior.Strict);
                mockDbCommand.Setup(cmd => cmd.ExecuteNonQuery()).Returns(0);
                mockDbCommand.Setup(cmd => cmd.Dispose());
                mockDbCommand.SetupSet(cmd => cmd.CommandText = It.IsAny<string>()).Verifiable();
                mockDbCommand.SetupSet(cmd => cmd.Transaction = null);
                mockDbCommand.SetupSet(cmd => cmd.CommandTimeout = It.IsAny<int>());
                mockDbCommand.SetupGet(cmd => cmd.CommandText).Returns(bulkCmd);

                mockDbConnection.Setup(c => c.CreateCommand()).Returns(() => mockDbCommand.Object);

                Assert.DoesNotThrow(() => bulk.Flush());
                Assert.That(bulk.ToString().Length, Is.EqualTo(0));

                mockDbCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(val => val == bulkCmd));
                mockDbCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
            }
        }

        [Test]
        public void Bulk_ShouldFlushAsync()
        {
            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection.Setup(c => c.CreateCommand()).Returns(() => new SqlCommand());

            using (IBulkedDbConnection bulk = mockDbConnection.Object.CreateBulkedDbConnection())
            {
                using (IDbCommand cmd = bulk.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM 'KUTYA'";
                    cmd.ExecuteNonQuery();
                }

                string bulkCmd = bulk.ToString();

                var mockDbCommand = new Mock<IDbCommand>(MockBehavior.Strict);
                mockDbCommand.Setup(cmd => cmd.ExecuteNonQuery()).Returns(0);
                mockDbCommand.Setup(cmd => cmd.Dispose());
                mockDbCommand.SetupSet(cmd => cmd.CommandText = It.IsAny<string>()).Verifiable();
                mockDbCommand.SetupSet(cmd => cmd.Transaction = null);
                mockDbCommand.SetupSet(cmd => cmd.CommandTimeout = It.IsAny<int>());
                mockDbCommand.SetupGet(cmd => cmd.CommandText).Returns(bulkCmd);

                mockDbConnection.Setup(c => c.CreateCommand()).Returns(() => mockDbCommand.Object);

                Assert.DoesNotThrowAsync(() => bulk.FlushAsync());
                Assert.That(bulk.ToString().Length, Is.EqualTo(0));

                mockDbCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(val => val == bulkCmd));
                mockDbCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
            }
        }

        [Test]
        public void Bulk_ShouldHandleParameters()
        {
            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection.Setup(c => c.CreateCommand()).Returns(() => new SqlCommand());

            using (IBulkedDbConnection bulk = mockDbConnection.Object.CreateBulkedDbConnection())
            {
                using (IDbCommand cmd = bulk.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Region (RegionID, RegionDescription) VALUES (@RegionID, @RegionDescription)";
                    cmd.Parameters.Add(new SqlParameter { DbType = DbType.Int32, ParameterName = "@RegionID", Value = 1 });
                    cmd.Parameters.Add(new SqlParameter { DbType = DbType.String, ParameterName = "@RegionDescription", Value = "cica" });
                    cmd.ExecuteNonQuery();
                }

                Assert.That(bulk.ToString(), Is.EqualTo("INSERT INTO Region (RegionID, RegionDescription) VALUES (1, 'cica');\r\n"));
            }
        }

        private class MyEntity 
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }

        [Test]
        public void Bulk_ShouldHandleNulls()
        {
            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection.Setup(c => c.CreateCommand()).Returns(() => new SqlCommand());

            using IBulkedDbConnection bulk = mockDbConnection.Object.CreateBulkedDbConnection();

            bulk.Insert(new MyEntity { Id = 1, Value = "cica" });
            bulk.Insert(new MyEntity { Id = 2, Value = null });

            Assert.That(bulk.ToString(), Is.EqualTo("INSERT INTO \"MyEntity\" (\"Id\",\"Value\") VALUES (1,'cica');\r\nINSERT INTO \"MyEntity\" (\"Id\",\"Value\") VALUES (2,null);\r\n"));
        }

        [Test]
        public void Bulk_ShouldNotAllowReadCommands()
        {
            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection.Setup(c => c.CreateCommand()).Returns(() => new SqlCommand());

            using (IBulkedDbConnection bulk = mockDbConnection.Object.CreateBulkedDbConnection())
            {
                using (IDbCommand cmd = bulk.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM 'CICA'";
                    Assert.Throws<NotSupportedException>(() => cmd.ExecuteReader());
                }
            }
        }

        [Test]
        public void Bulk_ShouldNotBeBulked()
        {
            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection.Setup(c => c.CreateCommand()).Returns(() => new SqlCommand());

            using (IBulkedDbConnection bulk = mockDbConnection.Object.CreateBulkedDbConnection())
            {
                Assert.Throws<InvalidOperationException>(() => bulk.CreateBulkedDbConnection());
            }
        }

        [Test]
        public void Bulk_ShouldNotBeTransacted()
        {
            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection.Setup(c => c.CreateCommand()).Returns(() => new SqlCommand());

            using (IBulkedDbConnection bulk = mockDbConnection.Object.CreateBulkedDbConnection())
            {
                Assert.Throws<NotSupportedException>(() => bulk.BeginTransaction());
                Assert.Throws<NotSupportedException>(() => bulk.BeginTransaction(default));
            }
        }

        public class Table 
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string Data { get; set; }
        }

        [Test]
        public void Bulk_ShouldWorkInRealLife() 
        {
            using (IDbConnection conn = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider).CreateDbConnection()) 
            {
                conn.Open();
                conn.CreateTable<Table>();

                using (IBulkedDbConnection bulk = conn.CreateBulkedDbConnection()) 
                {
                    bulk.Insert(new Table { Data = "cica" });
                    bulk.Insert(new Table { Data = "kutya" });

                    bulk.Flush();
                }

                Assert.That(conn.Select<Table>().Count, Is.EqualTo(2));
            }
        }
    }
}
