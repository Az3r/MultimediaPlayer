using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
namespace MultimediaPlayer
{
    public class MediaCommands
    {
        public static RoutedUICommand FullScreen { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Exit { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Switch { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Play { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Stop { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Random { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Next { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Previous { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Forward { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Backward { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Rename { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand RemoveSong { get; private set; } = new RoutedUICommand();

        public static RoutedUICommand Create { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand PlayPlaylist { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand RemovePlaylist { get; private set; } = new RoutedUICommand();
    }
}
