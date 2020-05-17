using System;
using System.Linq;

public static class MidiUtils
{
    // There are 49 halftones in the singable audio spectrum,
    // namely C2 (midi note 36, which is 65.41 Hz) to C6 (midi note 84, which is 1046.5023 Hz).
    public const int SingableNoteMin = 36;
    public const int SingableNoteMax = 84;
    public const int SingableNoteRange = 49;

    // Concert pitch A4 (440 Hz)
    public const int MidiNoteConcertPitch = 69;
    public const int MidiNoteConcertPitchFrequency = 440;

    // White keys: C = 0, D = 2, E = 4, F = 5, G = 7, A = 9, B = 11
    private static readonly int[] whiteKeyRelativeMidiNotes = { 0, 2, 4, 5, 7, 9, 11 };
    // Black keys: C# = 1, D# = 3, F# = 6, G# = 8, A# = 10
    private static readonly int[] blackKeyRelativeMidiNotes = { 1, 3, 6, 8, 10 };

    public static int GetOctave(int midiNote)
    {
        // 12: "C0"
        // 13: "C#0"
        // 14: "D0"
        // ...
        // 24: "C1"
        int octave = (midiNote / 12) - 1;
        return octave;
    }

    public static string GetAbsoluteName(int midiNote)
    {
        int octave = GetOctave(midiNote);
        string absoluteName = GetRelativeName(midiNote) + octave;
        return absoluteName;
    }

    public static string GetRelativeName(int midiNote)
    {
        midiNote = GetRelativePitch(midiNote);

        switch (midiNote)
        {
            case 0: return "C";
            case 1: return "C#";
            case 2: return "D";
            case 3: return "D#";
            case 4: return "E";
            case 5: return "F";
            case 6: return "F#";
            case 7: return "G";
            case 8: return "G#";
            case 9: return "A";
            case 10: return "A#";
            case 11: return "B";
            default:
                return midiNote.ToString();
        }
    }

    public static int GetRelativePitch(int midiNote)
    {
        return midiNote % 12;
    }

    public static int GetRoundedMidiNote(int recordedMidiNote, int targetMidiNote, int roundingDistance)
    {
        int distance = MidiUtils.GetRelativePitchDistance(recordedMidiNote, targetMidiNote);
        if (distance <= roundingDistance)
        {
            return targetMidiNote;
        }
        else
        {
            return recordedMidiNote;
        }
    }

    public static int GetRelativePitchDistance(int fromMidiNote, int toMidiNote)
    {
        int fromRelativeMidiNote = GetRelativePitch(fromMidiNote);
        int toRelativeMidiNote = GetRelativePitch(toMidiNote);

        // Distance when going from 2 to 10 via 3, 4, 5...
        int distanceUnwrapped = Math.Abs(toRelativeMidiNote - fromRelativeMidiNote);
        // Distance when going from 2 to 10 via 1, 11, 10
        int distanceWrapped = 12 - distanceUnwrapped;
        // Note that (distanceUnwrapped + distanceWrapped) == 12, which is going a full circle in any direction.

        // Distance in shortest direction is result distance
        return Math.Min(distanceUnwrapped, distanceWrapped);
    }

    public static int GetRelativePitchDistanceSigned(int fromMidiNote, int toMidiNote)
    {
        int toRelativeMidiNote = MidiUtils.GetRelativePitch(toMidiNote);
        int fromRelativeMidiNote = MidiUtils.GetRelativePitch(fromMidiNote);
        // Distance when going from 2 to 10 via 3, 4, 5... -> (8)
        // Distance when going from 10 to 2 via 9, 8, 7... -> (-8)
        int distanceUnwrapped = toRelativeMidiNote - fromRelativeMidiNote;
        // Distance when going from 2 to 10 via 1, 0, 11, 10 -> (-4)
        // Distance when going from 10 to 2 via 11, 0, 1, 2 -> (4)
        int distanceWrapped = distanceUnwrapped >= 0
            ? distanceUnwrapped - 12
            : distanceUnwrapped + 12;
        int distance = Math.Abs(distanceUnwrapped) < Math.Abs(distanceWrapped)
            ? distanceUnwrapped
            : distanceWrapped;
        return distance;
    }

    public static bool IsBlackPianoKey(int midiNote)
    {
        return blackKeyRelativeMidiNotes.Contains(GetRelativePitch(midiNote));
    }

    public static bool IsWhitePianoKey(int midiNote)
    {
        return whiteKeyRelativeMidiNotes.Contains(GetRelativePitch(midiNote));
    }

    public static int GetRoundedMidiNoteForRecordedMidiNote(Note targetNote, int recordedMidiNote, int roundingDistance)
    {
        if (targetNote.Type == ENoteType.Rap || targetNote.Type == ENoteType.RapGolden)
        {
            // Rap notes accept any noise as correct note.
            return targetNote.MidiNote;
        }
        else if (recordedMidiNote < MidiUtils.SingableNoteMin || recordedMidiNote > MidiUtils.SingableNoteMax)
        {
            // The pitch detection can fail, which is the case when the detected pitch is outside of the singable note range.
            // In this case, just assume that the player was singing correctly and round to the target note.
            return targetNote.MidiNote;
        }
        else
        {
            // Round recorded note if it is close to the target note.
            return GetRoundedMidiNote(recordedMidiNote, targetNote.MidiNote, roundingDistance);
        }
    }

    public static bool IsNoteHit(Note targetNote, PitchEvent pitchEvent, int roundingDistance)
    {
        return pitchEvent != null
            && GetRelativePitch(targetNote.MidiNote) == GetRelativePitch(GetRoundedMidiNoteForRecordedMidiNote(targetNote, pitchEvent.MidiNote, roundingDistance));
    }

    public static float[] PrecalculateHalftoneFrequencies(int noteMin, int noteRange)
    {
        float[] frequencies = new float[noteRange];
        for (int index = 0; index < frequencies.Length; index++)
        {
            float concertPitchOctaveOffset = ((noteMin + index) - MidiUtils.MidiNoteConcertPitch) / 12f;
            frequencies[index] = (float)(MidiUtils.MidiNoteConcertPitchFrequency * Math.Pow(2f, concertPitchOctaveOffset));
        }
        return frequencies;
    }

    public static int[] PrecalculateHalftoneDelays(int sampleRateHz, float[] halftoneFrequencies)
    {
        int[] noteDelays = new int[halftoneFrequencies.Length];
        for (int index = 0; index < halftoneFrequencies.Length; index++)
        {
            noteDelays[index] = Convert.ToInt32(sampleRateHz / halftoneFrequencies[index]);
        }
        return noteDelays;
    }
}
