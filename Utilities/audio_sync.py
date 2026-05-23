import sys
import subprocess
import numpy as np
import scipy.signal
import soundfile as sf
import io
import json

def extract_audio(video_path, sr=16000, start_sec=10, duration_sec=60):
    command = [
        'ffmpeg', '-ss', str(start_sec), '-t', str(duration_sec), '-i', video_path, 
        '-f', 'wav', '-ar', str(sr), '-ac', '1', '-y', '-loglevel', 'error', 'pipe:1'
    ]
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    stdout, stderr = process.communicate()

    if process.returncode != 0 or len(stdout) == 0:
        error_msg = stderr.decode('utf-8', errors='ignore').strip()
        raise RuntimeError(f"FFmpeg error on {video_path} (Start: {start_sec}s): {error_msg}")
    
    data, samplerate = sf.read(io.BytesIO(stdout))
    return data, samplerate

def find_offset(y_ref, y_target, sr):
    correlation = scipy.signal.correlate(y_ref, y_target, mode='full')
    lag = np.argmax(correlation) - (len(y_target) - 1)
    return lag / sr

if __name__ == "__main__":
    videos = sys.argv[1:]
    if len(videos) < 2:
        print(json.dumps([0.0]))
        sys.exit(0)
    
    sr = 16000
    start_time = 20
    analyze_duration = 60
    
    try:
        y_ref, _ = extract_audio(videos[0], sr, start_time, analyze_duration)
        offsets = [0.0]
        
        for vid in videos[1:]:
            y_target, _ = extract_audio(vid, sr, start_time, analyze_duration)
            offset = find_offset(y_ref, y_target, sr)
            offsets.append(offset)

        print(json.dumps(offsets))
    except Exception as e:
        print(json.dumps({"error": str(e)}))
        sys.exit(1)