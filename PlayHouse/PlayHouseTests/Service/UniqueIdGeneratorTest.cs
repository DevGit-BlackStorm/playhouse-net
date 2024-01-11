using ConcurrentCollections;
using FluentAssertions;
using PlayHouse.Service.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PlayHouseTests.Service
{
    public class UniqueIdGeneratorTest
    {
        private readonly UniqueIdGenerator _generator = new UniqueIdGenerator(1);

        [Fact]
        public void GeneratedIdsShouldBeUnique()
        {
            var ids = new HashSet<long>();
            for (int i = 0; i < 10000; i++)
            {
                long id = _generator.NextId();
                ids.Add(id);
            }
            ids.Count.Should().Be(10000);
        }

        [Fact]
        public void GeneratedIdsShouldBeInOrder()
        {
            var ids = new List<long>();
            for (int i = 0; i < 100; i++)
            {
                long id = _generator.NextId();
                ids.Add(id);
            }
            ids.Should().BeInAscendingOrder();
        }

        [Fact]
        public void GeneratedIdsShouldBeThreadSafe()
        {
            ThreadPool.SetMaxThreads(10, 10);
            var ids = new ConcurrentHashSet<long>();

            Parallel.For(0, 10000, i =>
            {
                ids.Add(_generator.NextId());
            });

            ids.Count.Should().Be(10000);
        }
    }
}
