using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Audsplit.UI;

internal static class AutoHideScroll
{
    private static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(900);
    private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(180));
    private static readonly DependencyProperty TimerProperty =
        DependencyProperty.RegisterAttached("Timer", typeof(DispatcherTimer), typeof(AutoHideScroll));
    private static readonly DependencyProperty AttachedProperty =
        DependencyProperty.RegisterAttached("Attached", typeof(bool), typeof(AutoHideScroll));

    public static void Attach(ScrollViewer viewer)
    {
        if ((bool)viewer.GetValue(AttachedProperty))
        {
            return;
        }

        viewer.SetValue(AttachedProperty, true);
        viewer.ScrollChanged += OnScrollChanged;
        viewer.PreviewMouseWheel += (_, _) => Show(viewer);
        viewer.Loaded += (_, _) =>
        {
            var bar = GetVerticalBar(viewer);
            if (bar is null)
            {
                return;
            }

            bar.MouseEnter += (_, _) => Show(viewer, hold: true);
            bar.MouseLeave += (_, _) => ScheduleHide(viewer);
            bar.Opacity = 0;
        };
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer viewer)
        {
            return;
        }

        if (Math.Abs(e.VerticalChange) < 0.01 && Math.Abs(e.ExtentHeightChange) < 0.01)
        {
            return;
        }

        Show(viewer);
    }

    private static void Show(ScrollViewer viewer, bool hold = false)
    {
        var bar = GetVerticalBar(viewer);
        if (bar is null || bar.Visibility != Visibility.Visible)
        {
            return;
        }

        bar.BeginAnimation(UIElement.OpacityProperty, null);
        bar.Opacity = 1;

        if (hold)
        {
            StopTimer(viewer);
            return;
        }

        ScheduleHide(viewer);
    }

    private static void ScheduleHide(ScrollViewer viewer)
    {
        var timer = viewer.GetValue(TimerProperty) as DispatcherTimer;
        if (timer is null)
        {
            timer = new DispatcherTimer { Interval = HideDelay };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                var bar = GetVerticalBar(viewer);
                if (bar is null || bar.IsMouseOver)
                {
                    return;
                }

                var fade = new DoubleAnimation(0, FadeDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                };
                bar.BeginAnimation(UIElement.OpacityProperty, fade);
            };
            viewer.SetValue(TimerProperty, timer);
        }

        timer.Stop();
        timer.Start();
    }

    private static void StopTimer(ScrollViewer viewer)
    {
        if (viewer.GetValue(TimerProperty) is DispatcherTimer timer)
        {
            timer.Stop();
        }
    }

    private static ScrollBar? GetVerticalBar(ScrollViewer viewer)
    {
        viewer.ApplyTemplate();
        return viewer.Template?.FindName("PART_VerticalScrollBar", viewer) as ScrollBar;
    }
}
