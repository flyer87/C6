using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using C6.Collections;
using static System.Diagnostics.Contracts.Contract;
using static C6.Contracts.ContractMessage;

namespace C6.UserGuideExamples
{
    public class ViewExample
    {
        public void Main()
        {
            var list = new HashedLinkedList<int> { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59 };

            // View of item
            list.ViewOf(2);

            // Last view of item
            list.LastViewOf(11);

            // View in range
            var view = list.View(0, 3);

            // Slide with offset
            view.Slide(3);

            // Slide with offset and set lenght
            view.Slide(-3, 3);

            // Try slide with offset and set lenght
            var hasSucceed = view.TrySlide(-10, 3);             

            // Try slide with offset and set lenght
            view.TrySlide(3, -3);            

            var view2 = list.View(0, 3);
            var view3 = list.View(1, 3);
            // Check if views overlap
            Overlap(view2, view3);

            // Check if views contained in each other
            ContainsView(view2, view3);            

            // Span views
            var spannedView = view2.Span(view3);            

            // Invalidate all views by shuffle
            list.Shuffle();

            // Check if view is valid
            Console.WriteLine($"View is valid? {view.IsValid}");
        }

        public static bool Overlap<T>(IList<T> u, IList<T> w)
        {
            #region Code Contractss           
            // Not null
            Requires(u != null, ItemMustBeNonNull);

            // Not null
            Requires(w != null, ItemMustBeNonNull);

            // Must be view
            Requires(u.Underlying == null, NotAView);

            // Must be view
            Requires(w.Underlying == null, NotAView);

            // Must have the same underlying list
            Requires(u.Underlying != w.Underlying, UnderlyingListMustBeTheSame);

            #endregion

            return u.Offset < w.Offset + w.Count && w.Offset < u.Offset + u.Count;
        }

        public static bool ContainsView<T>(IList<T> u, IList<T> w)
        {
            #region Code Contractss
            // Not null
            Requires(u != null, ItemMustBeNonNull);

            // Not null
            Requires(w != null, ItemMustBeNonNull);

            // Must be view
            Requires(u.Underlying == null, NotAView);

            // Must be view
            Requires(w.Underlying == null, NotAView);

            // Must have the same underlying list
            Requires(u.Underlying != w.Underlying, UnderlyingListMustBeTheSame);

            #endregion

            if (w.Count > 0)
                return u.Offset <= w.Offset && w.Offset + w.Count <= u.Offset + u.Count;

            return u.Offset < w.Offset && w.Offset < u.Offset + u.Count;
        }
    }
}
