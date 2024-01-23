# naudio-effects

DSP effects for NAudio (compressor, limiter, EQ, gate) as ISampleProvider.

> v0.1 in progress.

## SilenceDetector
The SilenceDetector class detects periods of silence in an audio stream. It can be used to identify sections of audio that are below a certain threshold. You can use it to process audio in real-time by calling Process repeatedly, and then call Complete to finalize the detection.

