import numpy as np
import cv2
import tensorflow as tf
from flask import Flask, jsonify, request

app = Flask(__name__)

# Load the ScanComplete model
model = tf.keras.models.load_model('src\model.py')

def capture_camera_data():
    """
    Captures a frame from the camera and converts it into a 3D array.
    """
    cap = cv2.VideoCapture(0)  # Replace with the correct camera index if necessary

    ret, frame = cap.read()
    if not ret:
        raise ValueError("Failed to capture image from camera")
    
    depth_map = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)  # Assuming depth data can be extracted from the grayscale image
    scan_data = convert_depth_to_3d(depth_map)

    cap.release()
    return scan_data

def convert_depth_to_3d(depth_map):
    """
    Converts a 2D depth map to a 3D array (voxel grid).
    """
    height, width = depth_map.shape
    scan_data = np.zeros((height, width, 64))

    for i in range(height):
        for j in range(width):
            depth = int(depth_map[i, j] / 4)
            scan_data[i, j, :depth] = 1

    return scan_data

def preprocess_scan(scan_data):
    """
    Preprocess the scan data to fit the model's input format.
    """
    scan_data = np.expand_dims(scan_data, axis=0)  # Add batch dimension
    scan_data = np.expand_dims(scan_data, axis=-1)  # Add channel dimension
    return scan_data

def process_ar_scan(scan_data):
    """
    Processes the 3D scan data using the ScanComplete model.
    """
    preprocessed_data = preprocess_scan(scan_data)
    completed_scan, semantic_labels = model.predict(preprocessed_data)

    completed_scan = np.squeeze(completed_scan, axis=0)
    completed_scan = np.squeeze(completed_scan, axis=-1)
    semantic_labels = np.argmax(semantic_labels, axis=-1)

    return completed_scan, semantic_labels

@app.route('/process_scan', methods=['POST'])
def process_scan():
    """
    API endpoint to process scan data captured by the camera.
    """
    scan_data = capture_camera_data()
    completed_scan, semantic_labels = process_ar_scan(scan_data)

    # Convert numpy arrays to list for JSON serialization
    completed_scan_list = completed_scan.tolist()
    semantic_labels_list = semantic_labels.tolist()

    return jsonify({'completed_scan': completed_scan_list, 'semantic_labels': semantic_labels_list})

if __name__ == "__main__":
    app.run(host='0.0.0.0', port=5000)
