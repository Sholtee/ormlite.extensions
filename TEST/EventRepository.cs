/********************************************************************************
* EventRepository.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions.EventStream.Tests
{
    [TestFixture]
    public class EventRepositoryTests
    {
        private static readonly IDbConnectionFactory ConnectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);

        public class MyEvent1 
        {
            public string Prop1 { get; set; }
        }

        public class MyEvent2
        {
            public int Prop2 { get; set; }
        }

        public class EventTable : Event<string>
        {
        }

        public class MyView : Aggregate<string, MyEvent1, MyEvent2>
        {
            public string Prop1 { get; set; }

            public int? Prop2 { get; set; }

            public override void Apply(MyEvent1 evt) => Prop1 = evt.Prop1;

            public override void Apply(MyEvent2 evt) => Prop2 = evt.Prop2;
        }

        [Test]
        public void Apply_ShoudThrowOnUnknownEvent() =>
            Assert.Throws<InvalidOperationException>(() => EventRepository<string, EventTable, MyView >.Apply(new MyView(), new EventTable { Type = typeof(Object).AssemblyQualifiedName, Payload = "{}"}));

        [Test]
        public void Apply_ShouldCallTheProperApplyFunction()
        {
            Mock<MyView> mockView = new(MockBehavior.Strict);
            mockView.Setup(v => v.Apply(It.Is<MyEvent2>(evt => evt.Prop2 == 1986)));

            Assert.DoesNotThrow(() => EventRepository<string, EventTable, MyView>.Apply(mockView.Object, new EventTable
            {
                Type = typeof(MyEvent2).AssemblyQualifiedName,
                Payload = JsonSerializer.Serialize(new MyEvent2 
                { 
                    Prop2 = 1986 
                })
            }));
            mockView.Verify(v => v.Apply(It.IsAny<MyEvent2>()), Times.Once);
        }

        [Test]
        public async Task QueryViewsByStreamId_ShouldFilterById()
        {
            using IDbConnection conn = ConnectionFactory.OpenDbConnection();
            conn.CreateTable<EventTable>();

            conn.Insert(new EventTable
            {
                StreamId = "stream_1",
                CreatedAtUtc = DateTime.UtcNow.Ticks,
                Type = typeof(MyEvent1).AssemblyQualifiedName,
                Payload = JsonSerializer.Serialize(new MyEvent1 { Prop1 = "cica" })
            });

            conn.Insert(new EventTable
            {
                StreamId = "stream_1",
                CreatedAtUtc = DateTime.UtcNow.Ticks,
                Type = typeof(MyEvent2).AssemblyQualifiedName,
                Payload = JsonSerializer.Serialize(new MyEvent2 { Prop2 = 1986 })
            });

            conn.Insert(new EventTable
            {
                StreamId = "stream_2",
                CreatedAtUtc = DateTime.UtcNow.Ticks,
                Type = typeof(MyEvent1).AssemblyQualifiedName,
                Payload = JsonSerializer.Serialize(new MyEvent1 { Prop1 = "kutya" })
            });

            EventRepository<string, EventTable, MyView> repo = new(conn);

            IList<MyView> views = await repo.QueryViewsByStreamId(default, "stream_1");

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0].StreamId, Is.EqualTo("stream_1"));
            Assert.That(views[0].Prop1, Is.EqualTo("cica"));
            Assert.That(views[0].Prop2, Is.EqualTo(1986));
        }
    }
}
