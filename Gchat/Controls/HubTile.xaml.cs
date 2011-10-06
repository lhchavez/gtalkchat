// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Globalization;

namespace Gchat.Controls {
    /// <summary>
    /// Represents an animated tile that supports an image and a title.
    /// Furthermore, it can also be associated with a message or a notification.
    /// </summary>
    /// <QualityBand>Preview</QualityBand>
    public partial class HubTile : UserControl {
        /// <summary>
        /// Represents the number of steps inside the pipeline of stalled images
        /// </summary>
        internal int _stallingCounter;

        /// <summary>
        /// Flag that determines if the hub tile has a primary text string associated to it.
        /// If it does not, the hub tile will not drop.
        /// </summary>
        internal bool _canDrop;

        /// <summary>
        /// Flag that determines if the hub tile has a secondary text string associated to it.
        /// If it does not, the hub tile will not flip.
        /// </summary>
        internal bool _canFlip;

        #region Source DependencyProperty

        /// <summary>
        /// Gets or sets the image source.
        /// </summary>
        public ImageSource Source {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Identifies the Source dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ImageSource), typeof(HubTile), new PropertyMetadata(null));

        #endregion

        #region Title DependencyProperty

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Identifies the Title dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(HubTile), new PropertyMetadata(string.Empty, new PropertyChangedCallback(OnTitleChanged)));

        /// <summary>
        /// Prevents the hub tile from transitioning into a Semiexpanded or Collapsed visual state if the title is not set.
        /// </summary>
        /// <param name="obj">The dependency object.</param>
        /// <param name="e">The event information.</param>
        private static void OnTitleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            HubTile tile = (HubTile)obj;

            if (string.IsNullOrEmpty((string)e.NewValue)) {
                tile._canDrop = false;
                tile.State = ImageState.Expanded;
            } else {
                tile._canDrop = true;
            }
        }

        #endregion

        #region Notification DependencyProperty

        /// <summary>
        /// Gets or sets the notification alert.
        /// </summary>
        public string Notification {
            get { return (string)GetValue(NotificationProperty); }
            set { SetValue(NotificationProperty, value); }
        }

        /// <summary>
        /// Identifies the Notification dependency property.
        /// </summary>
        public static readonly DependencyProperty NotificationProperty =
            DependencyProperty.Register("Notification", typeof(string), typeof(HubTile), new PropertyMetadata(string.Empty, new PropertyChangedCallback(OnBackContentChanged)));

        /// <summary>
        /// Prevents the hub tile from transitioning into a Flipped visual state if neither the notification alert nor the message are set.
        /// </summary>
        /// <param name="obj">The dependency object.</param>
        /// <param name="e">The event information.</param>
        private static void OnBackContentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            HubTile tile = (HubTile)obj;

            // If there is a new notification or a message, the hub tile can flip.
            if ((!(string.IsNullOrEmpty(tile.Notification)) && tile.DisplayNotification)
                || (!(string.IsNullOrEmpty(tile.Message)) && !tile.DisplayNotification)) {
                tile._canFlip = true;
            } else {
                tile._canFlip = false;
                tile.State = ImageState.Expanded;
            }
        }

        #endregion

        #region Message DependencyProperty

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        /// <summary>
        /// Identifies the Message dependency property.
        /// </summary>
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(HubTile), new PropertyMetadata(string.Empty, new PropertyChangedCallback(OnBackContentChanged)));

        #endregion

        #region DisplayNotification DependencyProperty

        /// <summary>
        /// Gets or sets the flag for new notifications.
        /// </summary>
        public bool DisplayNotification {
            get { return (bool)GetValue(DisplayNotificationProperty); }
            set { SetValue(DisplayNotificationProperty, value); }
        }

        /// <summary>
        /// Identifies the DisplayNotification dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayNotificationProperty =
            DependencyProperty.Register("DisplayNotification", typeof(bool), typeof(HubTile), new PropertyMetadata(false, new PropertyChangedCallback(OnBackContentChanged)));

        #endregion

        #region IsFrozen DependencyProperty

        /// <summary>
        /// Gets or sets the flag for images that do not animate.
        /// </summary>
        public bool IsFrozen {
            get { return (bool)GetValue(IsFrozenProperty); }
            set { SetValue(IsFrozenProperty, value); }
        }

        /// <summary>
        /// Identifies the IsFrozen dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFrozenProperty =
            DependencyProperty.Register("IsFrozen", typeof(bool), typeof(HubTile), new PropertyMetadata(false, new PropertyChangedCallback(OnIsFrozenChanged)));

        /// <summary>
        /// Removes the frozen image from the enabled image pool or the stalled image pipeline.
        /// Adds the non-frozen image to the enabled image pool.  
        /// </summary>
        /// <param name="obj">The dependency object.</param>
        /// <param name="e">The event information.</param>
        private static void OnIsFrozenChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            HubTile tile = (HubTile)obj;

            if ((bool)e.NewValue) {
                HubTileService.FreezeHubTile(tile);
            } else {
                HubTileService.UnfreezeHubTile(tile);
            }
        }

        #endregion

        #region GroupTag DependencyProperty

        /// <summary>
        /// Gets or sets the group tag.
        /// </summary>
        public string GroupTag {
            get { return (string)GetValue(GroupTagProperty); }
            set { SetValue(GroupTagProperty, value); }
        }

        /// <summary>
        /// Identifies the GroupTag dependency property.
        /// </summary>
        public static readonly DependencyProperty GroupTagProperty =
            DependencyProperty.Register("GroupTag", typeof(string), typeof(HubTile), new PropertyMetadata(string.Empty));

        #endregion

        #region State DependencyProperty

        /// <summary>
        /// Gets or sets the visual state.
        /// </summary>
        internal ImageState State {
            get { return (ImageState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        /// <summary>
        /// Identifies the State dependency property.
        /// </summary>
        private static readonly DependencyProperty StateProperty =
                DependencyProperty.Register("State", typeof(ImageState), typeof(HubTile), new PropertyMetadata(ImageState.Expanded, OnImageStateChanged));

        /// <summary>
        /// Triggers the transition between visual states.
        /// </summary>
        /// <param name="obj">The dependency object.</param>
        /// <param name="e">The event information.</param>
        private static void OnImageStateChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            ((HubTile)obj).UpdateVisualState();
        }

        #endregion

        /// <summary>
        /// Updates the visual state.
        /// </summary>
        private void UpdateVisualState() {
            /*VisualState state;

            switch (State) {
                case ImageState.Expanded:
                    state = Expanded;
                    break;
                case ImageState.Semiexpanded:
                    state = Semiexpanded;
                    break;
                case ImageState.Collapsed:
                    state = Collapsed;
                    break;
                case ImageState.Flipped:
                    state = Flipped;
                    break;
                default:
                    state = Expanded;
                    break;
            }
            */
            string state = State.ToString();
            VisualStateManager.GoToState(this, state, true);
        }

        /// <summary>
        /// Initializes a new instance of the HubTile class.
        /// </summary>
        public HubTile() {
            //DefaultStyleKey = typeof(HubTile);
            //Style = (Style)Resources["HubTileStyle"];
            Loaded += HubTile_Loaded;
            Unloaded += HubTile_Unloaded;
        }

        /// <summary>
        /// This event handler gets called as soon as a hub tile is added to the visual tree.
        /// A reference of this hub tile is passed on to the service singleton.
        /// </summary>
        /// <param name="sender">The hub tile.</param>
        /// <param name="e">The event information.</param>
        void HubTile_Loaded(object sender, RoutedEventArgs e) {
            HubTileService.InitializeReference(this);
        }

        /// <summary>
        /// This event handler gets called as soon as a hub tile is removed from the visual tree.
        /// Any existing reference of this hub tile is eliminated from the service singleton.
        /// </summary>
        /// <param name="sender">The hub tile.</param>
        /// <param name="e">The event information.</param>
        void HubTile_Unloaded(object sender, RoutedEventArgs e) {
            HubTileService.FinalizeReference(this);
        }
    }

    /// <summary>
    /// Represents the visual states of a Hub tile.
    /// </summary>
    internal enum ImageState {
        /// <summary>
        /// Expanded visual state value.
        /// </summary>
        Expanded = 0,

        /// <summary>
        /// Semiexpanded visual state value.
        /// </summary>
        Semiexpanded = 1,

        /// <summary>
        /// Collapsed visual state value.
        /// </summary>
        Collapsed = 2,

        /// <summary>
        /// Flipped visual state value.
        /// </summary>
        Flipped = 3,
    };

    /// <summary>
    /// Provides organized animations for the hub tiles.
    /// </summary>
    /// <QualityBand>Preview</QualityBand>
    public static class HubTileService {
        /// <summary>
        /// Number of steps in the pipeline
        /// </summary>
        private const int WaitingPipelineSteps = 3;

        /// <summary>
        /// Number of hub tile that can be animated at exactly the same time.
        /// </summary>
        private const int NumberOfSimultaneousAnimations = 1;

        /// <summary>
        /// Track resurrection for weak references.
        /// </summary>
        private const bool TrackResurrection = false;

        /// <summary>
        /// Timer to trigger animations in timely.
        /// </summary>        
        private static DispatcherTimer Timer = new DispatcherTimer();

        /// <summary>
        /// Random number generator to take certain random decisions.
        /// e.g. which hub tile is to be animated next.
        /// </summary>
        private static Random ProbabilisticBehaviorSelector = new Random();

        /// <summary>
        /// Pool that contains references to the hub tiles that are not frozen.
        /// i.e. hub tiles that can be animated at the moment.
        /// </summary>
        private static List<WeakReference> EnabledImagesPool = new List<WeakReference>();

        /// <summary>
        /// Pool that contains references to the hub tiles which are frozen.
        /// i.e. hub tiles that cannot be animated at the moment.
        /// </summary>
        private static List<WeakReference> FrozenImagesPool = new List<WeakReference>();

        /// <summary>
        /// Pipeline that contains references to the hub tiles that where animated previously.
        /// These are stalled briefly before they can be animated again.
        /// </summary>
        private static List<WeakReference> StalledImagesPipeline = new List<WeakReference>();

        /// <summary>
        /// Static constructor to add the tick event handler.
        /// </summary>        
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Attaching event handlers cannot be done inline.")]
        static HubTileService() {
            Timer.Tick += OnTimerTick;
        }

        /// <summary>
        /// Restart the timer to trigger animations.
        /// </summary>
        private static void RestartTimer() {
            if (!Timer.IsEnabled) {
                Timer.Interval = TimeSpan.FromMilliseconds(2500);
                Timer.Start();
            }
        }

        /// <summary>
        /// Add a reference to a newly instantiated hub tile.
        /// </summary>
        /// <param name="tile">The newly instantiated hub tile.</param>
        internal static void InitializeReference(HubTile tile) {
            WeakReference wref = new WeakReference(tile, TrackResurrection);
            if (tile.IsFrozen) {
                AddReferenceToFrozenPool(wref);
            } else {
                AddReferenceToEnabledPool(wref);
            }

            RestartTimer();
        }

        /// <summary>
        /// Remove all references of a hub tile before finalizing it.
        /// </summary>
        /// <param name="tile">The hub tile that is to be finalized.</param>
        internal static void FinalizeReference(HubTile tile) {
            WeakReference wref = new WeakReference(tile, TrackResurrection);
            HubTileService.RemoveReferenceFromEnabledPool(wref);
            HubTileService.RemoveReferenceFromFrozenPool(wref);
            HubTileService.RemoveReferenceFromStalledPipeline(wref);
        }

        /// <summary>
        /// Add a reference of a hub tile to the enabled images pool.
        /// </summary>
        /// <param name="tile">The hub tile to be added.</param>
        private static void AddReferenceToEnabledPool(WeakReference tile) {
            if (!ContainsTarget(EnabledImagesPool, tile.Target)) {
                EnabledImagesPool.Add(tile);
            }
        }

        /// <summary>
        /// Add a reference of a hub tile to the frozen images pool.
        /// </summary>
        /// <param name="tile">The hub tile to be added.</param>
        private static void AddReferenceToFrozenPool(WeakReference tile) {
            if (!ContainsTarget(FrozenImagesPool, tile.Target)) {
                FrozenImagesPool.Add(tile);
            }
        }

        /// <summary>
        /// Add a reference of a hub tile to the stalled images pipeline.
        /// </summary>
        /// <param name="tile">The hub tile to be added.</param>
        private static void AddReferenceToStalledPipeline(WeakReference tile) {
            if (!ContainsTarget(StalledImagesPipeline, tile.Target)) {
                StalledImagesPipeline.Add(tile);
            }
        }

        /// <summary>
        /// Remove the reference of a hub tile from the enabled images pool.
        /// </summary>
        /// <param name="tile">The hub tile to be removed.</param>
        private static void RemoveReferenceFromEnabledPool(WeakReference tile) {
            RemoveTarget(EnabledImagesPool, tile.Target);
        }

        /// <summary>
        /// Remove the reference of a hub tile from the frozen images pool.
        /// </summary>
        /// <param name="tile">The hub tile to be removed.</param>
        private static void RemoveReferenceFromFrozenPool(WeakReference tile) {
            RemoveTarget(FrozenImagesPool, tile.Target);
        }

        /// <summary>
        /// Remove the reference of a hub tile from the stalled images pipeline.
        /// </summary>
        /// <param name="tile">The hub tile to be removed.</param>
        private static void RemoveReferenceFromStalledPipeline(WeakReference tile) {
            RemoveTarget(StalledImagesPipeline, tile.Target);
        }

        /// <summary>
        /// Determine if there is a reference to a known target in a list.
        /// </summary>
        /// <param name="list">The list to be examined.</param>
        /// <param name="target">The known target.</param>
        /// <returns>True if a reference to the known target exists in the list. False otherwise.</returns>
        private static bool ContainsTarget(List<WeakReference> list, Object target) {
            for (int i = 0; i < list.Count; i++) {
                if (list[i].Target == target) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Remove a reference to a known target in a list.
        /// </summary>
        /// <param name="list">The list to be examined.</param>
        /// <param name="target">The known target.</param>
        private static void RemoveTarget(List<WeakReference> list, Object target) {
            for (int i = 0; i < list.Count; i++) {
                if (list[i].Target == target) {
                    list.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Executes the code to process a visual transition:
        /// 1. Stop the timer.
        /// 2. Advances the stalled tiles to the next step in the pipeline.
        /// If there is at least one tile that can be currently animated ...
        /// 3. Animate as many tiles as indicated.
        /// 4. Select a tile andomly from the pool of enabled tiles.
        /// 5. Based on this tile's current visual state, move it onto 
        /// the next one.
        /// 6. Set the stalling counter for the recently animated image.
        /// 7. Take it out from the pool and into the pipeline to prevent it 
        /// from being animated continuously.
        /// 8. Restart the timer with a randomly generated time interval
        /// between 100 and 3000 ms.
        /// Notice that if there are no hub tiles that can be animated, 
        /// the timer is not restarted.
        /// </summary>
        /// <param name="sender">The static timer.</param>
        /// <param name="e">The event information.</param>
        private static void OnTimerTick(object sender, EventArgs e) {
            Timer.Stop();

            for (int i = 0; i < StalledImagesPipeline.Count; i++) {
                if ((StalledImagesPipeline[i].Target as HubTile)._stallingCounter-- == 0) {
                    AddReferenceToEnabledPool(StalledImagesPipeline[i]);
                    RemoveReferenceFromStalledPipeline(StalledImagesPipeline[i]);
                    i--;
                }
            }

            if (EnabledImagesPool.Count > 0) {
                for (int j = 0; j < NumberOfSimultaneousAnimations; j++) {
                    int index = ProbabilisticBehaviorSelector.Next(EnabledImagesPool.Count);

                    switch ((EnabledImagesPool[index].Target as HubTile).State) {
                        case ImageState.Expanded:
                            //If the tile can neither drop nor flip, do not change state.
                            if (!(EnabledImagesPool[index].Target as HubTile)._canDrop && !(EnabledImagesPool[index].Target as HubTile)._canFlip) {
                                break;
                            }

                            //If the tile can only flip, change to the Flipped state.
                            if (!(EnabledImagesPool[index].Target as HubTile)._canDrop && (EnabledImagesPool[index].Target as HubTile)._canFlip) {
                                (EnabledImagesPool[index].Target as HubTile).State = ImageState.Flipped;
                                break;
                            }

                            //If the tile can only drop, change to the Semidropped state.
                            if (!(EnabledImagesPool[index].Target as HubTile)._canFlip && (EnabledImagesPool[index].Target as HubTile)._canDrop) {
                                (EnabledImagesPool[index].Target as HubTile).State = ImageState.Semiexpanded;
                                break;
                            }

                            //If the tile can drop and flip, change randomly either to the Semidropped state or the Flipped state.
                            if (ProbabilisticBehaviorSelector.Next(2) == 0) {
                                (EnabledImagesPool[index].Target as HubTile).State = ImageState.Semiexpanded;
                            } else {
                                (EnabledImagesPool[index].Target as HubTile).State = ImageState.Flipped;
                            }
                            break;
                        case ImageState.Semiexpanded:
                            (EnabledImagesPool[index].Target as HubTile).State = ImageState.Collapsed;
                            break;
                        case ImageState.Collapsed:
                            (EnabledImagesPool[index].Target as HubTile).State = ImageState.Expanded;
                            break;
                        case ImageState.Flipped:
                            (EnabledImagesPool[index].Target as HubTile).State = ImageState.Expanded;
                            break;
                    }
                    (EnabledImagesPool[index].Target as HubTile)._stallingCounter = WaitingPipelineSteps;
                    AddReferenceToStalledPipeline(EnabledImagesPool[index]);
                    RemoveReferenceFromEnabledPool(EnabledImagesPool[index]);
                }
            } else if (StalledImagesPipeline.Count == 0) {
                return;
            }

            Timer.Interval = TimeSpan.FromMilliseconds(ProbabilisticBehaviorSelector.Next(1, 31) * 100);
            Timer.Start();
        }

        /// <summary>
        /// Freeze a hub tile.
        /// </summary>
        /// <param name="tile">The hub tile to be frozen.</param>
        public static void FreezeHubTile(HubTile tile) {
            WeakReference wref = new WeakReference(tile, TrackResurrection);
            AddReferenceToFrozenPool(wref);
            RemoveReferenceFromEnabledPool(wref);
            RemoveReferenceFromStalledPipeline(wref);
        }

        /// <summary>
        /// Unfreezes a hub tile and restarts the timer if needed.
        /// </summary>
        /// <param name="tile">The hub tile to be unfrozen.</param>
        public static void UnfreezeHubTile(HubTile tile) {
            WeakReference wref = new WeakReference(tile, TrackResurrection);
            AddReferenceToEnabledPool(wref);
            RemoveReferenceFromFrozenPool(wref);
            RemoveReferenceFromStalledPipeline(wref);

            RestartTimer();
        }

        /// <summary>
        /// Freezes all the hub tiles with the specified group tag that are not already frozen.
        /// </summary>
        /// <param name="group">The group tag representing the hub tiles that should be frozen.</param>
        public static void FreezeGroup(string group) {
            for (int i = 0; i < EnabledImagesPool.Count; i++) {
                if ((EnabledImagesPool[i].Target as HubTile).GroupTag == group) {
                    (EnabledImagesPool[i].Target as HubTile).IsFrozen = true;
                    i--;
                }
            }

            for (int j = 0; j < StalledImagesPipeline.Count; j++) {
                if ((StalledImagesPipeline[j].Target as HubTile).GroupTag == group) {
                    (StalledImagesPipeline[j].Target as HubTile).IsFrozen = true;
                    j--;
                }
            }
        }

        /// <summary>
        /// Unfreezes all the hub tiles with the specified group tag 
        /// that are currently frozen and restarts the timer if needed.
        /// </summary>
        /// <param name="group">The group tag representing the hub tiles that should be unfrozen.</param>
        public static void UnfreezeGroup(string group) {
            for (int i = 0; i < FrozenImagesPool.Count; i++) {
                if ((FrozenImagesPool[i].Target as HubTile).GroupTag == group) {
                    (FrozenImagesPool[i].Target as HubTile).IsFrozen = false;
                    i--;
                }
            }

            RestartTimer();
        }
    }

    /// <summary>
    /// Converts a multi-line string into a single line string.
    /// </summary>
    internal class MultipleToSingleLineStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return ((string)value).Replace(Environment.NewLine, " ");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// If there is a new notification (value)
    /// Returns a Visible value for the notification block (parameter).
    /// Or a Collapsed value for the message block (parameter).
    /// Returns a opposite values otherwise.
    /// </summary>
    internal class VisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if ((bool)value ^ (bool)parameter) {
                return Visibility.Visible;
            } else {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}