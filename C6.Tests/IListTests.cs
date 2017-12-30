// This file is part of the C6 Generic Collection Library for C# and CLI
// See https://github.com/C6/C6/blob/master/LICENSE.md for licensing details.

using System;
using System.Linq;

using C6.Collections;
using C6.Contracts;
using C6.Tests.Contracts;
using C6.Tests.Helpers;

using NUnit.Framework;
using NUnit.Framework.Internal;

using static C6.Contracts.ContractMessage;
using static C6.Collections.ExceptionMessages;
using static C6.Tests.Helpers.CollectionEvent;
using static C6.Tests.Helpers.TestHelper;

using SC = System.Collections;
using SCG = System.Collections.Generic;


namespace C6.Tests
{
    [TestFixture]
    public abstract class GeneralViewTests : TestBase
    {
        #region Factories

        protected abstract IList<T> GetEmptyList<T>(SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false);

        protected abstract IList<T> GetList<T>(SCG.IEnumerable<T> enumerable, SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false);

        // in the middle       
        private static IList<string>[] GetNItemNonOverlappingViewsInTheMiddle(IList<string> coll)
        {
            var nItemViews = new IList<string>[5];
            const int count = 3;
            var index = coll.Count / 2 - 1; // from the middel

            nItemViews[0] = coll.View(index, count); // View(int, int)

            nItemViews[1] = coll.View(index, count);
            nItemViews[1].Slide(-3); // Slide(int)

            nItemViews[2] = coll.View(index, count);
            nItemViews[2].Slide(3, count); // Slide(int, int)

            nItemViews[3] = coll.View(index, count);
            nItemViews[3].TrySlide(-6); // not overlapping with nItemViews[1]

            nItemViews[4] = coll.View(index, count);
            nItemViews[4].TrySlide(6, count); // not overlapping with nItemViews[2]            

            return nItemViews;
        }

        private static IList<string>[] GetOneItemViewsInTheMiddle(IList<string> coll)
        {
            var oneItemViews = new IList<string>[7];
            const int count = 1;
            var index = coll.Count / 2;

            oneItemViews[0] = coll.View(index, count); // View(int, int)
            oneItemViews[1] = coll.View(index, count);
            oneItemViews[1].Slide(-3); // Slide(int)
            oneItemViews[2] = coll.View(index, count);
            oneItemViews[2].Slide(1, count); // Slide(int, int)
            oneItemViews[3] = coll.View(index, count);
            oneItemViews[3].TrySlide(-2); // TrySlide(int)
            oneItemViews[4] = coll.View(index, count);
            oneItemViews[4].TrySlide(1, count); // TrySlide(int, int)
            oneItemViews[5] = coll.ViewOf(coll[index]); // ViewOf
            oneItemViews[6] = coll.LastViewOf(coll[index]); // LastViewOf

            return oneItemViews;
        }

        private static IList<string>[] GetZeroItemViewsInTheMiddle(IList<string> coll)
        {
            var zeroItemViews = new IList<string>[5];
            const int count = 0;
            var index = coll.Count / 2;

            zeroItemViews[0] = coll.View(index, count); // View(int, int)
            zeroItemViews[1] = coll.View(index, count);
            zeroItemViews[1].Slide(-3); // Slide(int)
            zeroItemViews[2] = coll.View(index, count);
            zeroItemViews[2].Slide(1, count); // Slide(int, int)
            zeroItemViews[3] = coll.View(index, count);
            zeroItemViews[3].TrySlide(-2); // TrySlide(int)
            zeroItemViews[4] = coll.View(index, count);
            zeroItemViews[4].TrySlide(1, count); // TrySlide(int, int)            

            return zeroItemViews;
        }

        // at the beginning        
        private static IList<string>[] GetNItemContainedInEachOtherViewsAtTheBeginning(IList<string> coll)
        {
            var nItemViews = new IList<string>[5];
            const int index = 0;
            const int count = 7; // enough long, min. 6

            nItemViews[0] = coll.View(index, count); // View(int, int)
            nItemViews[1] = coll.View(index, count);
            nItemViews[1].Slide(0); // Slide(int)
            nItemViews[2] = coll.View(index, count);
            nItemViews[2].Slide(0, count); // Slide(int, int)
            nItemViews[3] = coll.View(index, count);
            nItemViews[3].TrySlide(0); // TrySlide(int)
            nItemViews[4] = coll.View(index, count);
            nItemViews[4].TrySlide(0, count); // TrySlide(int, int)            

            return nItemViews;
        }

        private static IList<string>[] GetOneItemContainedInEachOtherViewsAtTheBeginning(IList<string> coll)
        {
            var oneItemViews = new IList<string>[7];
            const int index = 0;
            const int count = 1;

            oneItemViews[0] = coll.View(index, count); // View(int, int)
            oneItemViews[1] = coll.View(index, count);
            oneItemViews[1].Slide(0); // Slide(int)
            oneItemViews[2] = coll.View(index, count);
            oneItemViews[2].Slide(0, count); // Slide(int, int)
            oneItemViews[3] = coll.View(index, count);
            oneItemViews[3].TrySlide(0); // TrySlide(int)
            oneItemViews[4] = coll.View(index, count);
            oneItemViews[4].TrySlide(0, count); // TrySlide(int, int)
            oneItemViews[5] = coll.ViewOf(coll[index]); // ViewOf
            oneItemViews[6] = coll.LastViewOf(coll[index]); // LastViewOf

            return oneItemViews;
        }

        private static IList<string>[] GetZeroItemViewsAtTheBeginning(IList<string> coll)
        {
            var zeroItemViews = new IList<string>[5];
            const int index = 0;
            const int count = 0;

            zeroItemViews[0] = coll.View(index, count); // View(int, int)
            zeroItemViews[1] = coll.View(index, count);
            zeroItemViews[1].Slide(0); // Slide(int)
            zeroItemViews[2] = coll.View(index, count);
            zeroItemViews[2].Slide(0, count); // Slide(int, int)
            zeroItemViews[3] = coll.View(index, count);
            zeroItemViews[3].TrySlide(0); // TrySlide(int)
            zeroItemViews[4] = coll.View(index, count);
            zeroItemViews[4].TrySlide(0, count); // TrySlide(int, int)

            return zeroItemViews;
        }

        // at the end        
        private static IList<string>[] GetNItemContainedInEachOtherViewsAtTheEnd(IList<string> coll)
        {
            var nItemViews = new IList<string>[5];
            var index = coll.Count - 10; // need enough for RemoveAt
            const int count = 10;

            nItemViews[0] = coll.View(index, count); // View(int, int)
            nItemViews[1] = coll.View(index, count);
            nItemViews[1].Slide(0); // Slide(int)
            nItemViews[2] = coll.View(index, count);
            nItemViews[2].Slide(0, count); // Slide(int, int)
            nItemViews[3] = coll.View(index, count);
            nItemViews[3].TrySlide(0); // TrySlide(int)
            nItemViews[4] = coll.View(index, count);
            nItemViews[4].TrySlide(0, count); // TrySlide(int, int)            

            return nItemViews;
        }

        private static IList<string>[] GetOneItemContainedInEachOtherViewsAtTheEnd(IList<string> coll)
        {
            var oneItemViews = new IList<string>[7];
            var index = coll.Count - 1;
            const int count = 1;

            oneItemViews[0] = coll.View(index, count); // View(int, int)
            oneItemViews[1] = coll.View(index, count);
            oneItemViews[1].Slide(0); // Slide(int)
            oneItemViews[2] = coll.View(index, count);
            oneItemViews[2].Slide(0, count); // Slide(int, int)
            oneItemViews[3] = coll.View(index, count);
            oneItemViews[3].TrySlide(0); // TrySlide(int)
            oneItemViews[4] = coll.View(index, count);
            oneItemViews[4].TrySlide(0, count); // TrySlide(int, int)
            oneItemViews[5] = coll.ViewOf(coll[index]); // ViewOf
            oneItemViews[6] = coll.LastViewOf(coll[index]); // LastViewOf

            return oneItemViews;
        }

        private static IList<string>[] GetZeroItemViewsAtTheEnd(IList<string> coll)
        {
            var zeroItemViews = new IList<string>[5];
            const int count = 0;
            var index = coll.Count;

            zeroItemViews[0] = coll.View(index, count); // View(int, int)
            zeroItemViews[1] = coll.View(index, count);
            zeroItemViews[1].Slide(0); // Slide(int)
            zeroItemViews[2] = coll.View(index, count);
            zeroItemViews[2].Slide(0, count); // Slide(int, int)
            zeroItemViews[3] = coll.View(index, count);
            zeroItemViews[3].TrySlide(0); // TrySlide(int)
            zeroItemViews[4] = coll.View(index, count);
            zeroItemViews[4].TrySlide(0, count); // TrySlide(int, int)            

            return zeroItemViews;
        }

        #endregion

        private IList<string> list, auxViewRight, auxViewLeft;

        [SetUp]
        public void SetUp()
        {
            // Arrange
            // !!! Replaced it with GetStringArray
            var array = new[] {
                "aa", "cc", "bb", "dd", "ee", "ff", "gg", "hh", "ii", "jj", "kk", "ll",
                "mm", "nn", "oo", "pp", "qq", "rr", "ss", "tt", "uu", "vv", "ww", "xx",
                "yy", "zz", "bbb", "aaa", "ccc", "ddd", "eee", "fff", "ggg", "hhh", "iii", "jjj",
                "kkk", "lll", "mmm", "nnn", "ooo", "ppp", "qqq", "rrr", "sss", "ttt", "uuu", "vvv",
                "www", "xxx", "zzz", "yyy"
            };

            // Arrange the collection 
            list = GetList(array);

            // Arrange auxilary views 
            auxViewLeft = list.View(0, 2);
            auxViewRight = list.View(list.Count - 15, 15); // > # length of created views
        }

        [TearDown]
        public void TearDown()
        {
            list = null;
            auxViewLeft = null;
            auxViewRight = null;
        }

        #region this[int]

        [Test]
        public void Viewthis_NItemNonOverlappingViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = GetUppercaseString(Random);

                // Act
                view[i] = item;

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveAt_OneItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                //if (view.IsEmpty) continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = GetUppercaseString(Random);

                // Act
                view[i] = item;

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void Viewthis_ZeroItemViewsInTheMiddle_ViolatesPrecondition()
        {
            // Arrange
            var item = GetUppercaseString(Random);
            // Act & Assert
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                Assert.That(() => view[0] = item, Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
            }
        }

        [Test]
        public void Viewthis_NItemContainedViewsTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = GetUppercaseString(Random);

                // Act
                view[i] = item;

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void Viewthis_OneItemContainedViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = GetUppercaseString(Random);

                // Act
                view[i] = item;

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void Viewthis_ZeroItemViewsAtTheBeginning_ViolatesPrecondition()
        {
            // Assert
            var item = GetUppercaseString(Random);

            // Act & Assert
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                Assert.That(() => view[0] = item, Violates.Precondition);
            }
        }

        [Test]
        public void Viewthis_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = GetUppercaseString(Random);

                // Act
                view[i] = item;

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void Viewthis_OneItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = GetUppercaseString(Random);

