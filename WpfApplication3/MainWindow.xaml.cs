using NextMidi.Data;
using NextMidi.Data.Domain;
using NextMidi.Data.Score;
using NextMidi.DataElement;
using NextMidi.Filing.Midi;
using NextMidi.MidiPort.Output;
using NextMidi.Time;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace WpfApplication3
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        MyMidiOutPort MyMidiOutPort;
        MidiData MidiData;
        ITempoMap TempoMap;
        MidiPlayer Player;
        BackgroundWorker worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string fname = "taylor_swift-shake_it_off.mid";
            if (!File.Exists(fname))
            {
                Console.WriteLine("File does not exist");
                return;
            }
            MidiData = MidiReader.ReadFrom(fname, Encoding.GetEncoding("shift-jis"));

            // 全ての MIDI ノートを 4 半音上げる
            foreach (var track in MidiData.Tracks)
            {
                foreach (var note in track.GetData<NoteEvent>())
                {
                    note.Velocity /= 2;
                }
            }

            // テンポマップを作成
            var domain = new MidiFileDomain(MidiData);
            TempoMap = domain.TempoMap;

            // MIDI ポートを作成
            MyMidiOutPort = new MyMidiOutPort(0);
            try
            {
                MyMidiOutPort.Open();
            }
            catch
            {
                Console.WriteLine("no such port exists");
                return;
            }

            // バックグラウンドワーカの設定
            worker.DoWork += worker_DoWork;

            // slider
            SldTempo.ValueChanged += SldTempo_ValueChanged;
            SldVelocity.ValueChanged += SldVelocity_ValueChanged;

            // MIDI プレーヤーを作成
            Player = new MidiPlayer(MyMidiOutPort);
            Player.Starting += Player_Starting;
            Player.Stopped += Player_Stopped;

            // MIDI ファイルを再生
            Player.Play(domain);
        }

        void SldVelocity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MyMidiOutPort != null)
            {
                MyMidiOutPort.DeltaVelocity = (int) e.NewValue;
            }
        }
        void SldTempo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MyMidiOutPort != null)
            {
                MyMidiOutPort.TickCoeff = e.NewValue;
            }
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Player.Stop();
        }

        void Player_Starting(object sender, EventArgs e)
        {
            worker.RunWorkerAsync();
        }

        void Player_Stopped(object sender, EventArgs e)
        {
            worker.CancelAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!worker.CancellationPending)
            {
                Dispatcher.Invoke(delegate()
                {
                    LblTempo.Content = TempoMap.GetTempo(Player.Tick).ToString();
                });

                
                System.Threading.Thread.Sleep(100);
            }
        }
    }

    class MyMidiOutPort : MidiOutPort
    {
        public int DeltaVelocity = 0;
        public double TickCoeff = 1.0;
        public MyMidiOutPort(int index):base(index)
        {
        }
        public new void Send(IMidiEvent data) {
            modifyEvent(data);
            base.Send(data);
        }
        private void modifyEvent(IMidiEvent data)
        {
            // TODO do something
            if (data is NoteOnEvent)
            {
                var note = (NoteOnEvent)data;
                note.Tick = (int)(note.Tick * TickCoeff);
                note.Velocity = (byte)Math.Max(0, Math.Min((int)note.Velocity + DeltaVelocity, 127));
            }
        }
    }

    //class MyMidiOutPort : IMidiOutPort
    //{
    //    MidiOutPort Delegate;
    //    public int DeltaVelocity = 0;
    //    public double TickCoeff = 1.0;

    //    public MyMidiOutPort(MidiOutPort midiOutPort)
    //    {
    //        Delegate = midiOutPort;
    //    }

    //    public void Send(IMidiEvent data)
    //    {
    //        modifyEvent(data);
    //        Delegate.Send(data);
    //    }

    //    public void Close()
    //    {
    //        Delegate.Close();
    //    }

    //    public bool IsOpen
    //    {
    //        get
    //        {
    //            return Delegate.IsOpen;
    //        }
    //        set
    //        {
    //            Delegate.IsOpen = value;
    //        }
    //    }

    //    public string Name
    //    {
    //        get { return Delegate.Name; }
    //    }

    //    public void Open()
    //    {
    //        Delegate.Open();
    //    }

    //    private void modifyEvent(IMidiEvent data)
    //    {
    //        // TODO do something
    //        if (data is NoteOnEvent)
    //        {
    //            var note = (NoteOnEvent)data;
    //            note.Tick = (int)(note.Tick * TickCoeff);
    //            note.Velocity = (byte) Math.Max(0, Math.Min((int)note.Velocity + DeltaVelocity, 127));
    //        }
    //    }
    //}
}
