using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
namespace MultimediaPlayer
{
    public class Playlist : INotifyPropertyChanged, IEquatable<Playlist>
    {
        public Playlist() { }
        public Playlist(string name) : this() { Name = name; }
        public Playlist(string name, IEnumerable<string> collection)
        {
            Name = name;
            SongLocations = new HashSet<string>(collection);
        }
        public string Name
        {
            get => mName;
            set
            {
                mName = value;
                OnPropertyChanged();
            }
        }
        public HashSet<string> SongLocations { get; set; } = new HashSet<string>();


        private string mName = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public bool Equals(Playlist other)
        {
            if (other is null) return false;
            return Name.Equals(other.Name, StringComparison.Ordinal);
        }
    }
}
