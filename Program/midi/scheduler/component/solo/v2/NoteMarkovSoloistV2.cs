using System;
using System.Collections.Generic;
using System.Linq;
using Core.midi;
using Core.midi.token.conversion.stage;
using Core.models.continuator;

namespace Program.midi.scheduler.component.solo.v2;

public record NoteRepresentation(
    int Note, // [0, 24]
    int Velocity, // [0, 1]
    int Ioi // [0, 3]
);

public class NoteMarkovSoloistV2: Soloist
{
    public List<NoteRepresentation> RepsFromNotes(List<MidiNote> notes)
    {
        // Optionally remove swing
        notes = notes
            .Select(it => it with
            {
                Time = TimingStage.RemoveSwing(it.Time, LeadSheet.Style == LeadSheet.StyleEnum.Swing)
            })
            .ToList();

        List<NoteRepresentation> res = [];
        
        for (var i = 0; i < notes.Count; i++)
        {
            var (_, time, _, nNote, nVelocity) = notes[i];

            time = TimingStage.RemoveSwing(time, LeadSheet.Style == LeadSheet.StyleEnum.Swing);

            int nIoi;
            if (i != notes.Count - 1)
                nIoi = (int)((notes[i + 1].Time - time) * 8);
            else
                nIoi = 2;
            if (nIoi > 3) nIoi = 3;
            if (nIoi < 0) nIoi = 0;
            
            nNote %= 24;
            nVelocity /= 64;

            var rep = new NoteRepresentation(
                nNote,
                nVelocity,
                nIoi
            );
            
            res.Add(rep);
        }

        return res;
    }

    public List<MidiNote> NotesFromReps(List<NoteRepresentation> reps, int generateMeasureCount)
    {
        List<MidiNote> res = [];

        var t = 0.0;
        foreach (var rep in reps)
        {
            var (note, velocity, ioi) = rep;

            note += 60;

            var fIoi = ioi / 8.0;

            var swungTime = t;
            var swungNextTime = t + fIoi;

            swungTime = TimingStage.ApplySwing(swungTime, LeadSheet.Style == LeadSheet.StyleEnum.Swing);
            swungNextTime = TimingStage.ApplySwing(swungNextTime, LeadSheet.Style == LeadSheet.StyleEnum.Swing);
            
            var newNote = new MidiNote(
                OutputName.Algorithm,
                swungTime,
                swungNextTime - swungTime,
                note,
                velocity * 64 + 32
            );
            
            res.Add(newNote);
            t += fIoi;

            if (t > generateMeasureCount)
                break;
        }
        
        return res;
    }

    public VariableOrderMarkov<NoteRepresentation> Model = new(
        it => it.GetHashCode(),
        (a, b) => Math.Abs(a.Note - b.Note),
        3
    );

    public LeadSheet LeadSheet;
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
        LeadSheet = leadSheet;
        
        IngestMeasures(solo.Measures, 0);
    }

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        // Create notes
        List<MidiNote> notes = [];
        for (var index = 0; index < measures.Count; index++)
        {
            var measure = measures[index];
            notes.AddRange(
                measure.Notes.Select(note => note with { Time = note.Time + index })
            );
        }

        var reps = RepsFromNotes(notes);
        Model.LearnSequence(reps);
    }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        // Generate reps
        var reps = Model.Generate(40);

        var notes = NotesFromReps(reps, generateMeasureCount);
        return MidiSong.FromNotes(notes).Measures;
    }
}