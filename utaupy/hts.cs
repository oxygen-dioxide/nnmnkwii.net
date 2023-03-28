using System.Linq;
using utaupy.Util;

//Reference:https://github.com/oatsu-gh/utaupy/blob/master/utaupy/hts.py

namespace utaupy.hts
{
    public class Note
    {
        public string[] contexts;
        public int position_100ns;
        public int position_100ns_backward;

        //contexts[0]
        public string? absolute_pitch;
        //contexts[1]
        public int? relative_pitch;
        //contexts[2]
        public int? key;
        //contexts[3]
        public string? beat;
        //contexts[4]
        //this variable is int in utaupy. However, some songs have non-integer tempo
        //so double is used here.
        //Note this when exporting as string.
        public double? tempo;
        //contexts[5]
        public int? number_of_syllables;
        //contexts[6]
        public int? length_10ms;
        //contexts[7]
        //musical duration measured in 96th note or 5 ticks
        public int? length;
        //contexts[17]
        public int? position;
        //contexts[18]
        public int? position_backward;
        //contexts[19]
        public int position_100ms;
        //contexts[20]

        public Note()
        {
            contexts = Enumerable.Repeat("xx", 60).ToArray();
            position_100ns = 0;//None
            position_100ns_backward = 0;//None
        }

        public void set_absolute_pitch(int notenum)
        {
            absolute_pitch = Util.MusicMath.GetToneName(notenum);
        }

        public void set_absolute_pitch(string absolute_pitch)
        {
            this.absolute_pitch = absolute_pitch;
        }
    
        public int notenum
        {
            get { return MusicMath.NameToTone(absolute_pitch); }
            set { absolute_pitch = MusicMath.GetToneName(value); }
        }

        public double? length_100ns
        {
            get 
            {
                if (tempo == null || length == null)
                {
                    return null;
                }
                return 25000000 * length / tempo;
            }
        }

    }
}
