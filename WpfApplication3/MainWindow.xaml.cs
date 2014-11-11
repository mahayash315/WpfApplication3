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
            var midiData = MidiReader.ReadFrom(fname, Encoding.GetEncoding("shift-jis"));

            // 全ての MIDI ノートを 4 半音上げる
            foreach (var track in midiData.Tracks)
            {
                foreach (var note in track.GetData<NoteEvent>())
                {
                    note.Note += 0;
                }
            }

            // テンポマップを作成
            var domain = new MyMidiFileDomain(midiData);
            TempoMap = domain.TempoMap;

            // MIDI ポートを作成
            var port = new MidiOutPort(0);
            try
            {
                port.Open();
            }
            catch
            {
                Console.WriteLine("no such port exists");
                return;
            }

            // バックグラウンドワーカの設定
            worker.DoWork += worker_DoWork;

            // MIDI プレーヤーを作成
            Player = new MidiPlayer(port);
            Player.Starting += Player_Starting;
            Player.Stopped += Player_Stopped;

            // MIDI ファイルを再生
            Player.Play(domain);
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
                    LblTempo.Content = TempoMap.GetTempo(Player.MusicTime.Tick).ToString();
                });
                System.Threading.Thread.Sleep(100);
            }
        }
    }

    class MyMidiFileDomain : IMidiFileDomain
    {
        MidiFileDomain Delegate;
        MyTempoMap MyTempoMap;

        public MyMidiFileDomain(MidiData midiData)
        {
            Delegate = new MidiFileDomain(midiData);
            MyTempoMap = new MyTempoMap(Delegate);
        }

        public NextMidi.Data.MidiData MidiData
        {
            get { return Delegate.MidiData; }
        }

        public NextMidi.Data.Score.IMusicMap MusicMap
        {
            get { return Delegate.MusicMap; }
        }

        public NextMidi.Data.Score.ITempoMap TempoMap
        {
            get { return MyTempoMap; }
        }
    }

    class MyTempoMap : ITempoMap
    {
        ITempoMap Delegate;

        public MyTempoMap(IMidiFileDomain domain)
        {
            Delegate = domain.TempoMap;
        }

        public int GetTempo(int tick)
        {
            return Delegate.GetTempo(tick);
        }

        public int ToMilliSeconds(int tick)
        {
            return Delegate.ToMilliSeconds(tick);
        }

        public int ToTick(int msec)
        {
            int tick = Delegate.ToTick(msec);
            return (int)((double)tick * 1.5);
        }

        public TimeSpan ToTime(int tick)
        {
            return Delegate.ToTime(tick);
        }

    }
}