                // Act
                view[i] = item;

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }

        }

        [Test]
        public void Viewthis_ZeroItemViewsAtTheEnd_ViolatesPrecondition()
        {
            // Arrange
            var item = GetUppercaseString(Random);

            // Act & Assert
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                Assert.That(() => view[0] = item, Violates.Precondition);
            }
        }

        #endregion

        #region Add(T)

        [Test]
        public void ViewAdd_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Add(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewAdd_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                //if (view.IsEmpty) continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Add(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewAdd_ZeroItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var views = GetZeroItemViewsInTheMiddle(list);
            var index = 0;
            foreach (var view in views) {
                //if (view.IsEmpty) continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Add(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewAdd_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Add(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewAdd_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Add(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewAdd_ZeroItemViewsAtTheBeginning_BothViewsOffsetAffected()
        {
            // Arrange 
            var views = GetZeroItemViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Add(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft + 1), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewAdd_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Add(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewAdd_OneItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Add(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewAdd_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange             
            var views = GetZeroItemViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Add(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region RemoveAt(int)

        [Test]
        public void ViewRemoveAt_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveAt(0);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveAt_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                if (view.IsEmpty)
                    continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveAt(0);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveAt_ZeroItemViewsInTheMiddle_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                //Assert.That(() => view.RemoveAt(0), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
                Assert.That(() => view.RemoveAt(0), Violates.Precondition);
            }
        }

        [Test] // right aux view has 2 item -> after 2 removeAt(0) right aux view's offset would not chnage ???
        public void ViewRemoveAt_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveAt(0);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveAt_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var index = Random.Next(0, views.Length);

            // Act
            // do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            view.RemoveAt(0);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveAt_ZeroItemViewsAtTheBeginning_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveAt(0), Violates.Precondition);
            }
        }

        [Test]
        public void ViewRemoveAt_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveAt(view.Count - 1);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveAt_OneItemContainedViewsAtTheEnd_AuxViewsOffsetNotAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = Random.Next(0, views.Length);

            // Act // Do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            view.RemoveAt(view.Count - 1);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            foreach (var v in views) {
                Assert.That(v.Count, Is.Zero, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveAt_ZeroItemViewsAtTheEnd_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveAt(0), Violates.Precondition);
            }
        }

        #endregion

        #region Insert(int, T)

        [Test]
        public void ViewInsertWithIndexZero_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Insert(0, item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertWithIndexZero_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                //if (view.IsEmpty) continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Insert(0, item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertWithIndexZero_ZeroItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var views = GetZeroItemViewsInTheMiddle(list);
            var index = 0;
            foreach (var view in views) {
                //if (view.IsEmpty) continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Insert(0, item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertWithIndexZero_NItemContainedViewsAtTheBeginning_BothViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Insert(0, item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft + 1), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertWithIndexZero_OneItemContainedViewsAtTheBeginning_BothViewsOffsetAffected()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Insert(0, item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft + 1), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertWithIndexZero_ZeroItemViewsAtTheBeginning_BothViewsOffsetAffected()
        {
            // Arrange 
            var views = GetZeroItemViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Insert(0, item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft + 1), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertWithIndexCount_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Insert(view.Count, item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertWithIndexCount_OneItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Insert(view.Count, item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertWihtIndexCount_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange             
            var views = GetZeroItemViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.Insert(view.Count, item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region InsertFirst()

        [Test]
        public void ViewInsertFirst_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertFirst(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertFirst_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                //if (view.IsEmpty) continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertFirst(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertFirst_ZeroItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var views = GetZeroItemViewsInTheMiddle(list);
            var index = 0;
            foreach (var view in views) {
                //if (view.IsEmpty) continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertFirst(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertFirst_NItemContainedViewsAtTheBeginning_BothViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertFirst(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft + 1), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertFirst_OneItemContainedViewsAtTheBeginning_BothViewsOffsetAffected()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertFirst(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft + 1), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertFirst_ZeroItemViewsAtTheBeginning_BothViewsOffsetAffected()
        {
            // Arrange 
            var views = GetZeroItemViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertFirst(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft + 1), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertFirst_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertFirst(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertFirst_OneItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertFirst(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertFirst_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange             
            var views = GetZeroItemViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertFirst(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region InsertLast()      

        [Test]
        public void ViewInsertLast_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertLast(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertLast_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                //if (view.IsEmpty) continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertLast(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertLast_ZeroItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var views = GetZeroItemViewsInTheMiddle(list);
            var index = 0;
            foreach (var view in views) {
                //if (view.IsEmpty) continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertLast(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertLast_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertLast(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertLast_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertLast(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertLast_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetZeroItemViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertLast(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft + 1), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight + 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertLast_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertLast(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertLast_OneItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertLast(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewInsertLast_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange             
            var views = GetZeroItemViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = GetUppercaseString(Random);

                // Act
                view.InsertFirst(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region RemoveFirst()

        [Test]
        public void ViewRemoveFirst_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveFirst();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveFirst_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                if (view.IsEmpty)
                    continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveFirst();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveFirst_ZeroItemViewsInTheMiddle_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveFirst(), Violates.PreconditionSaying(CollectionMustBeNonEmpty));
            }
        }

        [Test] // right aux view has 2 item -> after 2 removeAt(0) right aux view's offset would not chnage ???
        public void ViewRemoveFirst_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveFirst();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveFirst_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var index = 0;

            // Act
            // do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            view.RemoveFirst();

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveFirst_ZeroItemViewsAtTheBeginning_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveFirst(), Violates.Precondition);
            }
        }

        [Test]
        public void ViewRemoveFirst_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveFirst();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveFirst_OneItemContainedViewsAtTheEnd_AuxViewsOffsetNotAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = Random.Next(0, views.Length);

            // Act // Do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            view.RemoveFirst();

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            foreach (var v in views) {
                Assert.That(v.Count, Is.Zero, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveFirst_ZeroItemViewsAtTheEnd_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveFirst(), Violates.Precondition);
            }
        }

        #endregion

        #region RemoveLast()

        [Test]
        public void ViewRemoveLast_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveLast();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveLast_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                if (view.IsEmpty)
                    continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveLast();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveLast_ZeroItemViewsInTheMiddle_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveLast(), Violates.PreconditionSaying(CollectionMustBeNonEmpty));
            }
        }

        [Test] 
        public void ViewRemoveLast_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveLast();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveLast_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var index = 0;

            // Act
            // do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            view.RemoveLast();

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveLast_ZeroItemViewsAtTheBeginning_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveLast(), Violates.Precondition);
            }
        }

        [Test]
        public void ViewRemoveLast_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveLast();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveLast_OneItemViewsContainedAtTheEnd_AuxViewsOffsetNotAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = Random.Next(0, views.Length);

            // Act // Do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            view.RemoveLast();

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            foreach (var v in views) {
                Assert.That(v.Count, Is.Zero, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveLast_ZeroItemViewsAtTheEnd_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveLast(), Violates.Precondition);
            }
        }

        #endregion

        #region Reverse()

        [Test]
        public void ViewReverse_NItemNonOverlappingViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Reverse();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewReverse_OneItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                //if (view.IsEmpty) continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Reverse();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewReverse_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            // Act & Assert
            var views = GetZeroItemViewsInTheMiddle(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Reverse();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewReverse_NItemViewsAtTheBeginningLeftAuxContainedInView_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Reverse();

                // Assert     
                var newOffsetLeft = 2 * view.Offset + view.Count - offsetLeft - auxViewLeft.Count; // auxViewLeft contained in view
                Assert.That(auxViewLeft.Offset, Is.EqualTo(newOffsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewReverse_OneItemContainedViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Reverse();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewReverse_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Reverse();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewReverse_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Reverse();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewReverse_OneItemViewsContainedAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Reverse();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }

        }

        [Test]
        public void ViewReverse_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Reverse();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region Shuffle()

        [Test]
        public void ViewShuffle_NItemNonOverlappingViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Shuffle();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewShuffle_OneItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Shuffle();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewShuffle_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            // Act & Assert
            var views = GetZeroItemViewsInTheMiddle(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Shuffle();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewShuffle_NItemContainedViewsAtTheBeginning_LeftAuxViewInvalidatedRightAuxViewUnchnaged()
        {
            // Arrange 
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            var view = views.Choose(Random);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;

            // Act
            view.Shuffle();

            Assert.That(auxViewLeft.IsValid, Is.False, $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
        }

        [Test]
        public void ViewShuffle_OneItemContainedViewsAtTheBeginningViewsContainedInLeftAuxView_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var index = 0;
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Shuffle();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewShuffle_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var index = 0;
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Shuffle();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewShuffle_NItemViewsAtTheEndViewsContainedInRightAuxView_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Shuffle();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewShuffle_OneItemViewsAtTheEndViewsContainedInRightAuxView_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Shuffle();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }

        }

        [Test]
        public void ViewShuffle_ZeroItemViewsAtTheEndViewsContainedInRightAuxView_BothViewsOffsetNotChanged()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Shuffle();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region Sort()

        [Test]
        // TODO: For LinkedList<T> and HashedLinkedList<T> might fail until Sort() is fixed!
        public void ViewSort_NItemNonOverlappingViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Sort();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewSort_OneItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Sort();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewSort_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            // Act & Assert
            var views = GetZeroItemViewsInTheMiddle(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Sort();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewSort1_NItemContainedViewsAtTheBeginning_LeftAuxViewInvalidatedRightAuxViewUnchnaged()
        {
            // Arrange 
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            var view = views.Choose(Random);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;

            // Act
            view.Sort();

            // Assert                
            Assert.That(auxViewLeft.IsValid, Is.False, $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
        }

        [Test]
        public void ViewSort_OneItemContainedViewsAtTheBeginningViewsContainedInLeftAuxView_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var index = 0;
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Sort();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewSort_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var index = 0;
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Sort();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewSort_NItemViewsAtTheEndViewsContainedInRightAuxView_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Sort();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewSort_OneItemViewsAtTheEndViewsContainedInRightAuxView_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Sort();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }

        }

        [Test]
        public void ViewSort_ZeroItemViewsAtTheEndViewsContainedInRightAuxView_BothViewsOffsetNotChanged()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Sort();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        // TODO: For LinkedList<T> and HashedLinkedList<T> might fail until Sort() is fixed!
        public void ViewSort_NonOverlappingNeighbourViews_Corect()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items);

            var view1  = collection.View(0, Random.Next(1, collection.Count - 1));
            var view1RightEndPoint = view1.Offset + view1.Count;
            var view2 = collection.View(view1RightEndPoint, Random.Next(1, collection.Count - view1RightEndPoint)); // neighbor of view1 - nonoverlapping
            var view2Old = GetList(view2);

            // Act
            view1.Sort();

            // Assert
            Assert.That(view2, Is.EqualTo(view2Old).Using(ReferenceEqualityComparer));
        }
        

        #endregion

        #region RemoveIndexRange(int, int)

        [Test]
        public void ViewRemoveIndexRange_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveIndexRange(0, 1);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveIndexRange_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                if (view.IsEmpty)
                    continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveIndexRange(0, 1);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveIndexRange_ZeroItemViewsInTheMiddle_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveIndexRange(0, 1), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
            }
        }

        [Test] // right aux view has 2 item -> after 2 removeAt(0) right aux view's offset would not chnage ???
        public void ViewRemoveIndexRange_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveIndexRange(0, 1);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveIndexRange_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var index = 0;

            // Act
            // do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            view.RemoveIndexRange(0, 1);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveIndexRange_ZeroItemViewsAtTheBeginning_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveIndexRange(0, 2), Violates.Precondition);
            }
        }

        [Test]
        public void ViewRemoveIndexRange_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RemoveIndexRange(0, 1);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveIndexRange_OneItemContainedViewsAtTheEnd_AuxViewsOffsetNotAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = Random.Next(0, views.Length);

            // Act // Do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            view.RemoveIndexRange(0, 1);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            foreach (var v in views) {
                Assert.That(v.Count, Is.Zero, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveIndexRange_ZeroItemViewsAtTheEnd_ViolatesPrecondition()
        {
            // Act & Assert
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                Assert.That(() => view.RemoveIndexRange(0, 2), Violates.Precondition);
            }
        }


        #endregion

        #region Update(T, out T)

        [Test]
        public void ViewUpdateOut_NItemNonOverlappingViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                string outItem;

                // Act
                view.Update(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdateOut_OneItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                string outItem;

                // Act
                view.Update(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdateOut_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var views = GetZeroItemViewsInTheMiddle(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                string outItem;

                // Act
                view.Update(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdateOut_NItemContainedViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = list.Choose();
                string outItem;

                // Act
                view.Update(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdateOut_OneItemContaiedViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = list.Choose();
                string outItem;

                // Act
                view.Update(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdateOut_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                string outItem;

                // Act
                view.Update(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdateOut_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = list.Choose();
                string outItem;

                // Act
                view.Update(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdateOut_OneItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = list.Choose();
                string outItem;

                // Act
                view.Update(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }

        }

        [Test]
        public void ViewUpdateOut_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                string outItem;

                // Act
                view.Update(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region Update(T)

        [Test]
        public void ViewUpdate_NItemNonOverlappingViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.Update(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdate_OneItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.Update(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdate_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var views = GetZeroItemViewsInTheMiddle(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.Update(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdate_NItemContainedViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = list.Choose();

                // Act
                view.Update(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdate_OneItemContainedViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = list.Choose();

                // Act
                view.Update(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdate_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.Update(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdate_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = list.Choose();

                // Act
                view.Update(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewUpdate_OneItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = 0;
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var i = Random.Next(0, view.Count);
                var item = list.Choose();

                // Act
                view.Update(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }

        }

        [Test]
        public void ViewUpdate_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.Update(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region Clear()

        [Test]
        public void ViewClear_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var viewCount = view.Count;

                // Act
                view.Clear();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - viewCount), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewClear_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                if (view.IsEmpty)
                    continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Clear();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewClear_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Clear();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test] // right aux view has 2 item -> after 2 removeAt(0) right aux view's offset would not chnage ???
        public void ViewClear_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            // Arrange 
            var index = 0;
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);

            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var view = views[index];
            var viewCount = view.Count;

            // Act
            // do it on one of the views; They all are one-item overlapping views            
            view.Clear();

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - viewCount), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewClear_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var index = 0;

            // Act
            // do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            view.Clear();

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewClear_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Clear();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewClear_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            // Arrange
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;

            // Act
            // do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            view.Clear();

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewClear_OneItemContainedViewsAtTheEnd_AuxViewsOffsetNotAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var index = 0;
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var i = Random.Next(0, views.Length);

            // Act // Do it on one of the views; They all are one-item overlapping views            
            var view = views[i];
            view.Clear();

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            foreach (var v in views) {
                Assert.That(v.Count, Is.Zero, $"view {index}");
            }
        }

        [Test]
        public void ViewClear_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.Clear();

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region Remove(T)

        [Test]
        public void ViewRemove_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();

                // Act
                view.Remove(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemove_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                if (view.IsEmpty)
                    continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();

                // Act
                view.Remove(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemove_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.Remove(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemove_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            // Arrange
            var index = 0;
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;

            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            var i = Random.Next(0, views.Length);
            var view = views[i];
            var item = view.Choose();

            // Act // Do it on one of the views; 
            view.Remove(item);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
        }

        [Test]
        public void ViewRemove_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var index = Random.Next(0, views.Length);

            // Act
            // do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            var item = view.Choose();
            view.Remove(item);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewRemove_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.Remove(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemove_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();

                // Act
                view.Remove(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemove_OneItemContainedViewsAtTheEnd_AuxViewsOffsetNotAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = Random.Next(0, views.Length);

            // Act // Do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            var item = view.Choose();
            view.Remove(item);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            foreach (var v in views) {
                Assert.That(v.Count, Is.Zero, $"view {index}");
            }
        }

        [Test]
        public void ViewRemove_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.Remove(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region Remove(T, out T)

        [Test]
        public void ViewRemoveOut_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();
                string outItem;

                // Act
                view.Remove(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveOut_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            // Arrange
            var index = 0;
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var views = GetOneItemViewsInTheMiddle(list);
            var i = Random.Next(0, views.Length);
            var view = views[i];
            var item = view.Choose();
            string outItem;

            // Act // Do it on one of the views; 
            view.Remove(item, out outItem);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");

        }

        [Test]
        public void ViewRemoveOut_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                string outItem;

                // Act
                view.Remove(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveOut_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            // Arrange
            var index = 0;
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;

            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            var i = Random.Next(0, views.Length);
            var view = views[i];
            var item = view.Choose();
            string outItem;

            // Act // Do it on one of the views; 
            view.Remove(item, out outItem);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
        }

        [Test]
        public void ViewRemoveOut_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var index = Random.Next(0, views.Length);
            string outItem;

            // Act
            // do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            var item = view.Choose();
            view.Remove(item, out outItem);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveOut_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                string outItem;

                // Act
                view.Remove(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveOut_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();

                // Act
                view.Remove(item, out item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveOut_OneItemContainedViewsAtTheEnd_AuxViewsOffsetNotAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = Random.Next(0, views.Length);

            // Act // Do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            var item = view.Choose();
            view.Remove(item, out item);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            foreach (var v in views) {
                Assert.That(v.Count, Is.Zero, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveOut_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                string outItem;

                // Act
                view.Remove(item, out outItem);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region RemoveRange(IEnumerable<T>)

        [Test]
        public void ViewRemoveRange_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();
                var items = GetList(new[] { item });

                // Act
                view.RemoveRange(items);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveRange_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                if (view.IsEmpty)
                    continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();
                var items = GetList(new[] { item });

                // Act
                view.RemoveRange(items);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveRange_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                var items = GetList(new[] { item });

                // Act
                view.RemoveRange(items);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveRange_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            // Arrange
            var index = 0;
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;

            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            var i = Random.Next(0, views.Length);
            var view = views[i];
            var item = view.Choose();
            var items = GetList(new[] { item });

            // Act // Do it on one of the views; 
            view.RemoveRange(items);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
        }

        [Test]
        public void ViewRemoveRange_OneContainedItemViewsAtTheBeginning_RightAuxViewsOffsetAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var index = Random.Next(0, views.Length);
            var view = views[index];
            var item = view.Choose();
            var items = GetList(new[] { item });

            // Act
            // do it on one of the views; They all are one-item overlapping views                        
            view.RemoveRange(items);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveRange_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                var items = GetList(new[] { item });

                // Act
                view.RemoveRange(items);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveRange_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();
                var items = GetList(new[] { item });

                // Act
                view.RemoveRange(items);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveRange_OneItemContainedViewsAtTheEnd_AuxViewsOffsetNotAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = Random.Next(0, views.Length);

            // Act // Do it on one of the views; They all are one-item overlapping views            
            var view = views[index];
            var item = view.Choose();
            var items = GetList(new[] { item });
            view.RemoveRange(items);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            foreach (var v in views) {
                Assert.That(v.Count, Is.Zero, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveRange_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();
                var items = GetList(new[] { item });

                // Act
                view.RemoveRange(items);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region RemoveDuplicates(T)

        [Test]
        public void ViewRemoveDuplicates_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();

                // Act
                view.RemoveDuplicates(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveDuplicates_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                if (view.IsEmpty)
                    continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();

                // Act
                view.RemoveDuplicates(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveDuplicates_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.RemoveDuplicates(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveDuplicates_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            // Arrange
            var index = 0;
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;

            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            var i = Random.Next(0, views.Length);
            var view = views[i];
            var item = view.Choose();

            // Act // Do it on one of the views; 
            view.RemoveDuplicates(item);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
        }

        [Test]
        public void ViewRemoveDuplicates_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var index = Random.Next(0, views.Length);
            var view = views[index];
            var item = view.Choose();

            // Act
            // do it on one of the views; They all are one-item overlapping views                        
            view.RemoveDuplicates(item);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveDuplicates_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.RemoveDuplicates(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveDuplicates_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = view.Choose();

                // Act
                view.RemoveDuplicates(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRemoveDuplicates_OneItemContainedViewsAtTheEnd_AuxViewsOffsetNotAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            var index = Random.Next(0, views.Length);
            var view = views[index];
            var item = view.Choose();

            // Act 
            // Do it on one of the views; They all are one-item overlapping views            
            view.RemoveDuplicates(item);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
            foreach (var v in views) {
                Assert.That(v.Count, Is.Zero, $"view {index}");
            }
        }

        [Test]
        public void ViewRemoveDuplicates_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var item = list.Choose();

                // Act
                view.RemoveDuplicates(item);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        #region RetainRange(T)

        [Test]
        public void ViewRetainRange_NItemNonOverlappingViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetNItemNonOverlappingViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var retainItems = view.Skip(1).Take(view.Count - 1).ToArray();

                // Act
                view.RetainRange(retainItems);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRetainRange_OneItemViewsInTheMiddle_RightAuxViewsOffsetAffected()
        {
            var index = 0;
            var views = GetOneItemViewsInTheMiddle(list);
            foreach (var view in views) {
                if (view.IsEmpty)
                    continue;

                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var retainRange = view.Skip(1).Take(view.Count - 1).ToArray();

                // Act
                view.RetainRange(retainRange);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRetainRange_ZeroItemViewsInTheMiddle_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsInTheMiddle(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RetainRange(view);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRetainRange_NItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffected()
        {
            // Arrange
            var index = 0;
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;

            var views = GetNItemContainedInEachOtherViewsAtTheBeginning(list);
            var i = Random.Next(0, views.Length);
            var view = views[i];
            var retainItems = view.Skip(1).Take(view.Count - 1).ToList();

            // Act // Do it on one of the views; 
            view.RetainRange(retainItems);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
        }

        [Test]
        public void ViewRetainRange_OneItemContainedViewsAtTheBeginning_RightAuxViewsOffsetAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var views = GetOneItemContainedInEachOtherViewsAtTheBeginning(list);
            var offsetLeft = auxViewLeft.Offset;
            var offsetRight = auxViewRight.Offset;
            var index = Random.Next(0, views.Length);
            var view = views[index];
            var retainItems = view.Skip(1).Take(view.Count - 1).ToArray();

            // Act
            // do it on one of the views; They all are one-item overlapping views                        
            view.RetainRange(retainItems);

            // Assert    
            Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
            Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight - 1), $"view {index}");
            foreach (var v in views) {
                Assert.That(v, Is.Empty, $"view {index}");
            }
        }

        [Test]
        public void ViewRetainRange_ZeroItemViewsAtTheBeginning_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheBeginning(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RetainRange(view);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRetainRange_NItemContainedViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetNItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange                       
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var retainItems = view.Skip(1).Take(view.Count - 1).ToArray();

                // Act
                view.RetainRange(retainItems);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRetainRange_OneItemContainedViewsAtTheEnd_AuxViewsOffsetNotAffectedAndAllViewsGetEmpty()
        {
            // Arrange 
            var index = 0;
            var views = GetOneItemContainedInEachOtherViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;
                var retainRange = view.Skip(1).Take(view.Count - 1).ToArray();

                // Act
                view.RetainRange(retainRange);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        [Test]
        public void ViewRetainRange_ZeroItemViewsAtTheEnd_BothViewsOffsetNotChanged()
        {
            var index = 0;
            var views = GetZeroItemViewsAtTheEnd(list);
            foreach (var view in views) {
                // Arrange 
                var offsetLeft = auxViewLeft.Offset;
                var offsetRight = auxViewRight.Offset;

                // Act
                view.RetainRange(view);

                // Assert                
                Assert.That(auxViewLeft.Offset, Is.EqualTo(offsetLeft), $"view {index}");
                Assert.That(auxViewRight.Offset, Is.EqualTo(offsetRight), $"view {index}");
                Assert.That(view, Is.EqualTo(list.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer), $"view {index}");
                index++;
            }
        }

        #endregion

        // How many methods ?
    }

    [TestFixture]
    public abstract class IListTests : IIndexedTests
    {
        #region Factories

        protected abstract IList<T> GetEmptyList<T>(SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false);

        protected abstract IList<T> GetList<T>(SCG.IEnumerable<T> enumerable, SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false);

        private IList<string> GetStringList(Randomizer random, SCG.IEqualityComparer<string> equalityComparer = null, bool allowsNull = false)
            => GetList(GetStrings(random, GetCount(random)), equalityComparer, allowsNull);

        private static NonComparable[] GetNonComparables(Random random) => GetNonComparables(random, GetCount(random));
        private static NonComparable[] GetNonComparables(Random random, int count) => Enumerable.Range(0, count).Select(i => new NonComparable(random.Next())).ToArray();

        private static Comparable[] GetComparables(Random random) => GetComparables(random, GetCount(random));
        private static Comparable[] GetComparables(Random random, int count) => Enumerable.Range(0, count).Select(i => new Comparable(random.Next())).ToArray();

        private static IList<T> GetEmptyView<T>(IList<T> list)
        {
            var index = Random.Next(0, list.Count);
            const int count = 0;
            return list.View(index, count);
        }

        private static IList<T> GetView<T>(IList<T> list, int? minOffset = null, int? maxOffset = null)
        {
            var minValue = minOffset == null ? 0 :
                minOffset < 0 || minOffset > list.Count - 1 ? 0 : minOffset.Value;

            var maxValue = maxOffset == null ? list.Count - 1 :
                maxOffset < minValue || maxOffset > list.Count - 1 ? list.Count - 1 : maxOffset.Value;

            // OLD version: var offset = Random.Next(0, list.Count - 1);
            var offset = Random.Next(minValue, maxValue); // why Count - 1, but not Count. It is realted to: Random.Next(1,1);
            var count = Random.Next(1, list.Count - offset);

            return list.View(offset, count);
        }

        private static int GetOffset<T>(IList<T> view, Random random)
        {
            // TODO: Requires
            var maxOffset = view.Underlying.Count - view.Count;
            var withOffset = random.Next(0, maxOffset - view.Offset + 1);
            return withOffset;
        }

        private static int GetNewCount<T>(IList<T> view, int withOffset, Random random)
        {
            // TODO: Requires
            //var maxOffset = view.Underlying.Count - view.Count;
            //var withOffset = Random.Next(0, maxOffset + 1);
            //var newCount = Random.Next(0, view.Underlying.Count - (view.Offset + withOffset));

            //var maxOffset = view.Underlying.Count - view.Count;
            //var withOffset = Random.Next(0, maxOffset + 1);

            // ! var newCount = Random.Next(0, view.Underlying.Count - view.Offset + 1);
            var newOffset = view.Offset + withOffset;
            var newCount = random.Next(0, view.Underlying.Count - newOffset + 1);

            return newCount;
        }

        #region Inherited

        protected override IIndexed<T> GetEmptyIndexed<T>(SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false) => GetEmptyList(equalityComparer, allowsNull);

        protected override IIndexed<T> GetIndexed<T>(SCG.IEnumerable<T> enumerable, SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false) => GetList(enumerable, equalityComparer, allowsNull);

        #endregion

        #endregion

        #region Test Methods

        #region SC.ICollection

        #region Properties

        #region IsSynchronized

        [Test]
        public void IsSynchronized_RandomCollection_False()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act
            var isSynchronized = collection.IsSynchronized;

            // Assert
            Assert.That(isSynchronized, Is.False);
        }

        #endregion

        #region SyncRoot

        // TODO: Test?

        #endregion

        #endregion

        #region Methods

        #region CopyTo(Array, int)

        [Test]
        public void SCICollectionCopyTo_InvalidType_ThrowsArgumentException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var array = new int[collection.Count];

            // Act & Assert
            Assert.That(() => ((SC.ICollection) collection).CopyTo(array, 0), Throws.ArgumentException.Because("Target array type is not compatible with the type of items in the collection."));
        }

        [Test]
        public void SCICollectionCopyTo_InvalidDimension_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var count = collection.Count;
            var array = new string[count, count];

            // Act & Assert
            Assert.That(() => ((SC.ICollection) collection).CopyTo(array, 0), Violates.Precondition);
        }

        [Test]
        public void SCICollectionCopyTo_NullArray_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => ((SC.ICollection) collection).CopyTo(null, 0), Violates.Precondition);
        }

        [Test]
        public void SCICollectionCopyTo_NegativeIndex_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var array = new string[collection.Count];

            // Act & Assert
            Assert.That(() => ((SC.ICollection) collection).CopyTo(array, -1), Violates.Precondition);
        }

        [Test]
        public void SCICollectionCopyTo_IndexOutOfBound_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var array = new string[collection.Count];
            var index = Random.Next(1, collection.Count);

            // Act & Assert
            Assert.That(() => ((SC.ICollection) collection).CopyTo(array, index), Violates.Precondition);
        }

        [Test]
        public void SCICollectionCopyTo_EqualSizeArray_Equals()
        {
            // Arrange
            var collection = GetStringList(Random);
            var array = new string[collection.Count];

            // Act
            ((SC.ICollection) collection).CopyTo(array, 0);

            // Assert
            Assert.That(array, Is.EqualTo(collection).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCICollectionCopyTo_CopyToRandomIndex_SectionEquals()
        {
            // Arrange
            var collection = GetStringList(Random);
            var array = GetStrings(Random, (int) (collection.Count * 1.7));
            var arrayIndex = Random.Next(0, array.Length - collection.Count);

            // Act
            ((SC.ICollection) collection).CopyTo(array, arrayIndex);
            var section = array.Skip(arrayIndex).Take(collection.Count);

            // Assert
            Assert.That(section, Is.EqualTo(collection).Using(ReferenceEqualityComparer));
        }

        #endregion

        #endregion

        #endregion

        #region SC.IList

        #region this[int]

        [Test]
        public void SCIListItemGet_NegativeIndex_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(int.MinValue, 0);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index], Violates.Precondition);
        }

        [Test]
        public void SCIListItemGet_IndexOfCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = collection.Count;

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index], Violates.Precondition);
        }

        [Test]
        public void SCIListItemGet_IndexLargerThanCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var count = collection.Count;
            var index = Random.Next(count + 1, int.MaxValue);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index], Violates.Precondition);
        }

        [Test]
        public void SCIListItemGet_EmptyCollection_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetEmptyIndexed<string>();

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[0], Violates.Precondition);
        }

        [Test]
        public void SCIListItemGet_RandomCollectionWithNull_Null()
        {
            Run.If(AllowsNull);

            // Arrange
            var items = GetStrings(Random).WithNull(Random);
            var collection = GetList(items, allowsNull: true);
            var index = collection.ToArray().IndexOf(null);

            // Act
            var item = ((SC.IList) collection)[index];

            // Act & Assert
            Assert.That(item, Is.Null);
        }

        [Test]
        public void SCIListItemGet_RandomCollectionIndexZero_FirstItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var first = collection.First();

            // Act
            var item = ((SC.IList) collection)[0];

            // Assert
            Assert.That(item, Is.SameAs(first));
        }

        [Test]
        public void SCIListItemGet_RandomCollectionIndexCountMinusOne_LastItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var last = collection.Last();
            var count = collection.Count;

            // Act
            var item = ((SC.IList) collection)[count - 1];

            // Assert
            Assert.That(item, Is.SameAs(last));
        }

        [Test]
        public void SCIListItemGet_RandomCollectionRandomIndex_ItemAtPositionIndex()
        {
            // Arrange
            var collection = GetStringList(Random);
            var array = collection.ToArray();
            var index = Random.Next(0, array.Length);

            // Act
            var item = ((SC.IList) collection)[index];

            // Assert
            Assert.That(item, Is.SameAs(array[index]));
        }

        [Test]
        public void SCIListItemSet_InvalidType_ThrowsArgumentException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random);
            object item = Random.Next();
            var typeString = "System.String";
            var parameterName = "value";
            var exceptionMessage = $"The value \"{item}\" is not of type \"{typeString}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {parameterName}";

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index] = item, Throws.ArgumentException.Because(exceptionMessage));
        }

        [Test]
        public void SCIListItemSet_NegativeIndex_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(int.MinValue, 0);
            var item = Random.GetString();

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index] = item, Violates.Precondition);
        }

        [Test]
        public void SCIListItemSet_IndexOfCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = collection.Count;
            var item = Random.GetString();

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index] = item, Violates.Precondition);
        }

        [Test]
        public void SCIListItemSet_IndexLargerThanCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(collection.Count + 1, int.MaxValue);
            var item = Random.GetString();

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index] = item, Violates.Precondition);
        }

        [Test]
        public void SCIListItemSet_EmptyCollection_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var index = 0;
            var item = Random.GetString();

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index] = item, Violates.Precondition);
        }

        [Test]
        public void SCIListItemSet_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);
            var index = GetIndex(collection, Random);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index] = null, Violates.UncaughtPrecondition);
        }

        [Test]
        public void SCIListItemSet_RandomCollectionSetDuplicate_ViolatesPrecondition()
        {
            Run.If(!AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random);
            var item = collection.ToArray().Choose(Random);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index] = item, Violates.PreconditionSaying(CollectionMustAllowDuplicates));
        }

        [Test]
        public void SCIListItemSet_RandomCollectionSetDuplicate_Inserted()
        {
            Run.If(AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random);
            var item = collection.ToArray().Choose(Random);

            // Act
            ((SC.IList) collection)[index] = item;

            // Assert
            Assert.That(((SC.IList) collection)[index], Is.SameAs(item));
        }

        [Test]
        public void SCIListItemSet_AllowsNull_Null()
        {
            Run.If(AllowsNull);

            // Arrange
            var collection = GetStringList(Random, allowsNull: true);
            var index = GetIndex(collection, Random);
            var array = collection.ToArray();
            array[index] = null;

            // Act
            ((SC.IList) collection)[index] = null;

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListItemSet_RandomCollectionIndexZero_FirstItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = 0;
            var array = collection.ToArray();
            array[index] = item;

            // Act
            ((SC.IList) collection)[index] = item;

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListItemSet_RandomCollectionIndexCountMinusOne_LastItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = collection.Count - 1;
            var array = collection.ToArray();
            array[index] = item;

            // Act
            ((SC.IList) collection)[index] = item;

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListItemSet_RandomCollectionRandomIndex_ItemAtPositionIndex()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = GetIndex(collection, Random);
            var array = collection.ToArray();
            array[index] = item;

            // Act
            ((SC.IList) collection)[index] = item;

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListItemSet_RandomCollectionRandomIndex_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = GetIndex(collection, Random);
            var oldItem = collection[index];
            var expectedEvents = new[] {
                RemovedAt(oldItem, index, collection),
                Removed(oldItem, 1, collection),
                Inserted(item, index, collection),
                Added(item, 1, collection),
                Changed(collection),
            };

            // Act & Assert
            Assert.That(() => ((SC.IList) collection)[index] = item, Raises(expectedEvents).For(collection));
        }

        [Test]
        public void SCIListItemSet_SetDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = GetIndex(collection, Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            ((SC.IList) collection)[index] = item;

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCIListItemSet_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region Add(T)

        [Test]
        public void SCIListAdd_InvalidType_ThrowsArgumentException()
        {
            // Arrange
            var collection = GetStringList(Random);
            object item = Random.Next();
            var typeString = "System.String";
            var parameterName = "value";
            var exceptionMessage = $"The value \"{item}\" is not of type \"{typeString}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {parameterName}";

            // Act & Assert
            Assert.That(() => collection.Add(item), Throws.ArgumentException.Because(exceptionMessage));
        }

        [Test]
        public void SCIListAdd_DisallowsNullAddNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).Add(null), Violates.UncaughtPrecondition);
        }

        [Test]
        public void SCIListAdd_AllowsNullAddNull_ReturnsTrue()
        {
            Run.If(AllowsNull);
            // Arrange
            var collection = GetStringList(Random, allowsNull: true);
            var index = collection.Count;

            // Act
            var result = ((SC.IList) collection).Add(null);

            // Assert
            Assert.That(result, Is.EqualTo(index));
        }

        [Test]
        public void SCIListAdd_EmptyCollectionAddItem_CollectionIsSingleItemCollection()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var item = Random.GetString();
            var itemArray = new[] { item };

            // Act
            var result = ((SC.IList) collection).Add(item);

            // Assert
            Assert.That(result, Is.Zero);
            Assert.That(collection, Is.EqualTo(itemArray));
        }

        [Test]
        public void SCIListAdd_AddDuplicateItem_AllowsDuplicates()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items, CaseInsensitiveStringComparer.Default);
            var duplicateItem = items.Choose(Random).ToLower();
            var index = AllowsDuplicates ? collection.Count : -1;

            // Act
            var result = ((SC.IList) collection).Add(duplicateItem);

            // Assert
            Assert.That(result, Is.EqualTo(index));
        }

        // TODO: Add test to IList<T>.Add ensuring that order is the same
        [Test]
        public void SCIListAdd_ManyItems_Equivalent()
        {
            // Arrange
            var referenceEqualityComparer = ComparerFactory.CreateReferenceEqualityComparer<string>();
            var collection = GetEmptyList(referenceEqualityComparer);
            var count = Random.Next(100, 250);
            var items = GetStrings(Random, count);

            // Act
            foreach (var item in items) {
                ((SC.IList) collection).Add(item); // TODO: Verify that items were added?
            }

            // Assert
            Assert.That(collection, Is.EqualTo(items).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListAdd_AddItem_RaisesExpectedEvents()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items);
            var item = GetLowercaseString(Random);
            var expectedEvents = new[] {
                Added(item, 1, collection),
                Changed(collection)
            };

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).Add(item), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void SCIListAdd_AddDuplicateItem_RaisesNoEvents()
        {
            Run.If(!AllowsDuplicates);

            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items, CaseInsensitiveStringComparer.Default);
            var duplicateItem = items.Choose(Random).ToLower();

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).Add(duplicateItem), RaisesNoEventsFor(collection));
        }

        [Test]
        public void SCIListAdd_AddItemDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            ((SC.IList) collection).Add(item);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCIListAdd_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        //[Category("Unfinished")]
        //[Ignore("Unfinished")]
        public void SCIListAdd_FixedSizeCollection_ViolatesPrecondition()
        {
            Run.If(IsFixedSize);

            // Arrange
            var collection = GetStringList(Random);
            var item = GetUppercaseString(Random);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).Add(item), Violates.Precondition);
            //Assert.That(IsFixedSize, Is.False, "Tests have not been written yet");
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCIListAdd_Set_Fail()
        {
            Assert.That(!AllowsDuplicates, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region Contains(object)

        [Test]
        public void SCIListContains_InvalidType_False()
        {
            // Arrange
            var collection = GetStringList(Random);
            object item = Random.Next();

            // Act
            var contains = collection.Contains(item);

            // Assert
            Assert.That(contains, Is.False);
        }

        [Test]
        public void SCIListContains_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).Contains(null), Violates.UncaughtPrecondition);
        }

        [Test]
        public void SCIListContains_AllowNullContainsNull_True()
        {
            Run.If(AllowsNull);

            // Arrange
            var items = GetStrings(Random).WithNull(Random);
            var collection = GetList(items, allowsNull: true);

            // Act
            var contains = ((SC.IList) collection).Contains(null);

            // Assert
            Assert.That(contains, Is.True);
        }

        [Test]
        public void SCIListContains_AllowNullContainsNoNull_False()
        {
            Run.If(AllowsNull); // new

            // Arrange
            var items = GetStrings(Random);
            var collection = GetList(items, allowsNull: true);

            // Act
            var contains = ((SC.IList) collection).Contains(null);

            // Assert
            Assert.That(contains, Is.False);
        }

        [Test]
        public void SCIListContains_EmptyCollection_False()
        {
            // Arrange
            var collection = GetEmptyCollection<string>();
            var item = Random.GetString();

            // Act
            var contains = ((SC.IList) collection).Contains(item);

            // Assert
            Assert.That(contains, Is.False);
        }

        [Test]
        public void SCIListContains_SingleItemCollectionNonDuplicateItem_False()
        {
            // Arrange
            var item = GetUppercaseString(Random);
            var itemArray = new[] { item };
            var collection = GetList(itemArray);
            var nonDuplicateItem = item.ToLower();

            // Act
            var contains = ((SC.IList) collection).Contains(nonDuplicateItem);

            // Assert
            Assert.That(contains, Is.False);
        }

        [Test]
        public void SCIListContains_SingleItemCollectionDuplicateItem_True()
        {
            // Arrange
            var item = GetUppercaseString(Random);
            var itemArray = new[] { item };
            var collection = GetList(itemArray, CaseInsensitiveStringComparer.Default);
            var duplicateItem = item.ToLower();

            // Act
            var contains = ((SC.IList) collection).Contains(duplicateItem);

            // Assert
            Assert.That(contains, Is.True);
        }

        [Test]
        public void SCIListContains_SingleItemCollectionReferenceInequalItem_False()
        {
            // Arrange
            var item = Random.GetString();
            var itemArray = new[] { item };
            var collection = GetList(itemArray, ReferenceEqualityComparer);
            var nonDuplicateItem = string.Copy(item);

            // Act
            var contains = ((SC.IList) collection).Contains(nonDuplicateItem);

            // Assert
            Assert.That(contains, Is.False);
        }

        [Test]
        public void SCIListContains_RandomCollectionNewItem_False()
        {
            // Arrange
            var collection = GetStringList(Random, ReferenceEqualityComparer);
            var item = Random.GetString();

            // Act
            var contains = ((SC.IList) collection).Contains(item);

            // Assert
            Assert.That(contains, Is.False);
        }

        [Test]
        public void SCIListContains_RandomCollectionExistingItem_True()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items, CaseInsensitiveStringComparer.Default);
            var item = items.Choose(Random).ToLower();

            // Act
            var contains = ((SC.IList) collection).Contains(item);

            // Assert
            Assert.That(contains, Is.True);
        }

        [Test]
        public void SCIListContains_RandomCollectionNewItem_True()
        {
            // Arrange
            var items = GetStrings(Random);
            var collection = GetList(items, ReferenceEqualityComparer);
            var item = string.Copy(items.Choose(Random));

            // Act
            var contains = ((SC.IList) collection).Contains(item);

            // Assert
            Assert.That(contains, Is.False);
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCIListContains_Set_Fail()
        {
            Assert.That(!AllowsDuplicates, Is.False, "Tests have not been written yet");
        }

        #endregion

        // TODO: Should we rather cast the collection than the object?

        #region IndexOf(T)

        [Test]
        public void SCIListIndexOf_InvalidType_MinusOne()
        {
            // Arrange
            var collection = GetStringList(Random);
            object item = Random.Next();

            // Act
            var indexOf = collection.IndexOf(item);

            // Act & Assert
            Assert.That(indexOf, Is.EqualTo(-1));
        }

        [Test]
        public void SCIListIndexOf_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);

            // Act & Assert
            Assert.That(() => collection.IndexOf((object) null), Violates.UncaughtPrecondition);
        }

        [Test]
        public void SCIListIndexOf_AllowsNull_PositiveIndex()
        {
            Run.If(AllowsDuplicates);
            // Arrange
            var items = GetStrings(Random).WithNull(Random);
            var collection = GetList(items, allowsNull: true);
            var index = collection.ToArray().IndexOf(null);

            // Act
            var indexOf = collection.IndexOf((object) null);

            // Assert
            Assert.That(indexOf, Is.EqualTo(index));
        }

        [Test]
        public void SCIListIndexOf_EmptyCollection_TildeZero()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            object item = Random.GetString();

            // Act
            var indexOf = collection.IndexOf(item);

            // Assert
            Assert.That(indexOf, Is.EqualTo(-1));
        }

        [Test]
        public void SCIListIndexOf_RandomCollectionIndexOfNewItem_NegativeIndex()
        {
            // Arrange
            var items = GetStrings(Random);
            var collection = GetList(items);
            object item = items.DifferentItem(() => Random.GetString());
            var count = collection.Count;

            // Act
            var indexOf = collection.IndexOf(item);

            // Assert
            Assert.That(indexOf, Is.EqualTo(-1));
        }

        [Test]
        public void SCIListIndexOf_RandomCollectionIndexOfExistingItem_Index()
        {
            // Arrange
            var collection = GetStringList(Random, ReferenceEqualityComparer);
            var items = collection.ToArray();
            var index = Random.Next(0, items.Length);
            object item = items[index];

            // Act
            var indexOf = collection.IndexOf(item);

            // Assert
            Assert.That(indexOf, Is.EqualTo(index));
        }

        [Test]
        public void SCIListIndexOf_DuplicateItems_Zero()
        {
            // Arrange
            var count = GetCount(Random);
            var item = Random.GetString();
            var items = item.Repeat(count);
            var collection = GetList(items);

            // Act
            var indexOf = collection.IndexOf((object) item);

            // Assert
            Assert.That(indexOf, Is.Zero);
        }

        [Test]
        public void SCIListIndexOf_CollectionWithDuplicateItems_FirstIndex()
        {
            // Arrange
            var count = GetCount(Random);
            var item = Random.GetString();
            var items = GetStrings(Random).WithRepeatedItem(() => item, count, Random);
            var collection = GetList(items);
            var index = collection.ToArray().IndexOf(item);

            // Act
            var indexOf = collection.IndexOf((object) item);

            // Assert
            Assert.That(indexOf, Is.EqualTo(index));
        }

        [Test]
        public void SCIListIndexOf_RandomCollectionNewItem_GetsTildeIndex()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items);
            var item = GetLowercaseString(Random);

            // Act
            var expectedIndex = ~collection.IndexOf(item);
            collection.Add(item);
            var indexOf = collection.IndexOf((object) item);

            // Assert
            Assert.That(indexOf, Is.EqualTo(expectedIndex));
        }

        #endregion

        // TODO: Should we rather cast the collection than the object?

        #region Insert(int, object)

        [Test]
        public void SCIListInsert_InvalidType_ThrowsArgumentException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            object item = Random.Next();
            var typeString = "System.String";
            var parameterName = "value";
            var exceptionMessage = $"The value \"{item}\" is not of type \"{typeString}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {parameterName}";

            // Act & Assert
            Assert.That(() => collection.Insert(index, item), Throws.ArgumentException.Because(exceptionMessage));
        }

        [Test]
        public void SCIListInsert_NegativeIndex_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(int.MinValue, 0);
            object item = Random.GetString();

            // Act & Assert
            Assert.That(() => collection.Insert(index, item), Violates.Precondition);
        }

        [Test]
        public void SCIListInsert_IndexLargerThanCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(collection.Count + 1, int.MaxValue);
            object item = Random.GetString();

            // Act & Assert
            Assert.That(() => collection.Insert(index, item), Violates.UncaughtPrecondition);
        }

        [Test]
        public void SCIListInsert_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);
            var index = GetIndex(collection, Random, true);

            // Act & Assert
            Assert.That(() => collection.Insert(index, (object) null), Violates.UncaughtPrecondition);
        }

        [Test]
        public void SCIListInsert_RandomCollectionSetDuplicate_ViolatesPrecondition()
        {
            Run.If(!AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            object item = collection.ToArray().Choose(Random);

            // Act & Assert
            Assert.That(() => collection.Insert(index, item), Violates.Precondition);
        }

        [Test]
        public void SCIListInsert_RandomCollectionSetDuplicate_Inserted()
        {
            Run.If(AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            object item = collection.ToArray().Choose(Random);

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection[index], Is.SameAs(item));
        }

        [Test]
        public void SCIListInsert_EmptyCollection_SingleItemCollection()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var index = 0;
            object item = Random.GetString();
            var array = new[] { item };

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListInsert_IndexOfCount_Appended()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = collection.Count;
            object item = Random.GetString();
            var array = collection.Append(item).ToArray();

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListInsert_AllowsNull_Null()
        {
            Run.If(AllowsNull);
            // Arrange
            var collection = GetStringList(Random, allowsNull: true);
            var index = GetIndex(collection, Random, true);
            var array = collection.ToArray().InsertItem(index, null);

            // Act
            collection.Insert(index, (object) null);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListInsert_RandomCollectionIndexZero_FirstItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            object item = Random.GetString();
            var index = 0;
            var array = collection.ToArray().InsertItem(index, item);

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListInsert_RandomCollectionIndexCountMinusOne_LastItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            object item = Random.GetString();
            var index = collection.Count - 1;
            var array = collection.ToArray().InsertItem(index, item);

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListInsert_RandomCollectionRandomIndex_ItemAtPositionIndex()
        {
            // Arrange
            var collection = GetStringList(Random);
            object item = Random.GetString();
            var index = GetIndex(collection, Random, true);
            var array = collection.ToArray().InsertItem(index, item);

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListInsert_RandomCollectionRandomIndex_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = GetIndex(collection, Random, true);
            var expectedEvents = new[] {
                Inserted(item, index, collection),
                Added(item, 1, collection),
                Changed(collection),
            };

            // Act & Assert
            Assert.That(() => collection.Insert(index, (object) item), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void SCIListInsert_InsertDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            object item = Random.GetString();
            var index = GetIndex(collection, Random, true);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Insert(index, item);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCIListInsert_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        //[Category("Unfinished")]
        //[Ignore("Unfinished")]
        public void SCIListInsert_FixedSizeCollection_ViolatesPrecondition()
        {
            Run.If(IsFixedSize);

            // Arrange
            var collection = GetStringList(Random);
            var item = GetUppercaseString(Random);
            var index = GetIndex(collection, Random, true);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).Insert(index, item), Violates.Precondition);
            // Assert.That(IsFixedSize, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region Remove(object)

        [Test]
        public void SCIListRemove_InvalidType_Nothing()
        {
            // Arrange
            var collection = GetStringList(Random);
            var array = collection.ToArray();
            object item = Random.Next();

            // Act
            collection.Remove(item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListRemove_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).Remove(null), Violates.UncaughtPrecondition);
        }

        [Test]
        public void SCIListRemove_AllowsNull_RemovesNull()
        {
            Run.If(AllowsNull);
            // Arrange
            var items = GetStrings(Random).WithNull(Random);
            var collection = GetList(items, allowsNull: true);
            var expected = collection.Where(item => item != null).ToList();

            // Act
            ((SC.IList) collection).Remove(null);

            // Assert
            Assert.That(collection, Is.EqualTo(expected).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListRemove_RemoveExistingItem_RaisesExpectedEvents()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items, CaseInsensitiveStringComparer.Default);
            var existingItem = items.Choose(Random);
            var item = existingItem.ToLower();
            var expectedEvents = new[] {
                Removed(existingItem, 1, collection),
                Changed(collection),
            };

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).Remove(item), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void SCIListRemove_RemoveNewItem_RaisesNoEvents()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var item = GetLowercaseString(Random);
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).Remove(item), RaisesNoEventsFor(collection));
        }

        [Test]
        public void SCIListRemove_EmptyList_Empty()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var item = Random.GetString();

            // Act
            ((SC.IList) collection).Remove(item);

            // Assert
            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void SCIListRemove_RemoveExistingItem_Removed()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var item = items.Choose(Random).ToLower(); // TODO: Could potentially fail, if there are duplicates
            var collection = GetList(items, CaseInsensitiveStringComparer.Default);
            var expected = collection.SkipIndex(collection.IndexOf(item)).ToList();

            // Act
            ((SC.IList) collection).Remove(item);

            // Assert
            Assert.That(collection, Is.EqualTo(expected).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListRemove_RemoveNewItem_Unchanged()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var item = GetLowercaseString(Random);
            var collection = GetList(items);
            var expected = collection.ToArray();

            // Act
            ((SC.IList) collection).Remove(item);

            // Assert
            Assert.That(collection, Is.EqualTo(expected).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListRemove_RemoveItemDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var item = items.Choose(Random).ToLower();
            var collection = GetList(items, CaseInsensitiveStringComparer.Default);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            ((SC.IList) collection).Remove(item);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void SCIListRemove_RemoveItemDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items);
            var item = GetLowercaseString(Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            ((SC.IList) collection).Remove(item);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCIListRemove_ReadOnlyList_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCIListRemove_DuplicatesByCounting_Fail()
        {
            // TODO: Only one item is replaced based on AllowsDuplicates/DuplicatesByCounting
            Assert.That(DuplicatesByCounting, Is.False, "Tests have not been written yet");
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCIListRemove_Set_Fail()
        {
            Assert.That(!AllowsDuplicates, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region RemoveAt(int)

        [Test]
        public void SCIListRemoveAt_EmptyCollection_ViolatesPrecondtion()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).RemoveAt(0), Violates.Precondition);
        }

        [Test]
        public void SCIListRemoveAt_NegativeIndex_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(int.MinValue, 0);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).RemoveAt(index), Violates.Precondition);
        }

        [Test]
        public void SCIListRemoveAt_IndexOfCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = collection.Count;

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).RemoveAt(index), Violates.Precondition);
        }

        [Test]
        public void SCIListRemoveAt_IndexLargerThanCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(collection.Count + 1, int.MaxValue);

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).RemoveAt(index), Violates.Precondition);
        }

        [Test]
        public void SCIListRemoveAt_RemoveDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(0, collection.Count);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            ((SC.IList) collection).RemoveAt(index);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void SCIListRemoveAt_RandomCollection_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(0, collection.Count);
            var item = collection[index];
            var expectedEvents = new[] {
                RemovedAt(item, index, collection),
                Removed(item, 1, collection),
                Changed(collection),
            };

            // Act & Assert
            Assert.That(() => ((SC.IList) collection).RemoveAt(index), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void SCIListRemoveAt_RandomCollection_ItemAtIndex()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(0, collection.Count);
            var expectedItem = collection[index];
            var array = collection.SkipIndex(index).ToArray();

            // Act
            ((SC.IList) collection).RemoveAt(index);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListRemoveAt_RandomCollectionWithNullRemoveNull_Null()
        {
            Run.If(AllowsNull);
            // Arrange
            var items = GetStrings(Random).WithNull(Random);
            var collection = GetList(items, allowsNull: true);
            var index = collection.IndexOf(null);
            var array = collection.SkipIndex(index).ToArray();

            // Act
            ((SC.IList) collection).RemoveAt(index);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListRemoveAt_SingleItemCollection_Item()
        {
            // Arrange
            var item = Random.GetString();
            var itemArray = new[] { item };
            var collection = GetList(itemArray);

            // Act
            ((SC.IList) collection).RemoveAt(0);

            // Assert
            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void SCIListRemoveAt_RemoveFirstItem_Removed()
        {
            // Arrange
            var collection = GetStringList(Random);
            var items = collection.ToArray();
            var index = 0;
            var firstItem = collection[index];

            // Act
            ((SC.IList) collection).RemoveAt(index);

            // Assert
            Assert.That(collection, Is.EqualTo(items.Skip(1)).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCIListRemoveAt_RemoveLastItem_Removed()
        {
            // Arrange
            var collection = GetStringList(Random);
            var count = collection.Count;
            var items = collection.ToArray();
            var index = count - 1;
            var lastItem = collection[index];

            // Act
            ((SC.IList) collection).RemoveAt(index);

            // Assert
            Assert.That(collection, Is.EqualTo(items.Take(index)).Using(ReferenceEqualityComparer));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCIListRemoveAt_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCIListRemoveAt_DuplicatesByCounting_Fail()
        {
            // TODO: Only one item is replaced based on AllowsDuplicates/DuplicatesByCounting
            Assert.That(DuplicatesByCounting, Is.False, "Tests have not been written yet");
        }

        #endregion

        #endregion

        #region SCG.IList<T>

        #region IndexOf(T)

        [Test]
        public void SCGIListIndexOf_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);

            // Act & Assert
            Assert.That(() => ((SCG.IList<string>) collection).IndexOf(null), Violates.UncaughtPrecondition);
        }

        [Test]
        public void SCGIListIndexOf_AllowsNull_PositiveIndex()
        {
            Run.If(AllowsDuplicates);
            // Arrange
            var items = GetStrings(Random).WithNull(Random);
            var collection = GetList(items, allowsNull: true);
            var index = collection.ToArray().IndexOf(null);

            // Act
            var indexOf = ((SCG.IList<string>) collection).IndexOf(null);

            // Assert
            Assert.That(indexOf, Is.EqualTo(index));
        }

        [Test]
        public void SCGIListIndexOf_EmptyCollection_TildeZero()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var item = Random.GetString();

            // Act
            var indexOf = ((SCG.IList<string>) collection).IndexOf(item);

            // Assert
            Assert.That(indexOf, Is.EqualTo(-1));
        }

        [Test]
        public void SCGIListIndexOf_RandomCollectionIndexOfNewItem_NegativeIndex()
        {
            // Arrange
            var items = GetStrings(Random);
            var collection = GetList(items);
            var item = items.DifferentItem(() => Random.GetString());
            var count = collection.Count;

            // Act
            var indexOf = ((SCG.IList<string>) collection).IndexOf(item);

            // Assert
            Assert.That(indexOf, Is.EqualTo(-1));
        }

        [Test]
        public void SCGIListIndexOf_RandomCollectionIndexOfExistingItem_Index()
        {
            // Arrange
            var collection = GetStringList(Random, ReferenceEqualityComparer);
            var items = collection.ToArray();
            var index = Random.Next(0, items.Length);
            var item = items[index];

            // Act
            var indexOf = ((SCG.IList<string>) collection).IndexOf(item);

            // Assert
            Assert.That(indexOf, Is.EqualTo(index));
        }

        [Test]
        public void SCGIListIndexOf_DuplicateItems_Zero()
        {
            // Arrange
            var count = GetCount(Random);
            var item = Random.GetString();
            var items = item.Repeat(count);
            var collection = GetList(items);

            // Act
            var indexOf = ((SCG.IList<string>) collection).IndexOf(item);

            // Assert
            Assert.That(indexOf, Is.Zero);
        }

        [Test]
        public void SCGIListIndexOf_CollectionWithDuplicateItems_FirstIndex()
        {
            // Arrange
            var count = GetCount(Random);
            var item = Random.GetString();
            var items = GetStrings(Random).WithRepeatedItem(() => item, count, Random);
            var collection = GetList(items);
            var index = collection.ToArray().IndexOf(item);

            // Act
            var indexOf = ((SCG.IList<string>) collection).IndexOf(item);

            // Assert
            Assert.That(indexOf, Is.EqualTo(index));
        }

        [Test]
        public void SCGIListIndexOf_RandomCollectionNewItem_GetsTildeIndex()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items);
            var item = GetLowercaseString(Random);

            // Act
            var expectedIndex = ~collection.IndexOf(item);
            collection.Add(item);
            var indexOf = ((SCG.IList<string>) collection).IndexOf(item);

            // Assert
            Assert.That(indexOf, Is.EqualTo(expectedIndex));
        }

        #endregion

        #region RemoveAt(int)

        [Test]
        public void SCGIListRemoveAt_EmptyCollection_ViolatesPrecondtion()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act & Assert
            Assert.That(() => ((SCG.IList<string>) collection).RemoveAt(0), Violates.Precondition);
        }

        [Test]
        public void SCGIListRemoveAt_NegativeIndex_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(int.MinValue, 0);

            // Act & Assert
            Assert.That(() => ((SCG.IList<string>) collection).RemoveAt(index), Violates.Precondition);
        }

        [Test]
        public void SCGIListRemoveAt_IndexOfCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = collection.Count;

            // Act & Assert
            Assert.That(() => ((SCG.IList<string>) collection).RemoveAt(index), Violates.Precondition);
        }

        [Test]
        public void SCGIListRemoveAt_IndexLargerThanCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(collection.Count + 1, int.MaxValue);

            // Act & Assert
            Assert.That(() => ((SCG.IList<string>) collection).RemoveAt(index), Violates.Precondition);
        }

        [Test]
        public void SCGIListRemoveAt_RemoveDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(0, collection.Count);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            ((SCG.IList<string>) collection).RemoveAt(index);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void SCGIListRemoveAt_RandomCollection_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(0, collection.Count);
            var item = collection[index];
            var expectedEvents = new[] {
                RemovedAt(item, index, collection),
                Removed(item, 1, collection),
                Changed(collection),
            };

            // Act & Assert
            Assert.That(() => ((SCG.IList<string>) collection).RemoveAt(index), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void SCGIListRemoveAt_RandomCollection_ItemAtIndex()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(0, collection.Count);
            var expectedItem = collection[index];
            var array = collection.SkipIndex(index).ToArray();

            // Act
            ((SCG.IList<string>) collection).RemoveAt(index);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCGIListRemoveAt_RandomCollectionWithNullRemoveNull_Null()
        {
            Run.If(AllowsNull);
            // Arrange
            var items = GetStrings(Random).WithNull(Random);
            var collection = GetList(items, allowsNull: true);
            var index = collection.IndexOf(null);
            var array = collection.SkipIndex(index).ToArray();

            // Act
            ((SCG.IList<string>) collection).RemoveAt(index);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCGIListRemoveAt_SingleItemCollection_Item()
        {
            // Arrange
            var item = Random.GetString();
            var itemArray = new[] { item };
            var collection = GetList(itemArray);

            // Act
            ((SCG.IList<string>) collection).RemoveAt(0);

            // Assert
            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void SCGIListRemoveAt_RemoveFirstItem_Removed()
        {
            // Arrange
            var collection = GetStringList(Random);
            var items = collection.ToArray();
            var index = 0;
            var firstItem = collection[index];

            // Act
            ((SCG.IList<string>) collection).RemoveAt(index);

            // Assert
            Assert.That(collection, Is.EqualTo(items.Skip(1)).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void SCGIListRemoveAt_RemoveLastItem_Removed()
        {
            // Arrange
            var collection = GetStringList(Random);
            var count = collection.Count;
            var items = collection.ToArray();
            var index = count - 1;
            var lastItem = collection[index];

            // Act
            ((SCG.IList<string>) collection).RemoveAt(index);

            // Assert
            Assert.That(collection, Is.EqualTo(items.Take(index)).Using(ReferenceEqualityComparer));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCGIListRemoveAt_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SCGIListRemoveAt_DuplicatesByCounting_Fail()
        {
            // TODO: Only one item is replaced based on AllowsDuplicates/DuplicatesByCounting
            Assert.That(DuplicatesByCounting, Is.False, "Tests have not been written yet");
        }

        #endregion

        #endregion

        #region IList<T>

        #region Properties

        #region First

        [Test]
        public void First_EmptyCollection_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act & Assert
            Assert.That(() => collection.First, Violates.PreconditionSaying(CollectionMustBeNonEmpty));
        }

        [Test]
        public void First_SingleItemCollection_Item()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item };
            var collection = GetList(items);

            // Act
            var first = collection.First;

            // Assert
            Assert.That(first, Is.SameAs(item));
        }

        [Test]
        public void First_RandomCollection_FirstItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = collection.First();

            // Act
            var first = collection.First;

            // Assert
            Assert.That(first, Is.EqualTo(item));
        }

        [Test]
        public void First_RandomCollectionStartingWithNull_Null()
        {
            Run.If(AllowsNull);

            // Arrange
            var items = new string[] { null }.Concat(GetStrings(Random));
            var collection = GetList(items, allowsNull: true);

            // Act
            var first = collection.First;

            // Assert
            Assert.That(first, Is.Null);
        }

        #endregion

        #region Last

        [Test]
        public void Last_EmptyCollection_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act & Assert
            Assert.That(() => collection.Last, Violates.PreconditionSaying(CollectionMustBeNonEmpty));
        }

        [Test]
        public void Last_SingleItemCollection_Item()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item };
            var collection = GetList(items);

            // Act
            var last = collection.Last;

            // Assert
            Assert.That(last, Is.SameAs(item));
        }

        [Test]
        public void Last_RandomCollection_LastItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = collection.Last();

            // Act
            var last = collection.Last;

            // Assert
            Assert.That(last, Is.EqualTo(item));
        }

        [Test]
        public void Last_RandomCollectionEndingWithNull_Null()
        {
            Run.If(AllowsNull);
            // Arrange
            var items = GetStrings(Random).Append(null);
            var collection = GetList(items, allowsNull: true);

            // Act
            var last = collection.Last;

            // Assert
            Assert.That(last, Is.Null);
        }

        #endregion

        #region this[int]

        [Test]
        public void ItemSet_NegativeIndex_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(int.MinValue, 0);
            var item = Random.GetString();

            // Act & Assert
            Assert.That(() => collection[index] = item, Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void ItemSet_IndexOfCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = collection.Count;
            var item = Random.GetString();

            // Act & Assert
            Assert.That(() => collection[index] = item, Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void ItemSet_IndexLargerThanCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(collection.Count + 1, int.MaxValue);
            var item = Random.GetString();

            // Act & Assert
            Assert.That(() => collection[index] = item, Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void ItemSet_EmptyCollection_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var index = 0;
            var item = Random.GetString();

            // Act & Assert
            Assert.That(() => collection[index] = item, Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void ItemSet_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);
            var index = GetIndex(collection, Random);

            // Act & Assert
            Assert.That(() => collection[index] = null, Violates.PreconditionSaying(ItemMustBeNonNull));
        }

        [Test]
        public void ItemSet_RandomCollectionSetDuplicate_ViolatesPrecondition()
        {
            Run.If(!AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random);
            var item = collection.ToArray().Choose(Random);

            // Act & Assert
            Assert.That(() => collection[index] = item, Violates.PreconditionSaying(CollectionMustAllowDuplicates));
        }

        [Test]
        public void ItemSet_RandomCollectionSetDuplicate_Inserted()
        {
            Run.If(AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random);
            var item = collection.ToArray().Choose(Random);

            // Act
            collection[index] = item;

            // Assert
            Assert.That(collection[index], Is.SameAs(item));
        }

        [Test]
        public void ItemSet_AllowsNull_Null()
        {
            Run.If(AllowsNull);
            // Arrange
            var collection = GetStringList(Random, allowsNull: true);
            var index = GetIndex(collection, Random);
            var array = collection.ToArray();
            array[index] = null;

            // Act
            collection[index] = null;

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void ItemSet_RandomCollectionIndexZero_FirstItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = 0;
            var array = collection.ToArray();
            array[index] = item;

            // Act
            collection[index] = item;

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void ItemSet_RandomCollectionIndexCountMinusOne_LastItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = collection.Count - 1;
            var array = collection.ToArray();
            array[index] = item;

            // Act
            collection[index] = item;

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void ItemSet_RandomCollectionRandomIndex_ItemAtPositionIndex()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = GetIndex(collection, Random);
            var array = collection.ToArray();
            array[index] = item;

            // Act
            collection[index] = item;

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void ItemSet_RandomCollectionRandomIndex_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = GetIndex(collection, Random);
            var oldItem = collection[index];
            var expectedEvents = new[] {
                RemovedAt(oldItem, index, collection),
                Removed(oldItem, 1, collection),
                Inserted(item, index, collection),
                Added(item, 1, collection),
                Changed(collection),
            };

            // Act & Assert
            Assert.That(() => collection[index] = item, Raises(expectedEvents).For(collection));
        }

        [Test]
        public void ItemSet_SetDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = GetIndex(collection, Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection[index] = item;

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void ItemSet_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region Offset

        [Test]
        public void Offset_List_IsZero()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(collection.Offset, Is.Zero);
        }

        [Test]
        public void Offset_EmptyView_IsNonNegative()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetEmptyView(collection);

            // Act & Assert
            Assert.That(view.Offset, Is.Not.Negative);
        }

        [Test]
        public void Offset_RandomView_ItemCountBeforeView()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var first = view.First();

            // Act & Assert 
            //Assert.That(view.First(), Is.SameAs(collection.Skip(view.Offset).First()));
            Assert.That(view.Offset, Is.EqualTo(collection.TakeWhile(x => x != first).Count()));
        }

        #endregion

        #region Underlying        

        [Test]
        public void Underlying_List_IsNull()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(collection.Underlying, Is.Null);
        }

        [Test]
        public void Underlying_EmptyView_IsList()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetEmptyView(collection);

            // Act & Assert
            Assert.That(view.Underlying, Is.SameAs(collection));
        }

        [Test]
        public void Underlying_RandomView_IsList()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);

            // Act & Assert 
            Assert.That(view.Underlying, Is.SameAs(collection));
        }

        [Test]
        public void Underlying_ViewOfView_IsList()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var subView = GetView(view);

            // Act & Assert 
            Assert.That(subView.Underlying, Is.SameAs(collection));
        }

        #endregion

        #endregion

        #region Methods

        #region Insert(int, T)

        [Test]
        public void Insert_NegativeIndex_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(int.MinValue, 0);
            var item = Random.GetString();

            // Act & Assert
            Assert.That(() => collection.Insert(index, item), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void Insert_IndexLargerThanCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(collection.Count + 1, int.MaxValue);
            var item = Random.GetString();

            // Act & Assert
            Assert.That(() => collection.Insert(index, item), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void Insert_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);
            var index = GetIndex(collection, Random, true);

            // Act & Assert
            Assert.That(() => collection.Insert(index, null), Violates.PreconditionSaying(ItemMustBeNonNull));
        }

        [Test]
        public void Insert_RandomCollectionSetDuplicate_ViolatesPrecondition()
        {
            Run.If(!AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var item = collection.ToArray().Choose(Random);

            // Act & Assert
            Assert.That(() => collection.Insert(index, item), Violates.PreconditionSaying(CollectionMustAllowDuplicates));
        }

        [Test]
        public void Insert_RandomCollectionSetDuplicate_Inserted()
        {
            Run.If(AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var item = collection.ToArray().Choose(Random);

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection[index], Is.SameAs(item));
        }

        [Test]
        public void Insert_EmptyCollection_SingleItemCollection()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var index = 0;
            var item = Random.GetString();
            var array = new[] { item };

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Insert_IndexOfCount_Appended()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = collection.Count;
            var item = Random.GetString();
            var array = collection.Append(item).ToArray();

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Insert_AllowsNull_Null()
        {
            Run.If(AllowsNull);
            // Arrange
            var collection = GetStringList(Random, allowsNull: true);
            var index = GetIndex(collection, Random, true);
            var array = collection.ToArray().InsertItem(index, null);

            // Act
            collection.Insert(index, null);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Insert_RandomCollectionIndexZero_FirstItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = 0;
            var array = collection.ToArray().InsertItem(index, item);

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Insert_RandomCollectionIndexCountMinusOne_LastItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = collection.Count - 1;
            var array = collection.ToArray().InsertItem(index, item);

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Insert_RandomCollectionRandomIndex_ItemAtPositionIndex()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = GetIndex(collection, Random, true);
            var expected = collection.InsertItem(index, item);

            // Act
            collection.Insert(index, item);

            // Assert
            Assert.That(collection, Is.EqualTo(expected).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Insert_RandomCollectionRandomIndex_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = GetIndex(collection, Random, true);
            var expectedEvents = new[] {
                Inserted(item, index, collection),
                Added(item, 1, collection),
                Changed(collection),
            };

            // Act & Assert
            Assert.That(() => collection.Insert(index, item), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void Insert_InsertDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var index = GetIndex(collection, Random, true);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Insert(index, item);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void Insert_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        //[Category("Unfinished")]
        //[Ignore("Unfinished")]
        public void Insert_FixedSizeCollection_ViolatesPreconditionSayingCollectionMustBeNonFixedSize()
        {
            Run.If(IsFixedSize);

            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var item = GetUppercaseString(Random);

            // Act & Assert
            Assert.That(() => collection.Insert(index, item), Violates.PreconditionSaying(CollectionMustBeNonFixedSize));
            //Assert.That(IsFixedSize, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region InsertFirst(int, T)

        [Test]
        public void InsertFirst_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);

            // Act & Assert
            Assert.That(() => collection.InsertFirst(null), Violates.PreconditionSaying(ItemMustBeNonNull));
        }

        [Test]
        public void InsertFirst_RandomCollectionInsertExistingFirst_ViolatesPrecondition()
        {
            Run.If(!AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var item = collection.ToArray().Choose(Random);

            // Act & Assert
            Assert.That(() => collection.InsertFirst(item), Violates.PreconditionSaying(CollectionMustAllowDuplicates));
        }

        [Test]
        public void InsertFirst_RandomCollectionInsertExistingFirst_InsertedFirst()
        {
            Run.If(AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var item = collection.ToArray().Choose(Random);
            var array = collection.ToArray().InsertItem(0, item);

            // Act
            collection.InsertFirst(item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertFirst_EmptyCollection_SingleItemCollection()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var item = Random.GetString();
            var array = new[] { item };

            // Act
            collection.InsertFirst(item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertFirst_AllowsNull_Null()
        {
            Run.If(AllowsNull);
            // Arrange
            var collection = GetStringList(Random, allowsNull: true);
            var array = collection.ToArray().InsertItem(0, null);

            // Act
            collection.InsertFirst(null);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertFirst_RandomCollectionInsertFirst_InsertedFirst()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var array = collection.ToArray().InsertItem(0, item);

            // Act
            collection.InsertFirst(item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertFirst_RandomCollectionInsertFirst_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var expectedEvents = new[] {
                Inserted(item, 0, collection),
                Added(item, 1, collection),
                Changed(collection)
            };

            // Act & Assert
            Assert.That(() => collection.InsertFirst(item), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void InsertFirst_InsertFirstDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.InsertFirst(item);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void InsertFirst_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        //[Category("Unfinished")]
        //[Ignore("Unfinished")]
        public void InsertFirst_FixedSizeCollection_ViolatesPreconditionSayingCollectionMustBeNonFixedSize()
        {
            Run.If(IsFixedSize);

            // Arrange
            var collection = GetStringList(Random);
            var item = GetUppercaseString(Random);

            // Act & Assert
            Assert.That(() => collection.InsertFirst(item), Violates.PreconditionSaying(CollectionMustBeNonFixedSize));
            //Assert.That(IsFixedSize, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region InsertLast(int, T)

        [Test]
        public void InsertLast_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);

            // Act & Assert
            Assert.That(() => collection.InsertLast(null), Violates.PreconditionSaying(ItemMustBeNonNull));
        }

        [Test]
        public void InsertLast_RandomCollectionInsertExistingLast_ViolatesPrecondition()
        {
            Run.If(!AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var item = collection.ToArray().Choose(Random);

            // Act & Assert
            Assert.That(() => collection.InsertLast(item), Violates.PreconditionSaying(CollectionMustAllowDuplicates));
        }

        [Test]
        public void InsertLast_RandomCollectionInsertExistingLast_InsertedLast()
        {
            Run.If(AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var item = collection.ToArray().Choose(Random);
            var array = collection.Append(item).ToArray();

            // Act
            collection.InsertLast(item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertLast_EmptyCollection_SingleItemCollection()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var item = Random.GetString();
            var array = new[] { item };

            // Act
            collection.InsertLast(item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertLast_AllowsNull_Null()
        {
            Run.If(AllowsNull);
            // Arrange
            var collection = GetStringList(Random, allowsNull: true);
            var array = collection.Append(null).ToArray();

            // Act
            collection.InsertLast(null);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertLast_RandomCollectionInsertLast_InsertedLast()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var array = collection.Append(item).ToArray();

            // Act
            collection.InsertLast(item);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertLast_RandomCollectionInsertLast_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();
            var expectedEvents = new[] {
                Inserted(item, collection.Count, collection),
                Added(item, 1, collection),
                Changed(collection)
            };

            // Act & Assert
            Assert.That(() => collection.InsertLast(item), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void InsertLast_InsertLastDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = Random.GetString();

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.InsertLast(item);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void InsertLast_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        //[Category("Unfinished")]
        //[Ignore("Unfinished")]
        public void InsertLast_FixedSizeCollection_ViolatesPreconditionSayingCollectionMustBeNonFixedSize()
        {
            Run.If(IsFixedSize);

            // Arrange
            var collection = GetStringList(Random);
            var item = GetUppercaseString(Random);

            // Act & Assert
            Assert.That(() => collection.InsertLast(item), Violates.PreconditionSaying(CollectionMustBeNonFixedSize));
            //Assert.That(IsFixedSize, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region InsertRange(int, IEnumerable<T>)

        [Test]
        public void InsertRange_NegativeIndex_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(int.MinValue, 0);
            var items = GetStrings(Random);

            // Act & Assert
            Assert.That(() => collection.InsertRange(index, items), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void InsertRange_IndexLargerThanCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(collection.Count + 1, int.MaxValue);
            var items = GetStrings(Random);

            // Act & Assert
            Assert.That(() => collection.InsertRange(index, items), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void InsertRange_NullEnumerable_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);
            var index = GetIndex(collection, Random, true);

            // Act & Assert
            Assert.That(() => collection.InsertRange(index, null), Violates.PreconditionSaying(ArgumentMustBeNonNull));
        }

        [Test]
        public void InsertRange_DisallowsNullsInEnumerable_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);
            var index = GetIndex(collection, Random, true);
            var items = GetStrings(Random).WithNull(Random);

            // Act & Assert
            Assert.That(() => collection.InsertRange(index, items), Violates.PreconditionSaying(ItemsMustBeNonNull));
        }

        [Test]
        public void InsertRange_RandomCollectionInsertExistingItems_ViolatesPrecondition()
        {
            Run.If(!AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var count = GetCount(Random);
            var items = collection.ShuffledCopy(Random).Take(count);

            // Act & Assert
            Assert.That(() => collection.InsertRange(index, items), Violates.PreconditionSaying(CollectionMustAllowDuplicates));
        }

        [Test]
        public void InsertRange_RandomCollectionInsertExistingItem_ViolatesPrecondition()
        {
            Run.If(!AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var item = collection.ToArray().Choose(Random);
            var items = GetStrings(Random).WithRepeatedItem(() => item, 1, Random);

            // Act & Assert
            Assert.That(() => collection.InsertRange(index, items), Violates.PreconditionSaying(CollectionMustAllowDuplicates));
        }

        [Test]
        public void InsertRange_RandomCollectionInsertExistingItems_InsertedRange()
        {
            Run.If(AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var count = GetCount(Random);
            var items = collection.ShuffledCopy(Random).Take(count).ToArray();
            var array = collection.ToArray().InsertItems(index, items);

            // Act
            collection.InsertRange(index, items);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertRange_RandomCollectionInsertExistingItem_InsertedRange()
        {
            Run.If(AllowsDuplicates);

            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var item = collection.ToArray().Choose(Random);
            var items = GetStrings(Random).WithRepeatedItem(() => item, 1, Random);
            var array = collection.ToArray().InsertItems(index, items);

            // Act
            collection.InsertRange(index, items);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertRange_EmptyCollection_Items()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var index = 0;
            var items = GetStrings(Random);

            // Act
            collection.InsertRange(index, items);

            // Assert
            Assert.That(collection, Is.EqualTo(items).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertRange_IndexOfCount_Appended()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = collection.Count;
            var items = GetStrings(Random);
            var array = collection.Concat(items).ToArray();

            // Act
            collection.InsertRange(index, items);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertRange_AllowsNull_InsertedRangeWithNull()
        {
            Run.If(AllowsNull);
            // Arrange
            var collection = GetStringList(Random, allowsNull: true);
            var index = GetIndex(collection, Random, true);
            var items = GetStrings(Random).WithNull(Random);
            var array = collection.ToArray().InsertItems(index, items);

            // Act
            collection.InsertRange(index, items);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertRange_RandomCollectionIndexZero_FirstItems()
        {
            // Arrange
            var collection = GetStringList(Random);
            var items = GetStrings(Random);
            var index = 0;
            var array = collection.ToArray().InsertItems(index, items);

            // Act
            collection.InsertRange(index, items);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertRange_RandomCollectionIndexCountMinusOne_LastItem()
        {
            // Arrange
            var collection = GetStringList(Random);
            var items = GetStrings(Random);
            var index = collection.Count - 1;
            var array = collection.ToArray().InsertItems(index, items);

            // Act
            collection.InsertRange(index, items);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertRange_RandomCollectionRandomIndex_ItemAtPositionIndex()
        {
            // Arrange
            var collection = GetStringList(Random);
            var items = GetStrings(Random);
            var index = GetIndex(collection, Random, true);
            var array = collection.ToArray().InsertItems(index, items);

            // Act
            collection.InsertRange(index, items);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertRange_RandomCollectionRandomIndex_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var items = GetStrings(Random);
            var index = GetIndex(collection, Random, true);
            var expectedEvents = items.SelectMany((item, i) => new[] { Inserted(item, index + i, collection), Added(item, 1, collection) }).Append(Changed(collection)).ToArray();

            // Act & Assert
            Assert.That(() => collection.InsertRange(index, items), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void InsertRange_InsertEmptyRange_Nothing()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var items = NoStrings;
            var array = collection.ToArray();

            // Act
            collection.InsertRange(index, items);

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertRange_InsertEmptyRange_RaisesNoEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var items = NoStrings;

            // Act & Assert
            Assert.That(() => collection.InsertRange(index, items), RaisesNoEventsFor(collection));
        }

        [Test]
        public void InsertRange_InsertEmptyRangeDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var items = NoStrings;

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.InsertRange(index, items);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void InsertRange_BadEnumerable_ThrowsExceptionButCollectionDoesNotChange()
        {
            // Arrange
            var collection = GetStringList(Random, ReferenceEqualityComparer, allowsNull: true);
            var index = GetIndex(collection, Random, true);
            var badEnumerable = GetStrings(Random).AsBadEnumerable();
            var array = collection.ToArray();

            // Act & Assert
            Assert.That(() => collection.InsertRange(index, badEnumerable), Throws.TypeOf<BadEnumerableException>());
            Assert.That(collection, Is.EquivalentTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void InsertRange_InsertRangeDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var items = GetStrings(Random);
            var index = GetIndex(collection, Random, true);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.InsertRange(index, items);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void InsertRange_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        //[Category("Unfinished")]
        //[Ignore("Unfinished")]
        public void InsertRange_FixedSizeCollection_Fail_ViolatesPreconditionSayingCollectionMustBeNonFixedSize()
        {
            Run.If(IsFixedSize);

            var collection = GetStringList(Random);
            var index = GetIndex(collection, Random, true);
            var items = GetStrings(Random);

            // Act & Assert
            Assert.That(() => collection.InsertRange(index, items), Violates.PreconditionSaying(CollectionMustBeNonFixedSize));
            //Assert.That(IsFixedSize, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region IsSorted()

        [Test]
        public void IsSorted_EmptyCollection_True()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act
            var isSorted = collection.IsSorted();

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSorted_SingleItemCollection_True()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item };
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted();

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSorted_TwoItemsAscending_True()
        {
            // Arrange
            var items = new[] { Random.Next(int.MinValue, 0), Random.Next() };
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted();

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSorted_TwoItemsDescending_False()
        {
            // Arrange
            var items = new[] { Random.Next(), Random.Next(int.MinValue, 0) };
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted();

            // Assert
            Assert.That(isSorted, Is.False);
        }

        [Test]
        public void IsSorted_TwoEqualItems_True()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item, string.Copy(item) };
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted();

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSorted_EqualItems_True()
        {
            // Arrange
            var count = GetCount(Random);
            var item = Random.GetString();
            var items = TestHelper.Repeat(() => string.Copy(item), count);
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted();

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSorted_NonComparables_ThrowsArgumentException()
        {
            // Arrange
            var items = GetNonComparables(Random);
            var collection = GetList(items);

            // Act & Assert
            // TODO: This is not the exception stated in the documentation!
            Assert.That(() => collection.IsSorted(), Throws.ArgumentException.Because("At least one object must implement IComparable."));
        }

        [Test]
        public void IsSorted_Comparables_ThrowsNothing()
        {
            // Arrange
            var items = GetComparables(Random);
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.IsSorted(), Throws.Nothing);
        }

        [Test]
        public void IsSorted_NonDescendingRandomCollection_True()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count);
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted();

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSorted_Descending_False()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem + 1, previousItem + maxGap)), count).Reverse();
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted();

            // Assert
            Assert.That(isSorted, Is.False);
        }

        [Test]
        public void IsSorted_AllButLastAreSorted_False()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count).Append(new Comparable(previousItem - 1));
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted();

            // Assert
            Assert.That(isSorted, Is.False);
        }

        #endregion

        #region IsSorted(Comparison<T>)

        [Test]
        public void IsSortedComparison_NullComparison_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => collection.IsSorted((Comparison<string>) null), Violates.PreconditionSaying(ArgumentMustBeNonNull));
        }

        [Test]
        public void IsSortedComparison_EmptyCollection_True()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            Comparison<string> comparison = string.Compare;

            // Act
            var isSorted = collection.IsSorted(comparison);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedComparison_SingleItemCollection_True()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item };
            var collection = GetList(items);
            Comparison<string> comparison = string.Compare;

            // Act
            var isSorted = collection.IsSorted(comparison);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedComparison_TwoItemsAscending_True()
        {
            // Arrange
            var items = new[] { Random.Next(int.MinValue, 0), Random.Next() };
            var collection = GetList(items);
            Comparison<int> comparison = (x, y) => x.CompareTo(y);

            // Act
            var isSorted = collection.IsSorted(comparison);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedComparison_TwoItemsDescending_False()
        {
            // Arrange
            var items = new[] { Random.Next(), Random.Next(int.MinValue, 0) };
            var collection = GetList(items);
            Comparison<int> comparison = (x, y) => x.CompareTo(y);

            // Act
            var isSorted = collection.IsSorted(comparison);

            // Assert
            Assert.That(isSorted, Is.False);
        }

        [Test]
        public void IsSortedComparison_TwoEqualItems_True()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item, string.Copy(item) };
            var collection = GetList(items);
            Comparison<string> comparison = string.Compare;

            // Act
            var isSorted = collection.IsSorted(comparison);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedComparison_EqualItems_True()
        {
            // Arrange
            var count = GetCount(Random);
            var item = Random.GetString();
            var items = TestHelper.Repeat(() => string.Copy(item), count);
            var collection = GetList(items);
            Comparison<string> comparison = string.Compare;

            // Act
            var isSorted = collection.IsSorted(comparison);

            // Assert
            Assert.That(isSorted, Is.True);
        }


        [Test]
        public void IsSortedComparison_Comparables_ThrowsNothing()
        {
            // Arrange
            var items = GetComparables(Random);
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.IsSorted(_nonComparableComparison), Throws.Nothing);
        }

        [Test]
        public void IsSortedComparison_NonDescendingRandomCollection_True()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count);
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted(_nonComparableComparison);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedComparison_Descending_False()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem + 1, previousItem + maxGap)), count).Reverse();
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted(_nonComparableComparison);

            // Assert
            Assert.That(isSorted, Is.False);
        }

        [Test]
        public void IsSortedComparison_AllButLastAreSorted_False()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count).Append(new Comparable(previousItem - 1));
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted(_nonComparableComparison);

            // Assert
            Assert.That(isSorted, Is.False);
        }

        #endregion

        #region IsSorted(IComparer<T>)

        [Test]
        public void IsSortedIComparer_NullComparer_DoesNotViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => collection.IsSorted((SCG.IComparer<string>) null), Does.Not.ViolatePrecondition());
        }

        [Test]
        public void IsSortedIComparer_EmptyCollection_True()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var comparer = SCG.Comparer<string>.Default;

            // Act
            var isSorted = collection.IsSorted(comparer);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedIComparer_SingleItemCollection_True()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item };
            var collection = GetList(items);
            var comparer = SCG.Comparer<string>.Default;

            // Act
            var isSorted = collection.IsSorted(comparer);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedIComparer_TwoItemsAscending_True()
        {
            // Arrange
            var items = new[] { Random.Next(int.MinValue, 0), Random.Next() };
            var collection = GetList(items);
            var comparer = SCG.Comparer<int>.Default;

            // Act
            var isSorted = collection.IsSorted(comparer);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedIComparer_TwoItemsDescending_False()
        {
            // Arrange
            var items = new[] { Random.Next(), Random.Next(int.MinValue, 0) };
            var collection = GetList(items);
            var comparer = SCG.Comparer<int>.Default;

            // Act
            var isSorted = collection.IsSorted(comparer);

            // Assert
            Assert.That(isSorted, Is.False);
        }

        [Test]
        public void IsSortedIComparer_TwoEqualItems_True()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item, string.Copy(item) };
            var collection = GetList(items);
            var comparer = SCG.Comparer<string>.Default;

            // Act
            var isSorted = collection.IsSorted(comparer);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedIComparer_EqualItems_True()
        {
            // Arrange
            var count = GetCount(Random);
            var item = Random.GetString();
            var items = TestHelper.Repeat(() => string.Copy(item), count);
            var collection = GetList(items);
            var comparer = SCG.Comparer<string>.Default;

            // Act
            var isSorted = collection.IsSorted(comparer);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedIComparer_NonComparables_ThrowsArgumentException()
        {
            // Arrange
            var items = GetNonComparables(Random);
            var collection = GetList(items);

            // Act & Assert
            // TODO: This is not the exception stated in the documentation!
            Assert.That(() => collection.IsSorted((SCG.IComparer<NonComparable>) null), Throws.ArgumentException.Because("At least one object must implement IComparable."));
        }

        [Test]
        public void IsSortedIComparer_Comparables_ThrowsNothing()
        {
            // Arrange
            var items = GetComparables(Random);
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.IsSorted(NonComparableComparer), Throws.Nothing);
        }

        [Test]
        public void IsSortedIComparer_NonDescendingRandomCollection_True()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count);
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted(NonComparableComparer);

            // Assert
            Assert.That(isSorted, Is.True);
        }

        [Test]
        public void IsSortedIComparer_Descending_False()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem + 1, previousItem + maxGap)), count).Reverse();
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted(NonComparableComparer);

            // Assert
            Assert.That(isSorted, Is.False);
        }

        [Test]
        public void IsSortedIComparer_AllButLastAreSorted_False()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count).Append(new Comparable(previousItem - 1));
            var collection = GetList(items);

            // Act
            var isSorted = collection.IsSorted(NonComparableComparer);

            // Assert
            Assert.That(isSorted, Is.False);
        }

        #endregion

        #region RemoveFirst()

        [Test]
        public void RemoveFirst_EmptyCollection_ViolatesPrecondtion()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act & Assert
            Assert.That(() => collection.RemoveFirst(), Violates.PreconditionSaying(CollectionMustBeNonEmpty));
        }

        [Test]
        public void RemoveFirst_RemoveFirstDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.RemoveFirst();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void RemoveFirst_RandomCollection_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = collection.First;
            var expectedEvents = new[] {
                RemovedAt(item, 0, collection),
                Removed(item, 1, collection),
                Changed(collection)
            };

            // Act & Assert
            Assert.That(() => collection.RemoveFirst(), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void RemoveFirst_RandomCollectionWithNullRemoveNull_Null()
        {
            Run.If(AllowsNull);
            // Arrange
            var collection = GetStringList(Random, allowsNull: true);
            collection[0] = null;

            // Act
            var removeFirst = collection.RemoveFirst();

            // Assert
            Assert.That(removeFirst, Is.Null);
        }

        [Test]
        public void RemoveFirst_SingleItemCollection_Empty()
        {
            // Arrange
            var item = Random.GetString();
            var itemArray = new[] { item };
            var collection = GetList(itemArray);

            // Act
            var removeFirst = collection.RemoveFirst();

            // Assert
            Assert.That(removeFirst, Is.SameAs(item));
            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void RemoveFirst_RemoveFirstItem_Removed()
        {
            // Arrange
            var collection = GetStringList(Random);
            var firstItem = collection.First;
            var array = collection.Skip(1).ToArray();

            // Act
            var removeFirst = collection.RemoveFirst();

            // Assert
            Assert.That(removeFirst, Is.SameAs(firstItem));
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void RemoveFirst_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        //[Category("Unfinished")]
        //[Ignore("Unfinished")]
        public void RemoveFirst_FixedSizeCollection_ViolatesPreconditionSayingCollectionMustBeNonFixedSize()
        {
            Run.If(IsFixedSize);

            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => collection.RemoveFirst(), Violates.PreconditionSaying(CollectionMustBeNonFixedSize));
            //Assert.That(IsFixedSize, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region RemoveLast()

        [Test]
        public void RemoveLast_EmptyCollection_ViolatesPrecondtion()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act & Assert
            Assert.That(() => collection.RemoveLast(), Violates.PreconditionSaying(CollectionMustBeNonEmpty));
        }

        [Test]
        public void RemoveLast_RemoveLastDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.RemoveLast();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void RemoveLast_RandomCollection_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = collection.Last;
            var expectedEvents = new[] {
                RemovedAt(item, collection.Count - 1, collection),
                Removed(item, 1, collection),
                Changed(collection)
            };

            // Act & Assert
            Assert.That(() => collection.RemoveLast(), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void RemoveLast_RandomCollectionWithNullRemoveNull_Null()
        {
            Run.If(AllowsNull);
            // Arrange
            var collection = GetStringList(Random, allowsNull: true);
            collection[collection.Count - 1] = null;

            // Act
            var removeLast = collection.RemoveLast();

            // Assert
            Assert.That(removeLast, Is.Null);
        }

        [Test]
        public void RemoveLast_SingleItemCollection_Empty()
        {
            // Arrange
            var item = Random.GetString();
            var itemArray = new[] { item };
            var collection = GetList(itemArray);

            // Act
            var removeLast = collection.RemoveLast();

            // Assert
            Assert.That(removeLast, Is.SameAs(item));
            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void RemoveLast_RemoveLastItem_Removed()
        {
            // Arrange
            var collection = GetStringList(Random);
            var lastItem = collection.Last;
            var array = collection.Take(collection.Count - 1).ToArray();

            // Act
            var removeLast = collection.RemoveLast();

            // Assert
            Assert.That(removeLast, Is.SameAs(lastItem));
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void RemoveLast_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        [Test]
        //[Category("Unfinished")]
        //[Ignore("Unfinished")]
        public void RemoveLast_FixedSizeCollection_ViolatesPreconditionSayingCollectionMustBeNonFixedSize()
        {
            Run.If(IsFixedSize);

            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => collection.RemoveLast(), Violates.PreconditionSaying(CollectionMustBeNonFixedSize));
            //Assert.That(IsFixedSize, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region Reverse()

        [Test]
        public void Reverse_EmptyCollection_Nothing()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act
            collection.Reverse();

            // Assert
            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void Reverse_EmptyCollection_RaisesNoEvents()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act & Assert
            Assert.That(() => collection.Reverse(), RaisesNoEventsFor(collection));
        }

        [Test]
        public void Reverse_RandomCollection_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);

            var expectedEvents = new[] {
                Changed(collection)
            };

            // Act & Assert
            Assert.That(() => collection.Reverse(), Raises(expectedEvents).For(collection));
        }

        [Test]
        public void Reverse_ReverseDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Reverse();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void Reverse_ReverseDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Reverse();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void Reverse_RandomCollectionWithNull_Reversed()
        {
            Run.If(AllowsNull);

            // Arrange
            var items = GetStrings(Random).WithNull(Random);
            var collection = GetList(items, allowsNull: true);
            var reversedCollection = collection.ToArray().Reverse();

            // Act
            collection.Reverse();

            // Assert
            Assert.That(collection, Is.EqualTo(reversedCollection));
        }

        [Test]
        public void Reverse_SingleItemCollection_RaisesNoEvents()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item };
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Reverse(), RaisesNoEventsFor(collection));
        }

        [Test]
        public void Reverse_SingleItemCollectionReverseDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item };
            var collection = GetList(items);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Reverse();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void Reverse_RandomCollection_Reversed()
        {
            // Arrange
            var collection = GetStringList(Random);
            var reversedCollection = collection.ToArray().Reverse();

            // Act
            collection.Reverse();

            // Assert
            Assert.That(collection, Is.EqualTo(reversedCollection));
        }

        [Test]
        public void Reverse_ReverseReversedRandomCollection_OriginalCollection()
        {
            // Arrange
            var collection = GetStringList(Random);
            var array = collection.ToArray();

            // Act
            collection.Reverse();
            collection.Reverse();

            // Assert
            Assert.That(collection, Is.EqualTo(array).Using(ReferenceEqualityComparer));
        }

        // coll.reverse() - vs valid
        [Test]
        public void Reverse_RandomCollection_ViewIsValid()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);

            // Act
            collection.Reverse();

            // Assert
            Assert.That(view.IsValid, Is.True);
        }

        // v.reverse(), v in w, w is valid
        [Test]
        public void Reverse_View1ContainedInView2_View2IsValid()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view2 = collection.View(2, 4); // longer
            var view1 = collection.View(3, 2);

            // Act
            view1.Reverse();

            // Assert
            Assert.That(view2.IsValid, Is.True);
        }

        // v.reverse(), w in v, w is valid
        [Test]
        public void Reverse_View2ContainedInView1_View2IsValid()
        {
            // Arrange
            var items = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

            var collection = GetList(items); //GetStringList(Random);
            var view2 = collection.View(3, 2);
            var view1 = collection.View(2, 4); // longer           

            // Act
            view1.Reverse();

            // Assert
            Assert.That(view2.IsValid, Is.True);
        }

        // v.reverse(), v and w - overlaping - w no longer valid
        [Test]
        public void Reverse_View1AndView2Overlaping_View2NotValid()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view2 = collection.View(2, 4); // longer           
            var view1 = collection.View(3, 4);

            // Act
            view1.Reverse();

            // Assert
            Assert.That(view2.IsValid, Is.False);
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void Reverse_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region Shuffle()

        [Test]
        public void Shuffle_EmptyCollection_RaisesNoEvents()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act & Assert
            Assert.That(() => collection.Shuffle(), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void Shuffle_SingleItemCollection_RaisesNoEvents()
        {
            // Arrange
            var items = GetStrings(Random, 1);
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Shuffle(), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.EqualTo(items).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Shuffle_RandomCollection_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => collection.Shuffle(), RaisesCollectionChangedEventFor(collection));
        }

        [Test]
        public void Shuffle_RandomCollection_ContainsSameItems()
        {
            // Arrange
            var collection = GetStringList(Random);
            var array = collection.ToArray();

            // Act
            collection.Shuffle();

            // Assert
            Assert.That(collection, Is.EquivalentTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Shuffle_RandomCollection_NotEqualThreeTimes()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act
            collection.Shuffle();
            var result1 = collection.ToArray();
            collection.Shuffle();
            var result2 = collection.ToArray();
            collection.Shuffle();
            var result3 = collection.ToArray();

            // Assert
            Assert.That(result1, Is.EquivalentTo(result2).And.EquivalentTo(result3));
            Assert.That(result1, Is.Not.EqualTo(result2).And.Not.EqualTo(result3), "If this test fails more than once, the Shuffle() method likely contains an error");
        }

        [Test]
        public void Shuffle_ShuffleDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Shuffle();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void Shuffle_ShuffleEmptyCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Shuffle();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void Shuffle_ShuffleSingleItemCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var items = GetStrings(Random, 1);
            var collection = GetList(items);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Shuffle();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        // coll.shuffle(), view not valid
        [Test]
        public void Shuffle_RandomCollection_ViewsAreNotValid()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view1 = GetView(collection);
            var view2 = GetView(collection);

            // Act
            collection.Shuffle();

            // Assert
            // TODO: Multiple
            Assert.That(view1.IsValid, Is.False);
            Assert.That(view2.IsValid, Is.False);
        }

        // v.shuffle(), v in w, w is valid
        [Test]
        public void Shuffle_View1ContainedInView2_View2IsValid()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view2 = collection.View(2, 4); // longer
            var view1 = collection.View(3, 2);

            // Act
            view1.Shuffle();

            // Assert
            Assert.That(view2.IsValid, Is.True);
        }

        // v.shuffle(), w in v, w is not valid
        [Test]
        public void Shuffle_View2ContainedInView1_View2IsNotValid()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view2 = collection.View(3, 2);
            var view1 = collection.View(2, 4); // longer           

            // Act
            view1.Shuffle();

            // Assert
            Assert.That(view2.IsValid, Is.False);
        }

        // v.shuffle, v and w - overlaping - w no longer valid
        [Test]
        public void Shuffle_View1AndView2Overlaping_View2NotValid()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view2 = collection.View(2, 4); // longer           
            var view1 = collection.View(3, 4);

            // Act
            view1.Shuffle();

            // Assert
            Assert.That(view2.IsValid, Is.False);
        }


        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void Shuffle_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region Shuffle(Random)

        [Test]
        public void ShuffleRandom_NullArgument_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => collection.Shuffle(null), Violates.PreconditionSaying(ArgumentMustBeNonNull));
        }

        [Test]
        public void ShuffleRandom_EmptyCollection_RaisesNoEvents()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act & Assert
            Assert.That(() => collection.Shuffle(Random), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Empty);
        }

        [Test]
        public void ShuffleRandom_SingleItemCollection_RaisesNoEvents()
        {
            // Arrange
            var items = GetStrings(Random, 1);
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Shuffle(Random), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.EqualTo(items).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void ShuffleRandom_RandomCollection_RaisesExpectedEvents()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => collection.Shuffle(Random), RaisesCollectionChangedEventFor(collection));
        }

        [Test]
        public void ShuffleRandom_RandomCollection_ContainsSameItems()
        {
            // Arrange
            var collection = GetStringList(Random);
            var array = collection.ToArray();

            // Act
            collection.Shuffle(Random);

            // Assert
            Assert.That(collection, Is.EquivalentTo(array).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void ShuffleRandom_RandomCollection_NotEqualThreeTimes()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act
            collection.Shuffle(Random);
            var result1 = collection.ToArray();
            collection.Shuffle(Random);
            var result2 = collection.ToArray();
            collection.Shuffle(Random);
            var result3 = collection.ToArray();

            // Assert
            Assert.That(result1, Is.EquivalentTo(result2).And.EquivalentTo(result3));
            Assert.That(result1, Is.Not.EqualTo(result2).And.Not.EqualTo(result3), "If this test fails more than once, the Shuffle(Random) method likely contains an error");
        }

        [Test]
        public void ShuffleRandom_IsFixedSizedCollection_ThrowsNothing()
        {
            Run.If(IsFixedSize);

            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => collection.Shuffle(Random), Throws.Nothing);
        }

        [Test]
        public void ShuffleRandom_EqualCollections_SameResultUsingEqualRandom()
        {
            // Arrange
            var seed = Random.Next();
            var random1 = new Random(seed);
            var random2 = new Random(seed);
            var collection1 = GetStringList(Random);
            var collection2 = GetList(collection1);

            // Act
            collection1.Shuffle(random1);
            collection2.Shuffle(random2);

            // Assert
            Assert.That(collection1, Is.EqualTo(collection2).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void ShuffleRandom_ShuffleDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Shuffle(Random);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void ShuffleRandom_ShuffleEmptyCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Shuffle(Random);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void ShuffleRandom_ShuffleSingleItemCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var items = GetStrings(Random, 1);
            var collection = GetList(items);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Shuffle(Random);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void ShuffleRandom_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region Sort()

        [Test]
        public void Sort_EmptyCollection_Nothing()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void Sort_SingleItemCollection_Nothing()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item };
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void Sort_TwoItemsAscending_Nothing()
        {
            // Arrange
            var items = new[] { Random.Next(int.MinValue, 0), Random.Next() };
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void Sort_TwoItemsDescending_Sorted()
        {
            // Arrange
            var items = new[] { Random.Next(), Random.Next(int.MinValue, 0) };
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void Sort_TwoEqualItems_Nothing()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item, string.Copy(item) };
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void Sort_EqualItems_Nothing()
        {
            // Arrange
            var count = GetCount(Random);
            var item = Random.GetString();
            var items = TestHelper.Repeat(() => string.Copy(item), count);
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        // TODO: Test again when NUnit changes its behavior
        [Test]
        [Ignore("Because NUnit does not allow null values in Ordered: https://github.com/nunit/nunit/issues/1473")]
        public void Sort_RandomCollectionWithNull_Sorted()
        {
            // Arrange
            var items = GetStrings(Random);
            var index = Random.Next(1, items.Length);
            items[index] = null;
            var collection = GetList(items, allowsNull: true);

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void Sort_NonComparables_ThrowsArgumentException()
        {
            // Arrange
            var items = GetNonComparables(Random);
            var collection = GetList(items);

            // Act & Assert
            // TODO: This is not the exception stated in the documentation!
            Assert.That(() => collection.Sort(), Throws.ArgumentException.Because("At least one object must implement IComparable."));
        }

        [Test]
        public void Sort_NonDescendingRandomCollection_Nothing()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count);
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void Sort_Descending_Sorted()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem + 1, previousItem + maxGap)), count).Reverse();
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void Sort_AllButLastAreSorted_Sorted()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count).Append(new Comparable(previousItem - 1));
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void Sort_RandomCollection_Sorted()
        {
            // Arrange
            var items = GetStrings(Random, 10000);
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void Sort_SortEmptyCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetEmptyList<string>();

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void Sort_SortSingleItemCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var items = GetStrings(Random, 1);
            var collection = GetList(items);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void Sort_SortSortedCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count);
            var collection = GetList(items);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void Sort_SortDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort();

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        // coll.sort(), views not valid
        [Test]
        public void Sort_RandomCollection_ViewsAreNotValid()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view1 = GetView(collection);
            //var view2 = GetView(collection);

            // Act
            collection.Sort();

            // Assert
            // TODO: Multiple
            Assert.That(view1.IsValid, Is.False);
            //Assert.That(view2.IsValid, Is.False);
        }

        // v.sort(), v in w, w is valid
        [Test]
        public void Sort_View1ContainedInView2_View2IsValid()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view2 = collection.View(2, 4); // longer
            var view1 = collection.View(3, 2);

            // Act
            view1.Sort();

            // Assert
            Assert.That(view2.IsValid, Is.True);
        }

        // v.sort(), w in v, w is not valid
        [Test]
        public void Sort_View2ContainedInView1_View2IsNotValid()
        {
            // Arrange
            var collection = new HashedLinkedList<string>(new[] { "1", "8", "a3", "v4", "b5", "y6", "7", "8" }, SCG.EqualityComparer<string>.Default); //GetStringList(Random);
            var view2 = collection.View(3, 2);
            var view1 = collection.View(2, 4); // longer           

            // Act
            view1.Sort();

            // Assert
            Assert.That(view2.IsValid, Is.False);
        }

        // v.sort, v and w - overlaping - w no longer valid
        [Test]
        public void Sort_View1AndView2Overlaping_View2NotValid()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view2 = collection.View(2, 4); // longer           
            var view1 = collection.View(3, 4);

            // Act
            view1.Shuffle();

            // Assert
            Assert.That(view2.IsValid, Is.False);
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void Sort_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region Sort(Comparison<T>)

        [Test]
        public void SortComparison_NullComparison_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => collection.Sort((Comparison<string>) null), Violates.PreconditionSaying(ArgumentMustBeNonNull));
        }

        [Test]
        public void SortComparison_EmptyCollection_Nothing()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            Comparison<string> comparison = string.Compare;

            // Act & Assert
            Assert.That(() => collection.Sort(comparison), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortComparison_SingleItemCollection_Nothing()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item };
            var collection = GetList(items);
            Comparison<string> comparison = string.Compare;

            // Act & Assert
            Assert.That(() => collection.Sort(comparison), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortComparison_TwoItemsAscending_Nothing()
        {
            // Arrange
            var items = new[] { Random.Next(int.MinValue, 0), Random.Next() };
            var collection = GetList(items);
            Comparison<int> comparison = (x, y) => x.CompareTo(y);

            // Act & Assert
            Assert.That(() => collection.Sort(comparison), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortComparison_TwoItemsDescending_Sorted()
        {
            // Arrange
            var items = new[] { Random.Next(), Random.Next(int.MinValue, 0) };
            var collection = GetList(items);
            Comparison<int> comparison = (x, y) => x.CompareTo(y);

            // Act & Assert
            Assert.That(() => collection.Sort(comparison), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortComparison_TwoEqualItems_Nothing()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item, string.Copy(item) };
            var collection = GetList(items);
            Comparison<string> comparison = string.Compare;

            // Act & Assert
            Assert.That(() => collection.Sort(comparison), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortComparison_EqualItems_Nothing()
        {
            // Arrange
            var count = GetCount(Random);
            var item = Random.GetString();
            var items = TestHelper.Repeat(() => string.Copy(item), count);
            var collection = GetList(items);
            Comparison<string> comparison = string.Compare;

            // Act & Assert
            Assert.That(() => collection.Sort(comparison), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        // TODO: Test again when NUnit changes its behavior
        [Test]
        [Ignore("Because NUnit does not allow null values in Ordered: https://github.com/nunit/nunit/issues/1473")]
        public void SortComparison_RandomCollectionWithNull_Sorted()
        {
            // Arrange
            var items = GetStrings(Random);
            var index = Random.Next(1, items.Length);
            items[index] = null;
            var collection = GetList(items, allowsNull: true);
            Comparison<string> comparison = string.Compare;

            // Act & Assert
            Assert.That(() => collection.Sort(comparison), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortComparison_NonDescendingRandomCollection_Nothing()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count);
            var collection = GetList(items);


            // Act & Assert
            Assert.That(() => collection.Sort(_nonComparableComparison), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortComparison_Descending_Sorted()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem + 1, previousItem + maxGap)), count).Reverse();
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(_nonComparableComparison), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortComparison_AllButLastAreSorted_Sorted()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count).Append(new Comparable(previousItem - 1));
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(_nonComparableComparison), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortComparison_RandomCollection_Sorted()
        {
            // Arrange
            var items = GetStrings(Random, 10000);
            var collection = GetList(items);
            Comparison<string> comparison = string.Compare;

            // Act & Assert
            Assert.That(() => collection.Sort(comparison), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortComparison_SortEmptyCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            Comparison<string> comparison = string.Compare;

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort(comparison);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void SortComparison_SortSingleItemCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var items = GetStrings(Random, 1);
            var collection = GetList(items);
            Comparison<string> comparison = string.Compare;

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort(comparison);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void SortComparison_SortSortedCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count);
            var collection = GetList(items);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort(_nonComparableComparison);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void SortComparison_SortDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            Comparison<string> comparison = string.Compare;

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort(comparison);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SortComparison_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region Sort(IComparer<T>)

        [Test]
        public void SortIComparer_NullComparison_DoesNotViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            Assert.That(() => collection.Sort((SCG.IComparer<string>) null), Does.Not.ViolatePrecondition());
        }

        [Test]
        public void SortIComparer_EmptyCollection_Nothing()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var comparer = SCG.Comparer<string>.Default;

            // Act & Assert
            Assert.That(() => collection.Sort(comparer), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortIComparer_SingleItemCollection_Nothing()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item };
            var collection = GetList(items);
            var comparer = SCG.Comparer<string>.Default;

            // Act & Assert
            Assert.That(() => collection.Sort(comparer), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortIComparer_TwoItemsAscending_Nothing()
        {
            // Arrange
            var items = new[] { Random.Next(int.MinValue, 0), Random.Next() };
            var collection = GetList(items);
            var comparer = SCG.Comparer<int>.Default;

            // Act & Assert
            Assert.That(() => collection.Sort(comparer), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortIComparer_TwoItemsDescending_Sorted()
        {
            // Arrange
            var items = new[] { Random.Next(), Random.Next(int.MinValue, 0) };
            var collection = GetList(items);
            var comparer = SCG.Comparer<int>.Default;

            // Act & Assert
            Assert.That(() => collection.Sort(comparer), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortIComparer_TwoEqualItems_Nothing()
        {
            // Arrange
            var item = Random.GetString();
            var items = new[] { item, string.Copy(item) };
            var collection = GetList(items);
            var comparer = SCG.Comparer<string>.Default;

            // Act & Assert
            Assert.That(() => collection.Sort(comparer), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortIComparer_EqualItems_Nothing()
        {
            // Arrange
            var count = GetCount(Random);
            var item = Random.GetString();
            var items = TestHelper.Repeat(() => string.Copy(item), count);
            var collection = GetList(items);
            var comparer = SCG.Comparer<string>.Default;

            // Act & Assert
            Assert.That(() => collection.Sort(comparer), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        // TODO: Test again when NUnit changes its behavior
        [Test]
        [Ignore("Because NUnit does not allow null values in Ordered: https://github.com/nunit/nunit/issues/1473")]
        public void SortIComparer_RandomCollectionWithNull_Sorted()
        {
            // Arrange
            var items = GetStrings(Random);
            var index = Random.Next(1, items.Length);
            items[index] = null;
            var collection = GetList(items, allowsNull: true);
            var comparer = SCG.Comparer<string>.Default;

            // Act & Assert
            Assert.That(() => collection.Sort(comparer), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortIComparer_NonDescendingRandomCollection_Nothing()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count);
            var collection = GetList(items);


            // Act & Assert
            Assert.That(() => collection.Sort(NonComparableComparer), RaisesNoEventsFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortIComparer_Descending_Sorted()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem + 1, previousItem + maxGap)), count).Reverse();
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(NonComparableComparer), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortIComparer_AllButLastAreSorted_Sorted()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count).Append(new Comparable(previousItem - 1));
            var collection = GetList(items);

            // Act & Assert
            Assert.That(() => collection.Sort(NonComparableComparer), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortIComparer_RandomCollection_Sorted()
        {
            // Arrange
            var items = GetStrings(Random, 10000);
            var collection = GetList(items);
            var comparer = SCG.Comparer<string>.Default;

            // Act & Assert
            Assert.That(() => collection.Sort(comparer), RaisesCollectionChangedEventFor(collection));

            // Assert
            Assert.That(collection, Is.Ordered);
        }

        [Test]
        public void SortIComparer_SortEmptyCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var comparer = SCG.Comparer<string>.Default;

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort(comparer);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void SortIComparer_SortSingleItemCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var items = GetStrings(Random, 1);
            var collection = GetList(items);
            var comparer = SCG.Comparer<string>.Default;

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort(comparer);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void SortIComparer_SortSortedCollectionDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var count = GetCount(Random);
            var previousItem = 0;
            var maxGap = 5;
            var items = TestHelper.Repeat(() => new Comparable(previousItem = Random.Next(previousItem, previousItem + maxGap)), count);
            var collection = GetList(items);
            var comparer = SCG.Comparer<Comparable>.Default;

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort(comparer);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void SortIComparer_SortDuringEnumeration_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var comparer = SCG.Comparer<string>.Default;

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.Sort(comparer);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        [Category("Unfinished")]
        [Ignore("Unfinished")]
        public void SortIComparer_ReadOnlyCollection_Fail()
        {
            Assert.That(IsReadOnly, Is.False, "Tests have not been written yet");
        }

        #endregion

        #region View(int, int)

        [Test]
        public void View_IndexLessThanZero_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(int.MinValue, 0);
            var count = 0;

            // Act & Assert
            Assert.That(() => collection.View(index, count), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void View_CountLessThanZero_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var count = Random.Next(int.MinValue, 0);
            var index = 0;

            // Act & Assert
            Assert.That(() => collection.View(index, count), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void View_IndexPlusCountGreaterThanUnderlyingCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(collection.Count + 1, int.MaxValue);
            var count = 0;

            // Act & Assert // TODO: somettimes doesn't throw an expcetio. When ?
            Assert.That(() => collection.View(index, count), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void View_ViewDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetStringList(Random);
            var index = Random.Next(0, collection.Count);
            var count = Random.Next(1, collection.Count - index);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.View(index, count);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void View_WithPostiveIndexAndCountAndEmptyCollection_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var index = Random.Next(1, int.MaxValue / 2 - 1);
            var count = Random.Next(1, int.MaxValue / 2 - 1);

            // Act & Assert
            Assert.That(() => collection.View(index, count), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        // more ...
        // IsValid

        #endregion

        #region ViewOf(T)

        [Test]
        public void ViewOf_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);

            // Act & Assert
            Assert.That(() => collection.ViewOf(null), Violates.PreconditionSaying(ItemMustBeNonNull));
        }

        [Test]
        public void ViewOf_AllowsNull_ViolatesPrecondition()
        {
            Run.If(AllowsNull);

            // Arrange
            var collection = GetStringList(Random, allowsNull: true);

            // Act & Assert
            Assert.That(() => collection.ViewOf(null), Throws.Nothing);
        }

        [Test]
        public void ViewOf_ExistingItem_IsTheSame()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = collection.First;

            // Act
            var view = collection.ViewOf(item);

            // Assert
            Assert.That(item, Is.SameAs(view.First));
        }

        [Test]
        public void ViewOf_NonExistingItem_NullView()
        {
            // Arrange
            var items = GetLowercaseStrings(Random);
            var collection = GetList(items);
            var item = GetUppercaseString(Random);

            // Act
            var view = collection.ViewOf(item);

            // Assert
            Assert.That(view, Is.Null);
        }

        [Test]
        public void ViewOf_ViewOfDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = collection.ToArray().Choose(Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.ViewOf(item);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void ViewOf_EmptyCollection_IsNull()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var item = GetUppercaseString(Random);

            // Act
            var viewOf = collection.ViewOf(item);

            // Assert
            Assert.That(viewOf, Is.Null);
        }

        // more ...

        #endregion

        #region LastViewOf(T)

        [Test]
        public void LastViewOf_DisallowsNull_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random, allowsNull: false);

            // Act & Assert
            Assert.That(() => collection.LastViewOf(null), Violates.PreconditionSaying(ItemMustBeNonNull));
        }

        [Test]
        public void LastViewOf_AllowsNull_ViolatesPrecondition()
        {
            Run.If(AllowsNull);

            // Arrange
            var collection = GetStringList(Random, allowsNull: true);

            // Act & Assert
            Assert.That(() => collection.LastViewOf(null), Throws.Nothing);
        }

        [Test]
        public void LastViewOf_ExistingItem_IsTheSame()
        {
            // Arrange
            var collection = new HashedLinkedList<string>(new[] { "1", "2", "3" }, SCG.EqualityComparer<string>.Default); //GetStringList(Random);
            var item = collection.Last;

            // Act
            var view = collection.LastViewOf(item);

            // Assert
            Assert.That(item, Is.SameAs(view.First));
        }

        [Test]
        public void LastViewOf_NonExistingItem_NullView()
        {
            // Arrange
            var items = GetLowercaseStrings(Random);
            var collection = GetList(items);
            var item = GetUppercaseString(Random);

            // Act
            var view = collection.LastViewOf(item);

            // Assert
            Assert.That(view, Is.Null);
        }

        [Test]
        public void LastViewOf_LastViewOfDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetStringList(Random);
            var item = collection.ToArray().Choose(Random);

            // Act
            var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();
            collection.LastViewOf(item);

            // Assert
            Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void LastViewOf_EmptyCollection_IsNull()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var item = GetUppercaseString(Random);

            // Act
            var view = collection.LastViewOf(item);

            // Assert
            Assert.That(view, Is.Null);
        }

        #endregion

        #region TrySlide(int)

        [Test]
        public void TrySlide_TrySlideDuringEnumerationOfView_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);

            var view = GetView(collection);
            var uC = view.Underlying.Count;
            var vC = view.Count;
            var origOffset = view.Offset;
            var withOffset = GetOffset(view, Random);

            // Act
            var viewEnumerator = view.GetEnumerator(); // TODO: coll's or view's enumerator ?
            viewEnumerator.MoveNext();
            var res = view.TrySlide(withOffset);

            // Assert
            TestContext.WriteLine("TrySlide_TrySlideDuringEnumerationOfView, res = {0}, withOffset = {1}, Orig. Offset = {2}, view Count = {3}, Underlying Count = {4}", res, withOffset, origOffset, vC, uC);
            Assert.That(() => viewEnumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void TrySlide_EmptyView_True()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var emptyView = GetEmptyView(collection);
            var withOffset = GetOffset(emptyView, Random);

            // Act
            var result = emptyView.TrySlide(withOffset);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void TrySlide_RandomView_RaisesNoEvents()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items);
            var view = GetView(collection);
            var withOffset = GetOffset(view, Random);

            // Act & Assert
            Assert.That(() => view.TrySlide(withOffset), RaisesNoEventsFor(collection));
        }

        [Test]
        public void TrySlide_NewOffsetLargerThanUnderlyingCount_False()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = Random.Next(view.Underlying.Count + 1, int.MaxValue);

            // Act & Assert
            Assert.That(view.TrySlide(withOffset), Is.False);
        }

        [Test]
        public void TrySlide_RandomViewNewOffsetOfUnderlyingCount_False()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = Random.Next(collection.Count + 1, int.MaxValue);

            // Act & Assert
            Assert.That(() => view.TrySlide(withOffset), Is.False);
        }

        // TODO: view.TrySlide(withOffset), withOffset < Underlying.0

        [Test]
        public void TrySlide_EmptyViewNewOffsetOfUnderlyingCount_True()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = GetOffset(view, Random);

            // Act & Assert
            Assert.That(() => view.TrySlide(withOffset), Is.True);
        }

        #endregion

        #region TrySlide(int, int)

        [Test]
        public void TrySlide2_NotAView_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);

            // Act & Assert
            //Assert.That(() => collection.TrySlide(index), Violates.PreconditionSaying(NotAView));
            Assert.That(() => collection.TrySlide(0, 0), Violates.Precondition);
        }

        [Test]
        public void TrySlide2_TrySlideDuringEnumerationOfView_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var uC = collection.Count;
            var view = GetView(collection);
            var vC = view.Count;
            var origOffset = view.Offset;
            var withOffset = GetOffset(view, Random);
            var newCount = GetNewCount(view, withOffset, Random);

            // Act
            var viewEnumerator = view.GetEnumerator(); // TODO: coll's or view's enumerator ?
            viewEnumerator.MoveNext();
            var res = view.TrySlide(withOffset, newCount);

            // Assert
            TestContext.WriteLine("TrySlide_TrySlideDuringEnumerationOfView, res = {0}, withOffset = {1}, Orig. Offset = {2}, view Count = {3}, Underlying Count = {4}", res, withOffset, origOffset, vC, uC);
            Assert.That(() => viewEnumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void TrySlide2_EmptyView_True()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var view = GetEmptyView(collection);
            var withOffset = GetOffset(view, Random);
            var newCount = GetNewCount(view, withOffset, Random);

            // Act
            var result = view.TrySlide(withOffset, newCount);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void TrySlide2_RandomView_RaisesNoEvents()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items); //TODO why GetCollection(items);
            var view = GetView(collection);
            var withOffset = GetOffset(view, Random);
            var newCount = GetNewCount(view, withOffset, Random);

            // Act & Assert
            Assert.That(() => view.TrySlide(withOffset, newCount), RaisesNoEventsFor(collection));
        }

        [Test]
        public void TrySlide2_NewOffsetPlusNewCountLargerThanUnderlyingCount_False()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var index = Random.Next(collection.Count + 1, int.MaxValue);

            // Act & Assert
            Assert.That(view.TrySlide(index, 0), Is.False);
        }

        [Test]
        public void TrySlide2_NewCountLessThanZero_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var newCount = Random.Next(int.MinValue, 0);

            // Act & Assert
            Assert.That(() => view.TrySlide(0, newCount), Is.False);
        }

        [Test]
        public void TrySlide2_NewOffsetLessThanZero_False()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var index = Random.Next(int.MinValue, -collection.Count);

            // Act & Assert
            Assert.That(view.TrySlide(index, 0), Is.False);
        }

        [Test]
        public void TrySlide2_RandomView_Equals()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = GetOffset(view, Random);
            var newCount = GetNewCount(view, withOffset, Random);

            // Act
            view.TrySlide(withOffset, newCount);

            // Assert
            // TODO: is it ok ?
            Assert.That(view, Is.EqualTo(collection.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer));
        }

        #endregion

        #region Slide(int, int)

        [Test]
        public void Slide2_NewOffSetLessThanZero_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = Random.Next(int.MinValue, -collection.Count);

            // Act & Assert
            Assert.That(() => view.Slide(withOffset, 0), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void Slide2_NewCountLessThanZero_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var newCount = Random.Next(int.MinValue, 0);

            // Act & Assert
            Assert.That(() => view.Slide(0, newCount), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void Slide2_NewOffsetPlusNewCountGraterThanUnderlyingCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = Random.Next(view.Underlying.Count + 1, int.MaxValue);

            // Act & Assert
            Assert.That(() => view.Slide(withOffset, 0), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void Slide2_SlideDuringEnumerationOfView_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = GetOffset(view, Random);
            var newCount = GetNewCount(view, withOffset, Random);

            // Act
            var viewEnumerator = view.GetEnumerator();
            viewEnumerator.MoveNext();
            view.Slide(withOffset, newCount);


            // Assert
            Assert.That(() => viewEnumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void Slide2_EmptyView_True()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var view = GetEmptyView(collection);
            var withOffset = GetOffset(view, Random);
            var newCount = GetNewCount(view, withOffset, Random);

            // Act
            var result = view.Slide(withOffset, newCount);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Slide2_RandomView_RaisesNoEvents()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items);
            var view = GetView(collection);
            var withOffset = GetOffset(view, Random);
            var newCount = GetNewCount(view, withOffset, Random);


            // Act & Assert
            Assert.That(() => view.Slide(withOffset, newCount), RaisesNoEventsFor(collection));
        }

        [Test]
        public void Slide2_RandomView_Equals()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = GetOffset(view, Random);
            var newCount = GetNewCount(view, withOffset, Random);

            // Act
            view.Slide(withOffset, newCount);

            // Assert            
            var coll = collection.Skip(view.Offset).Take(view.Count);
            Assert.That(view, Is.EqualTo(coll).Using(ReferenceEqualityComparer));
        }


        #endregion

        #region Slide(int)

        [Test]
        public void Slide_NewOffsetLessThanZero_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = Random.Next(int.MinValue, -collection.Count);

            // Act & Assert
            Assert.That(() => view.Slide(withOffset), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void Slide_NewOffsetPlusNewCountGraterThanUnderlyingCount_ViolatesPrecondition()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = Random.Next(view.Underlying.Count + 1, int.MaxValue);

            // Act & Assert
            Assert.That(() => view.Slide(withOffset), Violates.PreconditionSaying(ArgumentMustBeWithinBounds));
        }

        [Test]
        public void Slide_SlideDuringEnumerationOfView_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = GetOffset(view, Random);

            // Act
            var viewEnumerator = view.GetEnumerator();
            viewEnumerator.MoveNext();
            view.Slide(withOffset);

            // Assert
            Assert.That(() => viewEnumerator.MoveNext(), Throws.InvalidOperationException.Because(CollectionWasModified));
        }

        [Test]
        public void Slide_EmptyView_True()
        {
            // Arrange
            var collection = GetEmptyList<string>();
            var emptyView = GetEmptyView(collection);
            var withOffset = GetOffset(emptyView, Random);

            // Act
            var result = emptyView.Slide(withOffset);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Slide_RandomView_RaisesNoEvents()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items);
            var view = GetView(collection);
            var withOffset = GetOffset(view, Random);

            // Act & Assert
            Assert.That(() => view.Slide(withOffset), RaisesNoEventsFor(collection));
        }

        [Test]
        public void Slide_RandomView_Equals()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var withOffset = GetOffset(view, Random);

            view.Slide(withOffset);

            // Act & Assert
            //Assert.That(() => view.Slide(withOffset), Is.EqualTo(collection.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer));
            Assert.That(view, Is.EqualTo(collection.Skip(view.Offset).Take(view.Count)).Using(ReferenceEqualityComparer));
        }

        #endregion

        #region Span(IList)

        [Test]
        public void Span_SpanDuringEnumeration_ThrowsNothing()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);
            var view2 = GetView(view); // so we quarantee to get a view not sitting to left ot the view

            // Act
            var viewEnumerator = view.GetEnumerator();
            viewEnumerator.MoveNext();

            view.Span(view2);

            // Assert
            Assert.That(() => viewEnumerator.MoveNext(), Throws.Nothing);
        }

        [Test]
        public void Span_RaisesNoEvents()
        {
            // Arrange
            var items = GetUppercaseStrings(Random);
            var collection = GetList(items);
            var view = GetView(collection);
            var view2 = GetView(view);

            // Act & Assert
            Assert.That(() => view.Span(view2), RaisesNoEventsFor(collection));
        }


        // view is null
        [Test]
        public void Span_OtherViewIsNull_ViolatesPreconditionSayingArgumentMustBeNonNull()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);

            // Act & Assert
            Assert.That(() => view.Span(null), Violates.PreconditionSaying(ArgumentMustBeNonNull));
        }

        // list is given
        [Test]
        public void Span_OtherViewIsList_ViolatesPreconditionSayingNotAView()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);

            // Act & Assert
            Assert.That(() => view.Span(collection), Violates.PreconditionSaying(NotAView));
        }

        // underlying is different
        [Test]
        public void Span_OtherViewHasDifferentUnderlyingList_ViolatesPreconditionSayingUnderlyingListMustBeTheSame()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = GetView(collection);

            var collection2 = GetStringList(Random);
            var view2 = GetView(collection2);

            // Act & Assert
            Assert.That(() => view.Span(view2), Violates.PreconditionSaying(UnderlyingListMustBeTheSame));
        }

        [Test]
        public void Span_ViewsDontOverlapViewOnLeft_Equals()
        {
            // Arrange            
            var collection = GetStringList(Random);
            var view = collection.View(1, 3);
            var otherView = collection.View(5, 3);
            var expectedView = collection.View(1, otherView.Offset + otherView.Count - view.Offset);

            // Act
            var spannedView = view.Span(otherView);

            // Assert
            Assert.That(spannedView, Is.EqualTo(expectedView).Using(ReferenceEqualityComparer));
        }

        [Test]
        public void Span_ViewsDontOverlapOtherviewOnLeft_Null()
        {
            // Arrange            
            var collection = GetStringList(Random);
            var view = collection.View(5, 3);
            var otherView = collection.View(1, 3);

            // Act
            var spannedView = view.Span(otherView);

            // Assert
            Assert.That(spannedView, Is.Null);
        }

        [Test]
        public void Span_OtherviewBeginsWhereViewEnds_Equals()
        {
            // Arrange            
            var collection = GetStringList(Random);
            var view = collection.View(1, 3);
            var otherView = collection.View(4, 3);
            var expectedView = collection.View(1, otherView.Offset + otherView.Count - view.Offset);

            // Act
            var spannedView = view.Span(otherView);

            // Assert
            Assert.That(spannedView, Is.EqualTo(expectedView).Using(ReferenceEqualityComparer));
        }


        // v1 < v2
        [Test]
        public void Span_ViewAndOtherViewOverlappingViewOnLeft_Equals()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = collection.View(1, 3);
            var otherView = collection.View(2, 3);
            var expectedView = collection.View(1, otherView.Offset + otherView.Count - view.Offset);
            //var view2 = GetView(collection, minOffset: view.Offset, maxOffset: view.Offset + view.Count);

            // Act
            var spannedView = view.Span(otherView);

            // Assert
            Assert.That(spannedView, Is.EqualTo(expectedView).Using(ReferenceEqualityComparer));
        }

        // v2 < v1
        [Test]
        public void Span_ViewAndOtherviewOverlappingOtherviewOnLeft_Equals()
        {
            // Arrange
            var collection = GetStringList(Random);
            var view = collection.View(2, 3);
            var otherView = collection.View(1, 3);
            var expectedView = collection.View(2, otherView.Offset + otherView.Count - view.Offset);
            //var view2 = GetView(collection, minOffset: view.Offset, maxOffset: view.Offset + view.Count);
            // expectedView ?

            // Act
            var spannedView = view.Span(otherView);

            // Assert
            Assert.That(spannedView, Is.EqualTo(expectedView).Using(ReferenceEqualityComparer));
        }

        #endregion

        #endregion

        #endregion

        #endregion

        #region Nested Types

        private class NonComparable
        {
            public NonComparable(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }


        private class Comparable : NonComparable, IComparable<NonComparable>
        {
            public Comparable(int value) : base(value) { }
            public int CompareTo(NonComparable other) => Value.CompareTo(other.Value);
        }


        private readonly Comparison<NonComparable> _nonComparableComparison = (x, y) => x.Value.CompareTo(y.Value);

        private SCG.IComparer<NonComparable> NonComparableComparer => _nonComparableComparison.ToComparer();

        #endregion
    }
}