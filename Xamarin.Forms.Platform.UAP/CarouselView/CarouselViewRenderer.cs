﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Specialized;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Xamarin.Forms.Platform.UAP;

/*
The MIT License(MIT)

Copyright(c) 2017 Alexander Reyes(alexrainman1975 @gmail.com)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
IN THE SOFTWARE.
 */

namespace Xamarin.Forms.Platform.UWP
{
	/// <summary>
	/// CarouselView Renderer
	/// </summary>
	public class CarouselViewRenderer : ViewRenderer<CarouselView, UserControl>
	{
		UserControl nativeView;
		FlipView flipView;
		StackPanel indicators;

		ColorConverter converter;
		SolidColorBrush selectedColor;
		SolidColorBrush fillColor;

		double ElementWidth;
		double ElementHeight;

		// To hold all the rendered views
		ObservableCollection<FrameworkElement> Source;

		// To hold the indicators dots
		ObservableCollection<Shape> Dots;

		// To manage SizeChanged
		Timer timer;

		bool _disposed;

		protected override void OnElementChanged(ElementChangedEventArgs<CarouselView> e)
		{
			base.OnElementChanged(e);

			if (Control == null)
			{
				// Instantiate the native control and assign it to the Control property with
				// the SetNativeControl method
			}

			if (e.OldElement != null)
			{
				// Unsubscribe from event handlers and cleanup any resources
				if (flipView != null)
				{
					flipView.Loaded -= FlipView_Loaded;
					flipView.SelectionChanged -= FlipView_SelectionChanged;
					flipView.SizeChanged -= FlipView_SizeChanged;
				}

				if (Element != null)
				{
					if (Element.ItemsSource != null && Element.ItemsSource is INotifyCollectionChanged)
						((INotifyCollectionChanged)Element.ItemsSource).CollectionChanged -= ItemsSource_CollectionChanged;
				}
			}

			if (e.NewElement != null)
			{
				// Configure the control and subscribe to event handlers
				if (Element.ItemsSource != null && Element.ItemsSource is INotifyCollectionChanged)
					((INotifyCollectionChanged)Element.ItemsSource).CollectionChanged += ItemsSource_CollectionChanged;
			}
		}

		async void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// NewItems contains the item that was added.
			// If NewStartingIndex is not -1, then it contains the index where the new item was added.
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                InsertPage(Element?.ItemsSource.GetItem(e.NewStartingIndex), e.NewStartingIndex);
            }

