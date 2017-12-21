using System.Collections.Generic;
using System.Linq;

using C6.Tests.Contracts;
using C6.Tests.Helpers;

using NUnit.Framework;
using NUnit.Framework.Internal;

using static C6.Contracts.ContractMessage;
using static C6.Tests.Helpers.TestHelper;
using static C6.Tests.Helpers.CollectionEvent;
using static C6.Collections.ExceptionMessages;



namespace C6.Tests
{
    [TestFixture]
    public abstract class IQueueTests : IListenableTests
    {
        #region Factories

        protected abstract bool IsReadOnly { get; }

        protected abstract IQueue<T> GetEmptyQueue<T>(bool allowsNull = false);

        protected abstract IQueue<T> GetQueue<T>(IEnumerable<T> enumerable, bool allowsNull = false);

        #region Helpers

        private IQueue<string> GetStringQueue(Randomizer random, bool allowsNull = false)
            => GetQueue(GetStrings(random, GetCount(random)), allowsNull);

        #endregion

        #region Inherited

        protected override IListenable<T> GetEmptyListenable<T>(bool allowsNull = false)
            => GetEmptyQueue<T>(allowsNull);

        protected override IListenable<T> GetListenable<T>(IEnumerable<T> enumerable, bool allowsNull = false)
            => GetQueue(enumerable, allowsNull);

        #endregion

        #endregion

        #region TestMethods

        #region Properties        

        #region this[int]

        #endregion

        #endregion

        #region Methods

        #region Enqueue(T)

        [Test]
        public void Enqueue_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringQueue(Random, allowsNull: false);

            // Act & Assert
            Assert.That(() => collection.Enqueue(null), Violates.PreconditionSaying(ItemMustBeNonNull));
        }

        [Test]
        public void Enqueue_AllowsNull_Null()
        {
            Run.If(AllowsNull);

            // Arrange
            var collection = GetStringQueue(Random, allowsNull: true);
            var array = collection.Append(null).ToArray();

            // Act 
            collection.Enqueue(null);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }        

        [Test]
        public void Enqueue_EmptyCollection_SingleItemCollection()
        {
            // Arrange
            var collection = GetEmptyQueue<string>();
            var item = Random.GetString();
            var array = new[] { item };

            // Act
            collection.Enqueue(item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Enqueue_RandomCollection_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringQueue(Random);
            var item = Random.GetString();
            var expectedEvents = new[] {
                Inserted(item, collection.Count, collection),
                Added(item, 1, collection),
                Changed(collection)
            };

            // Act & Assert
            Assert.That(() => collection.Enqueue(item), Raises(expectedEvents).For(collection));
        }

        [Test]
        [Ignore("AllowsDuplicates not inherited")]
        public void Enqueue_RandomCollectionInsertExistingLast_InsertedLast()
        {
            // Arrange
            var collection = GetStringQueue(Random);
            var item = collection.ToArray().Choose(Random);
            var array = collection.Append(item).ToArray();

            // Act
            collection.Enqueue(item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Enqueue_RandomCollection_InsertedLast()
        {
            // Arrange
            var collection = GetStringQueue(Random);
            var item = Random.GetString();
            var array = collection.Append(item).ToArray();

            // Act
            collection.Enqueue(item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Enqueue_EmptyQueueManyItems_Equal()
        {
            // Arrange
            var collection = GetEmptyQueue<string>(); // ????
            var items = GetStrings(Random, Random.Next(100, 250));

            // Act
            foreach (var item in items)
            {
                collection.Enqueue(item);
            }

            // Assert
            Assert.That(collection, Is.EqualTo(items).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Enqueue_EnqueueItemDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringQueue(Random);
            var item = Random.GetString();

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Enqueue(item);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        #endregion


        #region Dequeue()

        [Test]
        public void Dequeue_EmptyCollection_ViolatesPrecondtion()
        {
            // Arrange
            var collection = GetEmptyQueue<string>();

            // Act & Assert
            Assert.That(() => collection.Dequeue(), Violates.PreconditionSaying(CollectionMustBeNonEmpty));
        }

        [Test]
        public void Dequeue_DequeueDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringQueue(Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Dequeue();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void Dequeue_RandomCollection_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringQueue(Random);
            var item = collection.First();
            var expectedEvents = new[] {
                RemovedAt(item, 0, collection), // using static C6.Tests.Helpers.CollectionEvent;
                Removed(item, 1, collection),
                Changed(collection)
            };

            // Act & Assert
            Assert.That(() => collection.Dequeue(), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void Dequeue_RandomCollectionWithNullRemoveNull_Null()
        {
            Run.If(AllowsNull);

            // Arrange
            var items = GetStrings(Random).InsertItem(0, null);
            var collection = GetQueue(items, allowsNull: true);

            // Act
            var removeFirst = collection.Dequeue();

            // Assert
            Assert.That(removeFirst, Is.Null);
        }

        [Test]
        public void Dequeue_SingleItemCollection_Empty()
        {
            // Arrange
            var item = Random.GetString();
            var itemArray = new[] { item };
            var collection = GetQueue(itemArray);

            // Act
            var removeLast = collection.Dequeue();

            // Assert
            Assert.That(removeLast, Is.SameAs(item));
            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void Dequeue_DequeueItem_Removed()
        {
            // Arrange
            var collection = GetStringQueue(Random);
            var firstItem = collection.First();
            var array = collection.Skip(1).ToArray();

            // Act
            var removeFirst = collection.Dequeue();

            // Assert
            Assert.That(removeFirst, Is.SameAs(firstItem));
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Dequeue_RandomCollectionPopUntilEmpty_Empty()
        {
            // Arrange
            var collection = GetStringQueue(Random);
            var count = collection.Count;

            // Act
            for (var i = 0; i < count; i++)
            {
                collection.Dequeue();
            }

            // Assert
            Assert.That(collection, Is.Empty);
        }

        #endregion

        #endregion

        #endregion
    }
}
