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
using System.Windows.Shapes;
using System.ComponentModel;

namespace SaveScummer
{
  /// <summary>
  /// Interaction logic for Restore.xaml
  /// </summary>
  public partial class Restore : Window, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private string m_SaveScumFolder;
    private string m_SaveScumBackupFolder;
    private List<string> m_SaveNames;
    private Dictionary<string, List<string>> m_SaveNameData;
    private string m_SelectedSaveName;
    private string m_SelectedSaveFile;


    public Restore(string savescumfolder, string savescumbackupfolder)
    {
      this.DataContext = this;
      InitializeComponent();
      m_SaveScumFolder = savescumfolder;
      m_SaveScumBackupFolder = savescumbackupfolder;

      m_SaveNames = new List<string>();
      m_SaveNameData = new Dictionary<string, List<string>>();
      m_SelectedSaveName = "";
      FindSaveNames();
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }

    private void SaveNameIndexChanged(object sender, System.EventArgs e)
    {
      if (ListBoxSaveNames.SelectedValue != null)
        m_SelectedSaveName = ListBoxSaveNames.SelectedValue.ToString();
      else
        m_SelectedSaveName = null;
      m_SelectedSaveFile = null;
      OnPropertyChanged("SaveNameData");
      OnPropertyChanged("CanRestore");
    }

    private void SaveNameDataIndexChanged(object sender, System.EventArgs e)
    {
      if (ListBoxSaveNamesData.SelectedValue != null)
        m_SelectedSaveFile = ListBoxSaveNamesData.SelectedValue.ToString();
      else
        m_SelectedSaveFile = null;
      OnPropertyChanged("CanRestore");
    }

    private void FindSaveNames()
    {
      IEnumerable<string> files = System.IO.Directory.EnumerateFiles(m_SaveScumBackupFolder);
      foreach (string currentFile in files)
      {
        string filename = currentFile.Substring(m_SaveScumBackupFolder.Length+1);
        int lastPeriod = filename.LastIndexOf('.');
        if(lastPeriod > 1)
        {
          int secondLastPeriod = filename.LastIndexOf('.', lastPeriod - 1);
          if(secondLastPeriod > 1)
          {
            string name = filename.Remove(secondLastPeriod);
            if(!m_SaveNames.Contains(name))
              m_SaveNames.Add(name);

            if (m_SaveNameData.ContainsKey(name))
            {
              m_SaveNameData[name].Add(filename);
            }
            else
            {
              m_SaveNameData[name] = new List<string>();
              m_SaveNameData[name].Add(filename);
            }

          }
        }
      }
      foreach(List<string> names in m_SaveNameData.Values)
      {
        names.Sort((a, b) => String.Compare(b,a));
      }
      OnPropertyChanged("SaveNames");
    }

    public List<string> SaveNames
    {
      get { return m_SaveNames; }
    }

    public List<string> SaveNameData
    {
      get
      {
        if (m_SaveNameData != null && m_SelectedSaveName != null)
          return m_SaveNameData[m_SelectedSaveName];
        else
          return null;
      }
    }

    public string SaveFile
    {
      get { return m_SelectedSaveFile; }
    }

    public bool CanRestore
    {
      get { return m_SelectedSaveFile != null && m_SelectedSaveFile != ""; }
    }

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
