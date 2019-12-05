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
        public static RoutedUICommand Exit { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Play { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Next { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Previous { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Rename { get; private set; } = new RoutedUICommand();
        public static RoutedUICommand Create { get; private set; } = new RoutedUICommand();
    }
}
