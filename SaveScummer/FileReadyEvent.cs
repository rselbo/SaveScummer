using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SaveScummer
{
  class FileReadyEvent
  {
    public class StringEventArgs : EventArgs
    {
      private string m_String;
      public StringEventArgs(string str)
      {
        m_String = str;
      }
      public string Data { get { return m_String; } }
    }

    private string m_Path;
    private DispatcherTimer m_Timer;
    private TimeSpan m_Interval;

    public event EventHandler<StringEventArgs> Ready;

    public FileReadyEvent(string path, TimeSpan interval, Dispatcher dispatcher)
    {      
      m_Path = path;
      m_Timer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher);
      m_Interval = interval;
      m_Timer.Tick += new EventHandler(Timer_Tick);
    }

    public void Start()
    {
      m_Timer.Interval = m_Interval;
      m_Timer.Start();
      Console.WriteLine("Starting FileReadyEvent timer");
    }

    public void Stop()
    {
      m_Timer.Stop();
      Console.WriteLine("Stopping FileReadyEvent timer");
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
      m_Timer.Stop();
      if (Ready != null)
      {
        Ready(this, new StringEventArgs(m_Path));
      }
    }
  }
}
