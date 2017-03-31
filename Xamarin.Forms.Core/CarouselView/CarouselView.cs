﻿using System;
using System.Threading.Tasks;
using System.Collections;

namespace Xamarin.Forms
{
	/// <summary>
	/// CarouselView Interface
	/// </summary>
	public class CarouselView : View //Layout<View>
	{
		public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create("ItemsSource", typeof(IList), typeof(CarouselView), null);

		public IList ItemsSource
		{
			get { return (IList)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create("ItemTemplate", typeof(DataTemplate), typeof(CarouselView), null);

		public DataTemplate ItemTemplate
		{
			get { return (DataTemplate)GetValue(ItemTemplateProperty); }
			set { SetValue(ItemTemplateProperty, value); }
		}

		public static readonly BindableProperty PositionProperty = BindableProperty.Create("Position", typeof(int), typeof(CarouselView), 0, BindingMode.TwoWay);

		public int Position
		{
			get { return (int)GetValue(PositionProperty); }
			set { SetValue(PositionProperty, value); }
		}

		public static readonly BindableProperty ShowIndicatorsProperty = BindableProperty.Create("ShowIndicators", typeof(bool), typeof(CarouselView), false);

		public bool ShowIndicators
		{
			get { return (bool)GetValue(ShowIndicatorsProperty); }
			set { SetValue(ShowIndicatorsProperty, value); }
		}

		public static readonly BindableProperty IndicatorsShapeProperty = BindableProperty.Create("IndicatorsShape", typeof(IndicatorsShape), typeof(CarouselView), IndicatorsShape.Circle);

		public IndicatorsShape IndicatorsShape
		{
			get { return (IndicatorsShape)GetValue(IndicatorsShapeProperty); }
			set { SetValue(IndicatorsShapeProperty, value); }
		}

		public static readonly BindableProperty PageIndicatorTintColorProperty = BindableProperty.Create("PageIndicatorTintColor", typeof(Color), typeof(CarouselView), Color.Silver);

		public Color PageIndicatorTintColor
		{
			get { return (Color)GetValue(PageIndicatorTintColorProperty); }
			set { SetValue(PageIndicatorTintColorProperty, value); }
		}

		public static readonly BindableProperty CurrentPageIndicatorTintColorProperty = BindableProperty.Create("CurrentPageIndicatorTintColor", typeof(Color), typeof(CarouselView), Color.Gray);

		public Color CurrentPageIndicatorTintColor
		{
			get { return (Color)GetValue(CurrentPageIndicatorTintColorProperty); }
			set { SetValue(CurrentPageIndicatorTintColorProperty, value); }
		}

		public static readonly BindableProperty OrientationProperty = BindableProperty.Create("Orientation", typeof(CarouselViewOrientation), typeof(CarouselView), CarouselViewOrientation.Horizontal);

		public CarouselViewOrientation Orientation
		{
			get { return (CarouselViewOrientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		public static readonly BindableProperty AnimateTransitionProperty = BindableProperty.Create("AnimateTransition", typeof(bool), typeof(CarouselView), true);

		public bool AnimateTransition
		{
			get { return (bool)GetValue(AnimateTransitionProperty); }
			set { SetValue(AnimateTransitionProperty, value); }
		}

		public static readonly BindableProperty IsSwipingEnabledProperty = BindableProperty.Create("IsSwipingEnabled", typeof(bool), typeof(CarouselView), true);

		public bool IsSwipingEnabled
		{
			get { return (bool)GetValue(IsSwipingEnabledProperty); }
			set { SetValue(IsSwipingEnabledProperty, value); }
		}

		// Android and iOS only
		public static readonly BindableProperty InterPageSpacingProperty = BindableProperty.Create("InterPageSpacing", typeof(int), typeof(CarouselView), 0);

		public int InterPageSpacing
		{
			get { return (int)GetValue(InterPageSpacingProperty); }
			set { SetValue(InterPageSpacingProperty, value); }
		}

		// Android and iOS only
		public static readonly BindableProperty InterPageSpacingColorProperty = BindableProperty.Create("InterPageSpacingColor", typeof(Color), typeof(CarouselView), Color.White);

		public Color InterPageSpacingColor
		{
			get { return (Color)GetValue(InterPageSpacingColorProperty); }
			set { SetValue(InterPageSpacingColorProperty, value); }
		}

		// UWP only
		public static readonly BindableProperty ArrowsProperty = BindableProperty.Create("Arrows", typeof(bool), typeof(CarouselView), false);

		public bool Arrows
		{
			get { return (bool)GetValue(ArrowsProperty); }
			set { SetValue(ArrowsProperty, value); }
		}

		public EventHandler PositionSelected;

		public Func<object, int, Task> InsertAction;

		public async Task InsertPage(object item, int position = -1)
		{
			if (InsertAction != null)
			{
				await InsertAction(item, position);
			}
		}

		public Func<int, Task> RemoveAction;

		public async Task RemovePage(int position)
		{
			if (RemoveAction != null)
			{
				await RemoveAction(position);
			}
		}
	}
}
