using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace Arthur_Showdown_Game
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ScreenOrientation = ScreenOrientation.SensorLandscape, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AtivarModoImersivo();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
            {
                AtivarModoImersivo();
            }
        }

        private void AtivarModoImersivo()
        {
            if (Window == null) return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P && Window.Attributes != null)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }

            WindowCompat.SetDecorFitsSystemWindows(Window, false);
            var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (controller != null)
            {
                controller.Hide(WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars());
                controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            }

#pragma warning disable CS0618
            if (Window.DecorView != null)
            {
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
                    SystemUiFlags.ImmersiveSticky |
                    SystemUiFlags.LayoutStable |
                    SystemUiFlags.LayoutHideNavigation |
                    SystemUiFlags.LayoutFullscreen |
                    SystemUiFlags.HideNavigation |
                    SystemUiFlags.Fullscreen);
            }
#pragma warning restore CS0618
        }
    }
}
