using System.ComponentModel;
using System.IO;
using System.Text;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Monitors the file at `_filePath` for changes and sends changes to `_reporterActor`.
    /// </summary>
    public class TailActor : UntypedActor
    {
        #region MessageTypes

       /// <summary>
       /// Signal that the file has been written too, and we need to read the next line of the file.
       /// </summary>
       public class FileWrite
       {
           public FileWrite(string fileName)
           {
               FileName = fileName;
           }
           
           public string FileName { get; }
       }

       /// <summary>
       /// Signal that the OS had an error accessing the file.
       /// </summary>
       public class FileError
       {
           public FileError(string fileName, string reason)
           {
               FileName = fileName;
               Reason = reason;
           }
           
           public string FileName { get; }
           
           public string Reason { get; }

       }

       /// <summary>
       /// Signal to read the initial contents of the file at actor startup.
       /// </summary>
       public class InitialRead
       {
           public InitialRead(string fileName, string text)
           {
               FileName = fileName;
               Text = text;
           }
           
           public string FileName { get; }
           
           public string Text { get; }
       }
       
       #endregion

       private readonly IActorRef _reporterActor;
       private readonly string _filePath;
       private readonly FileObserver _observer;
       private readonly Stream _fileStream;
       private readonly StreamReader _fileStreamReader;

       public TailActor(IActorRef reporterActor, string filePath)
       {
           _reporterActor = reporterActor;
           _filePath = filePath;
           
           // Start watching the file for changes.
           _observer = new FileObserver(Self, Path.GetFullPath(_filePath));
           _observer.Start();
           
           // Open the file stream with shared read/write permissions
           // (so file can be written to while open).
           _fileStream = new FileStream(Path.GetFullPath(_filePath), FileMode.Open, FileAccess.Read,
               FileShare.ReadWrite);
           _fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);
           
           // Read the initial contents of the file and send it to console as first msg
           var text = _fileStreamReader.ReadToEnd();
           Self.Tell(new InitialRead(_filePath, text));
       }

       protected override void OnReceive(object message)
       {
           switch (message)
           {
               case FileWrite _:
                   // Move file cursor forward.
                   // Pull results from current cursor position to end of file and write results to output.
                   // (This action assumes reading from a log file type format that is append-only.)
                   var text = _fileStreamReader.ReadToEnd();
                   if (!string.IsNullOrEmpty(text))
                   {
                       _reporterActor.Tell(text);
                   }
                   break;
               case FileError fe:
                   _reporterActor.Tell($"Tail error: {fe.Reason}.");
                   break;
               case InitialRead ir:
                   _reporterActor.Tell(ir.Text);
                   break;
           }
       }
    }
}