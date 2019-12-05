using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Resources;
using System.Globalization;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;

namespace MultimediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MediaViewModel ViewModel = new MediaViewModel();
        private bool IsMouseCapture = false;
        private Point SavedMousePoint;
        public MainWindow()
        {
            DataContext = ViewModel;
            InitializeComponent();
        }
        private void OnCaptureSongPosition(object sender, MouseButtonEventArgs e)
        {
            SongProgressBar.CaptureMouse();
            IsMouseCapture = true;
        }
        private void OnReleaseSongPosition(object sender, MouseButtonEventArgs e)
        {
            SongProgressBar.ReleaseMouseCapture();
            IsMouseCapture = false;

            ViewModel.PlayAtPercentage(SongProgressBar.Value);
            //rebinding
            Binding binding = new Binding
            {
                Source = ViewModel.SongProgress,
                Mode = BindingMode.OneWay
            };
            SongProgressBar.SetBinding(ProgressBar.ValueProperty, binding);
        }
        private void OnUpdateSongPosition(object sender, MouseEventArgs e)
        {
            if(IsMouseCapture && e.LeftButton == MouseButtonState.Pressed)
            {
                Point mouse = e.GetPosition(this);
                Point progressbar = SongProgressBar.TransformToAncestor(this).Transform(new Point(0, 0));
                double percent = (mouse.X - progressbar.X) / SongProgressBar.ActualWidth * 100.0;
                SongProgressBar.Value = percent;
            }
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Initialize();
        }


        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel.SaveAllPlaylist();
            ViewModel.Dispose();
        }
        private void OnListboxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbPlayList.SelectedItem is Playlist playlist)
            {
                ViewModel.LoadSongsFromPlaylist(playlist).ContinueWith(task =>
                {
                    IEnumerable<SongInfo> songs = task.Result;
                    ViewModel.CurrentSongCollection = new ObservableCollection<SongInfo>(songs);
                    ViewModel.CurrentSelectedPlaylistFile = playlist;
                });
            }
        }

        private void OnClickPreviousSong(object sender, RoutedEventArgs e)
        {
            ViewModel.PlayPreviousSong();
        }

        private void OnClickBackwardSong(object sender, RoutedEventArgs e)
        {
            ViewModel.MovePositionBackward();
        }

        private void OnClickSwitchPlayMode(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPlayList.Count == 0)
            {
                OnPlaySelectedSong(null, null);
            }
            else ViewModel.SwitchPlayMode();
        }

        private void OnClickForwardSong(object sender, RoutedEventArgs e)
        {
            ViewModel.MovePositionForward();
        }

        private void OnClickNextSong(object sender, RoutedEventArgs e)
        {
            ViewModel.PlayNextSong();
        }
        private void OnClickStopSong(object sender, RoutedEventArgs e)
        {
            ViewModel.StopPlaying();
        }

        private void OnClickReplaySong(object sender, RoutedEventArgs e)
        {
            ViewModel.IsReplayOn = !ViewModel.IsReplayOn;
        }

        private void OnClickShuffleSong(object sender, RoutedEventArgs e)
        {
            ViewModel.IsShuffleOn = !ViewModel.IsShuffleOn;
        }

        private void OnClickMuteVolumn(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SongVolume == 0) ViewModel.SongVolume = ViewModel.SavedVolume;
            else { ViewModel.SavedVolume = ViewModel.SongVolume; ViewModel.SongVolume = 0; }
        }

        private void OnPlaySelectedSong(object sender, MouseButtonEventArgs e)
        {
            List<Uri> collection = new List<Uri>();
            for (int i = 0; i < SongDataGrid.Items.Count; ++i)
            {
                SongInfo song = SongDataGrid.Items[i] as SongInfo;
                collection.Add(song.Uri);
            }
            int index = SongDataGrid.SelectedIndex < 0 ? 0 : SongDataGrid.SelectedIndex;
            ViewModel.PlayAsSelectedPlaylist(collection, index);
        }

        private void OnClickRecyclePlaylist(object sender, RoutedEventArgs e)
        {
            ViewModel.IsRecycleOn = !ViewModel.IsRecycleOn; 
        }

        private void OnCaptureVolume(object sender, MouseButtonEventArgs e)
        {
            VolumeProgressBar.CaptureMouse();
            IsMouseCapture = true;

            // save mouse's position at capture point and use this value to calculate offset
            SavedMousePoint = e.GetPosition(this);

            // save progress's value at capture time and use this value in OnUpdateVolume
            VolumeProgressBar.Tag = VolumeProgressBar.Value + SavedMousePoint.X - (VolumeProgressBar.TransformToAncestor(this).Transform(new Point(0, 0)).X + VolumeProgressBar.Value);
        }

        private void OnReleaseVolume(object sender, MouseButtonEventArgs e)
        {
            VolumeProgressBar.ReleaseMouseCapture();
            IsMouseCapture = false;
        }

        private void OnUpdateVolume(object sender, MouseEventArgs e)
        {
            if(IsMouseCapture && e.LeftButton == MouseButtonState.Pressed)
            {
                double offset = e.GetPosition(this).X - SavedMousePoint.X;
                double value = Convert.ToDouble(VolumeProgressBar.Tag) + offset / VolumeProgressBar.ActualWidth * 100.0;
                value = value < 0 ? 0 : value;
                value = value > 100 ? 100 : value;
                VolumeProgressBar.Value = value;
            }
        }

        private void ExitCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute =  true;
        }

        private void ExitExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void PlayCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void PlayExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.SwitchPlayMode();
        }

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
            }
        }
        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.IsReadOnly = true;
                tb.Background = Brushes.Transparent;
            }
        }
        private void OnTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.IsReadOnly = true;
                tb.Background = Brushes.Transparent;
            }
        }
        private ChildItem FindVisualChild<ChildItem>(DependencyObject obj) where ChildItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is ChildItem) return (ChildItem)child;
                else
                {
                    ChildItem childOfChild = FindVisualChild<ChildItem>(child);
                    if (childOfChild != null) return childOfChild;
                }
            }
            return null;
        }

        private TextBox GetTextBoxFromSelectedItem(ListBox listbox)
        {
            if (listbox.SelectedIndex < 0) return null;
            ListBoxItem item = listbox.ItemContainerGenerator.ContainerFromIndex(lbPlayList.SelectedIndex) as ListBoxItem;
            ContentPresenter presenter = FindVisualChild<ContentPresenter>(item);
            return presenter.ContentTemplate.FindName("tbPlaylistName", presenter) as TextBox;
        }

        private void RenameCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (lbPlayList.SelectedIndex >= 0) e.CanExecute = true;
        }

        private void RenameExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            TextBox textbox = GetTextBoxFromSelectedItem(lbPlayList);
            if (textbox != null)
            {
                textbox.IsReadOnly = false;
                textbox.Background = Brushes.White;
                textbox.SelectAll();
                Keyboard.Focus(textbox).Focus();
            }
        }

        private void OnDisplayAllSongs(object sender, MouseButtonEventArgs e)
        {
            ViewModel.CurrentSelectedPlaylistFile = ViewModel.PlaylistLibrary;
            ViewModel.CurrentSongCollection = ViewModel.AllSongs;
        }

        private void OnSelectCurrentItem(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((ListBoxItem)sender).IsSelected = true;
        }

        private void OnMediaFilesDropped(object sender, DragEventArgs e)
        {
            // Filter media files
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            ViewModel.UpdatePlaylistFromStrings(ViewModel.CurrentSelectedPlaylistFile, files);
        }

        private void CreateCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CreateExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.PlaylistFiles.Add(new Playlist("unamed"));
        }

        private void OnDisplayStudentInformation(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Nguyễn Mạnh Tuấn 1712875", "Student");
        }

        private void OnDisplayGithubLink(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Nguyễn Mạnh Tuấn 1712875", "Student");
        }
    }
    public class LinkListNodeToUri : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is LinkedListNode<Uri> node ? node.Value : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TimeSpanToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                double duration = timeSpan.TotalSeconds;
                string hours = Math.Floor(duration / 3600).ToString("00");
                duration %= 3600.0;
                string minutes = Math.Floor(duration / 60).ToString("00");
                duration %= 60.0;
                string seconds = Math.Floor(duration).ToString("00");

                string output = $"{hours}:{minutes}:{seconds}";
                return output;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class RemoveExtension : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string playlist)
            {
                return System.IO.Path.GetFileNameWithoutExtension(playlist);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string playlist)
            {
                return System.IO.Path.ChangeExtension(playlist, ".xml");
            }
            return null;
        }
    }
    public class ZeroOneToPercentage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value) * 100.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value) / 100.0;
        }
    }
}
