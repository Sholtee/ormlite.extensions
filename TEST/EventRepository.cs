/********************************************************************************
* EventRepository.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Text.Json;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.OrmLite.Extensions.EventStream.Tests
{
    [TestFixture]
    public class EventRepositoryTests
    {
        public class MyEvent1 
        {
            public string Prop1 { get; set; }
        }

        public class MyEvent2
        {
            public int Prop2 { get; set; }
        }

        public class MyView : Aggregate<string, MyEvent1, MyEvent2>
        {
            public override void Apply(MyEvent1 evt) => throw new NotImplementedException();

            public override void Apply(MyEvent2 evt) => throw new NotImplementedException();
        }

        [Test]
        public void ApplyFn_ShoudThrowOnUnknownEvent() =>
            Assert.Throws<InvalidOperationException>(() => EventRepository<string, MyView>.ApplyFn(new MyView(), new Event<string> { Type = "cica"}));

        [Test]
        public void ApplyFn_ShouldCallTheProperApplyFunction()
        {
            Mock<MyView> mockView = new(MockBehavior.Strict);
            mockView.Setup(v => v.Apply(It.Is<MyEvent2>(evt => evt.Prop2 == 1986)));

            Assert.DoesNotThrow(() => EventRepository<string, MyView>.ApplyFn(mockView.Object, new Event<string> 
            {
                Type = typeof(MyEvent2).FullName,
                Payload = JsonSerializer.Serialize(new MyEvent2 
                { 
                    Prop2 = 1986 
                })
            }));
            mockView.Verify(v => v.Apply(It.IsAny<MyEvent2>()), Times.Once);
        }
    }
}
