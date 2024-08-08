using System;
using UILayout;

namespace FaustVst
{
    public class ParameterDial : Dock
    {
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double DefaultValue { get; set; }
        public double RangePower { get; set; } = 1.0;
        public Action<double> ValueChangedAction { get; set; }
        public Action HoldAction { get; set; }

        ImageElement background;
        RotatingImageElement pointer;
        double currentValue;

        public ParameterDial()
        {
            MinValue = 0;
            MaxValue = 1;
            DefaultValue = 0.5;

            background = new ImageElement("DialBackground") { HorizontalAlignment = EHorizontalAlignment.Center, VerticalAlignment = EVerticalAlignment.Center };
            Children.Add(background);

            pointer = new RotatingImageElement("DialPointer") { HorizontalAlignment = EHorizontalAlignment.Center, VerticalAlignment = EVerticalAlignment.Center, Color = UIColor.Black };
            Children.Add(pointer);

            SetValue(DefaultValue);
        }

        public void SetDialColor(UIColor color)
        {
            background.Color = color;
        }

        public void SetPointerColor(UIColor color)
        {
            pointer.Color = color;
        }

        public void SetValue(double value)
        {
            currentValue = MathUtil.Clamp(value, MinValue, MaxValue);

            double val = (currentValue - MinValue) / (MaxValue - MinValue);

            double maxAngle = 143;

            double angle = -maxAngle + (val * maxAngle * 2);

            pointer.Rotation = MathUtil.ToRadians((float)angle);
        }

        double touchStartValue;

        public override bool HandleTouch(in Touch touch)
        {
            switch (touch.TouchState)
            {
                case ETouchState.Pressed:
                    CaptureTouch(touch);
                    touchStartValue = currentValue;
                    break;
                case ETouchState.Moved:
                case ETouchState.Held:
                    if (HaveTouchCapture)
                    {
                        double delta = TouchCaptureStartPosition.Y - touch.Position.Y;

                        double range = MaxValue - MinValue;

                        double newValue = touchStartValue + ((delta * range) / 160);    //(double)PixGame.Instance.ScreenPPI);

                        newValue = MathUtil.Clamp(newValue, MinValue, MaxValue);

                        SetValue(newValue);

                        if (ValueChangedAction != null)
                            ValueChangedAction(newValue);
                    }
                    break;
                case ETouchState.Released:
                case ETouchState.Invalid:
                    ReleaseTouch();
                    break;
                default:
                    break;
            }

            if (IsDoubleTap(touch))
            {
                SetValue(DefaultValue);

                if (ValueChangedAction != null)
                    ValueChangedAction(DefaultValue);
            }

            return true;
        }

        //public override bool HandleGesture(PixGesture gesture)
        //{
        //    if (gesture.GestureType == EPixGestureType.Hold)
        //    {
        //        if (HoldAction != null)
        //        {
        //            HoldAction();

        //            return true;
        //        }
        //    }

        //    return base.HandleGesture(gesture);
        //}
    }

    public class ParameterValueDisplay : NinePatchWrapper
    {
        public float HoldSeconds { get; set; }
        public float FadeSeconds { get; set; }
        public string ValueFormat { get; set; } = "0.0";

        float visibleSeconds = 0;
        StringBuilderTextBlock textBlock;
        double value = double.MinValue;

        public ParameterValueDisplay()
            : base(Layout.Current.GetImage("HoverTextOutline"))
        {
            Visible = false;

            HorizontalAlignment = EHorizontalAlignment.Center;
            VerticalAlignment = EVerticalAlignment.Center;

            HoldSeconds = 0.25f;
            FadeSeconds = 0.25f;

            textBlock = new StringBuilderTextBlock
            {
                TextColor = UIColor.Black,
                TextFont = Layout.Current.GetFont("SmallFont"),
                Margin = new LayoutPadding(5, 5),
                HorizontalAlignment = EHorizontalAlignment.Center,
                VerticalAlignment = EVerticalAlignment.Center
            };

            Child = textBlock;
        }

        public void SetValue(double value)
        {
            if (value != this.value)
            {
                this.value = value;

                textBlock.StringBuilder.Clear();
                textBlock.StringBuilder.AppendFormat(ValueFormat, value);
            }

            UpdateActive();
        }

        public void UpdateActive()
        {
            Visible = true;

            visibleSeconds = 0;

            Color = new UIColor((byte)Color.R, (byte)Color.G, (byte)Color.B, (byte)255);
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            if (Visible)
            {
                visibleSeconds += Layout.Current.SecondsElapsed;

                if (visibleSeconds > HoldSeconds)
                {
                    if (visibleSeconds > (HoldSeconds + FadeSeconds))
                    {
                        Visible = false;
                    }
                    else
                    {
                        byte alpha = (byte)(255 * MathUtil.Saturate(1.0f - ((visibleSeconds - HoldSeconds) / FadeSeconds)));

                        Color = new UIColor((byte)Color.R, (byte)Color.G, (byte)Color.B, alpha);
                    }
                }
            }
        }
    }
}
