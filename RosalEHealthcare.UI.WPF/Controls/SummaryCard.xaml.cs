using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class SummaryCard : UserControl
    {
        public SummaryCard()
        {
            InitializeComponent();
            this.Loaded += SummaryCard_Loaded;
        }

        private void SummaryCard_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply initial values after control is loaded
            ApplyInitialValues();
        }

        private void ApplyInitialValues()
        {
            if (TitleTextBlock != null) TitleTextBlock.Text = Title;
            if (ValueTextBlock != null) ValueTextBlock.Text = Value;
            if (IconTextBlock != null) IconTextBlock.Text = Icon;
            if (TopBorder != null) TopBorder.Background = TopColor;
            if (IconBorder != null) IconBorder.Background = IconBackground;
            if (TrendIconTextBlock != null)
            {
                TrendIconTextBlock.Text = TrendIcon;
                TrendIconTextBlock.Foreground = TrendColor;
            }
            if (TrendTextBlock != null)
            {
                TrendTextBlock.Text = TrendText;
                TrendTextBlock.Foreground = TrendColor;
            }
            if (TrendStackPanel != null)
            {
                TrendStackPanel.Visibility = string.IsNullOrEmpty(TrendText) ? Visibility.Collapsed : Visibility.Visible;
            }
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

        // Value Color (optional - for custom value coloring)
        public static readonly DependencyProperty ValueColorProperty =
            DependencyProperty.Register("ValueColor", typeof(Brush), typeof(SummaryCard),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(51, 51, 51)), OnValueColorChanged));

        public Brush ValueColor
        {
            get { return (Brush)GetValue(ValueColorProperty); }
            set { SetValue(ValueColorProperty, value); }
        }

        private static void OnValueColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SummaryCard;
            if (control?.ValueTextBlock != null)
                control.ValueTextBlock.Foreground = e.NewValue as Brush;
        }

        #endregion

        #region Click Event

        public static readonly RoutedEvent CardClickEvent = EventManager.RegisterRoutedEvent(
            "CardClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SummaryCard));

        public event RoutedEventHandler CardClick
        {
            add { AddHandler(CardClickEvent, value); }
            remove { RemoveHandler(CardClickEvent, value); }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            RaiseEvent(new RoutedEventArgs(CardClickEvent));
        }

        #endregion

        #region Mouse Events

        private void CardBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            var storyboard = new Storyboard();

            // Create Y translation animation
            var yAnimation = new DoubleAnimation
            {
                To = -5,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Ensure transform exists
            if (CardBorder.RenderTransform == null || !(CardBorder.RenderTransform is TranslateTransform))
            {
                CardBorder.RenderTransform = new TranslateTransform();
            }

            Storyboard.SetTarget(yAnimation, CardBorder);
            Storyboard.SetTargetProperty(yAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            storyboard.Children.Add(yAnimation);
            storyboard.Begin();

            // Also animate shadow
            AnimateShadow(15, 0.15);
        }

        private void CardBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            var storyboard = new Storyboard();

            var yAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(yAnimation, CardBorder);
            Storyboard.SetTargetProperty(yAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            storyboard.Children.Add(yAnimation);
            storyboard.Begin();

            // Reset shadow
            AnimateShadow(15, 0.08);
        }

        private void AnimateShadow(double blurRadius, double opacity)
        {
            var shadow = CardBorder.Effect as DropShadowEffect;
            if (shadow != null)
            {
                var blurAnimation = new DoubleAnimation
                {
                    To = blurRadius,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                var opacityAnimation = new DoubleAnimation
                {
                    To = opacity,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnimation);
                shadow.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnimation);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Animate the value change
        /// </summary>
        public void AnimateValueChange(string newValue)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
            fadeOut.Completed += (s, e) =>
            {
                Value = newValue;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(100));
                ValueTextBlock.BeginAnimation(OpacityProperty, fadeIn);
            };
            ValueTextBlock.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// Set all card properties at once
        /// </summary>
        public void SetCardData(string title, string value, string icon, Brush topColor, Brush iconBg, string trendText, string trendIcon, Brush trendColor)
        {
            Title = title;
            Value = value;
            Icon = icon;
            TopColor = topColor;
            IconBackground = iconBg;
            TrendText = trendText;
            TrendIcon = trendIcon;
            TrendColor = trendColor;
        }

        #endregion
    }
}