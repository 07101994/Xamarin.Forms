﻿
using Android.Content;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;

namespace Xamarin.Forms.Platform.Android
{
	public class VerticalViewPager : ViewPager, IViewPager
	{
		private bool isSwipingEnabled = true;

		public VerticalViewPager(Context context) : base(context, null)
		{
		}

		public VerticalViewPager(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			SetPageTransformer(false, new DefaultTransformer());
			// get rid of the overscroll drawing that happens on the left and right
			OverScrollMode = OverScrollMode.Never;
		}

		private MotionEvent SwapTouchEvent(MotionEvent ev)
		{
			float width = Width;
			float height = Height;

			float swappedX = (ev.GetY() / height) * width;
			float swappedY = (ev.GetX() / width) * height;

			ev.SetLocation(swappedX, swappedY);

			return ev;
		}

		public override bool OnTouchEvent(MotionEvent ev)
		{
			if (this.isSwipingEnabled)
			{
				return base.OnTouchEvent(SwapTouchEvent(ev));
			}

			return false;
		}

		public override bool OnInterceptTouchEvent(MotionEvent ev)
		{
			if (this.isSwipingEnabled)
			{
				bool intercept = base.OnInterceptTouchEvent(SwapTouchEvent(ev));
				//If not intercept, touch event should not be swapped.
				SwapTouchEvent(ev);
				return intercept;
			}

			return false;
		}

		public void SetPagingEnabled(bool enabled)
		{
			this.isSwipingEnabled = enabled;
		}
	}
}