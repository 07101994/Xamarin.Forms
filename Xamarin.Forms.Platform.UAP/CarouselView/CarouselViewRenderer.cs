﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Xamarin.Forms.Platform.UAP;

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

        SolidColorBrush selectedColor;
        SolidColorBrush fillColor;

        double ElementWidth;
        double ElementHeight;

        bool IsLoading;
        bool IsRemoving;

        bool _disposed;

        ObservableCollection<FrameworkElement> Source;
        ObservableCollection<Shape> Dots;

        Timer timer;

        protected override void OnElementChanged(ElementChangedEventArgs<CarouselView> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                // Instantiate the native control and assign it to the Control property with
                // the SetNativeControl method

                if (Element.Orientation == CarouselViewOrientation.Horizontal)
                    nativeView = new FlipViewControl();
                else
                    nativeView = new VerticalFlipViewControl();

                flipView = nativeView.FindName("flipView") as FlipView;

                //flipView.ManipulationMode = Element.IsSwipingEnabled ? ManipulationModes.All : ManipulationModes.None;

                indicators = nativeView.FindName("indicators") as StackPanel;
                indicators.Visibility = Element.ShowIndicators ? Visibility.Visible : Visibility.Collapsed;

                var converter = new ColorConverter();
                selectedColor = (SolidColorBrush)converter.Convert(Element.CurrentPageIndicatorTintColor, null, null, null);
                fillColor = (SolidColorBrush)converter.Convert(Element.PageIndicatorTintColor, null, null, null);

                SetNativeControl(nativeView);
            }

            if (e.OldElement != null)
            {
                // Unsubscribe from event handlers and cleanup any resources
                if (flipView != null)
                {
                    flipView.Loaded -= FlipView_Loaded;
                    flipView.SelectionChanged -= FlipView_SelectionChanged;
                }

                if (Element != null)
                {
                    Element.RemoveAction = null;
                    Element.InsertAction = null;
                }
            }

            if (e.NewElement != null)
            {
                // Configure the control and subscribe to event handlers

                flipView.Loaded += FlipView_Loaded;

                flipView.SelectionChanged += FlipView_SelectionChanged;

                flipView.SizeChanged += FlipView_SizeChanged;

                Element.RemoveAction = new Func<int, Task>(RemoveItem);
                Element.InsertAction = new Func<object, int, Task>(InsertItem);
            }
        }      

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            var rect = this.Element.Bounds;

            switch (e.PropertyName)
            {
                case "Width":
                    if (ElementWidth == 0)
                        ElementWidth = rect.Width;
                    break;
                case "Height":
                    if (ElementHeight == 0)
                        ElementHeight = rect.Height;
                    break;
                case "ShowIndicators":
                    indicators.Visibility = Element.ShowIndicators ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case "ItemsSource":
                    ItemsSourceChanged();
                    break;
				case "Position":
					if (Element.Position != -1 && !isSwiping)
					    SetCurrentItem(Element.Position);
					break;
            }

            if (Source == null && ElementWidth > 0 && ElementHeight > 0)
            {
                ItemsSourceChanged();
            }
        }

		// To avoid triggering Position changed
		bool isSwiping;

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

            var size = (Windows.Foundation.Size)args;
            ElementWidth = size.Width;
            ElementHeight = size.Height;

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                ItemsSourceChanged();
            });
        }

        private void FlipView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Element.Arrows)
            {
                ButtonHide(flipView, "PreviousButtonHorizontal");
                ButtonHide(flipView, "NextButtonHorizontal");
                ButtonHide(flipView, "PreviousButtonVertical");
                ButtonHide(flipView, "NextButtonVertical");
            }
        }

        private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoading && !IsRemoving)
            {
				isSwiping = true;
                Element.Position = flipView.SelectedIndex;
				isSwiping = false;

                UpdateIndicators();

                Element.PositionSelected?.Invoke(Element, EventArgs.Empty);
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

        public void ItemsSourceChanged()
        {
			if (Element != null && flipView != null)
			{
                if (Element.ItemsSource != null && Element.ItemsSource?.Count > 0)
                {
                    IsLoading = true;

					isSwiping = true;
					if (Element.ItemsSource != null)
					{
						if (Element.Position > Element.ItemsSource.Count - 1)
							Element.Position = Element.ItemsSource.Count - 1;

						if (Element.Position == -1)
							Element.Position = 0;
					}
					else {
						Element.Position = 0;
					}
					isSwiping = false;

                    var source = new List<FrameworkElement>();

                    //for (int j = 0; j <= Element.Position; j++)
                    for (int j = 0; j <= Element.ItemsSource.Count - 1; j++)
                    {
                        source.Add(CreateView(Element.ItemsSource[j]));
                    }

                    Source = new ObservableCollection<FrameworkElement>(source);

                    flipView.ItemsSource = Source;
                    //flipView.ItemsSource = Element.ItemsSource;
                    //flipView.ItemTemplateSelector = new MyTemplateSelector(Element);

                    var dots = new List<Shape>();

                    int i = 0;
                    foreach (var item in Element.ItemsSource)
                    {
                        dots.Add(CreateDot(i, Element.Position));
                        i++;
                    }

                    Dots = new ObservableCollection<Shape>(dots);

                    var dotsPanel = nativeView.FindName("dotsPanel") as ItemsControl;
                    dotsPanel.ItemsSource = Dots;

                    flipView.SelectedIndex = Element.Position;

                    //await Task.Delay(100);

                    /*for (var j = Element.Position + 1; j <= Element.ItemsSource.Count - 1; j++)
                    {
                        Source.Add(CreateView(Element.ItemsSource[j]));
                    }*/

                    IsLoading = false;
                }
                else
                {
					isSwiping = true;
					Element.Position = 0;
					isSwiping = false;
					
                    var source = new List<FrameworkElement>();
                    Source = new ObservableCollection<FrameworkElement>(source);

                    flipView.ItemsSource = Source;

                    var dots = new List<Shape>();
                    Dots = new ObservableCollection<Shape>(dots);

                    var dotsPanel = nativeView.FindName("dotsPanel") as ItemsControl;
                    dotsPanel.ItemsSource = Dots;
                }

				isSwiping = false;

				Element.PositionSelected?.Invoke(Element, EventArgs.Empty);
			}
        }

		public async Task InsertItem(object item, int position)
		{
			if (Element != null && flipView != null && Element.ItemsSource != null)
			{
				if (position > Element.ItemsSource.Count + 1)
					throw new CarouselViewException("Page cannot be inserted at a position bigger than ItemsSource.Count");

				if (position == -1)
				{
                    Element.ItemsSource.Add(item);
                    Source.Add(CreateView(item));
                    Dots.Add(CreateDot(-1, position));
                }
				else
				{
                    Element.ItemsSource.Insert(position, item);
                    Source.Insert(position, CreateView(item));
                    Dots.Insert(position, CreateDot(position, position));
                }

                await Task.Delay(100);

				if (Element.ItemsSource.Count == 1)
					Element.PositionSelected?.Invoke(Element, EventArgs.Empty);
			}
		}

        public async Task RemoveItem(int position)
        {
            if (Element != null && flipView != null && Element.ItemsSource != null && Element.ItemsSource?.Count > 0)
            {
				if (position > Element.ItemsSource.Count - 1)
					throw new CarouselViewException("Page cannot be removed at a position bigger than ItemsSource.Count - 1");

				if (Element.ItemsSource?.Count == 1)
				{
					Element.ItemsSource.RemoveAt(position);
					ItemsSourceChanged();
				}
				else {

					IsRemoving = true;

					if (position == Element.Position)
					{
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

						await Task.Delay(100);
					}

                    Element.ItemsSource.RemoveAt(position);
                    Source.RemoveAt(position);
                    
                    IsRemoving = false;

					isSwiping = true;
					Element.Position = flipView.SelectedIndex;
					isSwiping = false;

					Dots.RemoveAt(position);
					UpdateIndicators();

					Element.PositionSelected?.Invoke(Element, EventArgs.Empty);
				}
            }
        }

        void SetCurrentItem(int position)
        {
            if (Element != null && flipView != null && Element.ItemsSource != null && Element.ItemsSource?.Count > 0)
            {
				if (position > Element.ItemsSource.Count - 1)
					throw new CarouselViewException("Current page index cannot be bigger than ItemsSource.Count - 1");
				
                flipView.SelectedIndex = position;
            }
        }

        FrameworkElement CreateView(object item)
        {
            Xamarin.Forms.View formsView = null;
            var bindingContext = item;

			var dt = bindingContext as Xamarin.Forms.DataTemplate;

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

            //element.ManipulationMode = Element.IsSwipingEnabled ? ManipulationModes.All : ManipulationModes.None;

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
				b.Opacity = 0.0;
				b.IsHitTestVisible = false;
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

        /// <summary>
        /// Used for registration with dependency service
        /// </summary>
        public static void Init()
        {
            var temp = DateTime.Now;
        }
    }

    /*public class MyTemplateSelector : DataTemplateSelector
    {
        CarouselView Element;

        public MyTemplateSelector(CarouselView element)
        {
            Element = element;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            Xamarin.Forms.View formsView = null;
            var bindingContext = item;

            var selector = Element.ItemTemplate as Xamarin.Forms.DataTemplateSelector;
            if (selector != null)
                formsView = (Xamarin.Forms.View)selector.SelectTemplate(bindingContext, Element).CreateContent();
            else
                formsView = (Xamarin.Forms.View)Element.ItemTemplate.CreateContent();

            formsView.BindingContext = bindingContext;
            formsView.Parent = this.Element;

            var element = FormsViewToNativeUWP.ConvertFormsToNative(formsView, new Xamarin.Forms.Rectangle(0, 0, 300, 300));

            var template = CreateDateTemplate();
            var content = (StackPanel)template.LoadContent();
            
            content.Children.Add(element);

            return template;
        }

        DataTemplate CreateDateTemplate()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">");
            sb.Append("<StackPanel>");
            sb.Append("<StackPanel Orientation=\"Horizontal\" Margin=\"3,3,0,3\"><TextBlock Text=\"Name:\" Margin=\"0,0,5,0\"/><TextBlock Text=\"Name\"/></StackPanel>");
            sb.Append("<StackPanel Orientation=\"Horizontal\" Margin=\"3,3,0,3\"><TextBlock Text=\"Price:\" Margin=\"0,0,5,0\"/><TextBlock Text=\"Price\"/></StackPanel>");
            sb.Append("<StackPanel Orientation=\"Horizontal\" Margin=\"3,3,0,3\"><TextBlock Text=\"Author:\" Margin=\"0,0,5,0\"/><TextBlock Text=\"Author\"/></StackPanel>");
            sb.Append("</StackPanel>");
            sb.Append("</DataTemplate>");

            return  (DataTemplate)XamlReader.Load(sb.ToString());
        }
    }*/
}