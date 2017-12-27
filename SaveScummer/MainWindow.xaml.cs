using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;
using System.Windows.Forms;
using System.ComponentModel;

namespace SaveScummer
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    private FolderBrowserDialog m_BrowseFolderDialog;
    private string m_SaveScumFolder;
    private string m_SaveScumBackupFolder;
    private string m_BackupLog;
    private FileSystemWatcher m_FSWatcher;
    private Dictionary<string, FileReadyEvent> m_Timers;
    private Dispatcher m_MainDispatcher;

    public MainWindow()
    {
      this.DataContext = this;
      InitializeComponent();
      m_BrowseFolderDialog = new FolderBrowserDialog();
      m_Timers = new Dictionary<string, FileReadyEvent>();
      this.Closed += new EventHandler(OnExit);
      SettingsLoaded();
      m_MainDispatcher = Dispatcher.CurrentDispatcher;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
      DialogResult result = m_BrowseFolderDialog.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK)
      {
        SaveScumFolder = m_BrowseFolderDialog.SelectedPath;
        System.IO.Path.GetDirectoryName(SaveScumFolder);
        SaveScumBackupFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(SaveScumFolder), "backup");
      }
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
      Restore restore = new Restore(SaveScumFolder, SaveScumBackupFolder);
      if(restore.ShowDialog() == true)
      {
        string filename = restore.SaveFile;
        bool shouldReactivate = m_FSWatcher != null;
        ChangeActive(false);

        int lastPeriod = filename.LastIndexOf('.');
        if (lastPeriod > 1)
        {
          int secondLastPeriod = filename.LastIndexOf('.', lastPeriod - 1);
          if (secondLastPeriod > 1)
          {
            //find and remove the timestamp
            string name = filename.Remove(secondLastPeriod, lastPeriod - secondLastPeriod);

            string saveFile = System.IO.Path.Combine(m_SaveScumFolder, name);
            string backupSaveFile = System.IO.Path.Combine(m_SaveScumBackupFolder, filename);

            if (System.IO.File.Exists(backupSaveFile))
            {
              //if the original file exists, back it up now
              if(System.IO.File.Exists(saveFile))
                BackupFile(saveFile);

              BackupLog += "Restoring " + backupSaveFile + " to " + saveFile + "\n";
              Directory.CreateDirectory(System.IO.Path.GetDirectoryName(saveFile));
              File.Copy(backupSaveFile, saveFile, true);

              BackupLog += "done\n";

            }
            else
            {
              BackupLog += "Tried to restore " + backupSaveFile + " but the file does not exist";
            }

          }
        }
        ChangeActive(shouldReactivate);
      }
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
      ChangeActive(true);
    }

    private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      ChangeActive(false);
    }

    private void ChangeActive(bool isActive)
    {
      if(isActive)
      {
        if(m_FSWatcher == null)
        {
          m_FSWatcher = new FileSystemWatcher(SaveScumFolder);
          m_FSWatcher.Changed += new FileSystemEventHandler(OnChanged);
          m_FSWatcher.Created += new FileSystemEventHandler(OnChanged);
          m_FSWatcher.IncludeSubdirectories = true;

          m_FSWatcher.EnableRaisingEvents = true;
          BackupLog += "Watching " + SaveScumFolder + "\n";
          OnPropertyChanged("CanEdit");
        }
      }
      else
      {
        if(m_FSWatcher != null)
        {
          m_FSWatcher.EnableRaisingEvents = false;
          m_FSWatcher = null;
          m_Timers.Clear();
          BackupLog += "Stopped watching " + SaveScumFolder + "\n";
          OnPropertyChanged("CanEdit");
        }
      }

    }

    private void OnChanged(object source, FileSystemEventArgs e)
    {
      try
      {
        FileAttributes attributes = File.GetAttributes(e.FullPath);
        if ((attributes & FileAttributes.Directory) != FileAttributes.Directory)
        {
          FileReadyEvent fileReadyEvent = null;
          if(m_Timers.ContainsKey(e.FullPath))
          {
            fileReadyEvent = m_Timers[e.FullPath];
            fileReadyEvent.Stop();
          }
          else
          {
            fileReadyEvent = new FileReadyEvent(e.FullPath, new TimeSpan(0, 0, 5), m_MainDispatcher);
            fileReadyEvent.Ready += new EventHandler<FileReadyEvent.StringEventArgs>(OnFileReady);
            m_Timers.Add(e.FullPath, fileReadyEvent);
          }
          fileReadyEvent.Start();        
        }
      }
      catch (System.Exception)
      {
        //couldnt get attributes for file so just let it pass
      }
    }

    private void OnFileReady(object source, FileReadyEvent.StringEventArgs e)
    {
      m_Timers.Remove(e.Data);

      BackupFile(e.Data);
    }

    private void BackupFile(string path)
    {
      string fullPath = path;
      if (path.StartsWith(m_SaveScumFolder))
      {
        path = path.Substring(m_SaveScumFolder.Length).TrimStart('\\');
      }

      String dir = System.IO.Path.GetDirectoryName(path);
      String filename = System.IO.Path.GetFileNameWithoutExtension(path);
      String extension = System.IO.Path.GetExtension(path);

      String backupPath = System.IO.Path.Combine(dir, filename + DateTime.Now.ToString(".yyyyMMddHHmmss") + extension);
      String fullBackupPath = System.IO.Path.Combine(SaveScumBackupFolder, backupPath);

      BackupLog += "Backing up " + fullPath + " to " + backupPath + "\n";
      Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullBackupPath));
      File.Copy(fullPath, fullBackupPath);
      BackupLog += "done\n";

    }

    public void SettingsLoaded()
    {
      SaveScumFolder = Properties.Settings.Default.SaveScumFolder;
      SaveScumBackupFolder = Properties.Settings.Default.SaveScumBackupFolder;
    }

    public void OnExit(object sender, EventArgs e)
    {
      Properties.Settings.Default.Save();
    }

    public string SaveScumFolder
    {
      get { return m_SaveScumFolder; }
      set
      {
        if (value != m_SaveScumFolder)
        {
          m_SaveScumFolder = value;
          Properties.Settings.Default.SaveScumFolder = m_SaveScumFolder;
          OnPropertyChanged("SaveScumFolder");
          OnPropertyChanged("CanScum");
        }
      }
    }

    public string SaveScumBackupFolder
    {
      get { return m_SaveScumBackupFolder; }
      set
      {
        if (value != m_SaveScumBackupFolder)
        {
          m_SaveScumBackupFolder = value;
          Properties.Settings.Default.SaveScumBackupFolder = m_SaveScumBackupFolder;
          OnPropertyChanged("SaveScumBackupFolder");
          OnPropertyChanged("CanScum");
        }
      }
    }

    public string BackupLog
    {
      get { return m_BackupLog; }
      set
      {
        m_BackupLog = value;
        OnPropertyChanged("BackupLog");
      }
    }

    public bool CanScum
    {
      get { return !String.IsNullOrEmpty(SaveScumFolder); }
    }

    public bool CanEdit
    {
      get { return m_FSWatcher == null;  }
    }
    // Create the OnPropertyChanged method to raise the event 
    protected void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(name));
      }
    }

  }
}
