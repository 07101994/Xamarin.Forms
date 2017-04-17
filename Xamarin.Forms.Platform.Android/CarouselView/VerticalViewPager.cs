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
			OverScrollMode = OverScrollMode.Never;
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
				SwapTouchEvent(ev);
				return intercept;
			}

			return false;
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

		public void SetPagingEnabled(bool enabled)
		{
			this.isSwipingEnabled = enabled;
		}
	}
}