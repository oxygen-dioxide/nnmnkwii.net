using System;
using System.Collections.Generic;
using System.Text;

namespace utaupy.Util
{
    public static class MusicMath
    {
        public static readonly string[] KeysInOctave = {
            "C",
            "Db",
            "D",
            "Eb",
            "E",
            "F",
            "Gb",
            "G",
            "Ab",
            "A",
            "Bb",
            "B" ,
        };

        public static readonly Dictionary<string, int> NameInOctave = new Dictionary<string, int> {
            { "C", 0 }, { "C#", 1 }, { "Db", 1 },
            { "D", 2 }, { "D#", 3 }, { "Eb", 3 },
            { "E", 4 },
            { "F", 5 }, { "F#", 6 }, { "Gb", 6 },
            { "G", 7 }, { "G#", 8 }, { "Ab", 8 },
            { "A", 9 }, { "A#", 10 }, { "Bb", 10 },
            { "B", 11 },
        };

        public static string GetToneName(int noteNum)
        {
            return noteNum < 0 ? string.Empty : KeysInOctave[noteNum % 12] + (noteNum / 12 - 1).ToString();
        }

        public static int NameToTone(string name)
        {
            if (name.Length < 2)
            {
                return -1;
            }
            var str = name.Substring(0, (name[1] == '#' || name[1] == 'b') ? 2 : 1);
            var num = name.Substring(str.Length);
            if (!int.TryParse(num, out int octave))
            {
                return -1;
            }
            if (!NameInOctave.TryGetValue(str, out int inOctave))
            {
                return -1;
            }
            return 12 * (octave + 1) + inOctave;
        }

    }
}
