import os
import sys
import subprocess
import base64
from pdf2image import convert_from_path
from flask import Flask,Response,request, jsonify, send_file
from flask_cors import CORS


# Initialize Flask app
app = Flask(__name__)
CORS(app) 

def stream_file(file_path):
    """Generator function to stream a file."""
    with open(file_path, 'rb') as f:
        while chunk := f.read(8192):  # Read 8KB at a time
            yield chunk
def pptx_to_pdf(input_file, output_dir):
    command = ['libreoffice', '--headless', '--convert-to', 'pdf', input_file, '--outdir', output_dir]
    subprocess.run(command, check=True, capture_output=True, text=True)

def pdf_to_images(pdf_file, output_folder):
    """
    Convertit un fichier PDF en images (une image par slide) et les enregistre dans output_folder.
    """
    pages = convert_from_path(pdf_file)
    for i, page in enumerate(pages, start=1):
        image_path = os.path.join(output_folder, f"slide_{i}.png")
        page.save(image_path, 'PNG')
        print(f"Enregistré {image_path}")

def process_pptx(pptx_path, output_base_folder="output"):
    """
    Pour chaque fichier PPTX, crée un dossier dans 'output' portant le nom du fichier (sans extension),
    convertit le PPTX en PDF puis le PDF en images.
    """
    base_name = os.path.splitext(os.path.basename(pptx_path))[0]
    output_dir = os.path.join(output_base_folder, base_name)
    os.makedirs(output_dir, exist_ok=True)
    
    # Dossier temporaire pour stocker le PDF
    temp_pdf_dir = "temp_pdf"
    os.makedirs(temp_pdf_dir, exist_ok=True)
    
    # Conversion du PPTX en PDF
    print(f"Conversion de {pptx_path} en PDF...")
    pptx_to_pdf(pptx_path, temp_pdf_dir)
    
    pdf_file = os.path.join(temp_pdf_dir, f"{base_name}.pdf")
    if not os.path.exists(pdf_file):
        raise FileNotFoundError(f"La conversion en PDF a échoué : {pdf_file} est introuvable.")
    
    # Conversion du PDF en images slide par slide
    print(f"Conversion de {pdf_file} en images...")
    pdf_to_images(pdf_file, output_dir)
    print(f"Traitement de {pptx_path} terminé. Les images sont dans {output_dir}")

# Flask route to handle PPTX file uploads and processing
@app.route('/upload', methods=['POST'])
def upload_pptx():
    input_folder = "/input"
    output_folder = "/output"

    if not os.path.isdir(input_folder):
        return jsonify({"error": f"Le dossier d'entrée '{input_folder}' n'existe pas."}), 400

    pptx_file = request.files.get('file')
    if not pptx_file:
        return jsonify({"error": "Aucun fichier PPTX reçu."}), 400
    
    pptx_path = os.path.join(input_folder, pptx_file.filename)
    pptx_file.save(pptx_path)
    
    try:
        process_pptx(pptx_path, output_folder)
        return jsonify({"message": "Fichier PPTX traité avec succès!"}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500
    

# def encode_image_to_base64(image_path):
#     """Encode an image to base64."""
#     with open(image_path, "rb") as image_file:
#         encoded_string = base64.b64encode(image_file.read()).decode("utf-8")
#     return encoded_string  

# # Flask route to return the list of image files as base64
# @app.route('/imas', methods=['GET'])
# def get_images():
#     output_folder = "/output"
#     if not os.path.isdir(output_folder):
#         return jsonify({"error": "Le dossier de sortie n'existe pas."}), 400

#     # List all files in the output folder
#     image_base64_list = []
#     for root, dirs, files in os.walk(output_folder):
#         for file in files:
#             if file.endswith('.png'):  # Only include PNG images
#                 image_path = os.path.join(root, file)
#                 # Convert image to base64 and add to the list
#                 encoded_image = encode_image_to_base64(image_path)
#                 image_base64_list.append(encoded_image)

#     if not image_base64_list:
#         return jsonify({"message": "Aucune image trouvée."}), 404
    
#     return jsonify({"images": image_base64_list}), 200

@app.route('/images', methods=['GET'])
def get_all_images():
    output_folder = "/output/9737"
    
    images = [f for f in os.listdir(output_folder) if f.endswith('.png')]
    
    if not images:
        return jsonify({"error": "Aucune image trouvée."}), 404
    
    # Retourner la première image de la liste
    image_path = os.path.join(output_folder, images[0])
    
    return send_file(image_path, mimetype='image/png')

# @app.route('/images', methods=['GET'])
# def get_all_images():
#     output_folder = "/output"
    
#     # Get all images in the output folder
#     images = [f for f in os.listdir(output_folder) if f.endswith('.png')]
    
#     if not images:
#         return jsonify({"error": "Aucune image trouvée."}), 404
    
#     # Generate URLs for the images
#     image_urls = [f"/images/{image}" for image in images]
    
    # return jsonify({"images": image_urls}), 200
@app.route('/stream_images', methods=['GET'])
def stream_images():
    output_folder = "/output/slides"
    
    # Get all images in the output folder
    images = [f for f in os.listdir(output_folder) if f.endswith('.png')]
    
    if not images:
        return jsonify({"error": "Aucune image trouvée."}), 404
    
    def generate():
        for image in images:
            image_path = os.path.join(output_folder, image)
            if os.path.exists(image_path):
                # Streaming each file one by one with appropriate headers
                yield f'--file-boundary\r\n'
                yield f'Content-Type: image/png\r\n'
                yield f'Content-Disposition: inline; filename="{image}"\r\n'
                yield f'Content-Length: {os.path.getsize(image_path)}\r\n\r\n'
                yield from stream_file(image_path)
                yield '\r\n'  # End of file stream
        
        yield '--file-boundary--\r\n'  # End of the multi-file stream

    return Response(generate(), content_type='multipart/mixed; boundary=file-boundary')
if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)
