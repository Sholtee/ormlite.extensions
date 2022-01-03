/********************************************************************************
* EventRepository.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions.Eventing.Tests
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

        public class EventTable2 : Event<Guid>
        {
        }

        public class MyView2 : Aggregate<Guid, MyEvent1, MyEvent2>
        {
            public string Prop1 { get; set; }

            public int? Prop2 { get; set; }

            public override void Apply(MyEvent1 evt) => Prop1 = evt.Prop1;

            public override void Apply(MyEvent2 evt) => Prop2 = evt.Prop2;
        }

        public static IEnumerable<string> UnknownEvents
        {
            get 
            {
                yield return "cica";
                yield return typeof(object).AssemblyQualifiedName;
            }
        }

        [Test]
        public void Apply_ShoudThrowOnUnknownEvent([ValueSource(nameof(UnknownEvents))] string unknownEvt) =>
            Assert.Throws<InvalidOperationException>(() => new EventRepository<string, EventTable, MyView>(new Mock<IDbConnection>(MockBehavior.Strict).Object).Apply(new MyView(), new EventTable { Type = unknownEvt, Payload = "{}"}));

        [Test]
        public void Apply_ShoudThrowOnNullEvent() =>
            Assert.Throws<InvalidOperationException>(() => new EventRepository<string, EventTable, MyView>(new Mock<IDbConnection>(MockBehavior.Strict).Object).Apply(new MyView(), new EventTable { Type = typeof(object).AssemblyQualifiedName, Payload = "null" }));

        [Test]
        public void Apply_ShouldCallTheProperApplyFunction()
        {
            Mock<MyView> mockView = new(MockBehavior.Strict);
            mockView.Setup(v => v.Apply(It.Is<MyEvent2>(evt => evt.Prop2 == 1986)));

            Assert.DoesNotThrow(() => new EventRepository<string, EventTable, MyView>(new Mock<IDbConnection>(MockBehavior.Strict).Object).Apply(mockView.Object, new EventTable
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

            IEventRepository<string, MyView> repo = new EventRepository<string, EventTable, MyView>(conn);

            IList<MyView> views = await repo.QueryViewsByStreamId(default, "stream_1");

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0].StreamId, Is.EqualTo("stream_1"));
            Assert.That(views[0].Prop1, Is.EqualTo("cica"));
            Assert.That(views[0].Prop2, Is.EqualTo(1986));
        }

        [Test]
        public async Task QueryViews_ShouldReturnAllEntities()
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

            IEventRepository<string, MyView> repo = new EventRepository<string, EventTable, MyView>(conn);

            IList<MyView> views = await repo.QueryViews();

            Assert.That(views.Count, Is.EqualTo(2));
            Assert.That(views.Any(view => view.StreamId == "stream_1" && view.Prop1 == "cica" && view.Prop2 == 1986));
            Assert.That(views.Any(view => view.StreamId == "stream_2" && view.Prop1 == "kutya" && view.Prop2 == null));
        }

        [Test]
        public async Task CreateEvent_ShouldApplyTheNewEventBeforeInsert()
        {
            using IDbConnection conn = ConnectionFactory.OpenDbConnection();
            conn.CreateTable<EventTable2>();

            IEventRepository<Guid, MyView2> repo = new EventRepository<Guid, EventTable2, MyView2>(conn);

            Guid streamId = Guid.NewGuid();

            MyView2 state = await repo.CreateEvent(streamId, new MyEvent1 { Prop1 = "cica" });
            Assert.That(state.StreamId, Is.EqualTo(streamId));
            Assert.That(state.Prop1, Is.EqualTo("cica"));
            Assert.That(state.Prop2, Is.Null);

            state = await repo.CreateEvent(streamId, new MyEvent2 { Prop2 = 1986 });
            Assert.That(state.StreamId, Is.EqualTo(streamId));
            Assert.That(state.Prop1, Is.EqualTo("cica"));
            Assert.That(state.Prop2, Is.EqualTo(1986));
        }

        [Test]
        public async Task QueryViews_ShouldWorkWithEventsCreateByCreateEvent()
        {
            using IDbConnection conn = ConnectionFactory.OpenDbConnection();
            conn.CreateTable<EventTable2>();

            IEventRepository<Guid, MyView2> repo = new EventRepository<Guid, EventTable2, MyView2>(conn);

            Guid streamId = Guid.NewGuid();

            await repo.CreateEvent(streamId, new MyEvent1 { Prop1 = "cica" });
            await repo.CreateEvent(streamId, new MyEvent2 { Prop2 = 1986 });

            IList<MyView2> views = await repo.QueryViewsByStreamId(default, streamId);

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0].StreamId, Is.EqualTo(streamId));
            Assert.That(views[0].Prop1, Is.EqualTo("cica"));
            Assert.That(views[0].Prop2, Is.EqualTo(1986));
        }
    }
}
