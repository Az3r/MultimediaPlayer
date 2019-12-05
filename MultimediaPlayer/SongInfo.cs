using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MultimediaPlayer
{
    public class SongInfo : INotifyPropertyChanged
    {
        public TimeSpan Duration
        {
            get => mDuration;
            set
            {
                mDuration = value;
                OnPropertyChanged();
            }
        }
        public string Artist
        {
            get => mArtist;
            set
            {
                mArtist = value;
                OnPropertyChanged();
            }
        }
        public string Album
        {
            get => mAlbum;
            set
            {
                mAlbum = value;
                OnPropertyChanged();
            }
        }
        public ImageSource Image
        {
            get => mImage;
            set
            {
                mImage = value;
                OnPropertyChanged();
            }
        }
        public string Title
        {
            get => mTitle;
            set
            {
                mTitle = value;
                OnPropertyChanged();
            }
        }
        public byte[] ImageData { get; set; }
        public Uri Uri { get; set; }

        private string mTitle;
        private ImageSource mImage = null;
        private string mArtist = string.Empty;
        private string mAlbum = string.Empty;
        private TimeSpan mDuration = new TimeSpan(0);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
