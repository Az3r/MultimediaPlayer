using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using System.Windows.Controls;

namespace MultimediaPlayer
{
    public class MediaViewModel : INotifyPropertyChanged, IDisposable
    {
        public MediaViewModel() { }
        public void Initialize()
        {
            // Create Playlists and Library directory
            if (!Directory.Exists(PlaylistDir)) Directory.CreateDirectory(PlaylistDir);
            if (!Directory.Exists(SongDir)) Directory.CreateDirectory(SongDir);

            // load playlists from playlist directory
            GetPlaylistFiles();

            // load songs from library.xml or Library directory
            string library = Path.Combine(Environment.CurrentDirectory, "library.xml");
            if (File.Exists(library)) PlaylistLibrary = LoadPlaylistFromXmlFile(library);
            else PlaylistLibrary = CreatePlaylistFromDirectory("library.xml", Path.Combine(Environment.CurrentDirectory, SongDir));
            if (PlaylistLibrary != null)
            {
                BackgroundWorker worker = new BackgroundWorker();
                LoadSongsFromPlaylist(PlaylistLibrary).ContinueWith((task) =>
                {
                    IEnumerable<SongInfo> songs = task.Result;
                    foreach (SongInfo item in songs) AllSongs.Add(item);
                    CurrentSelectedPlaylistFile = PlaylistLibrary;
                    CurrentSongCollection = AllSongs;
                });
            }

            // init media player
            mMediaPlayer.MediaOpened += MediaOpened;
            mMediaPlayer.MediaEnded += MediaEnded;
            mMediaPlayer.MediaFailed += MediaFailed;
            mMediaPlayer.LoadedBehavior = MediaState.Manual;
            mMediaPlayer.UnloadedBehavior = MediaState.Stop;
            mMediaPlayer.Play();

            // init timer to update song's position
            mSongTimer = new Timer()
            {
                Enabled = true,
                Interval = 1000.0 / 60.0
            };
            mSongTimer.Elapsed += TimerUpdateSongPosition;
        }

        public void Dispose()
        {
            mSongTimer.Dispose();
            mSongTimer = null;
        }

