# Evaluating Music Improvisation Algorithms with a Modular Trading Fours System

This repository contains all code used for the Master's thesis ['Evaluating Music Improvisation Algorithms with a Modular Trading Fours System'](https://resolver.tudelft.nl/uuid:822728ff-9769-429f-9a88-0f2e960a26e4).

## Contents

### Core/
This folder contains core components for MILES, including note and token representation, conversion functionality, and all models and algorithms.

### Console/
This folder contains a C# project which facilitates using the core components in a simple console application.
Existing 'routines' include tokenizing the Lakh and Weimar Jazz databases and converting MIDI files to *.notes files.

### Program/
This folder contains a Godot/C# project of the music improvisation system MILES. THis includes the UI / visualisation frontend, the MIDI engine and the recording / playback backend.

### Recordings/
This folder contains the *.notes recordings of all performances of both experiments, as well as information gathered by the evaluation forms.

### Plotting/
This folder contains all Python code of the analysis framework, which is responsible for generating all Matplotlib figures.

### EvaluationForm/
This is a GitHub submodule that links to [sjerpsthomas/evaluation-form](https://github.com/sjerpsthomas/evaluation-form), which contains all code of the evaluation form.

### THESIS.cwp
This [CakeWalk](https://www.bandlab.com/products/cakewalk#download) project was used for generating real-time audio during all performances.

## Symlinks
In order to run the code in this repository, the following symlinks (symbolic links) need to be made:

- `Program/recordings >> Recordings`
- `Plotting/ex1/recordings >> Recordings`
- `Plotting/ex2/recordings >> Recordings`
- `%APPDATA%/Godot/app_userdata/Program/saves >> Program/saves`