using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Build.Framework;
using MSBuildObjects.Annotations;

namespace MSBuildUI.Items
{
    public class MessageSink : INotifyPropertyChanged
    {
        public string Title { get; set; }

        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();

        public DispatcherOperation AddMessage(BuildEventArgs buildEventArgs)
        {
            return Application.Current.Dispatcher.InvokeAsync(() => Messages.Add(new Message(buildEventArgs)));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