        private void TimerUpdateSongPosition(object sender, ElapsedEventArgs e)
        {
            mMediaPlayer.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(SongPosition));
            });
        }
        public async Task<IEnumerable<SongInfo>> LoadSongsFromPlaylist(Playlist playlist)
        {
            Task<IEnumerable<SongInfo>> task = Task.Run<IEnumerable<SongInfo>>(() =>
            {
                if (playlist.SongLocations.Count == 0) return new SongInfo[0];
                List<SongInfo> list = new List<SongInfo>();
                foreach (var item in playlist.SongLocations)
                {
                    try
                    {
                        Uri uri = new Uri(item, UriKind.Absolute);
                        SongInfo song = GetSongInfo(uri);
                        list.Add(song);
                    }
                    catch (Exception e)
                    {
                        Helpers.ConsoleLogger.Error($"Error while importing file {item}: {e.GetType().Name} - {e.Message}");
                    }
                }
                return list;
            });
            return await task;
        }
        public void AddToPlaylistFile(Playlist playlist, IEnumerable<string> collection)
        {
            foreach (string item in collection)
            {
                playlist.SongLocations.Add(item);
            }
        }
        public async Task<IEnumerable<SongInfo>> LoadSongsFromUris(IEnumerable<Uri> collection)
        {
            Task<IEnumerable<SongInfo>> task = Task.Run<IEnumerable<SongInfo>>(() =>
            {
                List<SongInfo> songs = new List<SongInfo>();
                Parallel.ForEach(collection, (uri) =>
                {
                    try
                    {
                        SongInfo song = GetSongInfo(uri);
                        lock (mCollectionLock)
                        {
                            songs.Add(song);
                        }
                    }
                    catch (Exception e)
                    {
                        Helpers.ConsoleLogger.Error($"Error while importing file {uri.LocalPath}: {e.GetType().Name} - {e.Message}");
                    }
                });
                return songs;
            });
            return await task;
        }
        public async Task<IEnumerable<SongInfo>> LoadSongsFromLibrary(string path)
        {
            Task<IEnumerable<SongInfo>> task = Task.Run<IEnumerable<SongInfo>>(() =>
            {
                string library = Path.Combine(Environment.CurrentDirectory, path);
                List<SongInfo> songs = new List<SongInfo>();
                if (!Directory.Exists(library)) return songs;

                IEnumerable<string> files = Directory.EnumerateFiles(library, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(s =>
                    {
                        foreach (string type in MediaTypes)
                        {
                            if (s.EndsWith(type)) return true;
                        }
                        return false;
                    });
                Parallel.ForEach(files, (item) =>
                {
                    try
                    {
                        Uri uri = new Uri(item, UriKind.Absolute);
                        SongInfo song = GetSongInfo(uri);
                        lock (mCollectionLock)
                        {
                            songs.Add(song);
                        }
                    }
                    catch (Exception e)
                    {
                        Helpers.ConsoleLogger.Error($"Error while importing file {item}: {e.GetType().Name} - {e.Message}");
                    }
                });
                return songs;
            });
            return await task;
        }
        public SongInfo GetSongInfo(Uri uri)
        {
            TagLib.File metafile = TagLib.File.Create(uri.LocalPath);
            SongInfo song = new SongInfo()
            {
                Uri = uri,
                Artist = "Unknown Artist",
                Duration = metafile.Properties.Duration,
                ImageData = new byte[0],
                Album = "Unknown Album",
                Title = Path.GetFileName(uri.LocalPath)
            };
            song.Duration = metafile.Properties.Duration;

            TagLib.Tag tags = metafile.GetTag(TagLib.TagTypes.Id3v2);
            if (tags != null)
            {
                if (tags.Pictures.Length != 0) song.ImageData = tags.Pictures[0].Data.Data;
                else song.ImageData = new byte[0];

                if (!string.IsNullOrEmpty(tags.Title)) song.Title = tags.Title;
                if (!string.IsNullOrEmpty(tags.Album)) song.Album = tags.Album;
                if (string.IsNullOrEmpty(tags.FirstAlbumArtist))
                {
                    if (!string.IsNullOrEmpty(tags.FirstPerformer)) song.Artist = tags.FirstPerformer;
                }
                else song.Artist = tags.FirstAlbumArtist;
            }
            if (song.ImageData.Length != 0)
            {
                using (MemoryStream stream = new MemoryStream(song.ImageData))
                {
                    song.Image = BitmapFrame.Create(stream);
                }
            }
            else song.Image = new BitmapImage(new Uri("pack://application:,,,/icons/musical-note.png"));
            song.Image.Freeze();
            return song;
        }
        public int GetPlaylistFiles()
        {
            if (!Directory.Exists(PlaylistDir)) Directory.CreateDirectory(PlaylistDir);
            IEnumerable<string> files = Directory.EnumerateFiles(PlaylistDir, "*.xml", SearchOption.TopDirectoryOnly);
            PlaylistFiles.Clear();
            int count = 0;
            foreach (string item in files)
            {
                Playlist playlist = LoadPlaylistFromXmlFile(item);
                PlaylistFiles.Add(playlist);
                ++count;
            }

            return count;
        }
        public Playlist CreatePlaylistFromDirectory(string name, string dir)
        {
            if (!Directory.Exists(dir)) return null;
            IEnumerable<string> files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                                                    .Where(s =>
                                                    {
                                                        foreach (string type in MediaTypes)
                                                        {
                                                            if (s.EndsWith(type)) return true;
                                                        }
                                                        return false;
                                                    });
            return new Playlist(name, files);
        }
        private void DoUpdatePlaylist(object sender, DoWorkEventArgs e)
        {
            object[] paras = e.Argument as object[];
            Playlist playlist = paras[0] as Playlist;
            IEnumerable<string> collection = paras[1] as IEnumerable<string>;

            Helpers.ConsoleLogger.Info($"Recceive {collection.Count()} items");
            IEnumerable<string> mediafiles = collection.Where((s) =>
            {
                foreach (string type in MediaTypes)
                {
                    if (s.EndsWith(type)) return true;
                }
                return false;
            });
            Helpers.ConsoleLogger.Info($"Recceive {mediafiles.Count()} items after filtering");

            List<SongInfo> songs = new List<SongInfo>();
            foreach (string item in mediafiles)
            {
                try
                {
                    // get ony new songs from uris
                    Uri uri = new Uri(item);
                    if (playlist.SongLocations.Add(uri.LocalPath))
                    {
                        SongInfo songinfo = GetSongInfo(uri);
                        songs.Add(songinfo);
                        Helpers.ConsoleLogger.Info($"Receive new song with title: {songinfo.Title}");
                        Helpers.ConsoleLogger.Info($"Added new song {songinfo.Uri.LocalPath} to {playlist.Name}");
                    }
                }
                catch (Exception err)
                {
                    Helpers.ConsoleLogger.Error($"Error while importing file {item}: {err.GetType().Name} - {err.Message}");
                }
            }
            e.Result = songs;
        }
        public void UpdatePlaylistFromStrings(Playlist playlist, IEnumerable<string> collection)
        {
            if (collection is null || playlist is null) return;
            using (BackgroundWorker worker = new BackgroundWorker())
            {
                worker.RunWorkerAsync(new object[] { playlist, collection });
                worker.DoWork += DoUpdatePlaylist;
                worker.RunWorkerCompleted += (s, e) =>
                {
                    if (e.Result is null) return;

                    foreach (SongInfo song in e.Result as IEnumerable<SongInfo>)
                    {
                        // add new songs to library and current displayed song collection
                        if (PlaylistLibrary.SongLocations.Add(song.Uri.LocalPath) || AllSongs.Count < PlaylistLibrary.SongLocations.Count)
                        {
                            AllSongs.Add(song);
                            Helpers.ConsoleLogger.Info($"Added {song.Uri.LocalPath} to {PlaylistLibrary.Name}");
                            Helpers.ConsoleLogger.Info($"Added {song.Title} is added to AllSongCollection");
                        }
                        if (!ReferenceEquals(AllSongs, CurrentSongCollection)) CurrentSongCollection.Add(song);
                    }

                };
            }
        }

        public Playlist LoadPlaylistFromXmlFile(string path)
        {
            if (!File.Exists(path)) return null;
            if (Path.GetExtension(path).Equals(".xml", StringComparison.Ordinal) == false) return null;

            Playlist output = null;
            using (StreamReader stream = new StreamReader(path, Encoding.UTF8))
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(Playlist));
                output = deserializer.Deserialize(stream) as Playlist;
            }
            return output;
        }
        public void SavePlaylistToXmlFile(Playlist playlist, string path)
        {
            if (playlist is null) return;
            path = Path.ChangeExtension(path, ".xml");

            using (StreamWriter stream = new StreamWriter(path, false, Encoding.UTF8))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Playlist));
                serializer.Serialize(stream, playlist);
            }
        }
        public void SaveAllPlaylist()
        {
            if (!Directory.Exists(PlaylistDir)) return;

            SavePlaylistToXmlFile(PlaylistLibrary, Path.Combine(PlaylistLibrary.Name));
            foreach (Playlist item in PlaylistFiles)
            {
                SavePlaylistToXmlFile(item, Path.Combine(PlaylistDir, item.Name));
            }

        }
        public void DeletePlaylist(Playlist item)
        {
            File.Delete(Path.Combine(Environment.CurrentDirectory, PlaylistDir, item.Name));
            PlaylistFiles.Remove(item);
        }
        public void PlaySelectedPlaylist()
        {
            if (CurrentSelectedPlaylistFile != null)
            {
                List<Uri> uris = new List<Uri>();
                foreach (string item in CurrentSelectedPlaylistFile.SongLocations)
                {
                    try
                    {
                        uris.Add(new Uri(item));
                    }
                    catch (Exception e)
                    {
                        Helpers.ConsoleLogger.Error($"Error while open file {item}: {e.GetType().Name} - {e.Message}");
                    }
                }
                PlayAsSelectedPlaylist(uris);

            }
        }
        public void PlayNextSong()
        {
            if (SelectedPlayList.Count == 0) return;
            if (PlayingSongNode is null) PlayingSongNode = SelectedPlayList.First;
            else PlayingSongNode = PlayingSongNode.Next ?? SelectedPlayList.First;
            mMediaPlayer.Source = PlayingSongNode.Value;
        }
        public void PlayPreviousSong()
        {
            if (SelectedPlayList.Count == 0) return;
            if (PlayingSongNode is null) PlayingSongNode = SelectedPlayList.Last;
            else PlayingSongNode = PlayingSongNode.Previous ?? SelectedPlayList.Last;
            mMediaPlayer.Source = PlayingSongNode.Value;
        }
        public void MovePositionForward()
        {
            mMediaPlayer.Position = mMediaPlayer.Position.Add(new TimeSpan(0, 0, SongOffset));
        }
        public void MovePositionBackward()
        {
            mMediaPlayer.Position = mMediaPlayer.Position.Add(new TimeSpan(0, 0, -SongOffset));
        }

        public void SwitchPlayMode()
        {
            IsPlaying = !IsPlaying;
            if (IsPlaying && PlayingSongNode.Next == null) PlayNextSong();
            else if (IsPlaying) mMediaPlayer.Play();
            else mMediaPlayer.Pause();
        }
        public void StopPlaying()
        {
            IsPlaying = false;
            mMediaPlayer.Stop();
        }
        public void PlayAsSelectedPlaylist(IEnumerable<Uri> collection, int start = 0)
        {
            SelectedPlayList = new LinkedList<Uri>(collection);
            if (SelectedPlayList.Count != 0)
            {
                IsPlaying = true;
                PlayingSongNode = SelectedPlayList.First;
                while (start-- > 0) PlayingSongNode = PlayingSongNode.Next;
                mMediaPlayer.Source = PlayingSongNode.Value;
            }
        }

        public void PlayRandom()
        {
            if (SelectedPlayList.Count == 0) return;
            Random generator = new Random();
            int index = generator.Next(0, SelectedPlayList.Count);
            var node = SelectedPlayList.First;
            while (index-- > 0) node = node.Next;
            PlayingSongNode = node;
            mMediaPlayer.Source = PlayingSongNode.Value;
        }
        public void AddtoSelectedPlaylist(IEnumerable<string> collection)
        {
            foreach (string item in collection)
            {
                try
                {
                    Uri uri = new Uri(item);
                    SelectedPlayList.AddLast(uri);
                }
                catch (Exception e)
                {
                    Helpers.ConsoleLogger.Error($"Error while open file {item}: {e.GetType().Name} - {e.Message}");
                }
            }
        }
        public void PlayAtPercentage(double percent)
        {
            percent = Math.Abs(percent) % 101;
            SongPosition = new TimeSpan((long)(SongDuration.Ticks / 100.0 * percent));
        }
        private void MediaFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            Helpers.ConsoleLogger.Error($"{e.ErrorException.GetType().Name}: {e.ErrorException.Message}");
            PlayNextSong();
        }

        private void MediaEnded(object sender, EventArgs e)
        {
            if (IsReplayOn)
            {
                mMediaPlayer.Position = new TimeSpan(0);
                mMediaPlayer.Play();
            }
            else if (IsShuffleOn)
            {
                Random generator = new Random();
                int index = generator.Next(0, SelectedPlayList.Count);
                var node = SelectedPlayList.First;
                while (index-- > 0) node = node.Next;
                PlayingSongNode = node;
                mMediaPlayer.Source = PlayingSongNode.Value;
            }
            else if (IsRecycleOn || PlayingSongNode.Next != null)
            {
                PlayNextSong();
            }
            else IsPlaying = false;
        }

        private void MediaOpened(object sender, EventArgs e)
        {
            // update view to new playing song
            if (IsPlaying)
            {
                mMediaPlayer.Play();
                OnPropertyChanged(nameof(SongDuration));
            }
        }

        public TimeSpan SongPosition
        {
            get
            {
                OnPropertyChanged(nameof(SongProgress));
                return mMediaPlayer.Position;
            }
            set
            {
                mMediaPlayer.Position = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SongProgress));
            }
        }
        public TimeSpan SongDuration => mMediaPlayer.NaturalDuration.HasTimeSpan? mMediaPlayer.NaturalDuration.TimeSpan : new TimeSpan(0);
        public double SongProgress
        {
            get => SongDuration.Ticks == 0 ? 100 : mMediaPlayer.Position.TotalSeconds / SongDuration.TotalSeconds * 100.0;
            set
            {
                value = Math.Abs(value) % 101;
                SongPosition = new TimeSpan((long)(SongDuration.Ticks / 100.0 * value));
                OnPropertyChanged();
            }
        }
        public double SongVolume
        {
            get => mMediaPlayer.Volume;
            set => mMediaPlayer.Volume = value;
        }

        public bool IsPlaying
        {
            get => mIsPlaying;
            set
            {
                mIsPlaying = value;
                OnPropertyChanged();
            }
        }
        public bool IsReplayOn
        {
            get => mIsReplayOn;
            set
            {
                mIsReplayOn = value;
                OnPropertyChanged();
            }
        }
        public bool IsShuffleOn
        {
            get => mIsShuffleOn;
            set
            {
                mIsShuffleOn = value;
                OnPropertyChanged();
            }
        }
        public bool IsRecycleOn
        {
            get => mIsRecycleOn;
            set
            {
                mIsRecycleOn = value;
                OnPropertyChanged();
            }
        }
        public MediaElement MediaPlayer
        {
            get => mMediaPlayer;
            set
            {
                mMediaPlayer = value;
                OnPropertyChanged();
            }
        }

        public double SavedVolume { get; set; }
        public Playlist CurrentSelectedPlaylistFile { get; set; }
        public LinkedList<Uri> SelectedPlayList { get; set; } = new LinkedList<Uri>();
        private LinkedListNode<Uri> PlayingSongNode { get; set; }
        public ObservableCollection<Playlist> PlaylistFiles { get; set; } = new ObservableCollection<Playlist>();
        public ObservableCollection<SongInfo> CurrentSongCollection
        {
            get => mSongCollection;
            set
            {
                mSongCollection = value;
                OnPropertyChanged();
            }
        }


        private bool mIsRecycleOn = false;
        private bool mIsReplayOn = false;
        private bool mIsShuffleOn = false;
        private bool mIsPlaying = false;
        private ObservableCollection<SongInfo> mSongCollection = new ObservableCollection<SongInfo>();

        public ObservableCollection<SongInfo> AllSongs { get; set; } = new ObservableCollection<SongInfo>();

        private MediaElement mMediaPlayer = new MediaElement();
        private Timer mSongTimer = null;

        private readonly int SongOffset = 5;
        public Playlist PlaylistLibrary;
        public readonly string SongDir = "Library";
        public readonly string PlaylistDir = "Playlists";
        public readonly string[] MediaTypes = new string[] { ".mp3", ".wmv", ".mp4" };

        /*      Event Notifiers     */
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        /*      Lock object     */
        private object mCollectionLock = new object();
    }
}
