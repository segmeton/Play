using UnityEngine;

public class SlicedAudioSamplesAnalyzerTowardsTargetNote
{
    private readonly int sliceSize;
    private readonly IAudioSamplesAnalyzer otherAnalyzer;

    public SlicedAudioSamplesAnalyzerTowardsTargetNote(int sliceSize, IAudioSamplesAnalyzer otherAnalyzer)
    {
        this.sliceSize = sliceSize;
        this.otherAnalyzer = otherAnalyzer;
    }

    public PitchEvent ProcessAudioSamples(float[] sampleBuffer, int sampleStartIndex, int sampleEndIndex, MicProfile micProfile, Note targetNote, int roundingDistance)
    {
        // Try to find a hit by analyzing all the available buffer.
        PitchEvent pitchEvent = otherAnalyzer.ProcessAudioSamples(sampleBuffer, sampleStartIndex, sampleEndIndex, micProfile);
        if (MidiUtils.IsNoteHit(targetNote, pitchEvent, roundingDistance))
        {
            return pitchEvent;
        }

        // Try to find a hit by analyzing the buffer slice by slice.
        int sampleCount = sampleEndIndex - sampleStartIndex;
        if (sampleCount > sliceSize)
        {
            int sliceCount = sampleCount / sliceSize;
            for (int i = 0; i <= sliceCount; i++)
            {
                int sliceEndSampleBufferIndex = sampleStartIndex + (sliceSize * i);
                sliceEndSampleBufferIndex = NumberUtils.Limit(sliceEndSampleBufferIndex, sampleStartIndex, sampleEndIndex);
                int sliceStartSampleBufferIndex = sliceEndSampleBufferIndex - sliceSize;
                PitchEvent slicePitchEvent = otherAnalyzer.ProcessAudioSamples(sampleBuffer, sliceStartSampleBufferIndex, sliceEndSampleBufferIndex, micProfile);
                if (MidiUtils.IsNoteHit(targetNote, slicePitchEvent, roundingDistance))
                {
                    return slicePitchEvent;
                }
            }
        }

        // The full analysis result is the overall result, if no hit was found.
        return pitchEvent;
    }
}