			// OldItems contains the item that was removed.
			// If OldStartingIndex is not -1, then it contains the index where the old item was removed.
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                await RemovePage(e.OldStartingIndex);
            }

			// OldItems contains the moved item.
			// OldStartingIndex contains the index where the item was moved from.
			// NewStartingIndex contains the index where the item was moved to.
			if (e.Action == NotifyCollectionChangedAction.Move)
			{
				if (Element != null && flipView != null && Source != null)
				{
                    var obj = Source[e.OldStartingIndex];
					Source.RemoveAt(e.OldStartingIndex);
					Source.Insert(e.NewStartingIndex, obj);
				}
            }

			// NewItems contains the replacement item.
			// NewStartingIndex and OldStartingIndex are equal, and if they are not -1,
			// then they contain the index where the item was replaced.
			if (e.Action == NotifyCollectionChangedAction.Replace)
			{
				if (Element != null && flipView != null && Source != null)
				{
					Source[e.OldStartingIndex] = CreateView(e.NewItems[e.NewStartingIndex]);
				}
            }

			// No other properties are valid.
			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				if (Element != null)
				{
					SetPosition();
					SetNativeView();
					Element.PositionSelected?.Invoke(Element, Element.Position);
				}
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			var rect = this.Element?.Bounds;

			switch (e.PropertyName)
			{
				case "Width":
					// Save width only the first time to enable SizeChanged
					if (Element != null)
					{
						if (ElementWidth == 0)
							ElementWidth = ((Xamarin.Forms.Rectangle)rect).Width;
					}
					break;
				case "Height":
					// Save height only the first time to enable SizeChanged
					if (Element != null)
					{
						if (ElementHeight == 0)
							ElementHeight = ((Xamarin.Forms.Rectangle)rect).Height;
						SetNativeView();
						Element.PositionSelected?.Invoke(Element, Element.Position);
					}
					break;
				case "Orientation":
					if (Element != null)
					{
						SetNativeView();
						Element.PositionSelected?.Invoke(Element, Element.Position);
					}
					break;
				case "BackgroundColor":
					if (flipView != null)
						flipView.Background = (SolidColorBrush)converter.Convert(Element.BackgroundColor, null, null, null);
					break;
				case "IsSwipingEnabled":
					//flipView.ManipulationMode = Element.IsSwipingEnabled ? ManipulationModes.All : ManipulationModes.None;
					break;
				case "IndicatorsTintColor":
					fillColor = (SolidColorBrush)converter.Convert(Element.IndicatorsTintColor, null, null, null);
					SetIndicators();
					break;
				case "CurrentPageIndicatorTintColor":
					selectedColor = (SolidColorBrush)converter.Convert(Element.CurrentPageIndicatorTintColor, null, null, null);
					SetIndicators();
					break;
				case "IndicatorsShape":
					SetIndicators();
					break;
				case "ShowIndicators":
					if (indicators != null)
						indicators.Visibility = Element.ShowIndicators ? Visibility.Visible : Visibility.Collapsed;
					break;
				case "ItemsSource":
					if (Element != null)
					{
						SetPosition();
						SetNativeView();
						Element.PositionSelected?.Invoke(Element, Element.Position);
						if (Element.ItemsSource != null && Element.ItemsSource is INotifyCollectionChanged)
							((INotifyCollectionChanged)Element.ItemsSource).CollectionChanged += ItemsSource_CollectionChanged;
					}
					break;
				case "ItemTemplate":
					if (Element != null)
					{
						SetNativeView();
						Element.PositionSelected?.Invoke(Element, Element.Position);
					}
					break;
				case "Position":
					if (Element != null && !isSwiping)
						SetCurrentPage(Element.Position);
					break;
				case "ShowArrows":
					if (flipView != null)
						FlipView_Loaded(flipView, null);
					break;
			}
		}

		// To avoid triggering Position changed more than once
		bool isSwiping;

		// Arrows visibility
		private void FlipView_Loaded(object sender, RoutedEventArgs e)
		{
			ButtonHide(flipView, "PreviousButtonHorizontal");
			ButtonHide(flipView, "NextButtonHorizontal");
			ButtonHide(flipView, "PreviousButtonVertical");
			ButtonHide(flipView, "NextButtonVertical");
		}

		private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Element != null && !isSwiping)
			{
				Element.Position = flipView.SelectedIndex;
				UpdateIndicators();

				Element.PositionSelected?.Invoke(Element, flipView.SelectedIndex);
			}
		}

		// Reset timer as this is called multiple times
		private void FlipView_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (e.NewSize.Width != ElementWidth || e.NewSize.Height != ElementHeight)
			{
				if (timer != null)
					timer.Dispose();
				timer = null;

				timer = new Timer(OnTick, e.NewSize, 100, 100);
			}
		}

		private void OnTick(object args)
		{
			timer.Dispose();
			timer = null;

			// Save new dimensions when resize completes
			var size = (Windows.Foundation.Size)args;
			ElementWidth = size.Width;
			ElementHeight = size.Height;

			// Refresh UI
			Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
			{
				if (Element != null)
				{
					SetNativeView();
					Element.PositionSelected?.Invoke(Element, Element.Position);
				}
			});
		}

		void SetPosition()
		{
			isSwiping = true;
			if (Element.ItemsSource != null)
			{
				if (Element.Position > Element.ItemsSource.GetCount() - 1)
					Element.Position = Element.ItemsSource.GetCount() - 1;

				if (Element.Position == -1)
					Element.Position = 0;
			}
			else
			{
				Element.Position = 0;
			}
			isSwiping = false;
		}

		public void SetNativeView()
		{
			// Orientation BP
			if (Element.Orientation == CarouselViewOrientation.Horizontal)
				nativeView = new FlipViewControl();
			else
				nativeView = new VerticalFlipViewControl();

			flipView = nativeView.FindName("flipView") as FlipView;

			var source = new List<FrameworkElement>();
			if (Element.ItemsSource != null && Element.ItemsSource?.GetCount() > 0)
			{
				for (int j = 0; j <= Element.ItemsSource.GetCount() - 1; j++)
				{
					source.Add(CreateView(Element.ItemsSource.GetItem(j)));
				}
			}
			Source = new ObservableCollection<FrameworkElement>(source);
			flipView.ItemsSource = Source;

			//flipView.ItemsSource = Element.ItemsSource;
			//flipView.ItemTemplateSelector = new MyTemplateSelector(Element); (the way it should be)

			converter = new ColorConverter();

			// BackgroundColor BP
			flipView.Background = (SolidColorBrush)converter.Convert(Element.BackgroundColor, null, null, null);

			// IndicatorsTintColor BP
			fillColor = (SolidColorBrush)converter.Convert(Element.IndicatorsTintColor, null, null, null);

			// CurrentPageIndicatorTintColor BP
			selectedColor = (SolidColorBrush)converter.Convert(Element.CurrentPageIndicatorTintColor, null, null, null);

			// INDICATORS
			indicators = nativeView.FindName("indicators") as StackPanel;

			// IndicatorsShape BP
			SetIndicators();

			// ShowIndicators BP
			indicators.Visibility = Element.ShowIndicators ? Visibility.Visible : Visibility.Collapsed;

			flipView.Loaded += FlipView_Loaded;
			flipView.SelectionChanged += FlipView_SelectionChanged;
			flipView.SizeChanged += FlipView_SizeChanged;

			//IsSwipingEnabled BP (not working)
			//flipView.ManipulationMode = Element.IsSwipingEnabled ? ManipulationModes.All : ManipulationModes.None;

			if (Source.Count > 0)
			{
				flipView.SelectedIndex = Element.Position;
			}

			SetNativeControl(nativeView);
		}

		void SetIndicators()
		{
			if (Element != null)
			{
				var dots = new List<Shape>();

				if (Element.ItemsSource != null && Element.ItemsSource?.GetCount() > 0)
				{
					int i = 0;
					foreach (var item in Element.ItemsSource)
					{
						dots.Add(CreateDot(i, Element.Position));
						i++;
					}
				}

				Dots = new ObservableCollection<Shape>(dots);

				var dotsPanel = nativeView.FindName("dotsPanel") as ItemsControl;
				dotsPanel.ItemsSource = Dots;
			}
		}

		void UpdateIndicators()
		{
			var dotsPanel = nativeView.FindName("dotsPanel") as ItemsControl;
			int i = 0;
			foreach (var item in dotsPanel.Items)
			{
				((Shape)item).Fill = i == Element.Position ? selectedColor : fillColor;
				i++;
			}
		}

		void InsertPage(object item, int position)
		{
			if (Element != null && flipView != null && Source != null)
			{
				if (position <= Element.Position)
				{
					isSwiping = true;
					Element.Position++;
					isSwiping = false;
				}

				Source.Insert(position, CreateView(item));
				Dots.Insert(position, CreateDot(position, Element.Position));

				flipView.SelectedIndex = Element.Position;

				if (position <= Element.Position)
					Element.PositionSelected?.Invoke(Element, flipView.SelectedIndex);
			}
		}

		public async Task RemovePage(int position)
		{
			if (Element != null && flipView != null && Source != null && Source?.Count > 0)
			{
				// To remove latest page, rebuild flipview or the page wont disappear
				if (Source.Count == 1)
				{
					SetNativeView();
				}
				else
				{

					isSwiping = true;

					// To remove current page
					if (position == Element.Position)
					{
						// Swipe animation at position 0 doesn't work :(
						/*if (position == 0)
						{
							flipView.SelectedIndex = 1;
						}
						else
						{*/
						if (position > 0)
						{
							var newPos = position - 1;
							if (newPos == -1)
								newPos = 0;

							flipView.SelectedIndex = newPos;
						}

						// With a swipe transition
						if (Element.AnimateTransition)
							await Task.Delay(100);
					}

					Source.RemoveAt(position);

					Element.Position = flipView.SelectedIndex;

					Dots.RemoveAt(position);
					UpdateIndicators();

					isSwiping = false;

					Element.PositionSelected?.Invoke(Element, Element.Position);
				}
			}
		}

		void SetCurrentPage(int position)
		{
			if (flipView != null && Element.ItemsSource != null && Element.ItemsSource?.GetCount() > 0)
			{
				flipView.SelectedIndex = position;
			}
		}

		FrameworkElement CreateView(object item)
		{
			Xamarin.Forms.View formsView = null;
			var bindingContext = item;

			var dt = bindingContext as Xamarin.Forms.DataTemplate;

			// Support for List<DataTemplate> as ItemsSource
			if (dt != null)
			{
				formsView = (Xamarin.Forms.View)dt.CreateContent();
			}
			else
			{

				var selector = Element.ItemTemplate as Xamarin.Forms.DataTemplateSelector;
				if (selector != null)
					formsView = (Xamarin.Forms.View)selector.SelectTemplate(bindingContext, Element).CreateContent();
				else
					formsView = (Xamarin.Forms.View)Element.ItemTemplate.CreateContent();

				formsView.BindingContext = bindingContext;
			}

			formsView.Parent = this.Element;

			var element = formsView.ToWindows(new Xamarin.Forms.Rectangle(0, 0, ElementWidth, ElementHeight));

			return element;
		}

		Shape CreateDot(int i, int position)
		{
			if (Element.IndicatorsShape == IndicatorsShape.Circle)
			{
				return new Ellipse()
				{
					Fill = i == position ? selectedColor : fillColor,
					Height = 7,
					Width = 7,
					Margin = new Windows.UI.Xaml.Thickness(4, 12, 4, 12)
				};
			}
			else
			{
				return new Windows.UI.Xaml.Shapes.Rectangle()
				{
					Fill = i == position ? selectedColor : fillColor,
					Height = 6,
					Width = 6,
					Margin = new Windows.UI.Xaml.Thickness(4, 12, 4, 12)
				};
			}
		}

		private void ButtonHide(FlipView f, string name)
		{
			var b = FindVisualChild<Windows.UI.Xaml.Controls.Button>(f, name);
			if (b != null)
			{
				b.Opacity = Element.ShowArrows ? 1.0 : 0.0;
				b.IsHitTestVisible = Element.ShowArrows;
			}
		}

		private childItemType FindVisualChild<childItemType>(DependencyObject obj, string name) where childItemType : FrameworkElement
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(obj, i);
				if (child is childItemType && ((FrameworkElement)child).Name == name)
					return (childItemType)child;
				else
				{
					childItemType childOfChild = FindVisualChild<childItemType>(child, name);
					if (childOfChild != null)
						return childOfChild;
				}
			}
			return null;
		}

		/*public List<Control> AllChildren(DependencyObject parent)
        {
            var _list = new List<Control>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var _child = VisualTreeHelper.GetChild(parent, i);
                if (_child is Control)
                    _list.Add(_child as Control);
                _list.AddRange(AllChildren(_child));              
            }
            return _list;
        }*/

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				if (flipView != null)
				{
					flipView.SelectionChanged -= FlipView_SelectionChanged;
					flipView = null;
				}

				if (Element != null)
				{
					if (Element.ItemsSource != null && Element.ItemsSource is INotifyCollectionChanged)
						((INotifyCollectionChanged)Element.ItemsSource).CollectionChanged -= ItemsSource_CollectionChanged;
				}

				indicators = null;

				nativeView = null;

				_disposed = true;
			}

			try
			{
				base.Dispose(disposing);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				return;
			}
		}
	}

	// UWP DataTemplate doesn't support loadTemplate function as parameter
	// Having that, to render all the views ahead of time is not needed

	/*public class MyTemplateSelector : DataTemplateSelector
    {
        CarouselViewControl Element;

        public MyTemplateSelector(CarouselViewControl element)
        {
            Element = element;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            Xamarin.Forms.View formsView = null;
            var bindingContext = item;

            var dt = bindingContext as Xamarin.Forms.DataTemplate;

            // Support for List<DataTemplate> as ItemsSource
            if (dt != null)
			{
				formsView = (Xamarin.Forms.View)dt.CreateContent();
			}
			else {

				var selector = Element.ItemTemplate as Xamarin.Forms.DataTemplateSelector;
				if (selector != null)
					formsView = (Xamarin.Forms.View)selector.SelectTemplate(bindingContext, Element).CreateContent();
				else
					formsView = (Xamarin.Forms.View)Element.ItemTemplate.CreateContent();

				formsView.BindingContext = bindingContext;
			}

			formsView.Parent = this.Element;


			var element = FormsViewToNativeUWP.ConvertFormsToNative(formsView, new Xamarin.Forms.Rectangle(0, 0, ElementWidth, ElementHeight));

            var template = new DataTemplate(() => return element; ); // THIS IS NOT SUPPORTED :(

            return template;
        }
    }*/
}