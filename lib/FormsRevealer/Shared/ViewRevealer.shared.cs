﻿using System;
using System.ComponentModel;
using System.IO;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace FormsRevealer.Shared
{
    [Preserve(AllMembers = true)]
    [DesignTimeVisible(true)]
    public partial class ViewRevealer : ContentView
    {
        Grid _rootLayout;
        SKBitmap _childViewImage;
        SKCanvasView _canvasView;
        private float _revealProgress;

        private RevealState _state;

        public ViewRevealer()
        {
            // dependency service that creates a bitmap from a XF.View:
            // https://forums.xamarin.com/discussion/75408/is-it-possible-to-create-an-image-from-a-view-in-xamarin-forms
            // https://michaelridland.com/xamarin/creating-native-view-xamarin-forms-viewpage/

            // create an SKBitmap or better with that

            // reveal result in canvas

            // when done hide the canvas and show the childView

            this.BackgroundColor = Color.Transparent;
            _state = RevealState.Hidden;
            //ToDo: make InputTransparent while NOT Revealed
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();

            _canvasView = new SKCanvasView();
            _canvasView.PaintSurface += Paint;

            _rootLayout = new Grid();

            ChildView.Opacity = 0.0;
            _rootLayout.Children.Add(ChildView);

            Content = _rootLayout;

            //StartRevealAnimation();
        }

        public void StartRevealAnimation()
        {
            _state = RevealState.Revealing;

            ChildView.Opacity = 0.0;

            //_childViewImage = _childViewImage ?? GetChildViewImage();
            _childViewImage = GetChildViewImage();
            _rootLayout.Children.Add(_canvasView);

            var revealAnimation = new Animation(
                interpolatedValue =>
                {
                    _revealProgress = (float)interpolatedValue;
                    _canvasView.InvalidateSurface();
                },
                easing: RevealEasing
            );

            revealAnimation.Commit(this, "RevealAnimation", length: Convert.ToUInt32(RevealDurationMillis), finished: (d, b) =>
            {
                _state = RevealState.Revealed;
                ChildView.Opacity = 1.0;
                _rootLayout.Children.Remove(_canvasView);
            });
        }

        public void StartHidingAnimation()
        {
            _state = RevealState.Hiding;

            _childViewImage = GetChildViewImage();
            _rootLayout.Children.Add(_canvasView);
            ChildView.Opacity = 0.0;

            var hideAnimation = new Animation(
                interpolatedValue =>
                {
                    _revealProgress = (float)interpolatedValue;
                    _canvasView.InvalidateSurface();
                },
                start: 1.0,
                end: 0.0,
                easing: RevealEasing
            );

            hideAnimation.Commit(this, "HideAnimation", length: Convert.ToUInt32(RevealDurationMillis), finished: (d, b) =>
            {
                _state = RevealState.Hidden;
                _rootLayout.Children.Remove(_canvasView);
            });
        }

        private void Paint(object sender, SKPaintSurfaceEventArgs args)
        {
            var info = args.Info;
            var surface = args.Surface;
            var canvas = args.Surface.Canvas;

            canvas.Clear(SKColors.Transparent);

            var clippingPath = new SKPath();
            var hypotenuse = Math.Sqrt(Math.Pow(info.Width, 2) + Math.Pow(info.Height, 2));
            var radius = (float)(_revealProgress * hypotenuse);
            clippingPath.AddCircle(info.Width * HorizontalRevealAnchor,
                                    info.Height * VerticalRevealAnchor,
                                    radius);

            canvas.ClipPath(clippingPath);

            canvas.DrawBitmap(_childViewImage, new SKPoint(0.0f, 0.0f));
        }

        private SKBitmap GetChildViewImage()
        {
            var r = DependencyService.Get<IViewImageProvider>(DependencyFetchTarget.GlobalInstance).GetViewImage(ChildView);
            SKCodec codec = SKCodec.Create(new MemoryStream(r));
            return SKBitmap.Decode(codec);

            //string resourceID = "FormsRevealer.Sample.ape.jpg";
            //Assembly assembly = GetType().GetTypeInfo().Assembly;

            //SKBitmap resourceBitmap;
            //using (Stream stream = assembly.GetManifestResourceStream(resourceID))
            //{
            //    resourceBitmap = SKBitmap.Decode(stream);
            //}

            //return resourceBitmap;
        }
    }

    public enum RevealState
    {
        Hidden,
        Revealing,
        Hiding,
        Revealed
    }
}