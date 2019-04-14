using System;
using System.IO;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Transforms <see cref="FileSystemWatcher"/> events about a specific file into messages for
    /// <see cref="TailActor"/>.
    /// </summary>
    public class FileObserver : IDisposable
    {
        private readonly IActorRef _tailActor;
        private readonly string _absoluteFilePath;
        private FileSystemWatcher _watcher;
        private readonly string _fileDir;
        private readonly string _fileNameOnly;

        public FileObserver(IActorRef tailActor, string absoluteFilePath)
        {
            _tailActor = tailActor;
            _absoluteFilePath = absoluteFilePath;
            _fileDir = Path.GetDirectoryName(absoluteFilePath);
            _fileNameOnly = Path.GetFileName(absoluteFilePath);
        }

        /// <summary>
        /// Begin monitoring the file for changes.
        /// </summary>
        public void Start()
        {
            // Make watcher to observe our specific file.
            _watcher = new FileSystemWatcher(_fileDir, _fileNameOnly);
            
            // Watch our file for changes to the file name or for new messages being written to the file.
            _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            
            // Assign callbacks for event types.
            _watcher.Changed += OnFileChanged;
            _watcher.Error += OnFileError;
            
            // Start watching.
            _watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stop monitoring file.
        /// </summary>
        public void Dispose()
        {
            _watcher.Dispose();
        }

        /// <summary>
        /// Callback for <see cref="FileSystemWatcher"/> file error events.
        /// </summary>
        /// <param name="sender">The object raising the event.</param>
        /// <param name="e">The details of the event.</param>
        private void OnFileError(object sender, ErrorEventArgs e)
        {
            // Here we use a special ActorRefs.NoSender.
            // Since this event can happen many times, this choice is a little micro-optimization.
            _tailActor.Tell(new TailActor.FileError(_fileNameOnly, e.GetException().Message), ActorRefs.NoSender);
        }
        
        /// <summary>
        /// Callback for <see cref="FileSystemWatcher"/> file change events.
        /// </summary>
        /// <param name="sender">The object raising this event.</param>
        /// <param name="e">The details of this event.</param>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                // Here we use a special ActorRefs.NoSender.
                // Since this event can happen many times, this choice is a little micro-optimization.
                _tailActor.Tell(new TailActor.FileWrite(e.Name), ActorRefs.NoSender);
            }
        }
    }
}