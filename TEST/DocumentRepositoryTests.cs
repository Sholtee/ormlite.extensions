﻿/********************************************************************************
* DocumentRepositoryTests.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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

        public DocumentRepository<string, MyDocument, MyView> Repository { get; set; }

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
        public void TearDown() => Connection?.Dispose();

        public class MyView
        {
            public int Prop { get; set; }
        }

        public class MyDocument : Document<string, MyView>
        {
        }

        [Test]
        public async Task QueryBySimpleCondition_ShouldReturnTheProperViews()
        {
            Connection.Insert(new MyDocument { StreamId = "cica", Data = new MyView { Prop = 1986 } });
            Connection.Insert(new MyDocument { StreamId = "kutya", Data = new MyView { Prop = 2000 } });

            IList<MyView> result = await Repository.QueryBySimpleCondition<int>("$.Prop", prop => prop == 1986);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Prop, Is.EqualTo(1986));
        }
    }
}