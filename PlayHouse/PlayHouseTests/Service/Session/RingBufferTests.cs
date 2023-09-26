using CommonLib;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PlayHouseTests.Service.Session
{
    public class RingBufferTests
    {
        public RingBufferTests()
        {
            PooledBuffer.Init();
        }
        [Fact]
        public void Enqueue_WhenQueueIsFull_ShouldResizeBuffer()
        {
            var ringBuffer = new RingBuffer(2, 4);
            ringBuffer.Enqueue(0x01);
            ringBuffer.Enqueue(0x02);
            Action act = () => ringBuffer.Enqueue(0x03);

            act.Should().NotThrow();
            ringBuffer.Count.Should().Be(3);
        }

        //[Fact]
        //public void Enqueue_WhenResizingBeyondMaxCapacity_ShouldThrowInvalidOperationException()
        //{
        //    var ringBuffer = new RingBuffer(2, 2);
        //    ringBuffer.Enqueue(0x01);
        //    ringBuffer.Enqueue(0x02);

        //    Action act = () => ringBuffer.Enqueue(0x03);

        //    act.Should().Throw<InvalidOperationException>()
        //        .WithMessage("Queue has reached maximum capacity");
        //}

        [Fact]
        public void Dequeue_WhenQueueIsEmpty_ShouldThrowInvalidOperationException()
        {
            var ringBuffer = new RingBuffer(2);

            Action act = () => ringBuffer.Dequeue();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Queue is empty");
        }

        //[Fact]
        //public void PeekInt16_WhenIndexIsInvalid_ShouldThrowIndexOutOfRangeException()
        //{
        //    var ringBuffer = new RingBuffer(2);

        //    Action act = () => ringBuffer.PeekInt16(1);

        //    act.Should().Throw<IndexOutOfRangeException>();
        //}

        //[Fact]
        //public void PeekInt32_WhenIndexIsInvalid_ShouldThrowIndexOutOfRangeException()
        //{
        //    var ringBuffer = new RingBuffer(2);

        //    Action act = () => ringBuffer.PeekInt32(1);

        //    act.Should().Throw<IndexOutOfRangeException>();
        //}

        //[Fact]
        //public void SetInt16_WhenIndexIsInvalid_ShouldThrowIndexOutOfRangeException()
        //{
        //    var ringBuffer = new RingBuffer(2);

        //    Action act = () => ringBuffer.SetInt16(1, 0x0102);

        //    act.Should().Throw<IndexOutOfRangeException>();
        //}

        //[Fact]
        //public void SetByte_WhenIndexIsInvalid_ShouldThrowIndexOutOfRangeException()
        //{
        //    var ringBuffer = new RingBuffer(2);

        //    Action act = () => ringBuffer.SetByte(2, 0x01);

        //    act.Should().Throw<IndexOutOfRangeException>();
        //}

        [Fact]
        public void Clear_WhenCountIsGreaterThanSize_ShouldThrowArgumentException()
        {
            var ringBuffer = new RingBuffer(2);
            ringBuffer.Enqueue(0x01);

            Action act = () => ringBuffer.Clear(2);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WhenCapacityIsGreaterThanMaxCapacity_ShouldThrowArgumentException()
        {
            Action act = () => new RingBuffer(3, 2);

            act.Should().Throw<ArgumentException>()
                .WithMessage("capacity cannot be greater than maxCapacity");
        }

        [Fact]
        public void EnqueueDequeue_ShouldMaintainOrder()
        {
            var ringBuffer = new RingBuffer(2, 4);
            var inputData = new byte[] { 0x01, 0x02, 0x03 };
            foreach (var b in inputData)
                ringBuffer.Enqueue(b);

            var outputData = new byte[inputData.Length];
            for (int i = 0; i < inputData.Length; i++)
                outputData[i] = ringBuffer.Dequeue();

            outputData.Should().Equal(inputData);
        }

        [Fact]
        public void WriteAndReadLargeAmountOfData()
        {
            // Arrange
            var ringBuffer = new RingBuffer(256, 512); // 예시로 256바이트의 초기 크기를 가진 RingBuffer를 생성
            var largeAmountOfData = new byte[512]; // RingBuffer의 초기 크기를 초과하는 데이터

            var random = new Random();
            random.NextBytes(largeAmountOfData); // 데이터를 랜덤으로 채움

            // Act
            // 대량의 데이터를 Write
            ringBuffer.Write(largeAmountOfData, 0, largeAmountOfData.Length);

            // Assert
            ringBuffer.Count.Should().Be(largeAmountOfData.Length, because: "all data should be written to the buffer");

            // Act & Assert
            // 데이터를 하나씩 Read하며 원본 데이터와 비교
            for (int i = 0; i < largeAmountOfData.Length; i++)
            {
                var byteRead = ringBuffer.ReadByte();
                byteRead.Should().Be(largeAmountOfData[i], $"because the byte at position {i} should match the original data");
            }

            ringBuffer.Count.Should().Be(0, because: "all data should have been read");
        }

        [Fact]
        public void Should_Handle_Massive_Amount_Of_Data_Correctly()
        {
            var ringBuffer = new RingBuffer(1024, int.MaxValue);
            var rnd = new Random();
            var iterations = 10000; // 반복 횟수를 변경할 수 있습니다.
            var writtenData = new byte[iterations * sizeof(int)];

            // 대량의 데이터 작성
            for (int i = 0; i < iterations; i++)
            {
                var valueToWrite = rnd.Next(int.MinValue, int.MaxValue); // 임의의 int 값
                var bytes = BitConverter.GetBytes(valueToWrite);
                bytes.CopyTo(writtenData, i * sizeof(int));
                ringBuffer.WriteInt32(valueToWrite);
            }

            // Peek 테스트
            for (int i = 0; i < iterations; i++)
            {
                var expectedValue = BitConverter.ToInt32(writtenData, i * sizeof(int));
                var peekedValue = ringBuffer.PeekInt32(ringBuffer.ReaderIndex + i * sizeof(int));
                peekedValue.Should().Be(expectedValue, because: $"PeekInt32 should return the correct value at iteration {i} without modifying the buffer state.");
            }
            ringBuffer.Count.Should().Be(iterations * sizeof(int), because: "Peek should not change the buffer state.");

            // Read 테스트
            for (int i = 0; i < iterations; i++)
            {
                var expectedValue = BitConverter.ToInt32(writtenData, i * sizeof(int));
                var readValue = ringBuffer.ReadInt32();
                readValue.Should().Be(expectedValue, because: $"ReadInt32 should return the correct value at iteration {i}.");
            }
            ringBuffer.Count.Should().Be(0, because: "All bytes should have been read from the buffer.");
        }

        [Fact]
        public void Should_Throw_IndexOutOfRangeException_When_PeekInt16_After_Specific_Sequence_Of_Operations()
        {
            // Arrange
            var ringBuffer = new RingBuffer(128, int.MaxValue);
            var rnd = new Random();
            var iterations = 10000;
            //Action act = () =>
            int length = 32*3; // 원하는 배열의 길이를 설정합니다. 예시에서는 10으로 설정했습니다.
            byte[]  bytes = new byte[length];

            //{
            for (int i = 0; i < iterations; i++)
                {
                // byte[]를 Write
                //                    var valueToWrite = rnd.Next(int.MinValue, int.MaxValue); // 임의의 int 값
                //                  var bytes = BitConverter.GetBytes(valueToWrite);

                    rnd.NextBytes(bytes);
                    ringBuffer.Write(bytes, 0, bytes.Length);

                    // PeekInt16 호출
                    ringBuffer.PeekInt16(ringBuffer.ReaderIndex);

                    //// Clear(2) 호출
                    ringBuffer.Clear(2);

                    // 다양한 Read 함수 호출
                    ringBuffer.ReadInt16();
                    ringBuffer.ReadInt32();
                    ringBuffer.ReadInt16();
                    ringBuffer.ReadInt16();

                    // byte[]를 Read
                    var readBuffer = new byte[8]; // 예시로 8바이트 읽기
                    ringBuffer.Read(readBuffer, 0, readBuffer.Length);
                }
            //};

            //// Assert
            //act.Should()Throw<IndexOutOfRangeException>().WithMessage("Index was outside the bounds of the buffer.");
        }
    }
}
