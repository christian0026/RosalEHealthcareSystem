using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class SummaryCard : UserControl
    {
        public SummaryCard()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        // Title
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SummaryCard),
                new PropertyMetadata("Title", OnTitleChanged));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SummaryCard;
            if (control?.TitleTextBlock != null)
                control.TitleTextBlock.Text = e.NewValue?.ToString() ?? "";
        }

        // Value
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(SummaryCard),
                new PropertyMetadata("0", OnValueChanged));

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SummaryCard;
            if (control?.ValueTextBlock != null)
                control.ValueTextBlock.Text = e.NewValue?.ToString() ?? "0";
        }

        // Icon
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(string), typeof(SummaryCard),
                new PropertyMetadata("📊", OnIconChanged));

        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SummaryCard;
            if (control?.IconTextBlock != null)
                control.IconTextBlock.Text = e.NewValue?.ToString() ?? "📊";
        }

        // Top Color
        public static readonly DependencyProperty TopColorProperty =
            DependencyProperty.Register("TopColor", typeof(Brush), typeof(SummaryCard),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(76, 175, 80)), OnTopColorChanged));

        public Brush TopColor
        {
            get { return (Brush)GetValue(TopColorProperty); }
            set { SetValue(TopColorProperty, value); }
        }

        private static void OnTopColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SummaryCard;
            if (control?.TopBorder != null)
                control.TopBorder.Background = e.NewValue as Brush;
        }

        // Icon Background
        public static readonly DependencyProperty IconBackgroundProperty =
            DependencyProperty.Register("IconBackground", typeof(Brush), typeof(SummaryCard),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(232, 245, 233)), OnIconBackgroundChanged));

        public Brush IconBackground
        {
            get { return (Brush)GetValue(IconBackgroundProperty); }
            set { SetValue(IconBackgroundProperty, value); }
        }

        private static void OnIconBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SummaryCard;
            if (control?.IconBorder != null)
                control.IconBorder.Background = e.NewValue as Brush;
        }

        // Trend Text
        public static readonly DependencyProperty TrendTextProperty =
            DependencyProperty.Register("TrendText", typeof(string), typeof(SummaryCard),
                new PropertyMetadata("", OnTrendTextChanged));

        public string TrendText
        {
            get { return (string)GetValue(TrendTextProperty); }
            set { SetValue(TrendTextProperty, value); }
        }

        private static void OnTrendTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SummaryCard;
            var text = e.NewValue?.ToString() ?? "";

            if (control?.TrendStackPanel != null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    control.TrendStackPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    control.TrendStackPanel.Visibility = Visibility.Visible;
                    if (control.TrendTextBlock != null)
                        control.TrendTextBlock.Text = text;
                }
            }
        }

        // Trend Color
        public static readonly DependencyProperty TrendColorProperty =
            DependencyProperty.Register("TrendColor", typeof(Brush), typeof(SummaryCard),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(76, 175, 80)), OnTrendColorChanged));

        public Brush TrendColor
        {
            get { return (Brush)GetValue(TrendColorProperty); }
            set { SetValue(TrendColorProperty, value); }
        }

        private static void OnTrendColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SummaryCard;
            var brush = e.NewValue as Brush;

            if (control?.TrendIconTextBlock != null)
                control.TrendIconTextBlock.Foreground = brush;

            if (control?.TrendTextBlock != null)
                control.TrendTextBlock.Foreground = brush;
        }

        // Trend Icon
        public static readonly DependencyProperty TrendIconProperty =
            DependencyProperty.Register("TrendIcon", typeof(string), typeof(SummaryCard),
                new PropertyMetadata("▲", OnTrendIconChanged));

        public string TrendIcon
        {
            get { return (string)GetValue(TrendIconProperty); }
            set { SetValue(TrendIconProperty, value); }
        }

        private static void OnTrendIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SummaryCard;
            if (control?.TrendIconTextBlock != null)
                control.TrendIconTextBlock.Text = e.NewValue?.ToString() ?? "▲";
        }

        #endregion

        #region Mouse Events

        private void CardBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                To = -5,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(animation, CardBorder);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            CardBorder.RenderTransform = new TranslateTransform();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void CardBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(animation, CardBorder);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        #endregion
    }
}